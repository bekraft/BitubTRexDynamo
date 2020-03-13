using System;

using Bitub.Transfer;

namespace TRexIfc
{
    public interface ICancelableTaskNode 
    {
        bool IsCanceled { get; set; }

        string TaskName { get; set; }
        int ProgressPercentage { get; set; }
        string ProgressState { get; set; }
    }
}
