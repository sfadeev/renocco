namespace Nocco.Transformers
{
	public class IndexTransformer : AbstractSourceTransformer
	{
		public override SourceModel Transform(SourceInfo source)
		{
			return new SourceModel();
		}
	}
}