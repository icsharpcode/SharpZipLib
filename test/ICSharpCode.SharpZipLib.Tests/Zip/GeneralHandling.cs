using ICSharpCode.SharpZipLib.Tests.TestSupport;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Text;

namespace ICSharpCode.SharpZipLib.Tests.Zip
{
	/// <summary>
	/// This class contains test cases for Zip compression and decompression
	/// </summary>
	[TestFixture]
	public class GeneralHandling : ZipBase
	{
		private void AddRandomDataToEntry(ZipOutputStream zipStream, int size)
		{
			if (size > 0)
			{
				byte[] data = new byte[size];
				var rnd = new Random();
				rnd.NextBytes(data);

				zipStream.Write(data, 0, data.Length);
			}
		}

		private void ExerciseZip(CompressionMethod method, int compressionLevel,
			int size, string password, bool canSeek)
		{
			byte[] originalData = null;
			byte[] compressedData = MakeInMemoryZip(ref originalData, method, compressionLevel, size, password, canSeek);

			var ms = new MemoryStream(compressedData);
			ms.Seek(0, SeekOrigin.Begin);

			using (ZipInputStream inStream = new ZipInputStream(ms))
			{
				byte[] decompressedData = new byte[size];
				if (password != null)
				{
					inStream.Password = password;
				}

				ZipEntry entry2 = inStream.GetNextEntry();

				if ((entry2.Flags & 8) == 0)
				{
					Assert.AreEqual(size, entry2.Size, "Entry size invalid");
				}

				int currentIndex = 0;

				if (size > 0)
				{
					int count = decompressedData.Length;

					while (true)
					{
						int numRead = inStream.Read(decompressedData, currentIndex, count);
						if (numRead <= 0)
						{
							break;
						}
						currentIndex += numRead;
						count -= numRead;
					}
				}

				Assert.AreEqual(currentIndex, size, "Original and decompressed data different sizes");

				if (originalData != null)
				{
					for (int i = 0; i < originalData.Length; ++i)
					{
						Assert.AreEqual(decompressedData[i], originalData[i], "Decompressed data doesnt match original, compression level: " + compressionLevel);
					}
				}
			}
		}

		private string DescribeAttributes(FieldAttributes attributes)
		{
			string att = string.Empty;
			if ((FieldAttributes.Public & attributes) != 0)
			{
				att = att + "Public,";
			}

			if ((FieldAttributes.Static & attributes) != 0)
			{
				att = att + "Static,";
			}

			if ((FieldAttributes.Literal & attributes) != 0)
			{
				att = att + "Literal,";
			}

			if ((FieldAttributes.HasDefault & attributes) != 0)
			{
				att = att + "HasDefault,";
			}

			if ((FieldAttributes.InitOnly & attributes) != 0)
			{
				att = att + "InitOnly,";
			}

			if ((FieldAttributes.Assembly & attributes) != 0)
			{
				att = att + "Assembly,";
			}

			if ((FieldAttributes.FamANDAssem & attributes) != 0)
			{
				att = att + "FamANDAssembly,";
			}

			if ((FieldAttributes.FamORAssem & attributes) != 0)
			{
				att = att + "FamORAssembly,";
			}

			if ((FieldAttributes.HasFieldMarshal & attributes) != 0)
			{
				att = att + "HasFieldMarshal,";
			}

			return att;
		}

		/// <summary>
		/// Invalid passwords should be detected early if possible, seekable stream
		/// Note: Have a 1/255 chance of failing due to CRC collision (hence retried once)
		/// </summary>
		[Test]
		[Category("Zip")]
		[Retry(2)]
		public void InvalidPasswordSeekable()
		{
			byte[] originalData = null;
			byte[] compressedData = MakeInMemoryZip(ref originalData, CompressionMethod.Deflated, 3, 500, "Hola", true);

			var ms = new MemoryStream(compressedData);
			ms.Seek(0, SeekOrigin.Begin);

			byte[] buf2 = new byte[originalData.Length];
			int pos = 0;

			var inStream = new ZipInputStream(ms);
			inStream.Password = "redhead";

			ZipEntry entry2 = inStream.GetNextEntry();

			Assert.Throws<ZipException>(() =>
			{
				while (true)
				{
					int numRead = inStream.Read(buf2, pos, buf2.Length);
					if (numRead <= 0)
					{
						break;
					}
					pos += numRead;
				}
			});
		}

