using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;


class ViewZipFileClass
{			
	public static void Main(string[] args)
	{
		// Perform simple parameter checking.
		if ( args.Length < 1 ) {
			Console.WriteLine("Usage ViewZipFile NameOfFile");
			return;
		}
		
		if ( !File.Exists(args[0]) ) {
			Console.WriteLine("Cannot find file '{0}'", args[0]);
			return;
		}

		// For IO there should be exception handling but in this case its been ommitted
		
		byte[] data = new byte[4096];
		
		using (ZipInputStream s = new ZipInputStream(File.OpenRead(args[0]))) {
		
			ZipEntry theEntry;
			while ((theEntry = s.GetNextEntry()) != null) {
				Console.WriteLine("Name : {0}", theEntry.Name);
				Console.WriteLine("Date : {0}", theEntry.DateTime);
				Console.WriteLine("Size : (-1, if the size information is in the footer)");
				Console.WriteLine("      Uncompressed : {0}", theEntry.Size);
				Console.WriteLine("      Compressed   : {0}", theEntry.CompressedSize);
				
				if ( theEntry.IsFile ) {
					
					// Assuming the contents are text may be ok depending on what you are doing
					// here its fine as its shows how data can be read from a Zip archive.
					Console.Write("Show entry text (y/n) ?");
					
					if (Console.ReadLine() == "y") {
						int size = s.Read(data, 0, data.Length);
						while (size > 0) {
							Console.Write(Encoding.ASCII.GetString(data, 0, size));
							size = s.Read(data, 0, data.Length);
						}
					}
					Console.WriteLine();
				}
			}
			
			// Close can be ommitted as the using statement will do it automatically
			// but leaving it here reminds you that is should be done.
			s.Close();
		}
	}
}
