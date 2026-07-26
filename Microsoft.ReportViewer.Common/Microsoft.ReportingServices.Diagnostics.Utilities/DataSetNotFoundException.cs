using System;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.Diagnostics.Utilities
{
	[Serializable]
	internal sealed class DataSetNotFoundException : ReportCatalogException
	{
		public DataSetNotFoundException(string dataSet)
			: base(ErrorCode.rsDataSetNotFound, ErrorStrings.rsDataSetNotFound(dataSet), null, null)
		{
		}
	}
}
