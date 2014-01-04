namespace Nocco
{
	// Simple interface to transform input sources to html documentation.
	public interface ISourceTransformer
	{
		string Transform(SourceInfo source);
	}
}
