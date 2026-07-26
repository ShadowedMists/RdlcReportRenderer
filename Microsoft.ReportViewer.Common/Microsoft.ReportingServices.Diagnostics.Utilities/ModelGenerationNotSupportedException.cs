using System;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.Diagnostics.Utilities
{
	[Serializable]
	internal sealed class ModelGenerationNotSupportedException : ReportCatalogException
	{
		public ModelGenerationNotSupportedException()
			: base(ErrorCode.rsModelGenerationNotSupported, ErrorStrings.rsModelGenerationNotSupported, null, null)
		{
		}
	}
}
