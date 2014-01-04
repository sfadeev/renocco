namespace Nocco.Markdown
{
	public interface IMarkdownTransformer
	{
		// Transforms the Markdown-formatted text to HTML
		string Transform(string text);
	}
}