namespace Nocco.Transformers
{
	public abstract class AbstractSourceTransformer : ISourceTransformer
	{
		public string Name;

		public virtual bool ShouldIgnore(string filename)
		{
			return false;
		}

		public abstract SourceModel Transform(SourceInfo source);
	}
}