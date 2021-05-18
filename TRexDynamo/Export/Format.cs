using System;

using Autodesk.DesignScript.Runtime;

namespace Export
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
