using System;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.Diagnostics.Utilities
{
	[Serializable]
	internal sealed class InvalidDataSourceCredentialSettingForITokenDataExtensionException : ReportCatalogException
	{
		public InvalidDataSourceCredentialSettingForITokenDataExtensionException()
			: base(ErrorCode.rsInvalidDataSourceCredentialSettingForITokenDataExtension, ErrorStrings.rsInvalidDataSourceCredentialSettingForITokenDataExtension, null, null)
		{
		}
	}
}