		/// <summary>
		/// Check that GetNextEntry can handle the situation where part of the entry data has been read
		/// before the call is made.  ZipInputStream.CloseEntry wasnt handling this at all.
		/// </summary>
		[Test]
		[Category("Zip")]
		public void ExerciseGetNextEntry()
		{
			byte[] compressedData = MakeInMemoryZip(
				true,
				new RuntimeInfo(CompressionMethod.Deflated, 9, 50, null, true),
				new RuntimeInfo(CompressionMethod.Deflated, 2, 50, null, true),
				new RuntimeInfo(CompressionMethod.Deflated, 9, 50, null, true),
				new RuntimeInfo(CompressionMethod.Deflated, 2, 50, null, true),
				new RuntimeInfo(null, true),
				new RuntimeInfo(CompressionMethod.Stored, 2, 50, null, true),
				new RuntimeInfo(CompressionMethod.Deflated, 9, 50, null, true)
				);

			var ms = new MemoryStream(compressedData);
			ms.Seek(0, SeekOrigin.Begin);

			using (ZipInputStream inStream = new ZipInputStream(ms))
			{
				byte[] buffer = new byte[10];

				while (inStream.GetNextEntry() != null)
				{
					// Read a portion of the data, so GetNextEntry has some work to do.
					inStream.Read(buffer, 0, 10);
				}
			}
		}

		/// <summary>
		/// Invalid passwords should be detected early if possible, non seekable stream
		/// Note: Have a 1/255 chance of failing due to CRC collision (hence retried once)
		/// </summary>
		[Test]
		[Category("Zip")]
		[Retry(2)]
		public void InvalidPasswordNonSeekable()
		{
			byte[] originalData = null;
			byte[] compressedData = MakeInMemoryZip(ref originalData, CompressionMethod.Deflated, 3, 500, "Hola", false);

			var ms = new MemoryStream(compressedData);
			ms.Seek(0, SeekOrigin.Begin);

			byte[] buf2 = new byte[originalData.Length];
			int pos = 0;

			var inStream = new ZipInputStream(ms);
			inStream.Password = "redhead";

			ZipEntry entry2 = inStream.GetNextEntry();

			Assert.Throws<ZipException>(() =>
			{
				while (true)
				{
					int numRead = inStream.Read(buf2, pos, buf2.Length);
					if (numRead <= 0)
					{
						break;
					}
					pos += numRead;
				}
			});
		}

		/// <summary>
		/// Adding an entry after the stream has Finished should fail
		/// </summary>
		[Test]
		[Category("Zip")]
		//[ExpectedException(typeof(InvalidOperationException))]
		public void AddEntryAfterFinish()
		{
			var ms = new MemoryStream();
			var s = new ZipOutputStream(ms);
			s.Finish();
			//s.PutNextEntry(new ZipEntry("dummyfile.tst"));

			Assert.That(() => s.PutNextEntry(new ZipEntry("dummyfile.tst")),
				Throws.TypeOf<InvalidOperationException>());
		}

		/// <summary>
		/// Test setting file commment to a value that is too long
		/// </summary>
		[Test]
		[Category("Zip")]
		//[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void SetCommentOversize()
		{
			var ms = new MemoryStream();
			var s = new ZipOutputStream(ms);
			//s.SetComment(new String('A', 65536));

			Assert.That(() => s.SetComment(new String('A', 65536)),
				Throws.TypeOf<ArgumentOutOfRangeException>());
		}

		/// <summary>
		/// Check that simply closing ZipOutputStream finishes the zip correctly
		/// </summary>
		[Test]
		[Category("Zip")]
		public void CloseOnlyHandled()
		{
			var ms = new MemoryStream();
			var s = new ZipOutputStream(ms);
			s.PutNextEntry(new ZipEntry("dummyfile.tst"));
			s.Close();

			Assert.IsTrue(s.IsFinished, "Output stream should be finished");
		}

