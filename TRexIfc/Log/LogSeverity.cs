using Autodesk.DesignScript.Runtime;

namespace Log
{
    /// <summary>
    /// Severity enumeration
    /// </summary>
    public enum LogSeverity : int
    {
        /// <summary>
        /// Debugging messages
        /// </summary>
        Debug = -1,
        /// <summary>
        /// Simple information tag.
        /// </summary>
        Info = 0,
        /// <summary>
        /// A warning tag.
        /// </summary>
        Warning = 1,
        /// <summary>
        /// A critical warning which doesn't block
        /// </summary>
        Critical = 2,
        /// <summary>
        /// An error which blocks any subsequent actions
        /// </summary>
        Error = 3,
    }

#pragma warning disable CS1591
    
    [IsVisibleInDynamoLibrary(false)]
    public static class SeverityExtensions
    {
        public static bool AboveOrEqual(this LogSeverity s, LogSeverity baseSeverity)
        {
            return ((int)s) >= ((int)baseSeverity);
        }
    }
    
#pragma warning restore CS1591
}
