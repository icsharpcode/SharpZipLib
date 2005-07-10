using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;

using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Checksums;

using NUnit.Framework;

namespace ICSharpCode.SharpZipLib.Tests.Zip
{
	class MemStreamWithoutSeek : MemoryStream
	{
		public override bool CanSeek {
			get {
				return false;
			}
		}
	}

   class RuntimeInfo
   {
      public RuntimeInfo(CompressionMethod method, int compressionLevel, int size, string password, bool getCrc)
      {
         this.method = method;
         this.compressionLevel = compressionLevel;
         this.password = password;
         this.size = size;
         this.random = false;

         original = new byte[Size];
         if ( random )
         {
            System.Random rnd = new Random();
            rnd.NextBytes(original);
         }
         else
         {
            for ( int i = 0; i < size; ++i)
            {
               original[i] = (byte)'A';
            }
         }

         if ( getCrc )
         {
            Crc32 crc32 = new Crc32();
            crc32.Update(original, 0, size);
            crc = crc32.Value;
         }
      }

      byte[] original;
      public byte[] Original
      {
         get { return original; }
      }

      public CompressionMethod Method
      {
         get { return method; }
      }

      CompressionMethod method;

      public int CompressionLevel
      {
         get { return compressionLevel; }
      }

      int compressionLevel;

      public int Size
      {
         get { return size; }
      }

      int size;

      public string Password
      {
         get { return password; }
      }

      string password;

      bool Random
      {
         get { return random; }
      }

      bool random;

      public long Crc
      {
         get { return crc; }
      }

      long crc = -1;
   }

	/// <summary>
	/// This class contains test cases for Zip compression and decompression
	/// </summary>
	[TestFixture]
	public class ZipTestSuite
	{
		void AddRandomDataToEntry(ZipOutputStream zipStream, int size)
		{
			if (size > 0) {
				byte [] data = new byte[size];
				System.Random rnd = new Random();
				rnd.NextBytes(data);
			
				zipStream.Write(data, 0, data.Length);
			}
		}

		byte ScatterValue(byte rhs)
		{
			return (byte) (rhs * 253 + 7);
		}
		
		void AddKnownDataToEntry(ZipOutputStream zipStream, int size)
		{
			if (size > 0) {
				byte nextValue = 0;
				byte [] data = new byte[size];
				for (int i = 0; i < size; ++i) {
					data[i] = nextValue;
					nextValue = ScatterValue(nextValue);			
				}
				zipStream.Write(data, 0, data.Length);
			}
		}

      byte[] MakeMemZip(bool withSeek, params object[] createSpecs)
      {
         MemoryStream ms;
			
         if (withSeek == true) 
         {
            ms = new MemoryStream();
         } 
         else 
         {
            ms = new MemStreamWithoutSeek();
         }
			
         ZipOutputStream outStream = new ZipOutputStream(ms);

         int counter;

         for ( counter = 0; counter < createSpecs.Length; ++counter )
         {
            RuntimeInfo info = createSpecs[counter] as RuntimeInfo;
            outStream.Password = info.Password;

            if (info.Method != CompressionMethod.Stored)
               outStream.SetLevel(info.CompressionLevel); // 0 - store only to 9 - means best compression

            ZipEntry entry = new ZipEntry("entry" + counter + ".tst");
            entry.CompressionMethod = info.Method;
            if ( info.Crc >= 0 )
            {
               entry.Crc = info.Crc;
            }

            outStream.PutNextEntry(entry);
			
            if (info.Size > 0) 
            {
               outStream.Write(info.Original, 0, info.Original.Length);
            }
         }
      			
         outStream.Close();
         return ms.ToArray();
      }
		
