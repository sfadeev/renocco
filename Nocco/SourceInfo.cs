using Nocco.Transformers;

namespace Nocco
{
	public class SourceInfo
	{
		public ISourceTransformer Transformer { get; set; }
		public string InputPath { get; set; }
		public string OutputPath { get; set; }
		public string PathToRoot { get; set; }
	}
}