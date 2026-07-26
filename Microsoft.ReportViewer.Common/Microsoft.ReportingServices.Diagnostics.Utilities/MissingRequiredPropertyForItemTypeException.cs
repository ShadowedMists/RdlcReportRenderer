using System;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.Diagnostics.Utilities
{
	[Serializable]
	internal sealed class MissingRequiredPropertyForItemTypeException : ReportCatalogException
	{
		public MissingRequiredPropertyForItemTypeException(string propertyName)
			: base(ErrorCode.rsMissingRequiredPropertyForItemType, ErrorStrings.rsMissingRequiredPropertyForItemType(propertyName), null, null)
		{
		}
	}
}
