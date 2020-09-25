using System;
using Autodesk.DesignScript.Runtime;

namespace Log
{
    /// <summary>
    /// Action type of message
    /// </summary>
    [Flags]
    [IsVisibleInDynamoLibrary(false)]
    public enum LogReason
    {
        /// <summary>
        /// No action at all.
        /// </summary>
        None = 0,
        /// <summary>
        /// Any progress
        /// </summary>
        Any = 0xff,
        /// <summary>
        /// Loading
        /// </summary>
        Loaded = 0x01,
        /// <summary>
        /// Saving
        /// </summary>
        Saved = 0x02,
        /// <summary>
        /// Transformed
        /// </summary>
        Transformed = 0x04,
        /// <summary>
        /// Checked
        /// </summary>
        Checked = 0x08,
        /// <summary>
        /// Adding
        /// </summary>        
        Added = 0x10,
        /// <summary>
        /// Removing
        /// </summary>
        Removed = 0x20,
        /// <summary>
        /// Modifiying
        /// </summary>
        Modified = 0x40,
        /// <summary>
        /// Copied
        /// </summary>
        Copied = 0x80,
        /// <summary>
        /// Changing any
        /// </summary>
        Changed = 0xf4
    }
}
