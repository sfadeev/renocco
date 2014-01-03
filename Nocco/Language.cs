using System.Collections.Generic;
using System.Text.RegularExpressions;

// A smart class used for generating nice HTML based on the language of your choice.
namespace Nocco
{
	public class Language
	{
		public string Name;

		public string CommentSymbol;

		public Regex CommentMatcher { get { return new Regex(@"^\s*" + CommentSymbol + @"\s?"); } }

		public Regex CommentFilter { get { return new Regex(@"(^#![/]|^\s*#\{)"); } }

		public IDictionary<string, string> MarkdownMaps;

		public string[] Ignores;
	}
}
