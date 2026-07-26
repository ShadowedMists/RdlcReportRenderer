using System;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.Diagnostics.Utilities
{
	[Serializable]
	internal sealed class InvalidDataSourceSchemaException : ReportCatalogException
	{
		public InvalidDataSourceSchemaException()
			: base(ErrorCode.rsInvalidRSDSSchema, ErrorStrings.rsInvalidRSDSSchema, null, null)
		{
		}
	}
}
