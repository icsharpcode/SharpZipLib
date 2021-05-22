using System.IO;

namespace ICSharpCode.SharpZipLib.Tar
{
	internal static class TarStringExtension
	{
		public static string ClearTarPath(this string s)
		{
			return PathUtils.DropPathRoot(s).Replace(Path.DirectorySeparatorChar, '/');
		}
	}
}
