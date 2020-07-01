using System.Linq;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using Internal;

namespace Validation
{
    /// <summary>
    /// Abstract validation result wrapping a pipe of messages only
    /// </summary>
    public abstract class IfcValidationResult
    {
        #region Internals

        internal static readonly ILogger Log = GlobalLogging.LoggingFactory.CreateLogger<IfcValidationResult>();

        /// <summary>
        /// Validation messages as enumerable.
        /// </summary>
        internal protected IEnumerable<IfcValidationMessage> MessagePipe { get; set; }

        /// <summary>
        /// New result.
        /// </summary>
        internal protected IfcValidationResult()
        {
        }

        #endregion

        /// <summary>
        /// Retrieve all messages of given type.
        /// </summary>
        /// <param name="reportFilter">The IfcReportDomain filter</param>
        /// <returns>The validation messages</returns>
        public IfcValidationMessage[] Messages(object reportFilter)
        {
            IfcReportDomain domainFilter;
            if (!GlobalArgumentService.TryCastEnum<IfcReportDomain>(reportFilter, out domainFilter))
            {
                Log.LogWarning($"Parsing reportFilter failed in ({nameof(IfcValidationResult.Messages)}. Using '{domainFilter}'.");
                domainFilter = IfcReportDomain.AllIssues;
            }

            return MessagePipe.Where(m => domainFilter.HasFlag(m.Domain)).ToArray();
        }

        /// <summary>
        /// Directly retrieves the validations messages.
        /// </summary>
        /// <param name="validationTask">The task</param>
        /// <param name="reportFilter">The IfcReportDomain filter</param>
        /// <returns>An array of messages</returns>
        public static IfcValidationMessage[] Messages(IfcValidationTask validationTask, object reportFilter)
        {
            IfcReportDomain domainFilter;
            if (!GlobalArgumentService.TryCastEnum<IfcReportDomain>(reportFilter, out domainFilter))
            {
                Log.LogWarning($"Parsing reportFilter failed in ({nameof(IfcValidationResult.Messages)}. Using '{domainFilter}'.");
                domainFilter = IfcReportDomain.AllIssues;
            }

            return validationTask?.Result().Messages(domainFilter);
        }
    }
}
