using Autodesk.DesignScript.Runtime;
using Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Export
{
    /// <summary>
    /// Exported scene results
    /// </summary>
    public class SceneExportSummary
    {
        #region Internals

        internal SceneExportSummary()
        {
        }

        #endregion

        /// <summary>
        /// The full file name including path.
        /// </summary>
        public string FilePathName { get; protected set; }

        /// <summary>
        /// The file name.
        /// </summary>
        public string FileName { get => Path.GetFileName(FilePathName); }

        /// <summary>
        /// Any log messages
        /// </summary>
        public LogMessage[] Log { get; protected set; }

#pragma warning disable CS1591 

        public override string ToString()
        {
            var stamp = Log.GroupBy(m => m.Severity).Select(g => $"{g.Count()} {g.Key}").ToArray();
            return $"${string.Join("|", stamp)} ({FilePathName})";
        }

        [IsVisibleInDynamoLibrary(false)]
        public static SceneExportSummary ByResults(string filePathName, params LogMessage[] messages)
        {
            return new SceneExportSummary
            {
                FilePathName = filePathName,
                Log = messages
            };
        }

#pragma warning restore CS1591
    }
}
