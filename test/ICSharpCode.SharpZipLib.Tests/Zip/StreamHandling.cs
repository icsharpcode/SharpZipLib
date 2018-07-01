using System;
using System.IO;
using ICSharpCode.SharpZipLib.Tests.TestSupport;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using System.Threading;

namespace ICSharpCode.SharpZipLib.Tests.Zip
{
	/// <summary>
	/// This contains newer tests for stream handling. Much of this is still in GeneralHandling
	/// </summary>
	[TestFixture]
	public class StreamHandling : ZipBase
	{
		void MustFailRead(Stream s, byte[] buffer, int offset, int count)
		{
			bool exception = false;
			try {
				s.Read(buffer, offset, count);
			} catch {
				exception = true;
			}
			Assert.IsTrue(exception, "Read should fail");
		}

		[Test]
		[Category("Zip")]
		public void ParameterHandling()
		{
			byte[] buffer = new byte[10];
			byte[] emptyBuffer = new byte[0];

			var ms = new MemoryStream();
			var outStream = new ZipOutputStream(ms);
			outStream.IsStreamOwner = false;
			outStream.PutNextEntry(new ZipEntry("Floyd"));
			outStream.Write(buffer, 0, 10);
			outStream.Finish();

			ms.Seek(0, SeekOrigin.Begin);

			var inStream = new ZipInputStream(ms);
			ZipEntry e = inStream.GetNextEntry();

			MustFailRead(inStream, null, 0, 0);
			MustFailRead(inStream, buffer, -1, 1);
			MustFailRead(inStream, buffer, 0, 11);
			MustFailRead(inStream, buffer, 7, 5);
			MustFailRead(inStream, buffer, 0, -1);

			MustFailRead(inStream, emptyBuffer, 0, 1);

			int bytesRead = inStream.Read(buffer, 10, 0);
			Assert.AreEqual(0, bytesRead, "Should be able to read zero bytes");

			bytesRead = inStream.Read(emptyBuffer, 0, 0);
			Assert.AreEqual(0, bytesRead, "Should be able to read zero bytes");
		}

		/// <summary>
		/// Check that Zip64 descriptor is added to an entry OK.
		/// </summary>
		[Test]
		[Category("Zip")]
		public void Zip64Descriptor()
		{
			MemoryStream msw = new MemoryStreamWithoutSeek();
			var outStream = new ZipOutputStream(msw);
			outStream.UseZip64 = UseZip64.Off;

			outStream.IsStreamOwner = false;
			outStream.PutNextEntry(new ZipEntry("StripedMarlin"));
			outStream.WriteByte(89);
			outStream.Close();

			Assert.IsTrue(ZipTesting.TestArchive(msw.ToArray()));

			msw = new MemoryStreamWithoutSeek();
			outStream = new ZipOutputStream(msw);
			outStream.UseZip64 = UseZip64.On;

			outStream.IsStreamOwner = false;
			outStream.PutNextEntry(new ZipEntry("StripedMarlin"));
			outStream.WriteByte(89);
			outStream.Close();

			Assert.IsTrue(ZipTesting.TestArchive(msw.ToArray()));
		}

		[Test]
		[Category("Zip")]
		public void ReadAndWriteZip64NonSeekable()
		{
			MemoryStream msw = new MemoryStreamWithoutSeek();
			using (ZipOutputStream outStream = new ZipOutputStream(msw)) {
				outStream.UseZip64 = UseZip64.On;

				outStream.IsStreamOwner = false;
				outStream.PutNextEntry(new ZipEntry("StripedMarlin"));
				outStream.WriteByte(89);

				outStream.PutNextEntry(new ZipEntry("StripedMarlin2"));
				outStream.WriteByte(89);

				outStream.Close();
			}

			Assert.IsTrue(ZipTesting.TestArchive(msw.ToArray()));

			msw.Position = 0;

			using (ZipInputStream zis = new ZipInputStream(msw)) {
				while (zis.GetNextEntry() != null) {
					int len = 0;
					int bufferSize = 1024;
					byte[] buffer = new byte[bufferSize];
					while ((len = zis.Read(buffer, 0, bufferSize)) > 0) {
						// Reading the data is enough
					}
				}
			}
		}

		/// <summary>
		/// Check that adding an entry with no data and Zip64 works OK
		/// </summary>
		[Test]
		[Category("Zip")]
		public void EntryWithNoDataAndZip64()
		{
			MemoryStream msw = new MemoryStreamWithoutSeek();
			var outStream = new ZipOutputStream(msw);

			outStream.IsStreamOwner = false;
			var ze = new ZipEntry("Striped Marlin");
			ze.ForceZip64();
			ze.Size = 0;
			outStream.PutNextEntry(ze);
			outStream.CloseEntry();
			outStream.Finish();
			outStream.Close();

			Assert.IsTrue(ZipTesting.TestArchive(msw.ToArray()));
		}
		/// <summary>
		/// Empty zip entries can be created and read?
		/// </summary>

