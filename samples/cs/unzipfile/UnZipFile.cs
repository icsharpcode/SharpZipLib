// project created on 10.11.2001 at 13:09
using System;
using System.Text;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Data;

using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.GZip;


class MainClass
{			
	public static void Main(string[] args)
	{
		ZipInputStream s = new ZipInputStream(File.OpenRead(args[0]));
		
		ZipEntry theEntry;
		while ((theEntry = s.GetNextEntry()) != null) {
			
			Console.WriteLine(theEntry.Name);
			
			string directoryName = Path.GetDirectoryName(theEntry.Name);
			string fileName      = Path.GetFileName(theEntry.Name);
			
			// create directory
			Directory.CreateDirectory(directoryName);
			
			if (fileName != String.Empty) {
				FileStream streamWriter = File.Create(theEntry.Name);
				
				int size = 2048;
				byte[] data = new byte[2048];
				while (true) {
					size = s.Read(data, 0, data.Length);
					if (size > 0) {
						streamWriter.Write(data, 0, size);
					} else {
						break;
					}
				}
				
				streamWriter.Close();
			}
		}
		s.Close();
	}
}