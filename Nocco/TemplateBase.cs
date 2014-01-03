// A class to be used as the base class for the generated template.
using System;
using System.Collections.Generic;
using System.Text;

namespace Nocco
{
	public abstract class TemplateBase
	{
		private readonly StringBuilder _buffer = new StringBuilder();

		// Properties available from within the template
		public string Title { get; set; }
		public string PathToCss { get; set; }
		public string PathToJs { get; set; }
		public Func<string, string> GetSourcePath { get; set; }
		public IList<Section> Sections { get; set; }
		public IList<SourceInfo> Sources { get; set; }
		public string RawHtml { get; set; }

		// This `Execute` function will be defined in the inheriting template
		// class. It generates the HTML by calling `Write` and `WriteLiteral`.
		public abstract void Execute();

		public virtual void Write(object value)
		{
			WriteLiteral(value);
		}

		public virtual void WriteLiteral(object value)
		{
			_buffer.Append(value);
		}

		public string GetBuffer()
		{
			return _buffer.ToString();
		}
	}
}
