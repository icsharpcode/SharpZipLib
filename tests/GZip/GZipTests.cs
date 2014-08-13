using System;
using System.IO;
using System.Threading;

using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.GZip;

using NUnit.Framework;

using ICSharpCode.SharpZipLib.Tests.TestSupport;

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
			MemoryStream ms = new MemoryStream();
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
			MemoryStream ms = new MemoryStream();
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
            Assert.Inconclusive("This test not run. This test expect an exception on an empty stream, but in the GZipInputStream.Read() line 147 a comment indicate that if 0 bytes are available then it's an EOF.");
			GZipInputStream gzi = new GZipInputStream(new MemoryStream());
			bool exception = false;
			try {
				gzi.ReadByte();
			}
			catch {
				exception = true;
			}

			Assert.IsTrue(exception, "reading from an empty stream should cause an exception");
		}

		[Test]
		[Category("GZip")]
		public void OutputStreamOwnership()
		{
			TrackedMemoryStream memStream = new TrackedMemoryStream();
			GZipOutputStream s = new GZipOutputStream(memStream);

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
			TrackedMemoryStream memStream = new TrackedMemoryStream();
			GZipInputStream s = new GZipInputStream(memStream);

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
            TrackedMemoryStream memStream=new TrackedMemoryStream();
            GZipOutputStream s=new GZipOutputStream(memStream);
            s.Finish();
            Int64 length=memStream.Length;
            s.Close();
            Assert.AreEqual(length, memStream.ToArray().Length);
        }

        [Test]
        public void DoubleClose()
        {
            TrackedMemoryStream memStream=new TrackedMemoryStream();
            GZipOutputStream s=new GZipOutputStream(memStream);
            s.Finish();
            s.Close();
            s.Close();

            memStream=new TrackedMemoryStream();
            using( GZipOutputStream no2=new GZipOutputStream(memStream) )
            {
                s.Close();
            }
        }

        [Test]
        public void WriteAfterFinish()
        {
            TrackedMemoryStream memStream=new TrackedMemoryStream();
            GZipOutputStream s=new GZipOutputStream(memStream);
            s.Finish();

            try
            {
                s.WriteByte(7);
                Assert.Fail("Write should fail");
            }
            catch
            {
            }
        }

        [Test]
        public void WriteAfterClose()
        {
            TrackedMemoryStream memStream=new TrackedMemoryStream();
            GZipOutputStream s=new GZipOutputStream(memStream);
            s.Close();

            try
            {
                s.WriteByte(7);
                Assert.Fail("Write should fail");
            }
            catch
            {
            }
        }

		[Test]
		[Category("GZip")]
		[Category("Long Running")]
		public void BigStream()
		{
			window_ = new WindowedStream(0x3ffff);
			outStream_ = new GZipOutputStream(window_);
			inStream_ = new GZipInputStream(window_);

			long target = 0x10000000;
			readTarget_ = writeTarget_ = target;

			Thread reader = new Thread(Reader);
			reader.Name = "Reader";
			reader.Start();

			Thread writer = new Thread(Writer);
			writer.Name = "Writer";

			DateTime startTime = DateTime.Now;
			writer.Start();

			writer.Join();
			reader.Join();

			DateTime endTime = DateTime.Now;

			TimeSpan span = endTime - startTime;
			Console.WriteLine("Time {0}  processes {1} KB/Sec", span, (target / 1024) / span.TotalSeconds);
		}

		void Reader()
		{
			const int Size = 8192;
			int readBytes = 1;
			byte[] buffer = new byte[Size];

			long passifierLevel = readTarget_ - 0x10000000;

			while ( (readTarget_ > 0) && (readBytes > 0) ) {
				int count = Size;
				if (count > readTarget_) {
					count = (int)readTarget_;
				}

				readBytes = inStream_.Read(buffer, 0, count);
				readTarget_ -= readBytes;

				if (readTarget_ <= passifierLevel) {
					Console.WriteLine("Reader {0} bytes remaining", readTarget_);
					passifierLevel = readTarget_ - 0x10000000;
				}
			}

			Assert.IsTrue(window_.IsClosed, "Window should be closed");

			// This shouldnt read any data but should read the footer
			readBytes = inStream_.Read(buffer, 0, 1);
			Assert.AreEqual(0, readBytes, "Stream should be empty");
			Assert.AreEqual(0, window_.Length, "Window should be closed");
			inStream_.Close();
		}

		void Writer()
		{
			const int Size = 8192;

			byte[] buffer = new byte[Size];

			while (writeTarget_ > 0) {
				int thisTime = Size;
				if (thisTime > writeTarget_) {
					thisTime = (int)writeTarget_;
				}

				outStream_.Write(buffer, 0, thisTime);
				writeTarget_-= thisTime;
			}
			outStream_.Close();
		}

		WindowedStream window_;
		GZipOutputStream outStream_;
		GZipInputStream inStream_;
		long readTarget_;
		long writeTarget_;
	}
}
