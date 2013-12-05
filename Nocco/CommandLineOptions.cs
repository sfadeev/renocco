using CommandLine;
using CommandLine.Text;

namespace Nocco
{
	internal class CommandLineOptions
	{
		[Option('i', "input", HelpText = "Input directory path", DefaultValue = "./")]
		public string InputDir { get; set; }

		[Option('o', "output", HelpText = "Output directory path", DefaultValue = "./docs/")]
		public string OutputDir { get; set; }

		[OptionArray('t', "targets", HelpText = "Target files masks")]
		public string[] Targets { get; set; }

		[ParserState]
		public IParserState LastParserState { get; set; }

		[HelpOption('h', "help")]
		public string GetUsage()
		{
			return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
		}
	}
}