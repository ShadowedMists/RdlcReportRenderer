using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.ReportProcessing
{
	[Serializable]
	internal sealed class ReportProcessingException_InvalidScopeReference : Exception
	{
		internal ReportProcessingException_InvalidScopeReference(string scopeName)
			: base(string.Format(CultureInfo.CurrentCulture, RPRes.rsInvalidRuntimeScopeReference(scopeName)))
		{
		}
	}
}
