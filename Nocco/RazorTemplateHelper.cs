using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Razor;
using Microsoft.CSharp;

namespace Nocco
{
	public static class RazorTemplateHelper
	{
		private static Type _templateType;

		// Once all of the code is finished highlighting, we can generate the HTML file
		// and write out the documentation. Pass the completed sections into the template
		// found in `Resources/Nocco.cshtml`
		public static string GenerateHtml(SourceInfo source, SourceModel model, IList<SourceInfo> sources)
		{
			var htmlTemplate = CreateHtmlTemplate();

			htmlTemplate.Title = Path.GetFileName(source.InputPath);
			htmlTemplate.PathToCss = Path.Combine(source.PathToRoot, "nocco.css").Replace('\\', '/');
			htmlTemplate.PathToJs = Path.Combine(source.PathToRoot, "prettify.js").Replace('\\', '/');
			htmlTemplate.GetSourcePath =
				s => Path.Combine(source.PathToRoot, Path.GetExtension(s) == ".html" ? s : (s + ".html").Substring(2)).Replace('\\', '/');
			htmlTemplate.Sources = sources;
			htmlTemplate.Model = model;

			htmlTemplate.Execute();

			return htmlTemplate.GetBuffer();
		}

		// Create new instance of template to generate html output.
		private static TemplateBase CreateHtmlTemplate()
		{
			if (_templateType == null)
				_templateType = SetupRazorTemplate();

			return (TemplateBase)Activator.CreateInstance(_templateType);
		}

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

			var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			GeneratorResults razorResult;
			using (var reader = new StreamReader(Path.Combine(executingDirectory, "Resources", "Nocco.cshtml")))
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


	}
}
