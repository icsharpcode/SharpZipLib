using System;
using System.IO;

using NUnit.Framework;

using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.GZip;

namespace ICSharpCode.SharpZipLib.Tests.Base
{
	/// <summary>
	/// This class contains test cases for the Adler32 and Crc32 checksums
	/// </summary>
	[TestFixture]
	public class InflaterDeflaterTestSuite
	{
		/// <summary>
		/// Basic inflate/deflate test
		/// </summary>
		[Test]
		[Category("Base")]
		public void TestInflateDeflate()
		{
			MemoryStream ms = new MemoryStream();
			Deflater deflater = new Deflater(6);
			DeflaterOutputStream outStream = new DeflaterOutputStream(ms, deflater);
			
			byte[] buf = new byte[1000000];
			System.Random rnd = new Random();
			rnd.NextBytes(buf);
			
			outStream.Write(buf, 0, buf.Length);
			outStream.Flush();
			outStream.Finish();
			
			ms.Seek(0, SeekOrigin.Begin);
			
			InflaterInputStream inStream = new InflaterInputStream(ms);
			byte[] buf2 = new byte[buf.Length];
			int    pos  = 0;
			while (true) {
				int numRead = inStream.Read(buf2, pos, 4096);
				if (numRead <= 0) {
					break;
				}
				pos += numRead;
			}
			
			for (int i = 0; i < buf.Length; ++i) {
				Assert.AreEqual(buf2[i], buf[i]);
			}
		}
	}
}
