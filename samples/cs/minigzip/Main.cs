// project created on 11.11.2001 at 15:19
using System;
using System.IO;

using ICSharpCode.SharpZipLib.GZip;

class MainClass
{
	public static void Main(string[] args)
	{
		if (args[0] == "-d") { // decompress
			Stream s = new GZipInputStream(File.OpenRead(args[1]));
			FileStream fs = File.Create(Path.GetFileNameWithoutExtension(args[1]));
			int size = 2048;
			byte[] writeData = new byte[2048];
			while (true) {
				size = s.Read(writeData, 0, size);
				if (size > 0) {
					fs.Write(writeData, 0, size);
				} else {
					break;
				}
			}
			s.Close();
		} else { // compress
			Stream s = new GZipOutputStream(File.Create(args[0] + ".gz"));
			FileStream fs = File.OpenRead(args[0]);
			byte[] writeData = new byte[fs.Length];
			fs.Read(writeData, 0, (int)fs.Length);
			s.Write(writeData, 0, writeData.Length);
			s.Close();
		}
	}
}
