using System;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.Diagnostics.Utilities
{
	[Serializable]
	internal sealed class CannotDeleteRootPolicyException : ReportCatalogException
	{
		public CannotDeleteRootPolicyException()
			: base(ErrorCode.rsCannotDeleteRootPolicy, ErrorStrings.rsCannotDeleteRootPolicy, null, null)
		{
		}
	}
}
