using System;
using System.Text;
using System.Collections;
using System.IO;

using ICSharpCode.SharpZipLib.Zip;

class MainClass
{
	static public void Main(string[] args)
	{
		if ( args.Length < 1 ) {
			Console.WriteLine("Usage: ZipList file");
			return;
		}
		
		if ( !File.Exists(args[0]) ) {
			Console.WriteLine("Cannot find file");
			return;
		}
		
		using (ZipFile zFile = new ZipFile(args[0])) {
			Console.WriteLine("Listing of : " + zFile.Name);
			Console.WriteLine("");
			Console.WriteLine("Raw Size    Size      Date     Time     Name");
			Console.WriteLine("--------  --------  --------  ------  ---------");
			foreach (ZipEntry e in zFile) {
				DateTime d = e.DateTime;
				Console.WriteLine("{0, -10}{1, -10}{2}  {3}   {4}", e.Size, e.CompressedSize,
				                                                    d.ToString("dd-MM-yy"), d.ToString("HH:mm"),
				                                                    e.Name);
			}
		}
	}
}
