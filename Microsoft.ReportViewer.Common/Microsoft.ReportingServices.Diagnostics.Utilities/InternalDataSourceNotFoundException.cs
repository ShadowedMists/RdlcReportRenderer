using System;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.Diagnostics.Utilities
{
	[Serializable]
	internal sealed class InternalDataSourceNotFoundException : ReportCatalogException
	{
		public InternalDataSourceNotFoundException()
			: base(ErrorCode.rsInternalDataSourceNotFound, ErrorStrings.internalDataSourceNotFound, null, null)
		{
		}
	}
}
