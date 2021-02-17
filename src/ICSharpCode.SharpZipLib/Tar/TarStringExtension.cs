using System.IO;

namespace ICSharpCode.SharpZipLib.Tar
{
	internal static class TarStringExtension
	{
		public static string ClearTarPath(this string s)
		{
			if (Path.GetPathRoot(s) != null)
			{
				s = s.Substring(Path.GetPathRoot(s).Length);
			}
			return s.Replace(Path.DirectorySeparatorChar, '/');
		}
	}
}
