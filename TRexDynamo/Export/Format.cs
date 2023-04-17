using System;

using TRex.Internal;

using Newtonsoft.Json;
using Autodesk.DesignScript.Runtime;

namespace TRex.Export
{
    /// <summary>
    /// Generic export format description.
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Format
    {
        /// <summary>
        /// Default empty constructor.
        /// </summary>
        [JsonConstructor]
        public Format()
        { }

        /// <summary>
        /// New format declaration with extension, seperate ID and description.
        /// </summary>
        /// <param name="id">The ID</param>
        /// <param name="extension">The extension</param>
        /// <param name="description">The description</param>
        /// <exception cref="ArgumentNullException">If ID is null</exception>
        public Format(string id, string extension, string description)
        {
            ID = id ?? throw new ArgumentNullException(nameof(id));
            Extension = extension;
            Description = description;
        }

        /// <summary>
        /// New format declaration with extension ID and description.
        /// </summary>
        /// <param name="idAsExtension">The extension ID</param>
        /// <param name="description">The description</param>
        public Format(string idAsExtension, string description)
            : this(idAsExtension, idAsExtension, description)
        { }

        /// <summary>
        /// The format identifier.
        /// </summary>
        [JsonProperty]
        public string ID { get; set; }

        /// <summary>
        /// The file extension identifier.
        /// </summary>
        [JsonProperty]
        public string Extension { get; set; }

        /// <summary>
        /// The description of format.
        /// </summary>
        [JsonProperty]
        public string Description { get; set; }

        /// <summary>
        /// A new format specification by dynamic object.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>A format specification</returns>
        public static Format FromDynamic(dynamic format)
        {
            try
            {
                string id = format.ID;
                string extension = format.Extension;
                string description = format.Description;
                return new Format(id, extension, description);
            }
            catch (ArgumentNullException ane)
            {
                GlobalLogging.log.Warning("Not allowed {0}: {1}", format, ane.Message);
            }
            catch (Exception e)
            {
                GlobalLogging.log.Warning("Unable to parse '{0}' as format: {1}", format, e.Message);
            }
            return null;
        }

#pragma warning disable CS1591
        
        public override int GetHashCode()
        {
            return ID?.GetHashCode() ?? 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is Format f)
                return f.ID.Equals(ID);
            else
                return false;
        }

        public override string ToString()
        {
            return $"{Description} (*.{Extension})";
        }
        
#pragma warning restore CS1591
    }
}
