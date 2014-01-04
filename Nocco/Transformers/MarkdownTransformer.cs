using System.IO;
using MarkdownSharp;

namespace Nocco.Transformers
{
	public class MarkdownTransformer : AbstractSourceTransformer
	{
		public override SourceModel Transform(SourceInfo source)
		{
			var markdown = new Markdown();

			var text = File.ReadAllText(source.InputPath);

			var html = markdown.Transform(text);

			return new SourceModel { RawHtml = html };
		}
	}
}