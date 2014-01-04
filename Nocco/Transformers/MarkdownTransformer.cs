using System.IO;
using Nocco.Markdown;

namespace Nocco.Transformers
{
	public class MarkdownTransformer : AbstractSourceTransformer
	{
		public override SourceModel Transform(SourceInfo source)
		{
			IMarkdownTransformer markdown = new MarkdownDeepTransformer();

			var text = File.ReadAllText(source.InputPath);

			var html = markdown.Transform(text);

			return new SourceModel { RawHtml = html };
		}
	}
}