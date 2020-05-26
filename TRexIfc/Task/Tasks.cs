using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Task
{
    /// <summary>
    /// A collection of simple tasks
    /// </summary>
    public class Tasks
    {
        #region Internals

        internal Tasks()
        {
        }

        #endregion

        /// <summary>
        /// Returns an re-encoded string given the target encoding.
        /// </summary>
        /// <param name="text">The string value</param>
        /// <param name="targetEncoding">I.e. "ISO-8859-1" for Latin 1 or "UTF-8"</param>
        /// <returns>The re-encoded string</returns>
        public static string TextTo(string text, string targetEncoding)
        {
            Encoding targetEncoder = Encoding.GetEncoding(targetEncoding);
            var target = Encoding.Convert(Encoding.Default, targetEncoder, Encoding.Default.GetBytes(text));
            return targetEncoder.GetString(target);
        }

        /// <summary>
        /// Encodes to UTF-8.
        /// </summary>
        /// <param name="text">Some text</param>
        /// <returns>Encoded text</returns>
        public static string TextToUTF8(string text) => TextTo(text, "UTF-8");

        /// <summary>
        /// Encodes to Latin 1.
        /// </summary>
        /// <param name="text">Some text</param>
        /// <returns>Encoded text</returns>
        public static string TextToLatin1(string text) => TextTo(text, "ISO-8859-1");

        /// <summary>
        /// Will filter an array of textual data by (optionally case) duplicates.
        /// </summary>
        /// <param name="lines">The text</param>
        /// <param name="caseSensitive">Whether beeing case-sensitive</param>
        /// <returns>Filtered text</returns>
        public static string[] TextDistinctFilter(string[] lines, bool caseSensitive = false)
        {
            var set = new HashSet<string>(caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
            foreach (string l in lines) set.Add(l);
            
            return set.ToArray();
        }
    }
}
