using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Nocco.Markdown;

namespace Nocco.Transformers
{
	public class LanguageTransformer : AbstractSourceTransformer
	{
		public string CommentSymbol;

		public Regex CommentMatcher { get { return new Regex(@"^\s*" + CommentSymbol + @"\s?"); } }

		public Regex CommentFilter { get { return new Regex(@"(^#![/]|^\s*#\{)"); } }

		public IDictionary<string, string> MarkdownMaps;

		public string[] Ignores;

		public override bool ShouldIgnore(string filename)
		{
			return Ignores != null && Ignores.Any(filename.EndsWith);
		}

		// Generate the documentation for a source file by reading it in, splitting it
		// up into comment/code sections, highlighting them for the appropriate language,
		// and merging them into an HTML template.
		public override SourceModel Transform(SourceInfo source)
		{
			IMarkdownTransformer markdown = new MarkdownDeepTransformer();

			var sections = Parse(source);

			// Prepares a single chunk of code for HTML output and runs the text of its
			// corresponding comment through **Markdown**, using a C# implementation
			// called [MarkdownSharp](http://code.google.com/p/markdownsharp/).
			foreach (var section in sections)
			{
				section.DocsHtml = markdown.Transform(section.DocsHtml);
				section.CodeHtml = HttpUtility.HtmlEncode(section.CodeHtml);
			}

			return new SourceModel { Sections = sections };
		}

		// Given a string of source code, parse out each comment and the code that
		// follows it, and create an individual `Section` for it.
		private IList<Section> Parse(SourceInfo source)
		{
			var lines = File.ReadAllLines(source.InputPath);

			var sections = new List<Section>();
			var docsText = new StringBuilder();
			var codeText = new StringBuilder();

			Action<string, string> addSection = (docs, code) =>
			{
				if (MarkdownMaps != null)
				{
					docs = MarkdownMaps.Aggregate(docs,
						(currentDocs, map) => Regex.Replace(currentDocs, map.Key, map.Value, RegexOptions.Multiline));
				}

				sections.Add(new Section
				{
					DocsHtml = docs,
					CodeHtml = code
				});
			};

			foreach (var line in lines)
			{
				if (CommentMatcher.IsMatch(line) && !CommentFilter.IsMatch(line))
				{
					if (codeText.Length > 0)
					{
						addSection(docsText.ToString(), codeText.ToString());
						docsText.Length = 0;
						codeText.Length = 0;
					}

					docsText.AppendLine(CommentMatcher.Replace(line, string.Empty));
				}
				else
				{
					codeText.AppendLine(line);
				}
			}

			addSection(docsText.ToString(), codeText.ToString());

			return sections;
		}
	}
}