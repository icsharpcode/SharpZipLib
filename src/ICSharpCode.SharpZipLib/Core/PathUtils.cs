using System;
using System.IO;
using System.Linq;

namespace ICSharpCode.SharpZipLib.Core
{
	/// <summary>
	/// PathUtils provides simple utilities for handling paths.
	/// </summary>
	public static class PathUtils
	{
		/// <summary>
		/// Remove any path root present in the path and optionally replaces invalid path chars,
		/// as indicated by <see cref="Path.GetInvalidPathChars"/>, with <c>'_'</c>
		/// </summary>
		/// <param name="path">A <see cref="string"/> containing path information.</param>
		/// <param name="replaceInvalidChars">Replaces any invalid path chars</param>
		/// <returns>The path with the root removed if it was present; path otherwise.</returns>
		public static string DropPathRoot(string path, bool replaceInvalidChars = false)
		{
			// Replace any invalid path characters with '_' to prevent Path.GetPathRoot throwing
			var invalidChars = Path.GetInvalidPathChars();
			var cleanPath = new string(path.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());

			if (replaceInvalidChars)
			{
				path = cleanPath;
			}
			
			var stripLength = Path.GetPathRoot(cleanPath).Length;
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
