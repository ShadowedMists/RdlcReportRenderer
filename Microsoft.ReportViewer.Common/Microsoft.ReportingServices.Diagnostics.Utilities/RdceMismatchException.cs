using System;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.Diagnostics.Utilities
{
	[Serializable]
	internal sealed class RdceMismatchException : RSException
	{
		public RdceMismatchException(string rdceSet, string rdceConfigured)
			: base(ErrorCode.rsRdceMismatchError, ErrorStrings.rsRdceMismatchError(rdceSet, rdceConfigured), null, RSTrace.IsTraceInitialized ? RSTrace.CatalogTrace : null, null)
		{
		}
	}
}
