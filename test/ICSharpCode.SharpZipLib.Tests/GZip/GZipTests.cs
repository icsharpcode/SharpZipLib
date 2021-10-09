using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tests.TestSupport;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
			var ms = new MemoryStream();
			var outStream = new GZipOutputStream(ms);

			var buf = Utils.GetDummyBytes(size: 100000);

			outStream.Write(buf, 0, buf.Length);
			outStream.Flush();
			outStream.Finish();

			ms.Seek(0, SeekOrigin.Begin);

			var inStream = new GZipInputStream(ms);
			byte[] buf2 = new byte[buf.Length];
			int currentIndex = 0;
			int count = buf2.Length;

			while (true)
			{
				int numRead = inStream.Read(buf2, currentIndex, count);
				if (numRead <= 0)
				{
					break;
				}
				currentIndex += numRead;
				count -= numRead;
			}

			Assert.AreEqual(0, count);

			for (int i = 0; i < buf.Length; ++i)
			{
				Assert.AreEqual(buf2[i], buf[i]);
			}
		}

		/// <summary>
		/// Writing GZip headers is delayed so that this stream can be used with HTTP/IIS.
		/// </summary>
		[Test]
		[Category("GZip")]
		public void DelayedHeaderWriteNoData()
		{
			using var ms = new MemoryStream();
			Assert.Zero(ms.Length);

			using (new GZipOutputStream(ms))
			{
				Assert.Zero(ms.Length);
			}

			Assert.NotZero(ms.ToArray().Length);
		}


		/// <summary>
		/// Variant of DelayedHeaderWriteNoData testing flushing for https://github.com/icsharpcode/SharpZipLib/issues/382
		/// </summary>
		[Test]
		[Category("GZip")]
		public void DelayedHeaderWriteFlushNoData()
		{
			var ms = new MemoryStream();
			Assert.AreEqual(0, ms.Length);

			using (GZipOutputStream outStream = new GZipOutputStream(ms) { IsStreamOwner = false })
			{
				// #382 - test flushing the stream before writing to it.
				outStream.Flush();
			}

			ms.Seek(0, SeekOrigin.Begin);

			// Test that the gzip stream can be read
			var readStream = new MemoryStream();
			using (GZipInputStream inStream = new GZipInputStream(ms))
			{
				inStream.CopyTo(readStream);
			}

			byte[] data = readStream.ToArray();

			Assert.That(data, Is.Empty, "Should not have any decompressed data");
		}

		/// <summary>
		/// Writing GZip headers is delayed so that this stream can be used with HTTP/IIS.
		/// </summary>
		[Test]
		[Category("GZip")]
		public void DelayedHeaderWriteWithData()
		{
			var ms = new MemoryStream();
			Assert.AreEqual(0, ms.Length);
			using (GZipOutputStream outStream = new GZipOutputStream(ms))
			{
				Assert.AreEqual(0, ms.Length);
				outStream.WriteByte(45);

				// Should in fact contain header right now with
				// 1 byte in the compression pipeline
				Assert.AreEqual(10, ms.Length);
			}
			byte[] data = ms.ToArray();

			Assert.IsTrue(data.Length > 0);
		}

		/// <summary>
		/// variant of DelayedHeaderWriteWithData to test https://github.com/icsharpcode/SharpZipLib/issues/382
		/// </summary>
		[Test]
		[Category("GZip")]
		public void DelayedHeaderWriteFlushWithData()
		{
			var ms = new MemoryStream();
			Assert.AreEqual(0, ms.Length);
			using (GZipOutputStream outStream = new GZipOutputStream(ms) { IsStreamOwner = false })
			{
				Assert.AreEqual(0, ms.Length);

				// #382 - test flushing the stream before writing to it.
				outStream.Flush();
				outStream.WriteByte(45);
			}

			ms.Seek(0, SeekOrigin.Begin);

			// Test that the gzip stream can be read
			var readStream = new MemoryStream();
			using (GZipInputStream inStream = new GZipInputStream(ms))
			{
				inStream.CopyTo(readStream);
			}

			// Check that the data was read
			byte[] data = readStream.ToArray();
			CollectionAssert.AreEqual(new byte[] { 45 }, data, "Decompressed data should match initial data");
		}

		[Test]
		[Category("GZip")]
		public void ZeroLengthInputStream()
		{
			var gzi = new GZipInputStream(new MemoryStream());
			bool exception = false;
			int retval = int.MinValue;
			try
			{
				retval = gzi.ReadByte();
			}
			catch
			{
				exception = true;
			}

			Assert.IsFalse(exception, "reading from an empty stream should not cause an exception");
			Assert.That(retval, Is.EqualTo(-1), "should yield -1 byte value");
		}

		[Test]
		[Category("GZip")]
		public void OutputStreamOwnership()
		{
			var memStream = new TrackedMemoryStream();
			var s = new GZipOutputStream(memStream);

			Assert.IsFalse(memStream.IsClosed, "Shouldnt be closed initially");
			Assert.IsFalse(memStream.IsDisposed, "Shouldnt be disposed initially");

			s.Close();

			Assert.IsTrue(memStream.IsClosed, "Should be closed after parent owner close");
			Assert.IsTrue(memStream.IsDisposed, "Should be disposed after parent owner close");

			memStream = new TrackedMemoryStream();
			s = new GZipOutputStream(memStream);

			Assert.IsFalse(memStream.IsClosed, "Shouldnt be closed initially");
			Assert.IsFalse(memStream.IsDisposed, "Shouldnt be disposed initially");

			s.IsStreamOwner = false;
			s.Close();

			Assert.IsFalse(memStream.IsClosed, "Should not be closed after parent owner close");
			Assert.IsFalse(memStream.IsDisposed, "Should not be disposed after parent owner close");
		}

		[Test]
		[Category("GZip")]
		public void InputStreamOwnership()
		{
			var memStream = new TrackedMemoryStream();
			var s = new GZipInputStream(memStream);

			Assert.IsFalse(memStream.IsClosed, "Shouldnt be closed initially");
			Assert.IsFalse(memStream.IsDisposed, "Shouldnt be disposed initially");

			s.Close();

			Assert.IsTrue(memStream.IsClosed, "Should be closed after parent owner close");
			Assert.IsTrue(memStream.IsDisposed, "Should be disposed after parent owner close");

			memStream = new TrackedMemoryStream();
			s = new GZipInputStream(memStream);

			Assert.IsFalse(memStream.IsClosed, "Shouldnt be closed initially");
			Assert.IsFalse(memStream.IsDisposed, "Shouldnt be disposed initially");

			s.IsStreamOwner = false;
			s.Close();

			Assert.IsFalse(memStream.IsClosed, "Should not be closed after parent owner close");
			Assert.IsFalse(memStream.IsDisposed, "Should not be disposed after parent owner close");
		}

		[Test]
		public void DoubleFooter()
		{
			var memStream = new TrackedMemoryStream();
			var s = new GZipOutputStream(memStream);
			s.Finish();
			Int64 length = memStream.Length;
			s.Close();
			Assert.AreEqual(length, memStream.ToArray().Length);
		}

		[Test]
		public void DoubleClose()
		{
			var memStream = new TrackedMemoryStream();
			var s = new GZipOutputStream(memStream);
			s.Finish();
			s.Close();
			s.Close();

			memStream = new TrackedMemoryStream();
			using (new GZipOutputStream(memStream))
			{
				s.Close();
			}
		}

		[Test]
		public void WriteAfterFinish()
		{
			var memStream = new TrackedMemoryStream();
			var s = new GZipOutputStream(memStream);
			s.Finish();

			Assert.Throws<InvalidOperationException>(() => s.WriteByte(value: 7), "Write should fail");
		}

		[Test]
		public void WriteAfterClose()
		{
			var memStream = new TrackedMemoryStream();
			var s = new GZipOutputStream(memStream);
			s.Close();

			Assert.Throws<InvalidOperationException>(() => s.WriteByte(value: 7), "Write should fail");
		}

		/// <summary>
		/// Verify that if a decompression was successful for at least one block we're exiting gracefully.
		/// </summary>
		[Test]
		public void TrailingGarbage()
		{
			/* ARRANGE */
			var ms = new MemoryStream();
			var outStream = new GZipOutputStream(ms);

			// input buffer to be compressed
			var buf = Utils.GetDummyBytes(size: 100000, seed: 3);

			// compress input buffer
			outStream.Write(buf, 0, buf.Length);
			outStream.Flush();
			outStream.Finish();

			// generate random trailing garbage and add to the compressed stream
			Utils.WriteDummyData(ms, size: 4096, seed: 4);

			// rewind the concatenated stream
			ms.Seek(0, SeekOrigin.Begin);

			/* ACT */
			// decompress concatenated stream
			var inStream = new GZipInputStream(ms);
			byte[] buf2 = new byte[buf.Length];
			int currentIndex = 0;
			int count = buf2.Length;
			while (true)
			{
				var numRead = inStream.Read(buf2, currentIndex, count);
				if (numRead <= 0)
				{
					break;
				}
				currentIndex += numRead;
				count -= numRead;
			}

			/* ASSERT */
			Assert.Zero(count);
			for (int i = 0; i < buf.Length; ++i)
			{
				Assert.AreEqual(buf2[i], buf[i]);
			}
		}

		/// <summary>
		/// Test that if we flush a GZip output stream then all data that has been written
		/// is flushed through to the underlying stream and can be successfully read back
		/// even if the stream is not yet finished.
		/// </summary>
		[Test]
		[Category("GZip")]
		public void FlushToUnderlyingStream()
		{
			var ms = new MemoryStream();
			var outStream = new GZipOutputStream(ms);

			byte[] buf = Utils.GetDummyBytes(size: 100000);

			outStream.Write(buf, 0, buf.Length);
			// Flush output stream but don't finish it yet
			outStream.Flush();

			ms.Seek(0, SeekOrigin.Begin);

			var inStream = new GZipInputStream(ms);
			byte[] buf2 = new byte[buf.Length];
			int currentIndex = 0;
			int count = buf2.Length;

			while (true)
			{
				try
				{
					int numRead = inStream.Read(buf2, currentIndex, count);
					if (numRead <= 0)
					{
						break;
					}
					currentIndex += numRead;
					count -= numRead;
				}
				catch (GZipException)
				{
					// We should get an unexpected EOF exception once we've read all
					// data as the stream isn't yet finished.
					break;
				}
			}

			Assert.AreEqual(0, count);

			for (int i = 0; i < buf.Length; ++i)
			{
				Assert.AreEqual(buf2[i], buf[i]);
			}
		}

		[Test]
		[Category("GZip")]
		public void SmallBufferDecompression([Values(0, 1, 3)] int seed)
		{
			var outputBufferSize = 100000;
			var outputBuffer = new byte[outputBufferSize];
			var inputBuffer = Utils.GetDummyBytes(outputBufferSize * 4, seed);

			using var msGzip = new MemoryStream();
			using (var gzos = new GZipOutputStream(msGzip){IsStreamOwner = false})
			{
				gzos.Write(inputBuffer, 0, inputBuffer.Length);
			}

			msGzip.Seek(0, SeekOrigin.Begin);
	
			using (var gzis = new GZipInputStream(msGzip))
			using (var msRaw = new MemoryStream())
			{
				int readOut;
				while ((readOut = gzis.Read(outputBuffer, 0, outputBuffer.Length)) > 0)
				{
					msRaw.Write(outputBuffer, 0, readOut);
				}

				var resultBuffer = msRaw.ToArray();
				for (var i = 0; i < resultBuffer.Length; i++)
				{
					Assert.AreEqual(inputBuffer[i], resultBuffer[i]);
				}
			}
		}

		/// <summary>
		/// Should gracefully handle reading from a stream that becomes unreadable after
		///  all of the data has been read.
		/// </summary>
		/// <remarks>
		/// Test for https://github.com/icsharpcode/SharpZipLib/issues/379
		/// </remarks>
		[Test]
		[Category("Zip")]
		public void ShouldGracefullyHandleReadingANonReadableStream()
		{
			MemoryStream ms = new SelfClosingStream();
			using (var gzos = new GZipOutputStream(ms))
			{
				gzos.IsStreamOwner = false;
				Utils.WriteDummyData(gzos, size: 100000);
			}

			ms.Seek(0, SeekOrigin.Begin);

			using (var gzis = new GZipInputStream(ms))
			using (var msRaw = new MemoryStream())
			{
				gzis.CopyTo(msRaw);
			}
		}

		[Test]
		[Category("GZip")]
		[Category("Performance")]
		[Category("Long Running")]
		[Explicit("Long Running")]
		public void WriteThroughput()
		{
			PerformanceTesting.TestWrite(
				size: TestDataSize.Large,
				output: w => new GZipOutputStream(w)
			);
		}

		[Test]
		[Category("GZip")]
		[Category("Performance")]
		[Explicit("Long Running")]
		public void ReadWriteThroughput()
		{
			PerformanceTesting.TestReadWrite(
				size: TestDataSize.Large,
				input: w => new GZipInputStream(w),
				output: w => new GZipOutputStream(w)
			);
		}

		/// <summary>
		/// Basic compress/decompress test
		/// </summary>
		[Test]
		[Category("GZip")]
		public void OriginalFilename()
		{
			var content = "FileContents";


			using var ms = new MemoryStream();
			using (var outStream = new GZipOutputStream(ms) { IsStreamOwner = false })
			{
				outStream.FileName = "/path/to/file.ext";

				var writeBuffer = Encoding.ASCII.GetBytes(content);
				outStream.Write(writeBuffer, 0, writeBuffer.Length);
				outStream.Flush();
				outStream.Finish();
			}

			ms.Seek(0, SeekOrigin.Begin);

			using (var inStream = new GZipInputStream(ms))
			{
				var readBuffer = new byte[content.Length];
				inStream.Read(readBuffer, 0, readBuffer.Length);
				Assert.AreEqual(content, Encoding.ASCII.GetString(readBuffer));
				Assert.AreEqual("file.ext", inStream.GetFilename());
			}
		}
	}
}
