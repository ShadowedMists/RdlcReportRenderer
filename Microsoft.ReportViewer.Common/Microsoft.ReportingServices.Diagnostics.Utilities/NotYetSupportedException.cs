using System;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.Diagnostics.Utilities
{
	[Serializable]
	internal sealed class NotYetSupportedException : ReportCatalogException
	{
		public NotYetSupportedException()
			: base(ErrorCode.rsNotSupported, ErrorStrings.rsNotSupported, null, null)
		{
		}
	}
}
