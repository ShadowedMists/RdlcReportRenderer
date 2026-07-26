using System;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.Diagnostics.Utilities
{
	[Serializable]
	internal sealed class SecureConnectionRequiredException : ReportCatalogException
	{
		public SecureConnectionRequiredException()
			: base(ErrorCode.rsSecureConnectionRequired, ErrorStrings.rsSecureConnectionRequired, null, null)
		{
		}
	}
}
