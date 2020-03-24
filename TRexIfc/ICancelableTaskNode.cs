using System;

using Bitub.Transfer;

namespace TRexIfc
{
    /// <summary>
    /// Progressing task node interface.
    /// </summary>
    public interface ICancelableTaskNode : IProgress<ICancelableProgressState>
    {
        /// <summary>
        /// Initializes the node with an initial progress token.
        /// </summary>
        /// <param name="progressToken">The progress token</param>
        void InitNode(ICancelableProgressState progressToken);

        /// <summary>
        /// Whether the progress has been canceled.
        /// </summary>
        bool IsCanceled { get; set; }

        /// <summary>
        /// The task name.
        /// </summary>
        string TaskName { get; set; }
        /// <summary>
        /// The progress percentage.
        /// </summary>
        int ProgressPercentage { get; set; }
        /// <summary>
        /// The progress state.
        /// </summary>
        string ProgressState { get; set; }
    }
}
