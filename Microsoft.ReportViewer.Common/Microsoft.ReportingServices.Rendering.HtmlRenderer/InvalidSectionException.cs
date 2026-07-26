using Microsoft.ReportingServices.HtmlRendering;
using System;
using System.Runtime.Serialization;

namespace Microsoft.ReportingServices.Rendering.HtmlRenderer
{
	[Serializable]
	internal class InvalidSectionException : Exception
	{
		public InvalidSectionException()
			: base(RenderRes.rrInvalidSectionError)
		{
		}
	}
}
