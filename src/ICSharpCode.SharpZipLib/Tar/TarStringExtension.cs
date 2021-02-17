using System.IO;

namespace ICSharpCode.SharpZipLib.Tar
{
	internal static class TarStringExtension
	{
		public static string ClearTarPath(this string s)
		{
			var pathRoot = Path.GetPathRoot(s);
			if (!string.IsNullOrEmpty(pathRoot))
			{
				s = s.Substring(pathRoot.Length);
			}
			return s.Replace(Path.DirectorySeparatorChar, '/');
		}
	}
}
