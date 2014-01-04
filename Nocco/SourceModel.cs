using System.Collections.Generic;

namespace Nocco
{
	// Parsed source information. Contains sections with comments and code 
	// or raw html after processing markdown files.
	public class SourceModel
	{
		public IList<Section> Sections { get; set; }

		public string RawHtml { get; set; }
	}
}
