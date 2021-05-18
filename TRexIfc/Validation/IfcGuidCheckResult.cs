using System;

namespace TRex.Validation
{
    /// <summary>
    /// IFC GUID checking result.
    /// </summary>
    public class IfcGuidCheckResult : IfcValidationResult
    {
        #region Internals

        internal IfcGuidStore TheStore { get; private set; }

        internal IfcGuidCheckResult(IfcGuidStore theStore) : base()
        {
            TheStore = theStore;
        }

        #endregion

        /// <summary>
        /// Returns the current IFC GUID store.
        /// </summary>
        public IfcGuidStore IfcGuidStore() => TheStore;

        /// <summary>
        /// Gets the GUID store from given IFC validation task.
        /// </summary>
        /// <param name="validationTask">An IFC GUID validation task</param>
        /// <returns>The store or an exception</returns>
        public static IfcGuidStore IfcGuidStore(IfcValidationTask validationTask)
        {
            if (validationTask?.Result() is IfcGuidCheckResult r)
                return r.IfcGuidStore();
            else
                throw new ArgumentException("No IFC GUID checking task");
        }
    }
}
