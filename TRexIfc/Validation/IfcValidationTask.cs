using System;
using System.Threading;
using System.Collections.Generic;

using Bitub.Transfer;

using Internal;
using Store;
using Xbim.Ifc4.Interfaces;

using System.Runtime.CompilerServices;
using Autodesk.DesignScript.Runtime;

namespace Validation
{
    /// <summary>
    /// A validation task running as a delayed
    /// </summary>
    public class IfcValidationTask : NodeProgressing
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

#pragma warning disable CS1591

        [IsVisibleInDynamoLibrary(false)]
        public static IfcValidationTask NewIfcGuidCheckingTask(IfcGuidStore guidStore, IfcModel ifcModel)
        {
            if (null == ifcModel)
                throw new ArgumentNullException(nameof(ifcModel));
            if (null == guidStore)
                throw new ArgumentNullException(nameof(guidStore));

            return new IfcValidationTask(ifcModel, (t) =>
            {
                var innerModel = ifcModel.Store.XbimModel;
                if (null == innerModel)
                    throw new NotSupportedException("No internal model available");

                var progressToken = new CancelableProgressStateToken(true, innerModel.Instances.Count / 50);
                List<IfcValidationMessage> failingMessages = new List<IfcValidationMessage>();
                progressToken.Update(0, "IFC GUID checking");

                foreach (var instance in innerModel.Instances.OfType<IIfcRoot>())
                {
                    var message = guidStore.Put(ifcModel.Qualifier, instance);

                    if (progressToken.Done > 0.90 * progressToken.Total)
                        progressToken.IncreaseTotalEffort((long)Math.Floor(progressToken.Total * 1.25));

                    progressToken.Increment();
                    t.OnProgressChanged(new NodeProgressingEventArgs(Log.LogReason.Checked, progressToken, ifcModel.FileName));

                    failingMessages.Add(message);
                    if (progressToken.IsCanceled)
                        break;
                }

                progressToken.Update(progressToken.Total);
                t.OnProgressChanged(new NodeProgressingEventArgs(Log.LogReason.Checked, progressToken, ifcModel.Name));
                t.OnFinished(new NodeFinishedEventArgs(progressToken, Log.LogReason.Checked, ifcModel.FileName));

                return new IfcGuidCheckResult(guidStore) { MessagePipe = failingMessages };
            });
        }

#pragma warning restore CS1591
    }
}
