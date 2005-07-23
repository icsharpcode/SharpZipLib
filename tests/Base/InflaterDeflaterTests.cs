using System;
using System.IO;
using System.Text;
using System.Security;

using NUnit.Framework;

using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.GZip;

namespace ICSharpCode.SharpZipLib.Tests.Base
{
	/// <summary>
	/// This class contains test cases for Deflater/Inflater streams.
	/// </summary>
	[TestFixture]
	public class InflaterDeflaterTestSuite
	{
		void Inflate(MemoryStream ms, byte[] original, int level, bool zlib)
		{
			ms.Seek(0, SeekOrigin.Begin);
			
			Inflater inflater = new Inflater(!zlib);
			InflaterInputStream inStream = new InflaterInputStream(ms, inflater);
			byte[] buf2 = new byte[original.Length];
			int    pos  = 0;
			
			try
			{
				while (true) {
					int numRead = inStream.Read(buf2, pos, 4096);
					if (numRead <= 0) {
						break;
					}
					pos += numRead;
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine("Unexpected exception - '{0}'", ex.Message);
			}
		
			if ( pos != original.Length ) {
				Console.WriteLine("Original {0}, new {1}", original.Length, pos);
			}
			
			for (int i = 0; i < original.Length; ++i) {
				if ( buf2[i] != original[i] ) {
					string description = string.Format("Difference at {0} lvl {1} zlib {2} ", i, level, zlib);
					if ( original.Length < 2048 ) {
						StringBuilder builder = new StringBuilder(description);
						for (int d = 0; d < original.Length; ++d) {
							builder.AppendFormat("{0} ", original[i]);
						}
			
						Assert.Fail(builder.ToString());
					}
					else {
						Assert.Fail(description);
					}
				}
			}
		}

		MemoryStream Deflate(byte[] data, int level, bool zlib)
		{
			MemoryStream ms = new MemoryStream();
			
			Deflater deflater = new Deflater(level, !zlib);
			DeflaterOutputStream outStream = new DeflaterOutputStream(ms, deflater);
			
			outStream.Write(data, 0, data.Length);
			outStream.Flush();
			outStream.Finish();
		
			return ms;
		}

		void RandomDeflateInflate(int size, int level, bool zlib)
		{
			byte[] buf = new byte[size];
			System.Random rnd = new Random();
			rnd.NextBytes(buf);
			
			MemoryStream ms = Deflate(buf, level, zlib);
			Inflate(ms, buf, level, zlib);
		}

		/// <summary>
		/// Random inflate/deflate test using zlib headers.
		/// </summary>
		[Test]
		[Category("Base")]
		public void TestInflateDeflateZlib()
		{
			for (int level = 0; level < 10; ++level) {
				RandomDeflateInflate(100000, level, true);
			}
		}
		/// <summary>
		/// Random inflate/deflate using non-zlib variant
		/// </summary>
		[Test]
		[Category("Base")]
		public void TestInflateDeflateNonZlib()
		{
			for (int level = 0; level < 10; ++level) {
				RandomDeflateInflate(100000, level, false);
			}
		}
		
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
		
		[Test]
		[Category("Base")]
		public void CloseDeflatorWithNestedUsing()
		{
			string tempFile = null;
			try	{
				tempFile = Path.GetTempPath();
			} 
			catch (SecurityException) {
			}
			
			Assert.IsNotNull(tempFile, "No permission to execute this test?");
			if (tempFile != null) {
				tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
				
				using (FileStream diskFile = File.Create(tempFile))
				using (DeflaterOutputStream deflator = new DeflaterOutputStream(diskFile))
				using (StreamWriter txtFile = new StreamWriter(deflator)) {
					txtFile.Write("Hello");
					txtFile.Flush();
				}
		
				File.Delete(tempFile);
			}
		}

		[Test]
		[Category("Base")]
		public void CloseInflatorWithNestedUsing()
		{
			string tempFile = null;
			try	{
				tempFile = Path.GetTempPath();
			} 
			catch (SecurityException) {
			}
				
			Assert.IsNotNull(tempFile, "No permission to execute this test?");
			if (tempFile != null) {
				tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
				using (FileStream diskFile = File.Create(tempFile))
				using (DeflaterOutputStream deflator = new DeflaterOutputStream(diskFile))
				using (StreamWriter txtFile = new StreamWriter(deflator)) {
					txtFile.Write("Hello");
					txtFile.Flush();
				}
				
				// This wont actually fail...  Test is not valid
				using (FileStream diskFile = File.OpenRead(tempFile))
				using (InflaterInputStream deflator = new InflaterInputStream(diskFile))
				using (StreamReader reader = new StreamReader(deflator)) {
					reader.Peek();
				}
				
				File.Delete(tempFile);
			}
		}
	}
}