		byte[] MakeMemZip(ref byte[] original, CompressionMethod method, int compressionLevel, int size, string password, bool withSeek)
		{
			MemoryStream ms;
			
			if (withSeek == true) {
				ms = new MemoryStream();
			} else {
				ms = new MemStreamWithoutSeek();
			}
			
			ZipOutputStream outStream = new ZipOutputStream(ms);
         outStream.Password = password;
			
			if (method != CompressionMethod.Stored)
				outStream.SetLevel(compressionLevel); // 0 - store only to 9 - means best compression
			
			ZipEntry entry = new ZipEntry("dummyfile.tst");
			entry.CompressionMethod = method;
			
			outStream.PutNextEntry(entry);
			
			if (size > 0) {
				original = new byte[size];
				System.Random rnd = new Random();
				rnd.NextBytes(original);
			
				outStream.Write(original, 0, original.Length);
			}
			outStream.Close();
			return ms.ToArray();
		}
		
		void ExerciseZip(CompressionMethod method, int compressionLevel, int size, string password, bool canSeek)
		{
			byte[] originalData = null;
			byte[] compressedData = MakeMemZip(ref originalData, method, compressionLevel, size, password, canSeek);
			
			MemoryStream ms = new MemoryStream(compressedData);
			ms.Seek(0, SeekOrigin.Begin);
			
			ZipInputStream inStream = new ZipInputStream(ms);
			byte[] decompressedData = new byte[size];
			int    pos  = 0;
			if (password != null) {
				inStream.Password = password;
			}
			
			ZipEntry entry2 = inStream.GetNextEntry();
			
			if ((entry2.Flags & 8) == 0) {
				// -jr- problem here!!
				Assert.AreEqual(size, entry2.Size, "Entry size invalid");
			}
			
			if (size > 0) {
				while (true) {
					int numRead = inStream.Read(decompressedData, pos, 4096);
					if (numRead <= 0) {
						break;
					}
					pos += numRead;
				}
			}
		
			Assert.AreEqual(pos, size, "Original and decompressed data different sizes" );
			
			if (originalData != null) {
				for (int i = 0; i < originalData.Length; ++i) {
					Assert.AreEqual(decompressedData[i], originalData[i], "Decompressed data doesnt match original, compression level: " + compressionLevel);
				}
			}
		}

