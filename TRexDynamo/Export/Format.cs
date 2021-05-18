using System;

using TRex.Internal;

using Autodesk.DesignScript.Runtime;

namespace TRex.Export
{
    /// <summary>
    /// Generic export format description.
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public sealed class Format
    {
        public Format(string id, string extension, string description)
        {
            if (null == id)
                throw new ArgumentNullException(nameof(id));
            
            ID = id;
            Extension = extension;
            Description = description;
        }

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

        public Format(string idAsExtension, string description)
            : this(idAsExtension, idAsExtension, description)
        { }

        public string ID { get; private set; }
        public string Extension { get; private set; }
        public string Description { get; private set; }

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
    }
}
