using System;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.Diagnostics.Utilities
{
	[Serializable]
	internal sealed class CatalogItemContentInvalidException : ReportCatalogException
	{
		public CatalogItemContentInvalidException(string itemPath)
			: base(ErrorCode.rsItemContentInvalid, ErrorStrings.rsItemContentInvalid(itemPath), null, null)
		{
		}
	}
}
