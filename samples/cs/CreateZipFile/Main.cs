// project created on 10.11.2001 at 13:09
using System;
using System.IO;

using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.GZip;

class MainClass
{
	
	public static void Main(string[] args)
	{
		string[] filenames = Directory.GetFiles(args[0]);
		
		Crc32 crc = new Crc32();
		ZipOutputStream s = new ZipOutputStream(File.Create(args[1]));
		
		s.SetLevel(6); // 0 - store only to 9 - means best compression
		
		foreach (string file in filenames) {
			FileStream fs = File.OpenRead(file);
			
			byte[] buffer = new byte[fs.Length];
			fs.Read(buffer, 0, buffer.Length);
			ZipEntry entry = new ZipEntry(file);
			
			entry.DateTime = DateTime.Now;
			
			// set Size and the crc, because the information
			// about the size and crc should be stored in the header
			// if it is not set it is automatically written in the footer.
			// (in this case size == crc == -1 in the header)
			// Some ZIP programs have problems with zip files that don't store
			// the size and crc in the header.
			entry.Size = fs.Length;
			fs.Close();
			
			crc.Reset();
			crc.Update(buffer);
			
			entry.Crc  = crc.Value;
			
			s.PutNextEntry(entry);
			
			s.Write(buffer, 0, buffer.Length);
			
		}
		
		s.Finish();
		s.Close();
	}
}
