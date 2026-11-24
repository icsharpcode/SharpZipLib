using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Tests.TestSupport;
using NUnit.Framework;
using System.IO;

namespace ICSharpCode.SharpZipLib.Tests.BZip2
{
	/// <summary>
	/// This class contains test cases for Bzip2 compression
	/// </summary>
	[TestFixture]
	public class BZip2Suite
	{
		// Use the same random seed to guarantee all the code paths are followed
		const int RandomSeed = 4;
		
		/// <summary>
		/// Basic compress/decompress test BZip2
		/// </summary>
		[Test]
		[Category("BZip2")]
		public void BasicRoundTrip()
		{
			var ms = new MemoryStream();
			var outStream = new BZip2OutputStream(ms);
			
			var buf = Utils.GetDummyBytes(size: 10000, RandomSeed);

			outStream.Write(buf, offset: 0, buf.Length);
			outStream.Close();
			ms = new MemoryStream(ms.GetBuffer());
			ms.Seek(offset: 0, SeekOrigin.Begin);

			using BZip2InputStream inStream = new BZip2InputStream(ms);
			var buf2 = new byte[buf.Length];
			var pos = 0;
			while (true)
			{
				var numRead = inStream.Read(buf2, pos, count: 4096);
				if (numRead <= 0)
				{
					break;
				}
				pos += numRead;
			}

			for (var i = 0; i < buf.Length; ++i)
			{
				Assert.AreEqual(buf2[i], buf[i]);
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

				Assert.Zero(pos);
			}
		}

		[Test]
		[Category("BZip2")]
		[TestCase("425a6839314159265359c1c08044a00030cd00c346299717724538e2000001410000100244a00030cd00c3462997177245385090c1c086e2", TestName = "MalformedInput_GetAndMoveToFrontDecode")]
		[TestCase("425a6839314159265359c1c080e20003e8410000100244a000303200c3462997177245385090c1c080e2", TestName = "MalformedInput_HbCreateDecodeTables")]
		[TestCase("425a6839314159265359c1c080e2a90001410000100244a00030cd00c3462997177245384290c1c080e2", TestName = "MalformedInput_SetupBlock_OrigPtr")]
		[TestCase("425a6839314159265359c1c080e2000001410000100244a000bfcd00c346299717724538500010c080e2", TestName = "MalformedInput_RecvDecodingTables")]
		[TestCase("425a68c6314159265359c1c080e2000001410000100244a00030cd00c3672997177245385090c1c080e2", TestName = "MalformedInput_InvalidHeaderBlockSize")]
		public void MalformedInputThrowsBZip2Exception(string hexData)
		{
			var data = new byte[hexData.Length / 2];
			for (int i = 0; i < data.Length; i++)
			{
				data[i] = System.Convert.ToByte(hexData.Substring(i * 2, 2), 16);
			}

			Assert.Throws<BZip2Exception>(() =>
			{
				using var stream = new MemoryStream(data);
				using var bz2 = new BZip2InputStream(stream);
				var buffer = new byte[4096];
				while (bz2.Read(buffer, 0, buffer.Length) > 0) { }
			});
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
