using System;
using System.Threading;
using System.Collections.Generic;

using Bitub.Dto;

using TRex.Internal;
using TRex.Store;
using Xbim.Ifc4.Interfaces;

using System.Runtime.CompilerServices;
using Autodesk.DesignScript.Runtime;

namespace TRex.Validation
{ 

#pragma warning disable CS1591

    public class IfcValidationTask : ProgressingTask
    {
        #region Internals

        internal int TimeOutMillis { get; set; } = -1;

        internal CancellationTokenSource Cancellation { get; private set; }

        internal IfcModel OnIfcModel { get; private set; }

        internal Func<IfcValidationTask, IfcValidationResult> TaskDelegate { get; private set; }

        internal IfcValidationTask(IfcModel ifcModel, Func<IfcValidationTask, IfcValidationResult> taskDelegate)
        {
            if (null == ifcModel)
                throw new ArgumentNullException(nameof(ifcModel));
            if (null == taskDelegate)
                throw new ArgumentNullException(nameof(taskDelegate));

            OnIfcModel = ifcModel;
            TaskDelegate = taskDelegate;
        }

        internal IfcValidationResult TaskResult { get; private set; }

        #endregion

        /// <summary>
        /// Returns the result of validation task.
        /// </summary>
        /// <returns>The validation result</returns>
        public IfcValidationResult Result()
        {
            if (null != TaskResult)
                return TaskResult;

            Cancellation = new CancellationTokenSource();
            
            var running = System.Threading.Tasks.Task.Run(() => TaskDelegate(this));
            running.Wait(TimeOutMillis, Cancellation.Token);

            if (running.IsCompleted)
            {
                TaskResult = running.Result;
                return running.Result;
            }
            else
            {
                return null;
            }
        }

        [IsVisibleInDynamoLibrary(false)]
        public static IfcValidationTask NewIfcGuidCheckingTask(IfcGuidStore guidStore, IfcModel ifcModel)
        {
            if (null == ifcModel)
                throw new ArgumentNullException(nameof(ifcModel));
            if (null == guidStore)
                throw new ArgumentNullException(nameof(guidStore));

            return new IfcValidationTask(ifcModel, (t) =>
            {
                var innerModel = ifcModel.XbimModel;
                if (null == innerModel)
                    throw new NotSupportedException("No internal model available");

                var cp = t.CreateProgressMonitor(Log.LogReason.Checked);
                cp.NotifyProgressEstimateUpdate(innerModel.Instances.Count / 50);

                List<IfcValidationMessage> failingMessages = new List<IfcValidationMessage>();
                foreach (var instance in innerModel.Instances.OfType<IIfcRoot>())
                {
                    var message = guidStore.Put(ifcModel.Qualifier, instance);

                    if (cp.State.Done > 0.90 * cp.State.TotalEstimate)
                        cp.NotifyProgressEstimateUpdate((long)Math.Floor(cp.State.TotalEstimate * 1.25));

                    cp.NotifyOnProgressChange(1, "Checking unique IfcGUID values...");

                    failingMessages.Add(message);
                    if (cp.State.IsCanceled)
                        break;
                }

                cp.NotifyOnProgressEnd();

                return new IfcGuidCheckResult(guidStore) { MessagePipe = failingMessages };
            });
        }

#pragma warning restore CS1591
    }
}
