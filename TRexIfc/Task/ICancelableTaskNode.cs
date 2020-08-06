using System;

using Bitub.Transfer;

namespace Task
{
    /// <summary>
    /// Progressing task node interface.
    /// </summary>
    public interface ICancelableTaskNode : IProgress<ProgressStateToken>
    {
        /// <summary>
        /// Initializes the node with an initial progress token.
        /// </summary>
        /// <param name="progressToken">The progress token</param>
        void InitNode(ProgressStateToken progressToken);

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

        /// <summary>
        /// Reporting percentage and user state separately.
        /// </summary>
        /// <param name="percentage">The percentage starting with 0 </param>
        /// <param name="userState"></param>
        void Report(int percentage, object userState);
    }
}
