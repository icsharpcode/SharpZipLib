using System;
using System.IO;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tests.TestSupport;
using NUnit.Framework;
using System.Threading;
using System.Diagnostics;

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

			byte[] buf = new byte[100000];
			var rnd = new Random();
			rnd.NextBytes(buf);

			outStream.Write(buf, 0, buf.Length);
			outStream.Flush();
			outStream.Finish();

			ms.Seek(0, SeekOrigin.Begin);

			var inStream = new GZipInputStream(ms);
			byte[] buf2 = new byte[buf.Length];
			int currentIndex = 0;
			int count = buf2.Length;

			while (true) {
				int numRead = inStream.Read(buf2, currentIndex, count);
				if (numRead <= 0) {
					break;
				}
				currentIndex += numRead;
				count -= numRead;
			}

			Assert.AreEqual(0, count);

			for (int i = 0; i < buf.Length; ++i) {
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
			var ms = new MemoryStream();
			Assert.AreEqual(0, ms.Length);

			using (GZipOutputStream outStream = new GZipOutputStream(ms)) {
				Assert.AreEqual(0, ms.Length);
			}

			byte[] data = ms.ToArray();

			Assert.IsTrue(data.Length > 0);
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
			using (GZipOutputStream outStream = new GZipOutputStream(ms)) {
				Assert.AreEqual(0, ms.Length);
				outStream.WriteByte(45);

				// Should in fact contain header right now with
				// 1 byte in the compression pipeline
				Assert.AreEqual(10, ms.Length);
			}
			byte[] data = ms.ToArray();

			Assert.IsTrue(data.Length > 0);
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
			using (GZipOutputStream no2 = new GZipOutputStream(memStream)) {
				s.Close();
			}
		}

		[Test]
		public void WriteAfterFinish()
		{
			var memStream = new TrackedMemoryStream();
			var s = new GZipOutputStream(memStream);
			s.Finish();

			try {
				s.WriteByte(7);
				Assert.Fail("Write should fail");
			} catch {
			}
		}

		[Test]
		public void WriteAfterClose()
		{
			var memStream = new TrackedMemoryStream();
			var s = new GZipOutputStream(memStream);
			s.Close();

			try {
				s.WriteByte(7);
				Assert.Fail("Write should fail");
			} catch {
			}
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
			byte[] buf = new byte[100000];
			var rnd = new Random();
			rnd.NextBytes(buf);

            // compress input buffer
			outStream.Write(buf, 0, buf.Length);
			outStream.Flush();
			outStream.Finish();

            // generate random trailing garbage and add to the compressed stream
            byte[] garbage = new byte[4096];
            rnd.NextBytes(garbage);
            ms.Write(garbage, 0, garbage.Length);

            // rewind the concatenated stream
			ms.Seek(0, SeekOrigin.Begin);


            /* ACT */
            // decompress concatenated stream
			var inStream = new GZipInputStream(ms);
			byte[] buf2 = new byte[buf.Length];
			int currentIndex = 0;
			int count = buf2.Length;
            while (true) {
                int numRead = inStream.Read(buf2, currentIndex, count);
                if (numRead <= 0) {
                    break;
                }
                currentIndex += numRead;
                count -= numRead;
            }


            /* ASSERT */
			Assert.AreEqual(0, count);
			for (int i = 0; i < buf.Length; ++i) {
				Assert.AreEqual(buf2[i], buf[i]);
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


	}
}
