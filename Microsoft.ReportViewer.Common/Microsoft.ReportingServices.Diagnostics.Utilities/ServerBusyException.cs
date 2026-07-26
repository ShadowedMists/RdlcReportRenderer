using System;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.Diagnostics.Utilities
{
	[Serializable]
	internal sealed class ServerBusyException : ReportCatalogException
	{
		public ServerBusyException()
			: base(ErrorCode.rsServerBusy, ErrorStrings.rsServerBusy, null, null)
		{
		}
	}
}
