using System;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.Diagnostics.Utilities
{
	[Serializable]
	internal sealed class InvalidXmlException : ReportCatalogException
	{
		public InvalidXmlException()
			: base(ErrorCode.rsInvalidXml, ErrorStrings.rsInvalidXml, null, null)
		{
		}
	}
}
