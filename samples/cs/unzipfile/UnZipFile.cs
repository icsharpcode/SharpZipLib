// project created on 10.11.2001 at 13:09
using System;
using System.Text;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Data;

using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;


class MainClass
{			
	public static void Main(string[] args)
	{
		// Perform simple parameter checking.
		if ( args.Length < 1 ) {
			Console.WriteLine("Usage UnzipFile NameOfFile");
			return;
		}
		
		if ( !File.Exists(args[0]) ) {
			Console.WriteLine("Cannot find file '{0}'", args[0]);
			return;
		}

		using (ZipInputStream s = new ZipInputStream(File.OpenRead(args[0]))) {
		
			ZipEntry theEntry;
			while ((theEntry = s.GetNextEntry()) != null) {
				
				Console.WriteLine(theEntry.Name);
				
				string directoryName = Path.GetDirectoryName(theEntry.Name);
				string fileName      = Path.GetFileName(theEntry.Name);
				
				// create directory
				if ( directoryName.Length > 0 ) {
					Directory.CreateDirectory(directoryName);
				}
				
				if (fileName != String.Empty) {
					using (FileStream streamWriter = File.Create(theEntry.Name)) {
					
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
					}
				}
			}
		}
	}
}
