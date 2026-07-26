using System;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.Diagnostics.Utilities
{
	[Serializable]
	internal sealed class OperationNotSupportedNativeModeException : ReportCatalogException
	{
		public OperationNotSupportedNativeModeException()
			: base(ErrorCode.rsOperationNotSupportedNativeMode, ErrorStrings.rsOperationNotSupportedNativeMode, null, null)
		{
		}
	}
}