		[Test]
		[Category("Zip")]
		public void EmptyZipEntries()
		{
			var ms = new MemoryStream();
			var outStream = new ZipOutputStream(ms);

			for (int i = 0; i < 10; ++i) {
				outStream.PutNextEntry(new ZipEntry(i.ToString()));
			}

			outStream.Finish();

			ms.Seek(0, SeekOrigin.Begin);

			var inStream = new ZipInputStream(ms);

			int extractCount = 0;
			byte[] decompressedData = new byte[100];

			while ((inStream.GetNextEntry()) != null) {
				while (true) {
					int numRead = inStream.Read(decompressedData, extractCount, decompressedData.Length);
					if (numRead <= 0) {
						break;
					}
					extractCount += numRead;
				}
			}
			inStream.Close();
			Assert.AreEqual(extractCount, 0, "No data should be read from empty entries");
		}

		/// <summary>
		/// Empty zips can be created and read?
		/// </summary>
		[Test]
		[Category("Zip")]
		public void CreateAndReadEmptyZip()
		{
			var ms = new MemoryStream();
			var outStream = new ZipOutputStream(ms);
			outStream.Finish();

			ms.Seek(0, SeekOrigin.Begin);

			var inStream = new ZipInputStream(ms);
			while ((inStream.GetNextEntry()) != null) {
				Assert.Fail("No entries should be found in empty zip");
			}
		}

		/// <summary>
		/// Base stream is closed when IsOwner is true ( default);
		/// </summary>
		[Test]
		public void BaseClosedWhenOwner()
		{
			var ms = new TrackedMemoryStream();

			Assert.IsFalse(ms.IsClosed, "Underlying stream should NOT be closed");

			using (ZipOutputStream stream = new ZipOutputStream(ms)) {
				Assert.IsTrue(stream.IsStreamOwner, "Should be stream owner by default");
			}

			Assert.IsTrue(ms.IsClosed, "Underlying stream should be closed");
		}

		/// <summary>
		/// Check that base stream is not closed when IsOwner is false;
		/// </summary>
		[Test]
		public void BaseNotClosedWhenNotOwner()
		{
			var ms = new TrackedMemoryStream();

			Assert.IsFalse(ms.IsClosed, "Underlying stream should NOT be closed");

			using (ZipOutputStream stream = new ZipOutputStream(ms)) {
				Assert.IsTrue(stream.IsStreamOwner, "Should be stream owner by default");
				stream.IsStreamOwner = false;
			}
			Assert.IsFalse(ms.IsClosed, "Underlying stream should still NOT be closed");
		}

		/// <summary>
		/// Check that base stream is not closed when IsOwner is false;
		/// </summary>
		[Test]
		public void BaseClosedAfterFailure()
		{
			var ms = new TrackedMemoryStream(new byte[32]);

			Assert.IsFalse(ms.IsClosed, "Underlying stream should NOT be closed initially");
			bool blewUp = false;
			try {
				using (ZipOutputStream stream = new ZipOutputStream(ms)) {
					Assert.IsTrue(stream.IsStreamOwner, "Should be stream owner by default");
					try {
						stream.PutNextEntry(new ZipEntry("Tiny"));
						stream.Write(new byte[32], 0, 32);
					} finally {
						Assert.IsFalse(ms.IsClosed, "Stream should still not be closed.");
						stream.Close();
						Assert.Fail("Exception not thrown");
					}
				}
			} catch {
				blewUp = true;
			}

			Assert.IsTrue(blewUp, "Should have failed to write to stream");
			Assert.IsTrue(ms.IsClosed, "Underlying stream should be closed");
		}

		[Test]
		[Category("Zip")]
		[Category("Performance")]
		[Explicit("Long Running")]
		public void WriteThroughput()
		{
			PerformanceTesting.TestWrite(0x10000000, bs =>
			{
				var zos = new ZipOutputStream(bs);
				zos.PutNextEntry(new ZipEntry("0"));
				return zos;
			});
		}

	    [Test]
		[Category("Zip")]
		[Category("Performance")]
		[Explicit("Long Running")]
		public void SingleLargeEntry()
		{
			const string EntryName = "CantSeek";

			PerformanceTesting.TestReadWrite(
				size: TestDataSize.Large,
				input: bs =>
				{
					var zis = new ZipInputStream(bs);
					var entry = zis.GetNextEntry();

					Assert.AreEqual(entry.Name, EntryName);
					Assert.IsTrue((entry.Flags & (int)GeneralBitFlags.Descriptor) != 0);
					return zis;
				},
				output: bs =>
				{
					var zos = new ZipOutputStream(bs);
					zos.PutNextEntry(new ZipEntry(EntryName));
					return zos;
				}
			);
		}

	}
}
