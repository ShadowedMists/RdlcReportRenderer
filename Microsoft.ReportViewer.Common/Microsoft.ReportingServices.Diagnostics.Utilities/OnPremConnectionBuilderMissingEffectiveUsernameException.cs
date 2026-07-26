using System;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.Diagnostics.Utilities
{
	[Serializable]
	internal sealed class OnPremConnectionBuilderMissingEffectiveUsernameException : ReportCatalogException
	{
		public OnPremConnectionBuilderMissingEffectiveUsernameException()
			: base(ErrorCode.rsOnPremConnectionBuilderMissingEffectiveUsername, ErrorStrings.rsOnPremConnectionBuilderMissingEffectiveUsername, null, null)
		{
		}
	}
}
