using System.IO;

namespace ICSharpCode.SharpZipLib.Core
{
	/// <summary>
	/// PathUtils provides simple utilities for handling paths.
	/// </summary>
	public static class PathUtils
	{
		/// <summary>
		/// Remove any path root present in the path
		/// </summary>
		/// <param name="path">A <see cref="string"/> containing path information.</param>
		/// <returns>The path with the root removed if it was present; path otherwise.</returns>
		/// <remarks>Unlike the <see cref="System.IO.Path"/> class the path isn't otherwise checked for validity.</remarks>
		public static string DropPathRoot(string path)
		{
			var stripLength = Path.GetPathRoot(path).Length;
			while (path.Length > stripLength && (path[stripLength] == '/' || path[stripLength] == '\\')) stripLength++;
			return path.Substring(stripLength);
		}

		/// <summary>
		/// Returns a random file name in the users temporary directory, or in directory of <paramref name="original"/> if specified
		/// </summary>
		/// <param name="original">If specified, used as the base file name for the temporary file</param>
		/// <returns>Returns a temporary file name</returns>
		public static string GetTempFileName(string original)
		{
			string fileName;
			var tempPath = Path.GetTempPath();

			do
			{
				fileName = original == null
					? Path.Combine(tempPath, Path.GetRandomFileName())
					: $"{original}.{Path.GetRandomFileName()}";
			} while (File.Exists(fileName));

			return fileName;
		}
	}
}