		/// <summary>
		/// Basic compress/decompress test, no encryption, size is important here as its big enough
		/// to force multiple write to output which was a problem...
		/// </summary>
		[Test]
		[Category("Zip")]
		public void BasicDeflated()
		{
			for (int i = 0; i <= 9; ++i)
			{
				ExerciseZip(CompressionMethod.Deflated, i, 50000, null, true);
			}
		}

		/// <summary>
		/// Basic compress/decompress test, no encryption, size is important here as its big enough
		/// to force multiple write to output which was a problem...
		/// </summary>
		[Test]
		[Category("Zip")]
		public void BasicDeflatedNonSeekable()
		{
			for (int i = 0; i <= 9; ++i)
			{
				ExerciseZip(CompressionMethod.Deflated, i, 50000, null, false);
			}
		}

		/// <summary>
		/// Basic stored file test, no encryption.
		/// </summary>
		[Test]
		[Category("Zip")]
		public void BasicStored()
		{
			ExerciseZip(CompressionMethod.Stored, 0, 50000, null, true);
		}

		/// <summary>
		/// Basic stored file test, no encryption, non seekable output
		/// NOTE this gets converted to deflate level 0
		/// </summary>
		[Test]
		[Category("Zip")]
		public void BasicStoredNonSeekable()
		{
			ExerciseZip(CompressionMethod.Stored, 0, 50000, null, false);
		}

		[Test]
		[Category("Zip")]
		public void StoredNonSeekableKnownSizeNoCrc()
		{
			// This cannot be stored directly as the crc is not be known.
			const int TargetSize = 21348;
			const string Password = null;

			MemoryStream ms = new MemoryStreamWithoutSeek();

			using (ZipOutputStream outStream = new ZipOutputStream(ms))
			{
				outStream.Password = Password;
				outStream.IsStreamOwner = false;
				var entry = new ZipEntry("dummyfile.tst");
				entry.CompressionMethod = CompressionMethod.Stored;

				// The bit thats in question is setting the size before its added to the archive.
				entry.Size = TargetSize;

				outStream.PutNextEntry(entry);

				Assert.AreEqual(CompressionMethod.Deflated, entry.CompressionMethod, "Entry should be deflated");
				Assert.AreEqual(-1, entry.CompressedSize, "Compressed size should be known");

				var rnd = new Random();

				int size = TargetSize;
				byte[] original = new byte[size];
				rnd.NextBytes(original);

				// Although this could be written in one chunk doing it in lumps
				// throws up buffering problems including with encryption the original
				// source for this change.
				int index = 0;
				while (size > 0)
				{
					int count = (size > 0x200) ? 0x200 : size;
					outStream.Write(original, index, count);
					size -= 0x200;
					index += count;
				}
			}
			Assert.IsTrue(ZipTesting.TestArchive(ms.ToArray()));
		}

		[Test]
		[Category("Zip")]
		public void StoredNonSeekableKnownSizeNoCrcEncrypted()
		{
			// This cant be stored directly as the crc is not known
			const int TargetSize = 24692;
			const string Password = "Mabutu";

			MemoryStream ms = new MemoryStreamWithoutSeek();

			using (ZipOutputStream outStream = new ZipOutputStream(ms))
			{
				outStream.Password = Password;
				outStream.IsStreamOwner = false;
				var entry = new ZipEntry("dummyfile.tst");
				entry.CompressionMethod = CompressionMethod.Stored;

				// The bit thats in question is setting the size before its added to the archive.
				entry.Size = TargetSize;

				outStream.PutNextEntry(entry);

				Assert.AreEqual(CompressionMethod.Deflated, entry.CompressionMethod, "Entry should be stored");
				Assert.AreEqual(-1, entry.CompressedSize, "Compressed size should be known");

				var rnd = new Random();

				int size = TargetSize;
				byte[] original = new byte[size];
				rnd.NextBytes(original);

				// Although this could be written in one chunk doing it in lumps
				// throws up buffering problems including with encryption the original
				// source for this change.
				int index = 0;
				while (size > 0)
				{
					int count = (size > 0x200) ? 0x200 : size;
					outStream.Write(original, index, count);
					size -= 0x200;
					index += count;
				}
			}
			Assert.IsTrue(ZipTesting.TestArchive(ms.ToArray(), Password));
		}

