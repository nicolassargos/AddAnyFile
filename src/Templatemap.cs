using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MadsKristensen.AddAnyFile
{
	internal static class TemplateMap
	{
		private static readonly string _folder;
		private static readonly List<string> _templateFiles = new List<string>();
		private const string _defaultExt = ".txt";
		private const string _templateDir = ".templates";

		static TemplateMap()
		{
			var folder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			var userProfile = Path.Combine(folder, ".vs", _templateDir);

			if (Directory.Exists(userProfile))
			{
				_templateFiles.AddRange(Directory.GetFiles(userProfile, "*" + _defaultExt, SearchOption.AllDirectories));
			}

			var assembly = Assembly.GetExecutingAssembly().Location;
			_folder = Path.Combine(Path.GetDirectoryName(assembly), "Templates");
			_templateFiles.AddRange(Directory.GetFiles(_folder, "*" + _defaultExt, SearchOption.AllDirectories));
		}

		public static async Task<string> GetTemplateFilePathAsync(Project project, string file)
		{
			var name = Path.GetFileName(file);
			var safeName = name.StartsWith(".") ? name : Path.GetFileNameWithoutExtension(file);
			var relative = PackageUtilities.MakeRelative(project.GetRootFolder(), Path.GetDirectoryName(file) ?? "");

			var list = _templateFiles.ToList();

			AddTemplatesFromCurrentFolder(list, Path.GetDirectoryName(file));

			var templateFile = GetMatchingTemplateFromFileName(project, list, file);

			var template = await FileHelpers.ReplaceTokensAsync(project, safeName, relative, templateFile);
			return FileHelpers.NormalizeLineEndings(template);
		}

		private static void AddTemplatesFromCurrentFolder(List<string> list, string dir)
		{
			var current = new DirectoryInfo(dir);
			var dynaList = new List<string>();

			while (current != null)
			{
				var tmplDir = Path.Combine(current.FullName, _templateDir);

				if (Directory.Exists(tmplDir))
				{
					dynaList.AddRange(Directory.GetFiles(tmplDir, "*" + _defaultExt, SearchOption.AllDirectories));
				}

				current = current.Parent;
			}

			list.InsertRange(0, dynaList);
		}

		private static string GetMatchingTemplateFromFileName(Project project, List<string> templateFilePaths, string file)
		{
			var extension = FileHelpers.GetExtension(file).ToLowerInvariant();
			var name = FileHelpers.GetFileName(file);
			var safeName = name.StartsWith(".") ? name : name; // Path.GetFileNameWithoutExtension(file);

			// Look for direct file name matches
			bool directFileMatchingPredicate(string path) => Path.GetFileName(path).Equals(name + _defaultExt, StringComparison.OrdinalIgnoreCase);
			if (templateFilePaths.Any(directFileMatchingPredicate))
			{
				var tmplFile = templateFilePaths.FirstOrDefault(directFileMatchingPredicate);
				return Path.Combine(Path.GetDirectoryName(tmplFile), name + _defaultExt);//GetTemplate(name);
			}

			// Look for convention matches
			bool conventionMatchingPredicate(string path) => (safeName + _defaultExt).EndsWith(Path.GetFileName(path), StringComparison.OrdinalIgnoreCase);
			if (templateFilePaths.Any(conventionMatchingPredicate))
			{
				var tmplFile = templateFilePaths.FirstOrDefault(conventionMatchingPredicate);
				return tmplFile;
			}

			// Look for file extension matches
			bool extensionMatchingPredicate(string path) => Path.GetFileName(path).Equals(extension + _defaultExt, StringComparison.OrdinalIgnoreCase) && File.Exists(path);
			if (templateFilePaths.Any(extensionMatchingPredicate))
			{
				var tmplFile = templateFilePaths.FirstOrDefault(extensionMatchingPredicate);
				var tmpl = FileHelpers.AdjustForSpecific(project, safeName, extension);
				return Path.Combine(Path.GetDirectoryName(tmplFile), tmpl + _defaultExt); //GetTemplate(tmpl);
			}

			return null;
		}
	}
}
