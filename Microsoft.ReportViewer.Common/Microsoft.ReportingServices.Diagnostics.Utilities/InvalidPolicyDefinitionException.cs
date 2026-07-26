using System;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.Diagnostics.Utilities
{
	[Serializable]
	internal sealed class InvalidPolicyDefinitionException : ReportCatalogException
	{
		public InvalidPolicyDefinitionException(string principalName)
			: base(ErrorCode.rsInvalidPolicyDefinition, ErrorStrings.rsInvalidPolicyDefinition(principalName), null, null)
		{
		}
	}
}
