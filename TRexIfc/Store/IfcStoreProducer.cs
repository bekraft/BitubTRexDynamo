using System.IO;
using System.Collections;
using System.Collections.Generic;

using Autodesk.DesignScript.Runtime;

using Task;
using Log;

namespace Store
{
    /// <summary>
    /// An IFC model producer nodes loading via enumerable model access.
    /// </summary>
    public class IfcStoreProducer : IIfcStoreProducer
    {
        #region Internals

        // The task node
        private ICancelableTaskNode _taskNode;
        // The collector list
        private List<string> _filePathNames = new List<string>();

        internal IfcStoreProducer(ICancelableTaskNode taskNode)
        {
            _taskNode = taskNode;
            Logger = new Logger();
        }

        internal class IfcStoreEnumerator : IEnumerator<IfcStore>
        {
            private readonly IEnumerator _filePathNames;
            private readonly Logger _logger;
            private readonly ICancelableTaskNode _taskNode;

            internal IfcStoreEnumerator(IEnumerator enumerator, ICancelableTaskNode taskNode, Logger logger)
            {
                _filePathNames = enumerator;
                _logger = logger;
                _taskNode = taskNode;
            }

            public IfcStore Current { get; private set; }

            object IEnumerator.Current { get => Current; }

            public void Dispose()
            {
                Current?.Dispose();
                Current = null;
            }

            public bool MoveNext()
            {
                if (null == Current)
                {
                    _logger?.LogInfo("Start sequentially loading files.");
                    IfcStore.InitLogging(_logger);
                }
                else
                {
                    Current.Dispose();
                    Current = null;
                }

                if ((!_taskNode?.IsCanceled ?? true) && (_filePathNames?.MoveNext() ?? false))
                {
                    string fileName = _filePathNames.Current as string;
                    if (null != _taskNode)
                    {
                        _taskNode.TaskName = Path.GetFileName(fileName);
                        Current = IfcStore.ByLoad(fileName, _logger, _taskNode.Report);
                    }
                    else
                    {
                        Current = IfcStore.ByLoad(fileName, _logger, null);
                    }
                    return true;
                }
                else
                {
                    if (null != _taskNode)
                    {
                        _taskNode.TaskName = $"Read all files.";
                        _taskNode.ProgressState = "Done";

                        _logger?.LogInfo("All files have been loaded.");
                    }
                }
                return false;

            }

            public void Reset()
            {
                Current?.Dispose();
                Current = null;
                _filePathNames.Reset();
            }
        }

        #endregion

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
        /// The logger instance.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public Logger Logger { get; set; }

        /// <summary>
        /// Enqueues a file name to sequential IFC store producer.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public IfcStoreProducer EnqueueFileName(string fileName)
        {
            lock (_filePathNames)
                _filePathNames.Add(fileName);
            return this;
        }


#pragma warning disable CS1591

        public override string ToString()
        {
            return $"{GetType().Name} (";
        }

        public IEnumerator<IfcStore> GetEnumerator()
        {
            string[] filePathNames;
            lock (_filePathNames)
                filePathNames = _filePathNames.ToArray();

            return new IfcStoreEnumerator(filePathNames.GetEnumerator(), _taskNode, Logger);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#pragma warning restore CS1591
    }
}