		/// <summary>
		/// Empty zip entries can be created and read?
		/// </summary>
		[Test]
		[Category("Zip")]
		public void EmptyZipEntries()
		{
			MemoryStream ms = new MemoryStream();
			ZipOutputStream outStream = new ZipOutputStream(ms);
			for (int i = 0; i < 10; ++i) {
				outStream.PutNextEntry(new ZipEntry(i.ToString()));
			}
			outStream.Finish();
			
			ms.Seek(0, SeekOrigin.Begin);
			
			ZipInputStream inStream = new ZipInputStream(ms);
			
			int    extractCount  = 0;
			ZipEntry entry;
			byte[] decompressedData = new byte[100];
			while ((entry = inStream.GetNextEntry()) != null) {
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
		public void EmptyZip()
		{
			MemoryStream ms = new MemoryStream();
			ZipOutputStream outStream = new ZipOutputStream(ms);
			outStream.Finish();
			
			ms.Seek(0, SeekOrigin.Begin);
			
			ZipInputStream inStream = new ZipInputStream(ms);
			ZipEntry entry;
			while ((entry = inStream.GetNextEntry()) != null) {
				Assert.IsNull(entry, "No entries should be found in empty zip");
			}
		}

		/// <summary>
		/// Invalid passwords should be detected early if possible, seekable stream
		/// </summary>
		[Test]
		[Category("Zip")]
		[ExpectedException(typeof(ZipException))]
		public void InvalidPasswordSeekable()
		{
			byte[] originalData = null;
			byte[] compressedData = MakeMemZip(ref originalData, CompressionMethod.Deflated, 3, 500, "Hola", true);
			
			MemoryStream ms = new MemoryStream(compressedData);
			ms.Seek(0, SeekOrigin.Begin);
			
			byte[] buf2 = new byte[originalData.Length];
			int    pos  = 0;
			
			ZipInputStream inStream = new ZipInputStream(ms);
			inStream.Password = "redhead";
			
			ZipEntry entry2 = inStream.GetNextEntry();
			
			while (true) {
				int numRead = inStream.Read(buf2, pos, 4096);
				if (numRead <= 0) {
					break;
				}
				pos += numRead;
			}
		}
		
		/// <summary>
		/// Invalid passwords should be detected early if possible, non seekable stream
		/// </summary>
		[Test]
		[Category("Zip")]
		[ExpectedException(typeof(ZipException))]
		public void InvalidPasswordNonSeekable()
		{
			byte[] originalData = null;
			byte[] compressedData = MakeMemZip(ref originalData, CompressionMethod.Deflated, 3, 500, "Hola", false);
			
			MemoryStream ms = new MemoryStream(compressedData);
			ms.Seek(0, SeekOrigin.Begin);
			
			byte[] buf2 = new byte[originalData.Length];
			int    pos  = 0;
			
			ZipInputStream inStream = new ZipInputStream(ms);
			inStream.Password = "redhead";
			
			ZipEntry entry2 = inStream.GetNextEntry();
			
			while (true) {
				int numRead = inStream.Read(buf2, pos, 4096);
				if (numRead <= 0) {
					break;
				}
				pos += numRead;
			}
			
		}

		/// <summary>
		/// Setting entry comments to null should be allowed
		/// </summary>
		[Test]
		[Category("Zip")]
		public void NullEntryComment()
		{
			ZipEntry test = new ZipEntry("null");
			test.Comment = null;
		}
		
		/// <summary>
		/// Entries with null names arent allowed
		/// </summary>
		[Test]
		[Category("Zip")]
		[ExpectedException(typeof(ArgumentNullException))]
		public void NullEntryName()
		{
			string name = null;
			ZipEntry test = new ZipEntry(name);
		}
		
		/// <summary>
		/// Adding an entry after the stream has Finished should fail
		/// </summary>
		[Test]
		[Category("Zip")]
		[ExpectedException(typeof(InvalidOperationException))]
		public void AddEntryAfterFinish()
		{
			MemoryStream ms = new MemoryStream();
			ZipOutputStream s = new ZipOutputStream(ms);
			s.Finish();
			s.PutNextEntry(new ZipEntry("dummyfile.tst"));
		}
		
		/// <summary>
		/// Test setting file commment to a value that is too long
		/// </summary>
		[Test]
		[Category("Zip")]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void CommentTooLong()
		{
			MemoryStream ms = new MemoryStream();
			ZipOutputStream s = new ZipOutputStream(ms);
			s.SetComment(new String('A', 65536));			
		}
		
		/// <summary>
		/// Check that simply closing ZipOutputStream finishes the zip correctly
		/// </summary>
		[Test]
		[Category("Zip")]
		public void CloseOnlyHandled()
		{
			MemoryStream ms = new MemoryStream();
			ZipOutputStream s = new ZipOutputStream(ms);
			s.PutNextEntry(new ZipEntry("dummyfile.tst"));
			s.Close();
			
			Assert.IsTrue(s.IsFinished, "Output stream should be finished" );
		}

		/// <summary>
		/// Basic compress/decompress test, no encryption, size is important here as its big enough
		/// to force multiple write to output which was a problem...
		/// </summary>
		[Test]
		[Category("Zip")]
		public void BasicDeflated()
		{
			for (int i = 0; i <= 9; ++i) {
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
			for (int i = 0; i <= 9; ++i) {
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
		
		
		/// <summary>
		/// Basic compress/decompress test, with encryption, size is important here as its big enough
		/// to force multiple write to output which was a problem...
		/// </summary>
		[Test]
		[Category("Zip")]
		public void BasicDeflatedEncrypted()
		{
			for (int i = 0; i <= 9; ++i) {
				ExerciseZip(CompressionMethod.Deflated, i, 50000, "Rosebud", true);
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
			for (int i = 0; i <= 9; ++i) {
				ExerciseZip(CompressionMethod.Deflated, i, 50000, "Rosebud", false);
			}
		}
		
		[Test]
		[Category("Zip")]
		public void MixedEncryptedAndPlain()
		{
			byte[] compressedData = MakeMemZip(true, 
				new RuntimeInfo(CompressionMethod.Deflated, 2, 1, null, true),
				new RuntimeInfo(CompressionMethod.Deflated, 9, 1, "1234", false),
				new RuntimeInfo(CompressionMethod.Deflated, 2, 1, null, false),
				new RuntimeInfo(CompressionMethod.Deflated, 9, 1, "1234", true)
			);

			MemoryStream ms = new MemoryStream(compressedData);
			ZipInputStream inStream = new ZipInputStream(ms);
			inStream.Password = "1234";

			int  extractCount  = 0;
			ZipEntry entry;
			byte[] decompressedData = new byte[100];
			while ((entry = inStream.GetNextEntry()) != null) {
				extractCount = 0;
				while (true) {
					int numRead = inStream.Read(decompressedData, extractCount, decompressedData.Length);
					if (numRead <= 0) {
						break;
					}
					extractCount += numRead;
				}
			}
			inStream.Close();
		}

		[Test]
		[Category("Zip")]
		public void ArchiveTesting()
		{
			byte[] originalData = null;
			byte[] compressedData = MakeMemZip(ref originalData, CompressionMethod.Deflated,
			                                   6, 1024, null, true);
			
			MemoryStream ms = new MemoryStream(compressedData);
			ms.Seek(0, SeekOrigin.Begin);
			
			ZipFile testFile = new ZipFile(ms);
			
			Assert.IsTrue(testFile.TestArchive(true), "Unexpected error in archive detected");

			byte[] corrupted = new byte[compressedData.Length];
			Array.Copy(compressedData, corrupted, compressedData.Length);

			corrupted[123] = (byte)(~corrupted[123] & 0xff);
			ms = new MemoryStream(corrupted);
			
			testFile = new ZipFile(ms);

			Assert.IsFalse(testFile.TestArchive(true), "Error in archive not detected");
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
			MemStreamWithoutSeek ms = new MemStreamWithoutSeek();
			
			ZipOutputStream outStream = new ZipOutputStream(ms);
			outStream.SetLevel(8);
			Assert.AreEqual(8, outStream.GetLevel(), "Compression level invalid");
			
			ZipEntry entry = new ZipEntry("1.tst");
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
		/// Extra data for separate entries should be unique to that entry
		/// </summary>
		[Test]
		[Category("Zip")]
		public void ExtraDataUnique()
		{
			ZipEntry a = new ZipEntry("Basil");
			byte[] extra = new byte[4];
			extra[0] = 27;
			a.ExtraData = extra;
			
			ZipEntry b = new ZipEntry(a);
			b.ExtraData[0] = 89;
			Assert.IsTrue(b.ExtraData[0] != a.ExtraData[0], "Extra data not unique" + b.ExtraData[0] + " " + a.ExtraData[0]);
		}
		
		/// <summary>
		/// Check that adding too many entries is detected and handled
		/// </summary>
		[Test]
		[Category("Zip")]
		[Category("LongRunning")]
		[ExpectedException(typeof(ZipException))]
		public void TooManyEntries()
		{
			const int target = 65537;
			MemoryStream ms = new MemoryStream();
			ZipOutputStream s = new ZipOutputStream(ms);
			for (int i = 0; i < target; ++i) {
				s.PutNextEntry(new ZipEntry("dummyfile.tst"));
			}
			s.Finish();
			ms.Seek(0, SeekOrigin.Begin);
			ZipFile zipFile = new ZipFile(ms);
			Assert.AreEqual(target, zipFile.Size, "Incorrect number of entries stored");
		}

		void MakeZipFile(string name, string[] names, int size, string comment)
		{
			using (FileStream fs = File.Create(name)) {
				ZipOutputStream zOut = new ZipOutputStream(fs);
				zOut.SetComment(comment);
				for (int i = 0; i < names.Length; ++i) {
					zOut.PutNextEntry(new ZipEntry(names[i]));
					AddKnownDataToEntry(zOut, size);	
				}
				zOut.Close();
				fs.Close();
			}
		}
		
		void MakeZipFile(string name, string entryNamePrefix, int entries, int size, string comment)
		{
			using (FileStream fs = File.Create(name)) {
				ZipOutputStream zOut = new ZipOutputStream(fs);
				zOut.SetComment(comment);
				for (int i = 0; i < entries; ++i) {
					zOut.PutNextEntry(new ZipEntry(entryNamePrefix + (i + 1).ToString()));
					AddKnownDataToEntry(zOut, size);	
				}
				zOut.Close();
				fs.Close();
			}
		}
		
		
		void CheckKnownEntry(Stream inStream, int expectedCount) 
		{
			byte[] buffer = new Byte[1024];
			int bytesRead;
			int total = 0;
			byte nextValue = 0;
			while ((bytesRead = inStream.Read(buffer, 0, buffer.Length)) > 0) {
				total += bytesRead;
				for (int i = 0; i < bytesRead; ++i) {
					Assert.AreEqual(nextValue, buffer[i], "Wrong value read from entry");
					nextValue = ScatterValue(nextValue);			
				}
			}
			Assert.AreEqual(expectedCount, total, "Wrong number of bytes read from entry");
		}
		
		/// <summary>
		/// Simple round trip test for ZipFile class
		/// </summary>
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void ZipFileRoundTrip()
		{
			string tempFile = null;
			try {
				 tempFile = Path.GetTempPath();
			}
         catch (SecurityException) {
			}
			
			Assert.IsNotNull(tempFile, "No permission to execute this test?");
			
			if (tempFile != null) {
				tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
				MakeZipFile(tempFile, "", 10, 1024, "");
				
				ZipFile zipFile = new ZipFile(tempFile);
				foreach (ZipEntry e in zipFile) {
					Stream instream = zipFile.GetInputStream(e);
					CheckKnownEntry(instream, 1024);
		 		}
				
				zipFile.Close();
				
				File.Delete(tempFile);
			}
		}

		/// <summary>
		/// Check that ZipFile finds entries when its got a long comment
		/// </summary>
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void ZipFileFindEntriesLongComment()
		{
			string tempFile = null;
			try	{
				 tempFile = Path.GetTempPath();
			} catch (SecurityException) {
			}
			
			Assert.IsNotNull(tempFile, "No permission to execute this test?");
			
			if (tempFile != null) {
				tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
				string longComment = new String('A', 65535);
				MakeZipFile(tempFile, "", 1, 1, longComment);
				
				ZipFile zipFile = new ZipFile(tempFile);
				foreach (ZipEntry e in zipFile) {
					Stream instream = zipFile.GetInputStream(e);
					CheckKnownEntry(instream, 1);
		 		}
				
				zipFile.Close();
				
				File.Delete(tempFile);
			}
			
		}
		
		/// <summary>
		/// Check that ZipFile class handles no entries in zip file
		/// </summary>
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void ZipFileHandlesNoEntries()
		{
			string tempFile = null;
			try {
				 tempFile = Path.GetTempPath();
			} catch (SecurityException) {
			}
			
			Assert.IsNotNull(tempFile, "No permission to execute this test?");
			
			if (tempFile != null) {
				tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
				MakeZipFile(tempFile, "", 0, 1, "Aha");
				
				ZipFile zipFile = new ZipFile(tempFile);
				zipFile.Close();
				File.Delete(tempFile);
			}
			
		}
		
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void PartialStreamClosing()
		{
			string tempFile = null;
			try
			{
				 tempFile = Path.GetTempPath();
			}
			catch (SecurityException)
			{
			}
			
			Assert.IsNotNull(tempFile, "No permission to execute this test?");
			
			if (tempFile != null) {
				tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
				MakeZipFile(tempFile, new String[] {"Farriera", "Champagne", "Urban myth" }, 10, "Aha");
				
				ZipFile zipFile = new ZipFile(tempFile);
				
				Stream stream = zipFile.GetInputStream(0);
				stream.Close();
				stream = zipFile.GetInputStream(1);
				zipFile.Close();
				File.Delete(tempFile);
			}
		}
		
		/// <summary>
		/// Test ZipFile find method operation
		/// </summary>
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void ZipFileFind()
		{
			string tempFile = null;
			try
			{
				 tempFile = Path.GetTempPath();
			}
			catch (SecurityException)
			{
			}
			
			Assert.IsNotNull(tempFile, "No permission to execute this test?");
			
			if (tempFile != null) {
				tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
				MakeZipFile(tempFile, new String[] {"Farriera", "Champagne", "Urban myth" }, 10, "Aha");
				
				ZipFile zipFile = new ZipFile(tempFile);
				Assert.AreEqual(3, zipFile.Size, "Expected 1 entry");
				
				int testIndex = zipFile.FindEntry("Farriera", false);
				Assert.AreEqual(0, testIndex, "Case sensitive find failure");
				Assert.IsTrue(string.Compare(zipFile[testIndex].Name, "Farriera", false) == 0);
				
				testIndex = zipFile.FindEntry("Farriera", true);
				Assert.AreEqual(0, testIndex, "Case insensitive find failure");
				Assert.IsTrue(string.Compare(zipFile[testIndex].Name, "Farriera", true) == 0);
				
				testIndex = zipFile.FindEntry("urban mYTH", false);
				Assert.AreEqual(-1, testIndex, "Case sensitive find failure");
				
				testIndex = zipFile.FindEntry("urban mYTH", true);
				Assert.AreEqual(2, testIndex, "Case insensitive find failure");
				Assert.IsTrue(string.Compare(zipFile[testIndex].Name, "urban mYTH", true) == 0);
				
				testIndex = zipFile.FindEntry("Champane.", false);
				Assert.AreEqual(-1, testIndex, "Case sensitive find failure");
				
				testIndex = zipFile.FindEntry("Champane.", true);
				Assert.AreEqual(-1, testIndex, "Case insensitive find failure");
				
				zipFile.Close();
				File.Delete(tempFile);
			}
		}
		
		
		/// <summary>
		/// Test ZipEntry static file name cleaning methods
		/// </summary>
		[Test]
		[Category("Zip")]
		public void FilenameCleaning()
		{
			Assert.IsTrue(string.Compare(ZipEntry.CleanName("hello"), "hello") == 0);
			Assert.IsTrue(string.Compare(ZipEntry.CleanName(@"z:\eccles"), "eccles") == 0);
			Assert.IsTrue(string.Compare(ZipEntry.CleanName(@"\\server\share\eccles"), "eccles") == 0);
			Assert.IsTrue(string.Compare(ZipEntry.CleanName(@"\\server\share\dir\eccles"), "dir/eccles") == 0);
			Assert.IsTrue(string.Compare(ZipEntry.CleanName(@"\\server\share\eccles", false), "/eccles") == 0);
			Assert.IsTrue(string.Compare(ZipEntry.CleanName(@"c:\a\b\c\deus.dat", false), "/a/b/c/deus.dat") == 0);
		}

      /// <summary>
      /// Test for handling of zero lengths in compression using a formatter which
      /// will request reads of zero length...
      /// </summary>
      [Test]
      public void ZeroLength()
      {
         object data = new byte[0];
         byte[] zipped = ZipZeroLength(data);
         Console.WriteLine("Zipped size {0}", zipped.Length);
         object o = UnZipZeroLength(zipped);
      }
	
      byte[] ZipZeroLength(object data)
      {
         BinaryFormatter formatter = new BinaryFormatter();
         MemoryStream memStream = new MemoryStream();

         ZipOutputStream zipStream = new ZipOutputStream(memStream);
         zipStream.PutNextEntry(new ZipEntry("data"));
         formatter.Serialize(zipStream, data);
         zipStream.CloseEntry();
         zipStream.Close();
         byte[] resp = memStream.ToArray();
         memStream.Close();
		
         return resp;
      }
	
      object UnZipZeroLength(byte[] zipped)
      {
         if (zipped == null)
            return null;
			
         object ret = null;
         BinaryFormatter formatter = new BinaryFormatter();
         MemoryStream memStream = new MemoryStream(zipped);
         ZipInputStream zipStream = new ZipInputStream(memStream);
         ZipEntry zipEntry = zipStream.GetNextEntry();
         if (zipEntry != null)
            ret = formatter.Deserialize(zipStream);
         zipStream.Close();
         memStream.Close();
		
         return ret;
      }
	}
}
