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
			Console.WriteLine("Name : {0}", theEntry.Name);
			Console.WriteLine("Date : {0}", theEntry.DateTime);
			Console.WriteLine("Size : (-1, if the size information is in the footer)");
			Console.WriteLine("      Uncompressed : {0}", theEntry.Size);
			Console.WriteLine("      Compressed   : {0}", theEntry.CompressedSize);
			int size = 2048;
			byte[] data = new byte[2048];
			
			Console.Write("Show Entry (y/n) ?");
			
			if (Console.ReadLine() == "y") {
//				System.IO.Stream st = File.Create("G:\\a.tst");
				while (true) {
					size = s.Read(data, 0, data.Length);
//					st.Write(data, 0, size);
					if (size > 0) {
							Console.Write(new ASCIIEncoding().GetString(data, 0, size));
					} else {
						break;
					}
				}
//				st.Close();
			}
			Console.WriteLine();
		}
		s.Close();
	}
}
