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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Razor;
using MarkdownSharp;
using Microsoft.CSharp;

namespace Nocco
{
	public class Nocco
	{
		private static CommandLineOptions _options;
		private static string _executingDirectory;
		private static IList<SourceInfo> _sources;
		private static Type _templateType;

		//### Main Documentation Generation Functions

		// Generate the documentation for a source file by reading it in, splitting it
		// up into comment/code sections, highlighting them for the appropriate language,
		// and merging them into an HTML template.
		private static void GenerateDocumentation(SourceInfo source)
		{
			if (source.InputPath.EndsWith("index.html"))
			{
				GenerateHtml(source, null, null);
				return;
			}

			if (source.Language == null) return;

			if (Path.GetExtension(source.InputPath) == ".md")
			{
				var text = File.ReadAllText(source.InputPath);

				var markdown = new Markdown();
				var html = markdown.Transform(text);

				GenerateHtml(source, null, html);
			}
			else
			{
				var sections = Parse(source);
				
				Hightlight(sections);

				GenerateHtml(source, sections, null);
			}
		}

		// Given a string of source code, parse out each comment and the code that
		// follows it, and create an individual `Section` for it.
		private static List<Section> Parse(SourceInfo source)
		{
			var lines = File.ReadAllLines(source.InputPath);

			var sections = new List<Section>();
			var hasCode = false;
			var docsText = new StringBuilder();
			var codeText = new StringBuilder();

			Action<string, string> save = (docs, code) => sections.Add(new Section { DocsHtml = docs, CodeHtml = code });
			Func<string, string> mapToMarkdown = docs =>
			{
				if (source.Language.MarkdownMaps != null)
					docs = source.Language.MarkdownMaps.Aggregate(docs,
						(currentDocs, map) => Regex.Replace(currentDocs, map.Key, map.Value, RegexOptions.Multiline));
				return docs;
			};

			foreach (var line in lines)
			{
				if (source.Language.CommentMatcher.IsMatch(line) && !source.Language.CommentFilter.IsMatch(line))
				{
					if (hasCode)
					{
						save(mapToMarkdown(docsText.ToString()), codeText.ToString());
						hasCode = false;
						docsText = new StringBuilder();
						codeText = new StringBuilder();
					}
					docsText.AppendLine(source.Language.CommentMatcher.Replace(line, ""));
				}
				else
				{
					hasCode = true;
					codeText.AppendLine(line);
				}
			}
			save(mapToMarkdown(docsText.ToString()), codeText.ToString());

			return sections;
		}

		// Prepares a single chunk of code for HTML output and runs the text of its
		// corresponding comment through **Markdown**, using a C# implementation
		// called [MarkdownSharp](http://code.google.com/p/markdownsharp/).
		private static void Hightlight(IEnumerable<Section> sections)
		{
			var markdown = new Markdown();

			foreach (var section in sections)
			{
				section.DocsHtml = markdown.Transform(section.DocsHtml);
				section.CodeHtml = HttpUtility.HtmlEncode(section.CodeHtml);
			}
		}

		// Once all of the code is finished highlighting, we can generate the HTML file
		// and write out the documentation. Pass the completed sections into the template
		// found in `Resources/Nocco.cshtml`
		private static void GenerateHtml(SourceInfo source, IList<Section> sections, string rawHtml)
		{
			/*int depth;
			var destination = GetDestination(source, out depth);

			string pathToRoot = string.Concat(Enumerable.Repeat(".." + Path.DirectorySeparatorChar, depth));*/

			var htmlTemplate = (TemplateBase)Activator.CreateInstance(_templateType);

			htmlTemplate.Title = Path.GetFileName(source.InputPath);
			htmlTemplate.PathToCss = Path.Combine(source.PathToRoot, "nocco.css").Replace('\\', '/');
			htmlTemplate.PathToJs = Path.Combine(source.PathToRoot, "prettify.js").Replace('\\', '/');
			htmlTemplate.GetSourcePath =
				s => Path.Combine(source.PathToRoot, Path.GetExtension(s) == ".html" ? s : (s + ".html").Substring(2)).Replace('\\', '/');
			htmlTemplate.Sections = sections;
			htmlTemplate.RawHtml = rawHtml;
			htmlTemplate.Sources = _sources;

			htmlTemplate.Execute();

			File.WriteAllText(source.OutputPath, htmlTemplate.Buffer.ToString());
		}

		//### Helpers & Setup

