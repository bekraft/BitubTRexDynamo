using System.Linq;

using Xbim.Common;

using XbimValidationFlags = Xbim.Common.Enumerations.ValidationFlags;
using XbimValidationResult = Xbim.Common.ExpressValidation.ValidationResult;

namespace TRex.Validation
{

#pragma warning disable CS1591

    public sealed class IfcValidationMessage
    {
        #region Internals

        internal IfcValidationMessage()
        {
        }

        internal static IfcReportDomain ToValidationFlag(XbimValidationFlags vf)
        {
            if (vf.HasFlag(XbimValidationFlags.Properties) || vf.HasFlag(XbimValidationFlags.Inverses))
                return IfcReportDomain.Schema;
            else if (vf.HasFlag(XbimValidationFlags.EntityWhereClauses) || vf.HasFlag(XbimValidationFlags.TypeWhereClauses))
                return IfcReportDomain.SchemaConstraint;
            else if (vf == XbimValidationFlags.None)
                return IfcReportDomain.Passed;

            return IfcReportDomain.None;
        }

        internal static string GenerateSource(IPersist persist)
        {
            if (persist is IPersistEntity e)
                return $"#{e.EntityLabel} ({e.Model.SchemaVersion}.{e.ExpressType.ExpressName})";
            else
                return persist.ToString();
        }

        internal static IfcValidationMessage ByXbimValidationResult(XbimValidationResult validationResult)
        {
            return new IfcValidationMessage
            {
                Domain = ToValidationFlag(validationResult.IssueType),
                Source = GenerateSource(validationResult.Item),
                Reason = validationResult.IssueSource,
                Message = validationResult.Message,                
            };
        }

        internal static IfcValidationMessage ByResult(long id, int? parentId, IfcReportDomain type, 
            string source, string message, string reason, params object[] arguments)
        {
            return new IfcValidationMessage
            {
                Id = id,
                Parent = parentId,
                Domain = type,
                Source = source,
                Message = message,
                Reason = reason,
                Arguments = arguments
            };
        }

        #endregion

        public long Id { get; private set; }

        public long? Parent { get; private set; }

        public string Source { get; private set; }

        public IfcReportDomain Domain { get; private set; }

        public string Message { get; private set; }

        public string Reason { get; private set; }

        public object[] Arguments { get; private set; }

        /// <summary>
        /// Wraps the partial information into an array dump.
        /// </summary>
        /// <returns>An array of all informations</returns>
        public object[] ToData()
        {
            return new object[] { Id, Parent, Domain, Source, Reason, Message }.Concat(Arguments).ToArray();
        }

        public override string ToString()
        {
            return $"({Id}) {Domain} [{Source} - {Reason} '{Message}']";
        }

#pragma warning restore CS1591
    }
}
