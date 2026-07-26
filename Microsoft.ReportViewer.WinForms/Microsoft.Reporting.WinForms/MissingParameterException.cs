using System;
using System.Runtime.Serialization;

namespace Microsoft.Reporting.WinForms
{
	[Serializable]
	public sealed class MissingParameterException : ReportViewerException
	{
		internal MissingParameterException(string parameterName)
			: base(CommonStrings.MissingParameter(parameterName))
		{
		}
	}
}
