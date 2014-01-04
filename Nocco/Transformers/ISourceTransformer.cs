namespace Nocco.Transformers
{
	// Simple interface to transform input sources to html documentation.
	public interface ISourceTransformer
	{
		// Check if the file extension should be ignored
		// todo: should be moved from transformer to somewhere else
		bool ShouldIgnore(string filename);

		SourceModel Transform(SourceInfo source);
	}
}
