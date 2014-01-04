// **Nocco** is a quick-and-dirty, literate-programming-style documentation
// generator. It is a C# port of [Docco](http://jashkenas.github.com/docco/),
// which was written by [Jeremy Ashkenas](https://github.com/jashkenas) in
// Coffescript and runs on node.js.
//
// Nocco produces HTML that displays your comments alongside your code.
// Comments are passed through
// [Markdown](http://daringfireball.net/projects/markdown/syntax), and code is
// highlighted using [google-code-prettify](http://code.google.com/p/google-code-prettify/)
// syntax highlighting. This page is the result of running Nocco against its
// own source files.
//
// Currently, to build Nocco, you'll have to have Visual Studio 2010. The project
// depends on [MarkdownSharp](http://code.google.com/p/markdownsharp/) and you'll
// have to install [.NET MVC 3](http://www.asp.net/mvc/mvc3) to get the
// System.Web.Razor assembly. The MarkdownSharp is a NuGet package that will be
// installed automatically when you build the project.
//
// To use Nocco, run it from the command-line:
//
//     nocco *.cs
//
// ...will generate linked HTML documentation for the named source files, saving
// it into a `docs` folder.
//
// The [source for Nocco](http://github.com/dontangg/nocco) is available on GitHub,
// and released under the MIT license.
//
// If **.NET** doesn't run on your platform, or you'd prefer a more convenient
// package, get [Rocco](http://rtomayko.github.com/rocco/), the Ruby port that's
// available as a gem. If you're writing shell scripts, try
// [Shocco](http://rtomayko.github.com/shocco/), a port for the **POSIX shell**.
// Both are by [Ryan Tomayko](http://github.com/rtomayko). If Python's more
// your speed, take a look at [Nick Fitzgerald](http://github.com/fitzgen)'s
// [Pycco](http://fitzgen.github.com/pycco/).

// Import namespaces to allow us to type shorter type names.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Nocco.Transformers;

namespace Nocco
{
	public class Nocco
	{
		private static CommandLineOptions _options;
		private static string _executingDirectory;
		private static IList<SourceInfo> _sources;

		//### Main Documentation Generation Functions

		// Find all the files that match the pattern(s) passed in as arguments and
		// generate documentation for each one.
		public static void Generate()
		{
			if (_options.Targets.Length > 0)
			{
				Directory.CreateDirectory(_options.OutputDir);

				_executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

				var resources = new[] { "nocco.css", "prettify.js" };
				foreach (var resource in resources)
				{
					File.Copy(Path.Combine(_executingDirectory, "Resources", resource), Path.Combine(_options.OutputDir, resource), true);
				}

				var files = new List<string>();

				foreach (var target in _options.Targets)
				{
					files.AddRange(Directory.GetFileSystemEntries(_options.InputDir, target, SearchOption.AllDirectories));
				}

				_sources = CollectSourceInfos(files.Select(x => x.Replace('\\', '/')).OrderBy(x => x));

				foreach (var source in _sources.Where(x => x.Transformer != null))
				{
					var model = source.Transformer.Transform(source);
					var result = RazorTemplateHelper.GenerateHtml(source, model, _sources);
					File.WriteAllText(source.OutputPath, result);
				}
			}
		}

		//### Helpers & Setup

