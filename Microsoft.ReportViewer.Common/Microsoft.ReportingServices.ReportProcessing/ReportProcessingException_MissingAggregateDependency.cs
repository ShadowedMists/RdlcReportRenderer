using System;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.ReportProcessing
{
	[Serializable]
	internal sealed class ReportProcessingException_MissingAggregateDependency : Exception
	{
		internal ReportProcessingException_MissingAggregateDependency()
		{
		}
	}
}
