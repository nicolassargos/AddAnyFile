using EnvDTE;

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MadsKristensen.AddAnyFile
{
	internal static class FileHelpers
	{

		public static string AdjustForSpecific(Project project, string safeName, string extension)
		{
			if (Regex.IsMatch(safeName, "^I[A-Z].*"))
			{
				return extension += "-interface";
			}
			else if (Regex.IsMatch(safeName, @".+Enum$"))
			{
				return extension += "-enum";
			}
			else if (Regex.IsMatch(safeName, @".+Controller$") && project.IsMVCProject())
			{
				return extension += "-controller";
			}

			return extension;
		}

		public static string NormalizeLineEndings(string content)
		{
			if (string.IsNullOrEmpty(content))
			{
				return content;
			}

			return Regex.Replace(content, @"\r\n|\n\r|\n|\r", "\r\n");
		}

		public static async Task<string> ReplaceTokensAsync(Project project, string name, string relative, string templateFile)
		{
			if (string.IsNullOrEmpty(templateFile))
			{
				return templateFile;
			}

			var rootNs = project.GetRootNamespace();
			var ns = string.IsNullOrEmpty(rootNs) ? "MyNamespace" : rootNs;

			var mvcProjectControllerNs = project.GetMVCNamespace() ?? "";

			if (!string.IsNullOrEmpty(relative))
			{
				ns += "." + ProjectHelpers.CleanNameSpace(relative);
			}

			using (var reader = new StreamReader(templateFile))
			{
				var content = await reader.ReadToEndAsync();

				return content.Replace("{namespace}", ns)
							  .Replace("{itemname}", Path.GetFileNameWithoutExtension(name))
							  .Replace("{mvcprojectnamespace}", mvcProjectControllerNs);
			}
		}

		public static string GetFileName(string path)
		{
			if (path == null) return null;
			if (!path.Contains(".") || path.StartsWith(".") ) return path;

			var fileName = path.Split('\\').Last();

			return fileName.Split('.')[0];
		}

		public static string GetExtension(string path)
		{
			if (path == null || !path.Contains(".")) return null;

			var fileName = path.Split('\\').Last();
			int ExtensionPointIndex = fileName.IndexOf('.');
			var result = fileName.Substring(ExtensionPointIndex, fileName.Length - ExtensionPointIndex);

			return result;
		}
	}
}