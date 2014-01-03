namespace Nocco
{
	// Interface to process input sources to html documentation.
	public interface ISourceProcessor
	{
		string Process(SourceInfo source);
	}
}
