using System;
using System.Runtime.Serialization;

namespace Microsoft.Reporting.NETCore
{
	[Serializable]
	public sealed class ReportSecurityException : ReportViewerException
	{
		internal ReportSecurityException(string message)
			: base(message)
		{
		}
	}
}
