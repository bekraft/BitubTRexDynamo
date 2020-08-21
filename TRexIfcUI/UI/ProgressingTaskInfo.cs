using System;
using System.ComponentModel;
using Autodesk.DesignScript.Runtime;

using Bitub.Transfer;
using Internal;

namespace UI
{
    /// <summary>
    /// Simple information element for visual representation
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public class ProgressingTaskInfo
    {
        internal ProgressingTaskInfo(ProgressingTask task)
        {
            ElapsedSpan = GlobalLogging.DiagnosticStopWatch.Elapsed;
            Name = task.Name;

            if (null != task.LatestProgressEventArgs)
                State = task.LatestProgressEventArgs.GetProgressState() & (ProgressTokenState)0xf0;
        }

        /// <summary>
        /// Time span spent so far.
        /// </summary>
        public TimeSpan ElapsedSpan { get; set; }

        /// <summary>
        /// Name of task.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Latest progress state.
        /// </summary>
        public ProgressTokenState? State { get; set; }
    }
}
