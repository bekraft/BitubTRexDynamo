using System;

using Autodesk.DesignScript.Runtime;

namespace Validation
{
    /// <summary>
    /// Flag of reporting domain.
    /// </summary>
    [Flags]
    [IsVisibleInDynamoLibrary(false)]
    public enum IfcReportDomain : int
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Passing domain. No report of failure
        /// </summary>
        Passed = 0x100,
        /// <summary>
        /// Schema domain
        /// </summary>
        Schema = 0x01, 
        /// <summary>
        /// Schema constraints.
        /// </summary>
        SchemaConstraint = 0x02,
        /// <summary>
        /// Topology constraints
        /// </summary>
        TopologyConstraints = 0x04,
        /// <summary>
        /// Geometry constraints.
        /// </summary>
        GeometryConstraint = 0x08,
        /// <summary>
        /// Enabling all constraints (except for schema itself).
        /// </summary>
        AllConstraints = 0x0e,
        /// <summary>
        /// All constraints and schema definitions.
        /// </summary>
        AllIssues = 0x0f,
        /// <summary>
        /// Any other
        /// </summary>
        Other = 0x10
    }
}
