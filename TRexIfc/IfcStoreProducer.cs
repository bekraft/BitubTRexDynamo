﻿using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.DesignScript.Runtime;

using Xbim.Common;
using Bitub.Ifc;

using TRexIfc.Logging;

namespace TRexIfc
{
    /// <summary>
    /// An IFC model producer nodes loading via enumerable model access.
    /// </summary>
    public class IfcStoreProducer : IIfcStoreProducer
    {
        #region Internals
        internal IEnumerator FileNames { get; private set; }
        internal ICancelableTaskNode TaskNode { get; set; }

        internal IfcStoreProducer(ICancelableTaskNode taskNode)
        {
            TaskNode = taskNode;
        }

        internal IfcStoreProducer(string[] fileNames, ICancelableTaskNode taskNode = null)
        {
            FileNames = fileNames.GetEnumerator();
            TaskNode = taskNode;            
        }

        #endregion

        /// <summary>
        /// A list collection file names until first call.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public List<string> FileNameCollector { get; set; } = null;

        /// <summary>
        /// A new IFC store producer with reference to tasknode.
        /// </summary>
        /// <param name="taskNode">The task node</param>
        /// <returns>A new producer</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static IfcStoreProducer ByTaskNode(ICancelableTaskNode taskNode)
        {
            return new IfcStoreProducer(taskNode);
        }

        /// <summary>
        /// A new IFC model producer by given filenames.
        /// </summary>
        /// <param name="fileNames">An array of filen ames</param>
        /// <returns>An iterative producer</returns>
        public static IfcStoreProducer ByFileNames(string[] fileNames)
        {
            return new IfcStoreProducer(fileNames);
        }

        /// <summary>
        /// A new producer by given filenames.
        /// </summary>
        /// <param name="fileNames">An array of file names</param>
        /// <param name="taskNode">A task node to report progress</param>
        /// <returns>An iterative producer</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static IfcStoreProducer ByFileNames(string[] fileNames, ICancelableTaskNode taskNode)
        {
            return new IfcStoreProducer(fileNames, taskNode);
        }

        /// <summary>
        /// The current loaded model.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public IfcStore Current { get; private set; }

        /// <summary>
        /// The project's meta data.
        /// </summary>
        public IfcProjectMetadata ProjectMetadata { get; internal set; }

        /// <summary>
        /// The logger instance.
        /// </summary>
        public Logger Logger { get; internal set; }

#pragma warning disable CS1591

        object IEnumerator.Current { get => Current; }

        [IsVisibleInDynamoLibrary(false)]
        public void Dispose()
        {
            Current?.XbimModel.Dispose();
            FileNames = null;
        }

        [IsVisibleInDynamoLibrary(false)]
        public bool MoveNext()
        {
            if (null == FileNames && null != FileNameCollector)
                FileNames = FileNameCollector.GetEnumerator();

            if ((!TaskNode?.IsCanceled ?? true) && (FileNames?.MoveNext() ?? false))
            {
                Current?.XbimModel.Dispose();
                string fileName = FileNames.Current as string;
                if (null != TaskNode)
                    TaskNode.TaskName = Path.GetFileName(fileName);

                ReportProgressDelegate reportProgress = (p, s) =>
                {
                    if (null != TaskNode)
                    {
                        TaskNode.ProgressPercentage = p;
                        TaskNode.ProgressState = s?.ToString();
                    }
                };
                Current = IfcStore.Load(fileName, reportProgress);
                return true;
            }
            else
            {
                if (null != TaskNode)
                {
                    TaskNode.TaskName = $"Read all files.";
                    TaskNode.ProgressState = "Done";
                    FileNames = null;                    
                }
            }
            return false;
        }

        [IsVisibleInDynamoLibrary(false)]
        public void Reset()
        {
            FileNames?.Reset();
        }

        public override string ToString()
        {
            var status = null == FileNames ? "initiated" : "running";
            var name = Path.GetFileName(FileNames?.Current as string);
            return $"{GetType().Name}({status}) ({name})";
        }

#pragma warning restore CS1591 
    }
}
