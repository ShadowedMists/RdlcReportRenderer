using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.Diagnostics.Utilities
{
	internal sealed class FaultedDataSourceException : ReportCatalogException
	{
		public FaultedDataSourceException(ErrorCode errorCode, string errorString)
			: base(errorCode, errorString, null, null)
		{
		}
	}
}
