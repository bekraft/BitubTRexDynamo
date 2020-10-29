using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utils
{
    /// <summary>
    /// A collection of simple tasks
    /// </summary>
    public class Utils
    {
        #region Internals

        private Utils()
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

        /// <summary>
        /// Converts all lines into upper case invariant strings.
        /// </summary>
        /// <param name="line">The line</param>
        /// <returns>Upper case lines</returns>
        public static string ToUpper(string line)
        {
            return line.ToUpperInvariant();
        }
        
        /// <summary>
        /// Converts all lines into lower case invariant strings.
        /// </summary>
        /// <param name="line">The line</param>
        /// <returns>The lower case lines</returns>
        public static string ToLower(string line)
        {
            return line.ToLowerInvariant();
        }

        /// <summary>
        /// Flattens the dictionary as matrix of repitative keys and their associated values.
        /// </summary>
        /// <param name="dict">The data dictionary</param>
        /// <returns>Two-dimensional array of keys and values</returns>
        public static object[][] Dict2RowTable(Dictionary<object, object> dict)
        {
            return dict.SelectMany(e =>
            {
                if (e.Value is IEnumerable<object> en)
                    return en.Select(Value => new[] { e.Key, Value });
                else
                    return new[] { new[] { e.Key, e.Value } };
            }).ToArray();
        }

        /// <summary>
        /// Flattens the dictionary as matrix of repitative keys and their associated values.
        /// </summary>
        /// <param name="dict">The data dictionary</param>
        /// <returns>Two-dimensional array of keys and values</returns>
        public static object[][] Dict2ColumnTable(Dictionary<object, object> dict)
        {
            return dict.SelectMany(e =>
            {
                if (e.Value is IEnumerable<object> en)
                    return new[] { new[] { e.Key, en.OrderBy(e => e).ToArray() } };
                else
                    return new[] { new[] { e.Key, e.Value } };
            }).ToArray();
        }

    }
}
