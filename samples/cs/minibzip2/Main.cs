using System;
using System.IO;

using ICSharpCode.SharpZipLib.BZip2;

class MainClass
{
	public static void Main(string[] args)
	{
		if (args[0] == "-d") { // decompress
			BZip2.Decompress(File.OpenRead(args[1]), File.Create(Path.GetFileNameWithoutExtension(args[1])));
		} else { // compress
			BZip2.Compress(File.OpenRead(args[0]), File.Create(args[0] + ".bz"), 4096);
		}
	}
}
