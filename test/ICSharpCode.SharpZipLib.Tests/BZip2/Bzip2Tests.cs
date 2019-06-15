using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Tests.TestSupport;
using NUnit.Framework;
using System;
using System.IO;

namespace ICSharpCode.SharpZipLib.Tests.BZip2
{
	/// <summary>
	/// This class contains test cases for Bzip2 compression
	/// </summary>
	[TestFixture]
	public class BZip2Suite
	{
		/// <summary>
		/// Basic compress/decompress test BZip2
		/// </summary>
		[Test]
		[Category("BZip2")]
		public void BasicRoundTrip()
		{
			var ms = new MemoryStream();
			var outStream = new BZip2OutputStream(ms);

			byte[] buf = new byte[10000];
			var rnd = new Random();
			rnd.NextBytes(buf);

			outStream.Write(buf, 0, buf.Length);
			outStream.Close();
			ms = new MemoryStream(ms.GetBuffer());
			ms.Seek(0, SeekOrigin.Begin);

			using (BZip2InputStream inStream = new BZip2InputStream(ms))
			{
				byte[] buf2 = new byte[buf.Length];
				int pos = 0;
				while (true)
				{
					int numRead = inStream.Read(buf2, pos, 4096);
					if (numRead <= 0)
					{
						break;
					}
					pos += numRead;
				}

				for (int i = 0; i < buf.Length; ++i)
				{
					Assert.AreEqual(buf2[i], buf[i]);
				}
			}
		}

		/// <summary>
		/// Check that creating an empty archive is handled ok
		/// </summary>
		[Test]
		[Category("BZip2")]
		public void CreateEmptyArchive()
		{
			var ms = new MemoryStream();
			var outStream = new BZip2OutputStream(ms);
			outStream.Close();
			ms = new MemoryStream(ms.GetBuffer());

			ms.Seek(0, SeekOrigin.Begin);

			using (BZip2InputStream inStream = new BZip2InputStream(ms))
			{
				byte[] buffer = new byte[1024];
				int pos = 0;
				while (true)
				{
					int numRead = inStream.Read(buffer, 0, buffer.Length);
					if (numRead <= 0)
					{
						break;
					}
					pos += numRead;
				}

				Assert.AreEqual(pos, 0);
			}
		}

		[Test]
		[Category("BZip2")]
		[Category("Performance")]
		[Explicit("Long-running")]
		public void WriteThroughput()
		{
			PerformanceTesting.TestWrite(
				size: TestDataSize.Small,
				output: w => new BZip2OutputStream(w)
			);
		}

		[Test]
		[Category("BZip2")]
		[Category("Performance")]
		[Explicit("Long-running")]
		public void ReadWriteThroughput()
		{
			PerformanceTesting.TestReadWrite(
				size: TestDataSize.Small,
				input: w => new BZip2InputStream(w),
				output: w => new BZip2OutputStream(w)
			);
		}
	}
}
