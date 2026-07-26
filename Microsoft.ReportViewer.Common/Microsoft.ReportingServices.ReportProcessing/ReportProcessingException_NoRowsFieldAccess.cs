using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.ReportProcessing
{
	[Serializable]
	internal sealed class ReportProcessingException_NoRowsFieldAccess : Exception
	{
		internal ReportProcessingException_NoRowsFieldAccess()
			: base(string.Format(CultureInfo.CurrentCulture, RPRes.rsNoRowsFieldAccess))
		{
		}
	}
}
