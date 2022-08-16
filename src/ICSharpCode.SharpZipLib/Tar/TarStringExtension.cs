using System.IO;
using ICSharpCode.SharpZipLib.Core;

namespace ICSharpCode.SharpZipLib.Tar
{
	internal static class TarStringExtension
	{
		public static string ToTarArchivePath(this string s)
		{
			return PathUtils.DropPathRoot(s).Replace(Path.DirectorySeparatorChar, '/');
		}
	}
}
