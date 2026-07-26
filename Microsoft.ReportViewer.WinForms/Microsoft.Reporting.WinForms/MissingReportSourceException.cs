using System;
using System.Runtime.Serialization;

namespace Microsoft.Reporting.WinForms
{
	[Serializable]
	public sealed class MissingReportSourceException : ReportViewerException
	{
		public MissingReportSourceException()
			: base(CommonStrings.MissingReportSource)
		{
		}
	}
}
