using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Tests.TestSupport;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using System;
using System.IO;

namespace ICSharpCode.SharpZipLib.Tests.Zip
{
	/// <summary>
	/// This contains newer tests for stream handling. Much of this is still in GeneralHandling
	/// </summary>
	[TestFixture]
	public class StreamHandling : ZipBase
	{
		private void MustFailRead(Stream s, byte[] buffer, int offset, int count)
		{
			bool exception = false;
			try
			{
				s.Read(buffer, offset, count);
			}
			catch
			{
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
			using (ZipOutputStream outStream = new ZipOutputStream(msw))
			{
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

			using (ZipInputStream zis = new ZipInputStream(msw))
			{
				while (zis.GetNextEntry() != null)
				{
					int len = 0;
					int bufferSize = 1024;
					byte[] buffer = new byte[bufferSize];
					while ((len = zis.Read(buffer, 0, bufferSize)) > 0)
					{
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

			for (int i = 0; i < 10; ++i)
			{
				outStream.PutNextEntry(new ZipEntry(i.ToString()));
			}

			outStream.Finish();

			ms.Seek(0, SeekOrigin.Begin);

			var inStream = new ZipInputStream(ms);

			int extractCount = 0;
			byte[] decompressedData = new byte[100];

			while ((inStream.GetNextEntry()) != null)
			{
				while (true)
				{
					int numRead = inStream.Read(decompressedData, extractCount, decompressedData.Length);
					if (numRead <= 0)
					{
						break;
					}
					extractCount += numRead;
				}
			}
			inStream.Close();
			Assert.Zero(extractCount, "No data should be read from empty entries");
		}

		/// <summary>
		/// Test that calling Write with 0 bytes behaves.
		/// See issue @ https://github.com/icsharpcode/SharpZipLib/issues/123.
		/// </summary>
		[Test]
		[Category("Zip")]
		public void TestZeroByteWrite()
		{
			using (var ms = new MemoryStreamWithoutSeek())
			{
				using (var outStream = new ZipOutputStream(ms) { IsStreamOwner = false })
				{
					var ze = new ZipEntry("Striped Marlin");
					outStream.PutNextEntry(ze);

					var buffer = Array.Empty<byte>();
					outStream.Write(buffer, 0, 0);
				}

				ms.Seek(0, SeekOrigin.Begin);

				using (var inStream = new ZipInputStream(ms) { IsStreamOwner = false })
				{
					int extractCount = 0;
					byte[] decompressedData = new byte[100];

					while (inStream.GetNextEntry() != null)
					{
						while (true)
						{
							int numRead = inStream.Read(decompressedData, extractCount, decompressedData.Length);
							if (numRead <= 0)
							{
								break;
							}
							extractCount += numRead;
						}
					}
					Assert.Zero(extractCount, "No data should be read from empty entries");
				}
			}
		}

		[Test]
		[Category("Zip")]
		public void WriteZipStreamWithNoCompression([Values(0, 1, 256)] int contentLength)
		{
			var buffer = new byte[255];

			using (var dummyZip = Utils.GetDummyFile(0))
			using (var inputFile = Utils.GetDummyFile(contentLength))
			{
				// Filename is manually cleaned here to prevent this test from failing while ZipEntry doesn't automatically clean it
				var inputFileName = ZipEntry.CleanName(inputFile.Filename);

				using (var zipFileStream = File.OpenWrite(dummyZip.Filename))
				using (var zipOutputStream = new ZipOutputStream(zipFileStream))
				using (var inputFileStream = File.OpenRead(inputFile.Filename))
				{
					zipOutputStream.PutNextEntry(new ZipEntry(inputFileName)
					{
						CompressionMethod = CompressionMethod.Stored,
					});

					StreamUtils.Copy(inputFileStream, zipOutputStream, buffer);
				}

				using (var zf = new ZipFile(dummyZip.Filename))
				{
					var inputBytes = File.ReadAllBytes(inputFile.Filename);

					var entry = zf.GetEntry(inputFileName);
					Assert.IsNotNull(entry, "No entry matching source file \"{0}\" found in archive, found \"{1}\"", inputFileName, zf[0].Name);

					Assert.DoesNotThrow(() =>
					{
						using (var entryStream = zf.GetInputStream(entry))
						{
							var outputBytes = new byte[entryStream.Length];
							entryStream.Read(outputBytes, 0, outputBytes.Length);

							Assert.AreEqual(inputBytes, outputBytes, "Archive content does not match the source content");
						}
					}, "Failed to locate entry stream in archive");

					Assert.IsTrue(zf.TestArchive(testData: true), "Archive did not pass TestArchive");
				}

				
			}
		}

		[Test]
		[Category("Zip")]
		public void ZipEntryFileNameAutoClean()
		{
			using (var dummyZip = Utils.GetDummyFile(0))
			using (var inputFile = Utils.GetDummyFile()) {
				using (var zipFileStream = File.OpenWrite(dummyZip.Filename))
				using (var zipOutputStream = new ZipOutputStream(zipFileStream))
				using (var inputFileStream = File.OpenRead(inputFile.Filename))
				{
					// New ZipEntry created with a full file name path as it's name
					zipOutputStream.PutNextEntry(new ZipEntry(inputFile.Filename)
					{
						CompressionMethod = CompressionMethod.Stored,
					});

					inputFileStream.CopyTo(zipOutputStream);
				}

				using (var zf = new ZipFile(dummyZip.Filename))
				{
					// The ZipEntry name should have been automatically cleaned
					Assert.AreEqual(ZipEntry.CleanName(inputFile.Filename), zf[0].Name);
				}
			}
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
			while ((inStream.GetNextEntry()) != null)
			{
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

			using (ZipOutputStream stream = new ZipOutputStream(ms))
			{
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

			using (ZipOutputStream stream = new ZipOutputStream(ms))
			{
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
			try
			{
				using (ZipOutputStream stream = new ZipOutputStream(ms))
				{
					Assert.IsTrue(stream.IsStreamOwner, "Should be stream owner by default");
					try
					{
						stream.PutNextEntry(new ZipEntry("Tiny"));
						stream.Write(new byte[32], 0, 32);
					}
					finally
					{
						Assert.IsFalse(ms.IsClosed, "Stream should still not be closed.");
						stream.Close();
						Assert.Fail("Exception not thrown");
					}
				}
			}
			catch
			{
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

					Assert.AreEqual(EntryName, entry.Name);
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

		const string BZip2CompressedZip =
			"UEsDBC4AAAAMAEyxgU5p3ou9JwAAAAcAAAAFAAAAYS5kYXRCWmg5MUFZJlNZ0buMcAAAAkgACABA" +
			"ACAAIQCCCxdyRThQkNG7jHBQSwECMwAuAAAADABMsYFOad6LvScAAAAHAAAABQAAAAAAAAAAAAAA" +
			"AAAAAAAAYS5kYXRQSwUGAAAAAAEAAQAzAAAASgAAAAAA";

		/// <summary>
		/// Should fail to read a zip with BZip2 compression
		/// </summary>
		[Test]
		[Category("Zip")]
		public void ShouldReadBZip2EntryButNotDecompress()
		{
			var fileBytes = System.Convert.FromBase64String(BZip2CompressedZip);

			using (var input = new MemoryStream(fileBytes, false))
			{
				var zis = new ZipInputStream(input);
				var entry = zis.GetNextEntry();

				Assert.That(entry.Name, Is.EqualTo("a.dat"), "Should be able to get entry name");
				Assert.That(entry.CompressionMethod, Is.EqualTo(CompressionMethod.BZip2), "Entry should be BZip2 compressed");
				Assert.That(zis.CanDecompressEntry, Is.False, "Should not be able to decompress BZip2 entry");

				var buffer = new byte[1];
				Assert.Throws<ZipException>(() => zis.Read(buffer, 0, 1), "Trying to read the stream should throw");
			}
		}

		/// <summary>
		/// Test for https://github.com/icsharpcode/SharpZipLib/issues/341
		/// Should be able to read entries whose names contain invalid filesystem
		/// characters
		/// </summary>
		[Test]
		[Category("Zip")]
		public void ShouldBeAbleToReadEntriesWithInvalidFileNames()
		{
			var testFileName = "<A|B?C>.txt";

			using (var memoryStream = new MemoryStream())
			{
				using (var outStream = new ZipOutputStream(memoryStream))
				{
					outStream.IsStreamOwner = false;
					outStream.PutNextEntry(new ZipEntry(testFileName));
				}

				memoryStream.Seek(0, SeekOrigin.Begin);

				using (var inStream = new ZipInputStream(memoryStream))
				{
					var entry = inStream.GetNextEntry();
					Assert.That(entry.Name, Is.EqualTo(testFileName), "output name must match original name");
				}
			}
		}

		/// <summary>
		/// Test for https://github.com/icsharpcode/SharpZipLib/issues/507
		/// </summary>
		[Test]
		[Category("Zip")]
		public void AddingAnAESEntryWithNoPasswordShouldThrow()
		{
			using (var memoryStream = new MemoryStream())
			{
				using (var outStream = new ZipOutputStream(memoryStream))
				{
					var newEntry = new ZipEntry("test") { AESKeySize = 256 };

					Assert.Throws<InvalidOperationException>(() => outStream.PutNextEntry(newEntry));
				}
			}
		}

		[Test]
		[Category("Zip")]
		public void ShouldThrowDescriptiveExceptionOnUncompressedDescriptorEntry()
		{
			using (var ms = new MemoryStreamWithoutSeek())
			{
				using (var zos = new ZipOutputStream(ms))
				{
					zos.IsStreamOwner = false;
					var entry = new ZipEntry("testentry");
					entry.CompressionMethod = CompressionMethod.Stored;
					entry.Flags |= (int)GeneralBitFlags.Descriptor;
					zos.PutNextEntry(entry);
					zos.Write(new byte[1], 0, 1);
					zos.CloseEntry();
				}

				// Patch the Compression Method, since ZipOutputStream automatically changes it to Deflate when descriptors are used
				ms.Seek(8, SeekOrigin.Begin);
				ms.WriteByte((byte)CompressionMethod.Stored);
				ms.Seek(0, SeekOrigin.Begin);

				using (var zis = new ZipInputStream(ms))
				{
					zis.IsStreamOwner = false;
					var buf = new byte[32];
					zis.GetNextEntry();

					Assert.Throws(typeof(StreamUnsupportedException), () =>
					{
						zis.Read(buf, 0, buf.Length);
					});
				}
			}
		}
	}
}
