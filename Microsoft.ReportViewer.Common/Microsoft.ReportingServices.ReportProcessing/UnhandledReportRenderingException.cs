using Microsoft.ReportingServices.Diagnostics.Utilities;
using Microsoft.ReportingServices.OnDemandReportRendering;
using System;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.ReportProcessing
{
	[Serializable]
	internal sealed class UnhandledReportRenderingException : RSException
	{
		internal UnhandledReportRenderingException(ReportRenderingException innerException)
			: base(innerException.ErrorCode, innerException.Message, innerException, Global.RenderingTracer, null)
		{
		}

		internal UnhandledReportRenderingException(Exception innerException)
			: base(ErrorCode.rrRenderingError, RenderErrors.Keys.GetString(ErrorCode.rrRenderingError.ToString()), innerException, Global.RenderingTracer, null)
		{
		}
	}
}