		// Setup the Razor templating engine so that we can quickly pass the data in
		// and generate HTML.
		//
		// The file `Resources\Nocco.cshtml` is read and compiled into a new dll
		// with a type that extends the `TemplateBase` class. This new assembly is
		// loaded so that we can create an instance and pass data into it
		// and generate the HTML.
		private static Type SetupRazorTemplate()
		{
			var host = new RazorEngineHost(new CSharpRazorCodeLanguage())
			{
				DefaultBaseClass = typeof(TemplateBase).FullName,
				DefaultNamespace = "RazorOutput",
				DefaultClassName = "Template"
			};

			host.NamespaceImports.Add("System");

			GeneratorResults razorResult;
			using (var reader = new StreamReader(Path.Combine(_executingDirectory, "Resources", "Nocco.cshtml")))
			{
				razorResult = new RazorTemplateEngine(host).GenerateCode(reader);
			}

			var compilerParams = new CompilerParameters
			{
				GenerateInMemory = true,
				GenerateExecutable = false,
				IncludeDebugInformation = false,
				CompilerOptions = "/target:library /optimize"
			};

			compilerParams.ReferencedAssemblies.Add(typeof(Nocco).Assembly.CodeBase.Replace("file:///", "").Replace("/", "\\"));

			var codeProvider = new CSharpCodeProvider();
			var results = codeProvider.CompileAssemblyFromDom(compilerParams, razorResult.GeneratedCode);

			// Check for errors that may have occurred during template generation
			if (results.Errors.HasErrors)
			{
				foreach (var err in results.Errors.OfType<CompilerError>().Where(ce => !ce.IsWarning))
					Console.WriteLine("Error Compiling Template: ({0}, {1}) {2}", err.Line, err.Column, err.ErrorText);
			}

			return results.CompiledAssembly.GetType("RazorOutput.Template");
		}

		// A list of the languages that Nocco supports, mapping the file extension to
		// the symbol that indicates a comment. To add another language to Nocco's
		// repertoire, add it here.
		//
		// You can also specify a list of regular expression patterns and replacements. This
		// translates things like
		// [XML documentation comments](http://msdn.microsoft.com/en-us/library/b2s063f7.aspx) into Markdown.
		private static readonly Dictionary<string, Language> Languages = new Dictionary<string, Language>
		{
			{
				".md", new Language
				{
					Name = "markdown"
				}
			},
			{
				".js", new Language
				{
					Name = "javascript",
					Symbol = "//",
					Ignores = new [] { "min.js" }
				}
			},
			{
				".cs", new Language
				{
					Name = "csharp",
					Symbol = "///?",
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
				".vb", new Language
				{
					Name = "vb.net",
					Symbol = "'+",
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

		// Get the current language we're documenting, based on the extension.
		private static Language GetLanguage(string source)
		{
			var extension = Path.GetExtension(source);

			Language language;
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

				_templateType = SetupRazorTemplate();

				var files = new List<string>();

				foreach (var target in _options.Targets)
				{
					files.AddRange(Directory.GetFileSystemEntries(_options.InputDir, target, SearchOption.AllDirectories));
				}

				_sources = CollectSourceInfos(files);

				foreach (var source in _sources)
				{
					GenerateDocumentation(source);
				}
			}
		}

		private static IList<SourceInfo> CollectSourceInfos(IEnumerable<string> files)
		{
			// Don't include certain directories
			var foldersToExclude = new[] { "\\docs", "\\bin", "\\obj", "\\packages", "\\.nuget", "\\.git", "\\.svn" };

			var result = new List<SourceInfo>();

			var index = new SourceInfo
			{
				InputPath = Path.Combine(_options.InputDir, "index.html"),
			};

			result.Add(index);

			int depth;
			var destination = GetDestination(index.InputPath, out depth);
			var pathToRoot = string.Concat(Enumerable.Repeat(".." + Path.DirectorySeparatorChar, depth));

			index.OutputPath = destination;
			index.PathToRoot = pathToRoot;

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

				var language = GetLanguage(filename);

				if (language == null) continue;

				// Check if the file extension should be ignored
				if (language.Ignores != null && language.Ignores.Any(filename.EndsWith))
					continue;

				destination = GetDestination(filename, out depth);
				pathToRoot = string.Concat(Enumerable.Repeat(".." + Path.DirectorySeparatorChar, depth));

				result.Add(new SourceInfo
				{
					Language = language,
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
