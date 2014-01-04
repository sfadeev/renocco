namespace Nocco.Markdown
{
	public class MarkdownDeepTransformer : IMarkdownTransformer
	{
		private readonly MarkdownDeep.Markdown _markdown;

		public MarkdownDeepTransformer()
		{
			_markdown = new MarkdownDeep.Markdown();
		}

		public string Transform(string text)
		{
			return _markdown.Transform(text);
		}
	}
}