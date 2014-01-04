namespace Nocco.Markdown
{
	public class MarkdownSharpTransformer : IMarkdownTransformer
	{
		private readonly MarkdownSharp.Markdown _markdown;

		public MarkdownSharpTransformer()
		{
			_markdown = new MarkdownSharp.Markdown();
		}

		public string Transform(string text)
		{
			return _markdown.Transform(text);
		}
	}
}