		/// <summary>
		/// Basic compress/decompress test, with encryption, size is important here as its big enough
		/// to force multiple writes to output which was a problem...
		/// </summary>
		[Test]
		[Category("Zip")]
		public void BasicDeflatedEncrypted()
		{
			for (int i = 0; i <= 9; ++i)
			{
				ExerciseZip(CompressionMethod.Deflated, i, 50157, "Rosebud", true);
			}
		}

		/// <summary>
		/// Basic compress/decompress test, with encryption, size is important here as its big enough
		/// to force multiple write to output which was a problem...
		/// </summary>
		[Test]
		[Category("Zip")]
		public void BasicDeflatedEncryptedNonSeekable()
		{
			for (int i = 0; i <= 9; ++i)
			{
				ExerciseZip(CompressionMethod.Deflated, i, 50000, "Rosebud", false);
			}
		}

		[Test]
		[Category("Zip")]
		public void SkipEncryptedEntriesWithoutSettingPassword()
		{
			byte[] compressedData = MakeInMemoryZip(true,
				new RuntimeInfo("1234", true),
				new RuntimeInfo(CompressionMethod.Deflated, 2, 1, null, true),
				new RuntimeInfo(CompressionMethod.Deflated, 9, 1, "1234", true),
				new RuntimeInfo(CompressionMethod.Deflated, 2, 1, null, true),
				new RuntimeInfo(null, true),
				new RuntimeInfo(CompressionMethod.Stored, 2, 1, "4321", true),
				new RuntimeInfo(CompressionMethod.Deflated, 9, 1, "1234", true)
				);

			var ms = new MemoryStream(compressedData);
			var inStream = new ZipInputStream(ms);

			while (inStream.GetNextEntry() != null)
			{
			}

			inStream.Close();
		}

		[Test]
		[Category("Zip")]
		public void MixedEncryptedAndPlain()
		{
			byte[] compressedData = MakeInMemoryZip(true,
				new RuntimeInfo(CompressionMethod.Deflated, 2, 1, null, true),
				new RuntimeInfo(CompressionMethod.Deflated, 9, 1, "1234", false),
				new RuntimeInfo(CompressionMethod.Deflated, 2, 1, null, false),
				new RuntimeInfo(CompressionMethod.Deflated, 9, 1, "1234", true)
				);

			var ms = new MemoryStream(compressedData);
			using (ZipInputStream inStream = new ZipInputStream(ms))
			{
				inStream.Password = "1234";

				int extractCount = 0;
				int extractIndex = 0;
				ZipEntry entry;
				byte[] decompressedData = new byte[100];

				while ((entry = inStream.GetNextEntry()) != null)
				{
					extractCount = decompressedData.Length;
					extractIndex = 0;
					while (true)
					{
						int numRead = inStream.Read(decompressedData, extractIndex, extractCount);
						if (numRead <= 0)
						{
							break;
						}
						extractIndex += numRead;
						extractCount -= numRead;
					}
				}
				inStream.Close();
			}
		}

		/// <summary>
		/// Basic stored file test, with encryption.
		/// </summary>
		[Test]
		[Category("Zip")]
		public void BasicStoredEncrypted()
		{
			ExerciseZip(CompressionMethod.Stored, 0, 50000, "Rosebud", true);
		}

		/// <summary>
		/// Basic stored file test, with encryption, non seekable output.
		/// NOTE this gets converted deflate level 0
		/// </summary>
		[Test]
		[Category("Zip")]
		public void BasicStoredEncryptedNonSeekable()
		{
			ExerciseZip(CompressionMethod.Stored, 0, 50000, "Rosebud", false);
		}