		// A list of the languages that Nocco supports, mapping the file extension to
		// the symbol that indicates a comment. To add another language to Nocco's
		// repertoire, add it here.
		//
		// You can also specify a list of regular expression patterns and replacements. This
		// translates things like
		// [XML documentation comments](http://msdn.microsoft.com/en-us/library/b2s063f7.aspx) into Markdown.
		private static readonly Dictionary<string, ISourceTransformer> Languages = new Dictionary<string, ISourceTransformer>
		{
			{
				".md", new MarkdownTransformer
				{
					Name = "markdown"
				}
			},
			{
				".js", new LanguageTransformer
				{
					Name = "javascript",
					CommentSymbol = "//",
					Ignores = new [] { "min.js" }
				}
			},
			{
				".cs", new LanguageTransformer
				{
					Name = "csharp",
					CommentSymbol = "///?",
					Ignores = new[] { "Designer.cs" },
					MarkdownMaps = new Dictionary<string, string>
					{
						{ @"<c>([^<]*)</c>", "`$1`" },
						{ @"<param[^\>]*>([^<]*)</param>", "" },
						{ @"<returns>([^<]*)</returns>", "" },
						{ @"<see\s*cref=""([^""]*)""\s*/>", "see `$1`" },
						{ @"(</?example>|</?summary>|</?remarks>)", "" },
					}
				}
			},
			{
				".vb", new LanguageTransformer
				{
					Name = "vb.net",
					CommentSymbol = "'+",
					Ignores = new[] { "Designer.vb" },
					MarkdownMaps = new Dictionary<string, string>
					{
						{ @"<c>([^<]*)</c>", "`$1`" },
						{ @"<param[^\>]*>([^<]*)</param>", "" },
						{ @"<returns>([^<]*)</returns>", "" },
						{ @"<see\s*cref=""([^""]*)""\s*/>", "see `$1`" },
						{ @"(</?example>|</?summary>|</?remarks>)", "" },
					}
				}
			}
		};

		// Get the transformer for source we're documenting, based on the extension.
		private static ISourceTransformer GetTransformer(string source)
		{
			var extension = Path.GetExtension(source);

			ISourceTransformer language;
			if (extension != null && Languages.TryGetValue(extension, out language))
			{
				return language;
			}

			return null;
		}

		// Compute the destination HTML path for an input source file path. If the source
		// is `Example.cs`, the HTML will be at `docs/example.html`
		private static string GetDestination(string filepath, out int depth)
		{
			var dirs = Path.GetDirectoryName(filepath)
			               .Substring(1)
			               .Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
			depth = dirs.Length;

			var dest = Path.Combine(_options.OutputDir, string.Join(Path.DirectorySeparatorChar.ToString(), dirs));

			Directory.CreateDirectory(dest);

			return Path.Combine(_options.OutputDir, Path.GetExtension(filepath) == ".html" ? filepath : filepath + ".html");
		}

		private static IList<SourceInfo> CollectSourceInfos(IEnumerable<string> files)
		{
			// Don't include certain directories
			var foldersToExclude = new[] { "\\docs", "\\bin", "\\obj", "\\packages", "\\.nuget", "\\.git", "\\.svn" };

			var result = new List<SourceInfo>();

			var index = new SourceInfo
			{
				InputPath = Path.Combine(_options.InputDir, "index.html").Replace('\\', '/'),
				Transformer = new IndexTransformer()
			};

			int depth;
			var destination = GetDestination(index.InputPath, out depth);
			var pathToRoot = string.Concat(Enumerable.Repeat(".." + Path.DirectorySeparatorChar, depth));

			index.OutputPath = destination;
			index.PathToRoot = pathToRoot;

			result.Add(index);

			foreach (var filename in files)
			{
				if (foldersToExclude.Any(folder => Path.GetDirectoryName(filename).Contains(folder)))
				{
					continue;
				}

				if (Directory.Exists(filename))
				{
					result.Add(new SourceInfo { InputPath = filename });
					continue;
				}

				var transformer = GetTransformer(filename);

				if (transformer == null) continue;

				// Check if the file extension should be ignored
				if (transformer.ShouldIgnore(filename)) continue;

				destination = GetDestination(filename, out depth);
				pathToRoot = string.Concat(Enumerable.Repeat(".." + Path.DirectorySeparatorChar, depth));

				result.Add(new SourceInfo
				{
					Transformer = transformer,
					InputPath = filename,
					OutputPath = destination,
					PathToRoot = pathToRoot
				});
			}

			return result;
		}

		public static void Main(string[] args)
		{
			_options = new CommandLineOptions();

			if (CommandLine.Parser.Default.ParseArguments(args, _options))
			{
				Generate();
			}
		}
	}
}
