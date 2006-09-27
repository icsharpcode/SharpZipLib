using System;
using System.IO;

using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.GZip;

using NUnit.Framework;

namespace ICSharpCode.SharpZipLib.Tests.GZip
{
	/// <summary>
	/// This class contains test cases for GZip compression
	/// </summary>
	[TestFixture]
	public class GZipTestSuite
	{
		/// <summary>
		/// Basic compress/decompress test
		/// </summary>
		[Test]
		[Category("GZip")]
		public void TestGZip()
		{
			MemoryStream ms = new MemoryStream();
			GZipOutputStream outStream = new GZipOutputStream(ms);
			
			byte[] buf = new byte[100000];
			System.Random rnd = new Random();
			rnd.NextBytes(buf);
			
			outStream.Write(buf, 0, buf.Length);
			outStream.Flush();
			outStream.Finish();
			
			ms.Seek(0, SeekOrigin.Begin);
			
			GZipInputStream inStream = new GZipInputStream(ms);
			byte[] buf2 = new byte[buf.Length];
			int    currentIndex  = 0;
			int    count = buf2.Length;
			while (true) {
				int numRead = inStream.Read(buf2, currentIndex, count);
				if (numRead <= 0) {
					break;
				}
				currentIndex += numRead;
				count -= numRead;
			}
			
			for (int i = 0; i < buf.Length; ++i) {
				Assert.AreEqual(buf2[i], buf[i]);
			}
		}
	}
}