		/// <summary>
		/// Check that when the output stream cannot seek that requests for stored
		/// are in fact converted to defalted level 0
		/// </summary>
		[Test]
		[Category("Zip")]
		public void StoredNonSeekableConvertToDeflate()
		{
			var ms = new MemoryStreamWithoutSeek();

			var outStream = new ZipOutputStream(ms);
			outStream.SetLevel(8);
			Assert.AreEqual(8, outStream.GetLevel(), "Compression level invalid");

			var entry = new ZipEntry("1.tst");
			entry.CompressionMethod = CompressionMethod.Stored;
			outStream.PutNextEntry(entry);
			Assert.AreEqual(0, outStream.GetLevel(), "Compression level invalid");

			AddRandomDataToEntry(outStream, 100);
			entry = new ZipEntry("2.tst");
			entry.CompressionMethod = CompressionMethod.Deflated;
			outStream.PutNextEntry(entry);
			Assert.AreEqual(8, outStream.GetLevel(), "Compression level invalid");
			AddRandomDataToEntry(outStream, 100);

			outStream.Close();
		}

		/// <summary>
		/// Check that adding more than the 2.0 limit for entry numbers is detected and handled
		/// </summary>
		//[Test]
		//[Category("Zip")]
		//[Category("Long Running")]
		//public void Stream_64KPlusOneEntries()
		//{
		//	const int target = 65537;
		//	MemoryStream ms = new MemoryStream();
		//	using (ZipOutputStream s = new ZipOutputStream(ms)) {
		//		for (int i = 0; i < target; ++i) {
		//			s.PutNextEntry(new ZipEntry("dummyfile.tst"));
		//		}

		//		s.Finish();
		//		ms.Seek(0, SeekOrigin.Begin);
		//		using (ZipFile zipFile = new ZipFile(ms)) {
		//			Assert.AreEqual(target, zipFile.Count, "Incorrect number of entries stored");
		//		}
		//	}
		//}

		/// <summary>
		/// Check that Unicode filename support works.
		/// </summary>
		[Test]
		[Category("Zip")]
		public void Stream_UnicodeEntries()
		{
			var ms = new MemoryStream();
			using (ZipOutputStream s = new ZipOutputStream(ms))
			{
				s.IsStreamOwner = false;

				string sampleName = "\u03A5\u03d5\u03a3";
				var sample = new ZipEntry(sampleName);
				sample.IsUnicodeText = true;
				s.PutNextEntry(sample);

				s.Finish();
				ms.Seek(0, SeekOrigin.Begin);

				using (ZipInputStream zis = new ZipInputStream(ms))
				{
					ZipEntry ze = zis.GetNextEntry();
					Assert.AreEqual(sampleName, ze.Name, "Expected name to match original");
					Assert.IsTrue(ze.IsUnicodeText, "Expected IsUnicodeText flag to be set");
				}
			}
		}

		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void PartialStreamClosing()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");

			if (tempFile != null)
			{
				tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
				MakeZipFile(tempFile, new String[] { "Farriera", "Champagne", "Urban myth" }, 10, "Aha");

				using (ZipFile zipFile = new ZipFile(tempFile))
				{
					Stream stream = zipFile.GetInputStream(0);
					stream.Close();

					stream = zipFile.GetInputStream(1);
					zipFile.Close();
				}
				File.Delete(tempFile);
			}
		}

		private void TestLargeZip(string tempFile, int targetFiles)
		{
			const int BlockSize = 4096;

			byte[] data = new byte[BlockSize];
			byte nextValue = 0;
			for (int i = 0; i < BlockSize; ++i)
			{
				nextValue = ScatterValue(nextValue);
				data[i] = nextValue;
			}

			using (ZipFile zFile = new ZipFile(tempFile))
			{
				Assert.AreEqual(targetFiles, zFile.Count);
				byte[] readData = new byte[BlockSize];
				int readIndex;
				foreach (ZipEntry ze in zFile)
				{
					Stream s = zFile.GetInputStream(ze);
					readIndex = 0;
					while (readIndex < readData.Length)
					{
						readIndex += s.Read(readData, readIndex, data.Length - readIndex);
					}

					for (int ii = 0; ii < BlockSize; ++ii)
					{
						Assert.AreEqual(data[ii], readData[ii]);
					}
				}
				zFile.Close();
			}
		}

