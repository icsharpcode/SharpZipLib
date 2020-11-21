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
			string result = path;

			if (!string.IsNullOrEmpty(path))
			{
				if ((path[0] == '\\') || (path[0] == '/'))
				{
					// UNC name ?
					if ((path.Length > 1) && ((path[1] == '\\') || (path[1] == '/')))
					{
						int index = 2;
						int elements = 2;

						// Scan for two separate elements \\machine\share\restofpath
						while ((index <= path.Length) &&
							(((path[index] != '\\') && (path[index] != '/')) || (--elements > 0)))
						{
							index++;
						}

						index++;

						if (index < path.Length)
						{
							result = path.Substring(index);
						}
						else
						{
							result = "";
						}
					}
				}
				else if ((path.Length > 1) && (path[1] == ':'))
				{
					int dropCount = 2;
					if ((path.Length > 2) && ((path[2] == '\\') || (path[2] == '/')))
					{
						dropCount = 3;
					}
					result = result.Remove(0, dropCount);
				}
			}
			return result;
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
