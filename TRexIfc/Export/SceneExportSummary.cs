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

        public string FilePathName { get; protected set; }

        public string FileName { get => Path.GetFileName(FilePathName); }

        public LogMessage[] Log { get; protected set; }

#pragma warning disable CS1591 

        public override string ToString()
        {
            var stamp = Log.GroupBy(m => m.Severity).Select(g => $"{g.Count()} {g.Key}").ToArray();
            return $"${string.Join("|", stamp)} ({FilePathName})";
        }

        [IsVisibleInDynamoLibrary(false)]
        public SceneExportSummary ByResults(string filePathName, LogMessage[] messages)
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