		//      [Test]
		//      [Category("Zip")]
		//      [Category("CreatesTempFile")]
		public void TestLargeZipFile()
		{
			string tempFile = @"g:\\tmp";
			tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
			TestLargeZip(tempFile, 8100);
		}

		//      [Test]
		//      [Category("Zip")]
		//      [Category("CreatesTempFile")]
		public void MakeLargeZipFile()
		{
			string tempFile = null;
			try
			{
				//            tempFile = Path.GetTempPath();
				tempFile = @"g:\\tmp";
			}
			catch (SecurityException)
			{
			}

			Assert.IsNotNull(tempFile, "No permission to execute this test?");

			const int blockSize = 4096;

			byte[] data = new byte[blockSize];
			byte nextValue = 0;
			for (int i = 0; i < blockSize; ++i)
			{
				nextValue = ScatterValue(nextValue);
				data[i] = nextValue;
			}

			tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
			Console.WriteLine("Starting at {0}", DateTime.Now);
			try
			{
				//               MakeZipFile(tempFile, new String[] {"1", "2" }, int.MaxValue, "C1");
				using (FileStream fs = File.Create(tempFile))
				{
					var zOut = new ZipOutputStream(fs);
					zOut.SetLevel(4);
					const int TargetFiles = 8100;
					for (int i = 0; i < TargetFiles; ++i)
					{
						var e = new ZipEntry(i.ToString());
						e.CompressionMethod = CompressionMethod.Stored;

						zOut.PutNextEntry(e);
						for (int block = 0; block < 128; ++block)
						{
							zOut.Write(data, 0, blockSize);
						}
					}
					zOut.Close();
					fs.Close();

					TestLargeZip(tempFile, TargetFiles);
				}
			}
			finally
			{
				Console.WriteLine("Starting at {0}", DateTime.Now);
				//               File.Delete(tempFile);
			}
		}

		/// <summary>
		/// Test for handling of zero lengths in compression using a formatter which
		/// will request reads of zero length...
		/// </summary>
		[Test]
		[Category("Zip")]
		[Ignore("With ArraySegment<byte> for crc checking, this test doesn't throw an exception. Not sure if it's needed.")]
		public void SerializedObjectZeroLength()
		{
			bool exception = false;

			object data = new byte[0];
			// Thisa wont be zero length here due to serialisation.
			try
			{
				byte[] zipped = ZipZeroLength(data);

				object o = UnZipZeroLength(zipped);

				var returned = o as byte[];

				Assert.IsNotNull(returned, "Expected a byte[]");
				Assert.AreEqual(0, returned.Length);
			}
			catch (ArgumentOutOfRangeException)
			{
				exception = true;
			}

			Assert.IsTrue(exception, "Passing an offset greater than or equal to buffer.Length should cause an ArgumentOutOfRangeException");
		}

		/// <summary>
		/// Test for handling of serialized reference and value objects.
		/// </summary>
		[Test]
		[Category("Zip")]
		public void SerializedObject()
		{
			var sampleDateTime = new DateTime(1853, 8, 26);
			var data = (object)sampleDateTime;
			byte[] zipped = ZipZeroLength(data);
			object rawObject = UnZipZeroLength(zipped);

			var returnedDateTime = (DateTime)rawObject;

			Assert.AreEqual(sampleDateTime, returnedDateTime);

			string sampleString = "Mary had a giant cat it ears were green and smelly";
			zipped = ZipZeroLength(sampleString);

			rawObject = UnZipZeroLength(zipped);

			var returnedString = rawObject as string;

			Assert.AreEqual(sampleString, returnedString);
		}

		private byte[] ZipZeroLength(object data)
		{
			var formatter = new BinaryFormatter();
			var memStream = new MemoryStream();

			using (ZipOutputStream zipStream = new ZipOutputStream(memStream))
			{
				zipStream.PutNextEntry(new ZipEntry("data"));
				formatter.Serialize(zipStream, data);
				zipStream.CloseEntry();
				zipStream.Close();
			}

			byte[] result = memStream.ToArray();
			memStream.Close();

			return result;
		}

		private object UnZipZeroLength(byte[] zipped)
		{
			if (zipped == null)
			{
				return null;
			}

			object result = null;
			var formatter = new BinaryFormatter();
			var memStream = new MemoryStream(zipped);
			using (ZipInputStream zipStream = new ZipInputStream(memStream))
			{
				ZipEntry zipEntry = zipStream.GetNextEntry();
				if (zipEntry != null)
				{
					result = formatter.Deserialize(zipStream);
				}
				zipStream.Close();
			}
			memStream.Close();

			return result;
		}

		private void CheckNameConversion(string toCheck)
		{
			byte[] intermediate = ZipStrings.ConvertToArray(toCheck);
			string final = ZipStrings.ConvertToString(intermediate);

			Assert.AreEqual(toCheck, final, "Expected identical result");
		}

		[Test]
		[Category("Zip")]
		public void NameConversion()
		{
			CheckNameConversion("Hello");
			CheckNameConversion("a/b/c/d/e/f/g/h/SomethingLikeAnArchiveName.txt");
		}

		[Test]
		[Category("Zip")]
		public void UnicodeNameConversion()
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			ZipStrings.CodePage = 850;
			string sample = "Hello world";

			byte[] rawData = Encoding.ASCII.GetBytes(sample);

			string converted = ZipStrings.ConvertToStringExt(0, rawData);
			Assert.AreEqual(sample, converted);

			converted = ZipStrings.ConvertToStringExt((int)GeneralBitFlags.UnicodeText, rawData);
			Assert.AreEqual(sample, converted);

			// This time use some greek characters
			sample = "\u03A5\u03d5\u03a3";
			rawData = Encoding.UTF8.GetBytes(sample);

			converted = ZipStrings.ConvertToStringExt((int)GeneralBitFlags.UnicodeText, rawData);
			Assert.AreEqual(sample, converted);
		}

		/// <summary>
		/// Regression test for problem where the password check would fail for an archive whose
		/// date was updated from the extra data.
		/// This applies to archives where the crc wasnt know at the time of encryption.
		/// The date of the entry is used in its place.
		/// </summary>
		[Test]
		[Category("Zip")]
		[Ignore("at commit 60831547c868cc56d43f24473f7d5f2cc51fb754 this unit test passed but the behavior of ZipEntry.DateTime has changed completely ever since. Not sure if this unit test is still needed.")]
		public void PasswordCheckingWithDateInExtraData()
		{
			var ms = new MemoryStream();
			var checkTime = new DateTimeOffset(2010, 10, 16, 0, 3, 28, new TimeSpan(1, 0, 0));

			using (ZipOutputStream zos = new ZipOutputStream(ms))
			{
				zos.IsStreamOwner = false;
				zos.Password = "secret";
				var ze = new ZipEntry("uno");
				ze.DateTime = new DateTime(1998, 6, 5, 4, 3, 2);

				var zed = new ZipExtraData();

				zed.StartNewEntry();

				zed.AddData(1);

				TimeSpan delta = checkTime.UtcDateTime - new DateTime(1970, 1, 1, 0, 0, 0).ToUniversalTime();
				var seconds = (int)delta.TotalSeconds;
				zed.AddLeInt(seconds);
				zed.AddNewEntry(0x5455);

				ze.ExtraData = zed.GetEntryData();
				zos.PutNextEntry(ze);
				zos.WriteByte(54);
			}

			ms.Position = 0;
			using (ZipInputStream zis = new ZipInputStream(ms))
			{
				zis.Password = "secret";
				ZipEntry uno = zis.GetNextEntry();
				var theByte = (byte)zis.ReadByte();
				Assert.AreEqual(54, theByte);
				Assert.AreEqual(-1, zis.ReadByte()); // eof
				Assert.AreEqual(checkTime.DateTime, uno.DateTime);
			}
		}
	}
}
