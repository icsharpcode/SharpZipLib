using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Text;

using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Core;

using NUnit.Framework;

namespace ICSharpCode.SharpZipLib.Tests.Zip
{
	#region Support Classes
	class MemoryStreamWithoutSeek : MemoryStream
	{
		public override bool CanSeek 
		{
			get 
			{
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


		public RuntimeInfo(string password, bool isDirectory)
		{
			this.method = CompressionMethod.Stored;
			this.compressionLevel = 1;
			this.password = password;
			this.size = 0;
			this.random = false;
			isDirectory_ = isDirectory;
			original = new byte[0];
		}


		public byte[] Original
		{
			get { return original; }
		}

		public CompressionMethod Method
		{
			get { return method; }
		}

		public int CompressionLevel
		{
			get { return compressionLevel; }
		}

		public int Size
		{
			get { return size; }
		}

		public string Password
		{
			get { return password; }
		}

		bool Random
		{
			get { return random; }
		}

		public long Crc
		{
			get { return crc; }
		}

		public bool IsDirectory
		{
			get { return isDirectory_; }
		}

		#region Instance Fields
		byte[] original;
		CompressionMethod method;
		int compressionLevel;
		int size;
		string password;
		bool random;
		bool isDirectory_;
		long crc = -1;
		#endregion
	}

	class MemoryDataSource : IStaticDataSource
	{
		public MemoryDataSource(byte[] data)
		{
			data_ = data;
		}

		#region IDataSource Members

		public Stream GetSource()
		{
			return new MemoryStream(data_);
		}

		#endregion

		#region Instance Fields
		byte[] data_;
		#endregion
	}

	class StringMemoryDataSource : MemoryDataSource
	{
		public StringMemoryDataSource(string data)
			: base(Encoding.ASCII.GetBytes(data))
		{
		}
	}
	#endregion

	#region ZipBase
	public class ZipBase
	{
		static protected string GetTempFilePath()
		{
			string result = null;
			try
			{
				result = Path.GetTempPath();
			}
			catch ( SecurityException )
			{
			}
			return result;
		}

		protected byte[] MakeInMemoryZip(bool withSeek, params object[] createSpecs)
		{
			MemoryStream ms;
			
			if (withSeek == true) 
			{
				ms = new MemoryStream();
			} 
			else 
			{
				ms = new MemoryStreamWithoutSeek();
			}
			
			using (ZipOutputStream outStream = new ZipOutputStream(ms))
			{
				for ( int counter = 0; counter < createSpecs.Length; ++counter )
				{
					RuntimeInfo info = createSpecs[counter] as RuntimeInfo;
					outStream.Password = info.Password;

					if (info.Method != CompressionMethod.Stored)
						outStream.SetLevel(info.CompressionLevel); // 0 - store only to 9 - means best compression

					string entryName;

					if ( info.IsDirectory )
					{
						entryName = "dir" + counter + "/";
					}
					else
					{
						entryName = "entry" + counter + ".tst";
					}

					ZipEntry entry = new ZipEntry(entryName);
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
			}
			return ms.ToArray();
		}
		
		protected byte[] MakeInMemoryZip(ref byte[] original, CompressionMethod method,
			int compressionLevel, int size, string password, bool withSeek)
		{
			MemoryStream ms;
			
			if (withSeek == true) 
			{
				ms = new MemoryStream();
			} 
			else 
			{
				ms = new MemoryStreamWithoutSeek();
			}
			
			using (ZipOutputStream outStream = new ZipOutputStream(ms))
			{
				outStream.Password = password;
			
				if (method != CompressionMethod.Stored)
				{
					outStream.SetLevel(compressionLevel); // 0 - store only to 9 - means best compression
				}
			
				ZipEntry entry = new ZipEntry("dummyfile.tst");
				entry.CompressionMethod = method;
			
				outStream.PutNextEntry(entry);
			
				if (size > 0) 
				{
					original = new byte[size];
					System.Random rnd = new Random();
					rnd.NextBytes(original);
			
					outStream.Write(original, 0, original.Length);
				}
			}
			return ms.ToArray();
		}
		
		protected static void MakeTempFile(string name, int size)
		{
			using (FileStream fs = File.Create(name)) 
			{
				byte[] buffer = new byte[4096];
				while ( size > 0 )
				{
					fs.Write(buffer, 0, Math.Min(size, buffer.Length));
					size -= buffer.Length;
				}
			}
		}

		static protected byte ScatterValue(byte rhs)
		{
			return (byte) ((rhs * 253 + 7) & 0xff);
		}
		

		static void AddKnownDataToEntry(ZipOutputStream zipStream, int size)
		{
			if (size > 0) 
			{
				byte nextValue = 0;
				int bufferSize = Math.Min(size, 65536);
				byte [] data = new byte[bufferSize];
				int currentIndex = 0;
				for (int i = 0; i < size; ++i) 
				{

					data[currentIndex] = nextValue;
					nextValue = ScatterValue(nextValue);			

					currentIndex += 1;
					if ( (currentIndex >= data.Length) || (i + 1 == size) )
					{
						zipStream.Write(data, 0, currentIndex);
						currentIndex = 0;
					}
				}
			}
		}


		#region MakeZipFile Names
		protected void MakeZipFile(Stream storage, bool isOwner, string[] names, int size, string comment)
		{
			using ( ZipOutputStream zOut = new ZipOutputStream(storage) )
			{
				zOut.IsStreamOwner = isOwner;
				zOut.SetComment(comment);
				for (int i = 0; i < names.Length; ++i) 
				{
					zOut.PutNextEntry(new ZipEntry(names[i]));
					AddKnownDataToEntry(zOut, size);	
				}
				zOut.Close();
			}
		}

		protected void MakeZipFile(string name, string[] names, int size, string comment)
		{
			using (FileStream fs = File.Create(name)) 
			{
				using ( ZipOutputStream zOut = new ZipOutputStream(fs) )
				{
					zOut.SetComment(comment);
					for (int i = 0; i < names.Length; ++i) 
					{
						zOut.PutNextEntry(new ZipEntry(names[i]));
						AddKnownDataToEntry(zOut, size);	
					}
					zOut.Close();
				}
				fs.Close();
			}
		}
		#endregion
		#region MakeZipFile Entries
		protected void MakeZipFile(string name, string entryNamePrefix, int entries, int size, string comment)
		{
			using (FileStream fs = File.Create(name)) 
			using (ZipOutputStream zOut = new ZipOutputStream(fs))
			{
				zOut.SetComment(comment);
				for (int i = 0; i < entries; ++i) 
				{
					zOut.PutNextEntry(new ZipEntry(entryNamePrefix + (i + 1).ToString()));
					AddKnownDataToEntry(zOut, size);	
				}
			}
		}

		protected void MakeZipFile(Stream storage, bool isOwner,
			string entryNamePrefix, int entries, int size, string comment)
		{
			using ( ZipOutputStream zOut = new ZipOutputStream(storage) )
			{
				zOut.IsStreamOwner = isOwner;
				zOut.SetComment(comment);
				for (int i = 0; i < entries; ++i) 
				{
					zOut.PutNextEntry(new ZipEntry(entryNamePrefix + (i + 1).ToString()));
					AddKnownDataToEntry(zOut, size);	
				}
			}
		}


		#endregion

		protected static void CheckKnownEntry(Stream inStream, int expectedCount) 
		{
			byte[] buffer = new byte[1024];

			int bytesRead;
			int total = 0;
			byte nextValue = 0;
			while ((bytesRead = inStream.Read(buffer, 0, buffer.Length)) > 0) 
			{
				total += bytesRead;
				for (int i = 0; i < bytesRead; ++i) 
				{
					Assert.AreEqual(nextValue, buffer[i], "Wrong value read from entry");
					nextValue = ScatterValue(nextValue);			
				}
			}
			Assert.AreEqual(expectedCount, total, "Wrong number of bytes read from entry");
		}
		
		protected byte ReadByteChecked(Stream stream)
		{
			int rawValue = stream.ReadByte();
			Assert.IsTrue(rawValue >= 0);
			return (byte)rawValue;
		}

		protected int ReadInt(Stream stream)
		{
			return ReadByteChecked(stream) | 
				(ReadByteChecked(stream) << 8) |
				(ReadByteChecked(stream) << 16) |
				(ReadByteChecked(stream) << 24);
		}

		protected long ReadLong(Stream stream)
		{
			long result = ReadInt(stream) & 0xffffffff;
			return result | (((long)ReadInt(stream)) << 32);
		}

	}

	#endregion

	class TestHelper
	{
		static public void SaveMemoryStream(MemoryStream ms, string fileName)
		{
			byte[] data = ms.ToArray();
			using ( FileStream fs = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read) ) {
				fs.Write(data, 0, data.Length);
			}
		}
	}
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
			}
			catch
			{
				exception = true;
			}
			Assert.IsTrue(exception, "Read should fail");
		}
		
		[Test]
		public void ParameterHandling()
		{
			byte[] buffer = new byte[10];
			byte[] emptyBuffer = new byte[0];
			
			MemoryStream ms = new MemoryStream();
			ZipOutputStream outStream = new ZipOutputStream(ms);
			outStream.IsStreamOwner = false;
			outStream.PutNextEntry(new ZipEntry("Floyd"));
			outStream.Write(buffer, 0, 10);
			outStream.Finish();
			
			ms.Seek(0, SeekOrigin.Begin);
			
			ZipInputStream inStream = new ZipInputStream(ms);
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
		public void Zip64Descriptor()
		{
			MemoryStream msw = new MemoryStreamWithoutSeek();
			ZipOutputStream outStream = new ZipOutputStream(msw);

			outStream.IsStreamOwner = false;
			outStream.PutNextEntry(new ZipEntry("StripedMarlin"));
			outStream.WriteByte(89);
			outStream.Close();

			MemoryStream ms = new MemoryStream(msw.ToArray());
			using (ZipFile zf = new ZipFile(ms))
			{
				Assert.IsTrue(zf.TestArchive(true));
			}
		}
	}
	
	/// <summary>
	/// This class contains test cases for Zip compression and decompression
	/// </summary>
	[TestFixture]
	public class GeneralHandling : ZipBase
	{
		void AddRandomDataToEntry(ZipOutputStream zipStream, int size)
		{
			if (size > 0) 
			{
				byte [] data = new byte[size];
				System.Random rnd = new Random();
				rnd.NextBytes(data);
			
				zipStream.Write(data, 0, data.Length);
			}
		}

		void ExerciseZip(CompressionMethod method, int compressionLevel,
			int size, string password, bool canSeek)
		{
			byte[] originalData = null;
			byte[] compressedData = MakeInMemoryZip(ref originalData, method, compressionLevel, size, password, canSeek);
			
			MemoryStream ms = new MemoryStream(compressedData);
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

				int    currentIndex  = 0;
			
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
		
				Assert.AreEqual(currentIndex, size, "Original and decompressed data different sizes" );
			
				if (originalData != null) 
				{
					for (int i = 0; i < originalData.Length; ++i) 
					{
						Assert.AreEqual(decompressedData[i], originalData[i], "Decompressed data doesnt match original, compression level: " + compressionLevel);
					}
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
			for (int i = 0; i < 10; ++i) 
			{
				outStream.PutNextEntry(new ZipEntry(i.ToString()));
			}
			outStream.Finish();
			
			ms.Seek(0, SeekOrigin.Begin);
			
			ZipInputStream inStream = new ZipInputStream(ms);
			
			int extractCount  = 0;
			ZipEntry entry;
			byte[] decompressedData = new byte[100];

			while ((entry = inStream.GetNextEntry()) != null) 
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
			Assert.AreEqual(extractCount, 0, "No data should be read from empty entries");
		}

		string DescribeAttributes ( FieldAttributes attributes )
		{
			string att = string.Empty;
			if ( (FieldAttributes.Public & attributes) != 0 )
			{
				att = att + "Public,";
			}
				
			if ( (FieldAttributes.Static & attributes) != 0 )
			{
				att = att + "Static,";
			}

			if ( (FieldAttributes.Literal & attributes) != 0 )
			{
				att = att + "Literal,";
			}

			if ( (FieldAttributes.HasDefault & attributes) != 0 )
			{
				att = att + "HasDefault,";
			}

			if ( (FieldAttributes.InitOnly & attributes) != 0 )
			{
				att = att + "InitOnly,";
			}

			if ( (FieldAttributes.Assembly & attributes) != 0 )
			{
				att = att + "Assembly,";
			}

			if ( (FieldAttributes.FamANDAssem & attributes) != 0 )
			{
				att = att + "FamANDAssembly,";
			}

			if ( (FieldAttributes.FamORAssem & attributes) != 0 )
			{
				att = att + "FamORAssembly,";
			}

			if ( (FieldAttributes.HasFieldMarshal & attributes) != 0 )
			{
				att = att + "HasFieldMarshal,";
			}

			return att;
		}

		[Test]
		[Category("Zip")]
		[ExpectedException(typeof(NotSupportedException))]
		public void UnsupportedCompressionMethod()
		{
			ZipEntry ze = new ZipEntry("HumblePie");
			System.Type type = typeof(CompressionMethod);
			//			System.Reflection.FieldInfo[] info = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			System.Reflection.FieldInfo[] info = type.GetFields();

			CompressionMethod aValue = CompressionMethod.Deflated;
			for (int i = 0; i < info.Length; i++)
			{
				System.Reflection.FieldAttributes attributes = info[i].Attributes;
				DescribeAttributes(attributes);
				if ( (FieldAttributes.Static & attributes ) != 0 )
				{
					object obj = info[i].GetValue(null);
					string bb = obj.ToString();
					if ( bb == null )
					{
						throw new Exception();
					}
				}
				string x = string.Format("The value of {0} is: {1}",
					info[i].Name, info[i].GetValue(aValue));
			}

			ze.CompressionMethod = CompressionMethod.BZip2;
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
			while ((entry = inStream.GetNextEntry()) != null) 
			{
				Assert.Fail("No entries should be found in empty zip");
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
			byte[] compressedData = MakeInMemoryZip(ref originalData, CompressionMethod.Deflated, 3, 500, "Hola", true);
			
			MemoryStream ms = new MemoryStream(compressedData);
			ms.Seek(0, SeekOrigin.Begin);
			
			byte[] buf2 = new byte[originalData.Length];
			int    pos  = 0;
			
			ZipInputStream inStream = new ZipInputStream(ms);
			inStream.Password = "redhead";
			
			ZipEntry entry2 = inStream.GetNextEntry();
			
			while (true) 
			{
				int numRead = inStream.Read(buf2, pos, buf2.Length);
				if (numRead <= 0) 
				{
					break;
				}
				pos += numRead;
			}
		}
		
		/// <summary>
		/// Check that GetNextEntry can handle the situation where part of the entry data has been read
		/// before the call is made.  ZipInputStream.CloseEntry wasnt handling this at all.
		/// </summary>
		[Test]
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
			
			MemoryStream ms = new MemoryStream(compressedData);
			ms.Seek(0, SeekOrigin.Begin);
			
			using (ZipInputStream inStream = new ZipInputStream(ms))
			{
				byte[] buffer = new byte[10];

				ZipEntry entry;
				while ( (entry = inStream.GetNextEntry()) != null )
				{
					// Read a portion of the data, so GetNextEntry has some work to do.
					inStream.Read(buffer, 0, 10);
				}
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
			byte[] compressedData = MakeInMemoryZip(ref originalData, CompressionMethod.Deflated, 3, 500, "Hola", false);
			
			MemoryStream ms = new MemoryStream(compressedData);
			ms.Seek(0, SeekOrigin.Begin);
			
			byte[] buf2 = new byte[originalData.Length];
			int    pos  = 0;
			
			ZipInputStream inStream = new ZipInputStream(ms);
			inStream.Password = "redhead";
			
			ZipEntry entry2 = inStream.GetNextEntry();
			
			while (true) 
			{
				int numRead = inStream.Read(buf2, pos, buf2.Length);
				if (numRead <= 0) 
				{
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
		public void SetCommentOversize()
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
		
		/// <summary>
		/// Basic compress/decompress test, with encryption, size is important here as its big enough
		/// to force multiple write to output which was a problem...
		/// </summary>
		[Test]
		[Category("Zip")]
		public void BasicDeflatedEncrypted()
		{
			for (int i = 0; i <= 9; ++i) 
			{
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
			for (int i = 0; i <= 9; ++i) 
			{
				ExerciseZip(CompressionMethod.Deflated, i, 50000, "Rosebud", false);
			}
		}
		
		[Test]
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

			MemoryStream ms = new MemoryStream(compressedData);
			ZipInputStream inStream = new ZipInputStream(ms);

			ZipEntry entry;
			while ((entry = inStream.GetNextEntry()) != null) 
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

			MemoryStream ms = new MemoryStream(compressedData);
			using (ZipInputStream inStream = new ZipInputStream(ms))
			{
				inStream.Password = "1234";

				int extractCount  = 0;
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
			MemoryStreamWithoutSeek ms = new MemoryStreamWithoutSeek();
			
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
		/// Check that adding more than the 2.0 limit for entry numbers is detected and handled
		/// </summary>
		[Test]
		[Category("Zip")]
		[Category("LongRunning")]
		public void Stream_64KPlusOneEntries()
		{
			const int target = 65537;
			MemoryStream ms = new MemoryStream();
			using (ZipOutputStream s = new ZipOutputStream(ms))
			{

				for (int i = 0; i < target; ++i) 
				{
					s.PutNextEntry(new ZipEntry("dummyfile.tst"));
				}

				s.Finish();
				ms.Seek(0, SeekOrigin.Begin);
				using ( ZipFile zipFile = new ZipFile(ms) )
				{
					Assert.AreEqual(target, zipFile.Count, "Incorrect number of entries stored");
				}
			}
		}

		/// <summary>
		/// Check that Unicode filename support works.
		/// </summary>
		[Test]
		[Category("Zip")]
		public void Stream_UnicodeEntries()
		{
			MemoryStream ms = new MemoryStream();
			using (ZipOutputStream s = new ZipOutputStream(ms))
			{
				s.IsStreamOwner = false;

				string sampleName = "\u03A5\u03d5\u03a3";
				ZipEntry sample = new ZipEntry(sampleName);
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
				MakeZipFile(tempFile, new String[] {"Farriera", "Champagne", "Urban myth" }, 10, "Aha");
				
				using ( ZipFile zipFile = new ZipFile(tempFile) )
				{
				
					Stream stream = zipFile.GetInputStream(0);
					stream.Close();

					stream = zipFile.GetInputStream(1);
					zipFile.Close();
				}
				File.Delete(tempFile);
			}
		}

		void TestLargeZip(string tempFile, int targetFiles)
		{
			const int blockSize = 4096;

			byte [] data = new byte[blockSize];
			byte nextValue = 0;
			for (int i = 0; i < blockSize; ++i)
			{
				nextValue = ScatterValue(nextValue);			
				data[i] = nextValue;
			}

			using ( ZipFile zFile = new ZipFile(tempFile) )
			{
				Assert.AreEqual(targetFiles, zFile.Count);
				byte [] readData = new byte[blockSize];
				int readIndex;
				foreach (ZipEntry ze in zFile)
				{
					Stream s = zFile.GetInputStream(ze);
					readIndex = 0;
					while ( readIndex < readData.Length )
					{
						readIndex += s.Read(readData, readIndex, data.Length - readIndex);
					}
					for ( int ii = 0; ii < blockSize; ++ii)
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
			
			if (tempFile != null) 
			{
				const int blockSize = 4096;

				byte [] data = new byte[blockSize];
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
						ZipOutputStream zOut = new ZipOutputStream(fs);
						zOut.SetLevel(4);
						const int TargetFiles = 8100;
						for (int i = 0; i < TargetFiles; ++i) 
						{
							ZipEntry e = new ZipEntry(i.ToString());
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

			using ( ZipOutputStream zipStream = new ZipOutputStream(memStream) )
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
	
		object UnZipZeroLength(byte[] zipped)
		{
			if (zipped == null)
			{
				return null;
			}
			
			object result = null;
			BinaryFormatter formatter = new BinaryFormatter();
			MemoryStream memStream = new MemoryStream(zipped);
			using ( ZipInputStream zipStream = new ZipInputStream(memStream) )
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

		[Test]
		[Category("Zip")]
		public void NameTransforms()
		{
			INameTransform t = new ZipNameTransform(@"C:\Slippery");
			Assert.AreEqual("Pongo/Directory/", t.TransformDirectory(@"C:\Slippery\Pongo\Directory"), "Value should be trimmed and converted");
			Assert.AreEqual("PoNgo/Directory/", t.TransformDirectory(@"c:\slipperY\PoNgo\Directory"), "Trimming should be case insensitive");
			Assert.AreEqual("slippery/Pongo/Directory/", t.TransformDirectory(@"d:\slippery\Pongo\Directory"), "Trimming should be case insensitive");
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
		}

		void CheckNameConversion(string toCheck)
		{
			byte[] intermediate = ZipConstants.ConvertToArray(toCheck);
			string final = ZipConstants.ConvertToString(intermediate);
		
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
			ZipConstants.DefaultCodePage = 850;
			string sample = "Hello world";

			byte[] rawData = Encoding.ASCII.GetBytes(sample);

			string converted = ZipConstants.ConvertToStringExt(0, rawData);
			Assert.AreEqual(sample, converted);

			converted = ZipConstants.ConvertToStringExt((int)GeneralBitFlags.UnicodeText, rawData);
			Assert.AreEqual(sample, converted);

			// This time use some greek characters
			sample = "\u03A5\u03d5\u03a3";
			rawData = Encoding.UTF8.GetBytes(sample);

			converted = ZipConstants.ConvertToStringExt((int)GeneralBitFlags.UnicodeText, rawData);
			Assert.AreEqual(sample, converted);
		}

		/// <summary>
		/// Regression test for problem where the password check would fail for an archive whose
		/// date was updated from the extra data.
		/// This applies to archives where the crc wasnt know at the time of encryption.
		/// The date of the entry is used in its place.
		/// </summary>
		[ Test ]
		public void PasswordCheckingWithDateInExtraData()
		{
			MemoryStream ms = new MemoryStream();
			DateTime checkTime = new DateTime(2010, 10, 16, 0, 3, 28);

			using ( ZipOutputStream zos = new ZipOutputStream(ms))
			{
				zos.IsStreamOwner = false;
				zos.Password = "secret";
				ZipEntry ze = new ZipEntry("uno");
				ze.DateTime = new DateTime(1998, 6, 5, 4, 3, 2);

				ZipExtraData zed = new ZipExtraData();

				zed.StartNewEntry();

				zed.AddData(1);

				TimeSpan delta = checkTime.ToUniversalTime() - new System.DateTime ( 1970, 1, 1, 0, 0, 0 ).ToUniversalTime();
				int seconds = (int)delta.TotalSeconds;
				zed.AddLeInt(seconds);
				zed.AddNewEntry(0x5455);

				ze.ExtraData = zed.GetEntryData();
				zos.PutNextEntry(ze);
				zos.WriteByte(54);
			}

			ms.Position = 0;
			using (ZipInputStream zis = new ZipInputStream(ms) )
			{
				zis.Password = "secret";
				ZipEntry uno = zis.GetNextEntry();
				byte theByte = (byte)zis.ReadByte();
				Assert.AreEqual(54, theByte);
				Assert.AreEqual(-1, zis.ReadByte());
				Assert.AreEqual(checkTime, uno.DateTime);
			}
		}
	}

	[TestFixture]
	public class ZipExtraDataHandling : ZipBase
	{
		/// <summary>
		/// Extra data for separate entries should be unique to that entry
		/// </summary>
		[Test]
		[Category("Zip")]
		public void IsDataUnique()
		{
			ZipEntry a = new ZipEntry("Basil");
			byte[] extra = new byte[4];
			extra[0] = 27;
			a.ExtraData = extra;
			
			ZipEntry b = (ZipEntry)a.Clone();
			b.ExtraData[0] = 89;
			Assert.IsTrue(b.ExtraData[0] != a.ExtraData[0], "Extra data not unique " + b.ExtraData[0] + " " + a.ExtraData[0]);

			ZipEntry c = (ZipEntry)a.Clone();
			c.ExtraData[0] = 45;
			Assert.IsTrue(a.ExtraData[0] != c.ExtraData[0], "Extra data not unique " + a.ExtraData[0] + " " + c.ExtraData[0]);
		}
		[Test]
		[Category("Zip")]
		public void BasicOperations()
		{
			ZipExtraData zed = new ZipExtraData(null);
			Assert.AreEqual(0, zed.Length);

			zed = new ZipExtraData(new byte[] { 1, 0, 0, 0 });
			Assert.AreEqual(4, zed.Length, "A length should be 4");

			ZipExtraData zed2 = new ZipExtraData();
			zed2.AddEntry(1, new byte[] { });

			byte[] data = zed.GetEntryData();
			for ( int i = 0; i < data.Length; ++i )
			{
				Assert.AreEqual(zed2.GetEntryData()[i], data[i]);
			}
			
			Assert.AreEqual(4, zed2.Length, "A1 length should be 4");

			bool findResult =  zed.Find(2);
			Assert.IsFalse(findResult, "A - Shouldnt find tag 2");

			findResult = zed.Find(1);
			Assert.IsTrue(findResult, "A - Should find tag 1");
			Assert.AreEqual(0, zed.ValueLength, "A- Length of entry should be 0");
			Assert.AreEqual(-1, zed.ReadByte());
			Assert.AreEqual(0, zed.GetStreamForTag(1).Length, "A - Length of stream should be 0");

			zed = new ZipExtraData(new byte[] { 1, 0, 3, 0, 1, 2, 3 });
			Assert.AreEqual(7, zed.Length, "Expected a length of 7");

			findResult = zed.Find(1);
			Assert.IsTrue(findResult, "B - Should find tag 1");
			Assert.AreEqual(3, zed.ValueLength, "B - Length of entry should be 3");
			for ( int i = 1; i <= 3; ++i )
			{
				Assert.AreEqual(i, zed.ReadByte());
			}
			Assert.AreEqual(-1, zed.ReadByte());

			Stream s = zed.GetStreamForTag(1);
			Assert.AreEqual(3, s.Length, "B.1 Stream length should be 3");
			for ( int i = 1; i <= 3; ++i )
			{
				Assert.AreEqual(i, s.ReadByte());
			}
			Assert.AreEqual(-1, s.ReadByte());

			zed = new ZipExtraData(new byte[] { 1, 0, 3, 0, 1, 2, 3, 2, 0, 1, 0, 56 });
			Assert.AreEqual(12, zed.Length, "Expected a length of 12");

			findResult = zed.Find(1);
			Assert.IsTrue(findResult, "C.1 - Should find tag 1");
			Assert.AreEqual(3, zed.ValueLength, "C.1 - Length of entry should be 3");
			for ( int i = 1; i <= 3; ++i )
			{
				Assert.AreEqual(i, zed.ReadByte());
			}
			Assert.AreEqual(-1, zed.ReadByte());

			findResult = zed.Find(2);
			Assert.IsTrue(findResult, "C.2 - Should find tag 2");
			Assert.AreEqual(1, zed.ValueLength, "C.2 - Length of entry should be 1");
			Assert.AreEqual(56, zed.ReadByte());
			Assert.AreEqual(-1, zed.ReadByte());

			s = zed.GetStreamForTag(2);
			Assert.AreEqual(1, s.Length);
			Assert.AreEqual(56, s.ReadByte());
			Assert.AreEqual(-1, s.ReadByte());

			zed = new ZipExtraData();
			zed.AddEntry(7, new byte[] { 33, 44, 55 });
			findResult = zed.Find(7);
			Assert.IsTrue(findResult, "Add.1 should find new tag");
			Assert.AreEqual(3, zed.ValueLength, "Add.1 length should be 3");
			Assert.AreEqual(33, zed.ReadByte());
			Assert.AreEqual(44, zed.ReadByte());
			Assert.AreEqual(55, zed.ReadByte());
			Assert.AreEqual(-1, zed.ReadByte());

			zed.AddEntry(7, null);
			findResult = zed.Find(7);
			Assert.IsTrue(findResult, "Add.2 should find new tag");
			Assert.AreEqual(0, zed.ValueLength, "Add.2 length should be 0");

			zed.StartNewEntry();
			zed.AddData(0xae);
			zed.AddNewEntry(55);

			findResult = zed.Find(55);
			Assert.IsTrue(findResult, "Add.3 should find new tag");
			Assert.AreEqual(1, zed.ValueLength, "Add.3 length should be 1");
			Assert.AreEqual(0xae, zed.ReadByte());
			Assert.AreEqual(-1, zed.ReadByte());

			zed = new ZipExtraData();
			zed.StartNewEntry();
			zed.AddLeLong(0);
			zed.AddLeLong(-4);
			zed.AddLeLong(-1);
			zed.AddLeLong(long.MaxValue);
			zed.AddLeLong(long.MinValue);
			zed.AddLeLong(0x123456789ABCDEF0);
			zed.AddLeLong(unchecked((long)0xFEDCBA9876543210));
			zed.AddNewEntry(567);

			s = zed.GetStreamForTag(567);
			long longValue = ReadLong(s);
			Assert.AreEqual(longValue, zed.ReadLong(), "Read/stream mismatch");
			Assert.AreEqual(0, longValue, "Expected long value of zero");

			longValue = ReadLong(s);
			Assert.AreEqual(longValue, zed.ReadLong(), "Read/stream mismatch");
			Assert.AreEqual(-4, longValue, "Expected long value of -4");

			longValue = ReadLong(s);
			Assert.AreEqual(longValue, zed.ReadLong(), "Read/stream mismatch");
			Assert.AreEqual(-1, longValue, "Expected long value of -1");

			longValue = ReadLong(s);
			Assert.AreEqual(longValue, zed.ReadLong(), "Read/stream mismatch");
			Assert.AreEqual(long.MaxValue, longValue, "Expected long value of MaxValue");

			longValue = ReadLong(s);
			Assert.AreEqual(longValue, zed.ReadLong(), "Read/stream mismatch");
			Assert.AreEqual(long.MinValue, longValue, "Expected long value of MinValue");

			longValue = ReadLong(s);
			Assert.AreEqual(longValue, zed.ReadLong(), "Read/stream mismatch");
			Assert.AreEqual(0x123456789abcdef0, longValue, "Expected long value of MinValue");

			longValue = ReadLong(s);
			Assert.AreEqual(longValue, zed.ReadLong(), "Read/stream mismatch");
			Assert.AreEqual(unchecked((long)0xFEDCBA9876543210), longValue, "Expected long value of MinValue");
		}

		[Test]
		[Category("Zip")]
		public void UnreadCountValid()
		{
			ZipExtraData zed = new ZipExtraData(new byte[] { 1, 0, 0, 0 });
			Assert.AreEqual(4, zed.Length, "Length should be 4");
			Assert.IsTrue(zed.Find(1), "Should find tag 1");
			Assert.AreEqual(0, zed.UnreadCount);

			// seven bytes
			zed = new ZipExtraData(new byte[] { 1, 0, 7, 0, 1, 2, 3, 4, 5, 6, 7 });
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			for ( int i = 0; i < 7; ++i )
			{
				Assert.AreEqual(7 - i, zed.UnreadCount);
				zed.ReadByte();
			}

			zed.ReadByte();
			Assert.AreEqual(0, zed.UnreadCount);
		}

		[Test]
		[Category("Zip")]
		public void Skipping()
		{
			ZipExtraData zed = new ZipExtraData(new byte[] { 1, 0, 7, 0, 1, 2, 3, 4, 5, 6, 7 });
			Assert.AreEqual(11, zed.Length, "Length should be 11");
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			Assert.AreEqual(7, zed.UnreadCount);
			Assert.AreEqual(4, zed.CurrentReadIndex);

			zed.ReadByte();
			Assert.AreEqual(6, zed.UnreadCount);
			Assert.AreEqual(5, zed.CurrentReadIndex);

			zed.Skip(1);
			Assert.AreEqual(5, zed.UnreadCount);
			Assert.AreEqual(6, zed.CurrentReadIndex);

			zed.Skip(-1);
			Assert.AreEqual(6, zed.UnreadCount);
			Assert.AreEqual(5, zed.CurrentReadIndex);

			zed.Skip(6);
			Assert.AreEqual(0, zed.UnreadCount);
			Assert.AreEqual(11, zed.CurrentReadIndex);

			bool exceptionCaught = false;

			try
			{
				zed.Skip(1);
			}
			catch(ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Should fail to skip past end");

			Assert.AreEqual(0, zed.UnreadCount);
			Assert.AreEqual(11, zed.CurrentReadIndex);

			zed.Skip(-7);
			Assert.AreEqual(7, zed.UnreadCount);
			Assert.AreEqual(4, zed.CurrentReadIndex);

			try
			{
				zed.Skip(-1);
			}
			catch(ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Should fail to skip before beginning");

		}

		[Test]
		[Category("Zip")]
		public void ReadOverrunLong()
		{
			ZipExtraData zed = new ZipExtraData(new byte[] { 1, 0, 0, 0 });
			Assert.AreEqual(4, zed.Length, "Length should be 4");
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			// Empty Tag
			bool exceptionCaught = false;
			try
			{
				zed.ReadLong();
			}
			catch(ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Expected EOS exception");

			// seven bytes
			zed = new ZipExtraData(new byte[] { 1, 0, 7, 0, 1, 2, 3, 4, 5, 6, 7 });
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			exceptionCaught = false;
			try
			{
				zed.ReadLong();
			}
			catch(ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Expected EOS exception");

			zed = new ZipExtraData(new byte[] { 1, 0, 15, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 });
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			zed.ReadLong();

			exceptionCaught = false;
			try
			{
				zed.ReadLong();
			}
			catch(ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Expected EOS exception");
		}

		[Test]
		[Category("Zip")]
		public void ReadOverrunInt()
		{
			ZipExtraData zed = new ZipExtraData(new byte[] { 1, 0, 0, 0 });
			Assert.AreEqual(4, zed.Length, "Length should be 4");
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			// Empty Tag
			bool exceptionCaught = false;
			try
			{
				zed.ReadInt();
			}
			catch(ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Expected EOS exception");

			// three bytes
			zed = new ZipExtraData(new byte[] { 1, 0, 3, 0, 1, 2, 3 });
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			exceptionCaught = false;
			try
			{
				zed.ReadInt();
			}
			catch(ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Expected EOS exception");

			zed = new ZipExtraData(new byte[] { 1, 0, 7, 0, 1, 2, 3, 4, 5, 6, 7 });
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			zed.ReadInt();

			exceptionCaught = false;
			try
			{
				zed.ReadInt();
			}
			catch(ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Expected EOS exception");
		}

		[Test]
		[Category("Zip")]
		public void ReadOverrunShort()
		{
			ZipExtraData zed = new ZipExtraData(new byte[] { 1, 0, 0, 0 });
			Assert.AreEqual(4, zed.Length, "Length should be 4");
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			// Empty Tag
			bool exceptionCaught = false;
			try
			{
				zed.ReadShort();
			}
			catch(ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Expected EOS exception");

			// Single byte
			zed = new ZipExtraData(new byte[] { 1, 0, 1, 0, 1 });
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			exceptionCaught = false;
			try
			{
				zed.ReadShort();
			}
			catch(ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Expected EOS exception");

			zed = new ZipExtraData(new byte[] { 1, 0, 2, 0, 1, 2 });
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			zed.ReadShort();

			exceptionCaught = false;
			try
			{
				zed.ReadShort();
			}
			catch(ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Expected EOS exception");
		}
	}

	[TestFixture]
	public class FastZipHandling : ZipBase
	{
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void Basics()
		{
			const string tempName1 = "a.dat";

			MemoryStream target = new MemoryStream();

			string tempFilePath = GetTempFilePath();
			Assert.IsNotNull(tempFilePath, "No permission to execute this test?");

			string addFile = Path.Combine(tempFilePath, tempName1);
			MakeTempFile(addFile, 1);

			try
			{
				FastZip fastZip = new FastZip();
				fastZip.CreateZip(target, tempFilePath, false, @"a\.dat", null);

				MemoryStream archive = new MemoryStream(target.ToArray());
				using ( ZipFile zf = new ZipFile(archive) )
				{
					Assert.AreEqual(1, zf.Count);
					ZipEntry entry = zf[0];
					Assert.AreEqual(tempName1, entry.Name);
					Assert.AreEqual(1, entry.Size);
					Assert.IsTrue(zf.TestArchive(true));

					zf.Close();
				}
			}
			finally
			{
				File.Delete(tempName1);
			}
		}
	}

	[TestFixture]
	public class ZipFileHandling : ZipBase
	{
		/// <summary>
		/// Check that adding too many entries is detected and handled
		/// </summary>
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void Zip64Entries()
		{
			const int target = 65537;

			using ( ZipFile zipFile = ZipFile.Create(Path.GetTempFileName()) )
			{
				zipFile.BeginUpdate();

				for ( int i = 0; i < target; ++i )
				{
					ZipEntry ze = new ZipEntry(i.ToString());
					ze.CompressedSize = 0;
					ze.Size = 0;
					zipFile.Add(ze);
				}
				zipFile.CommitUpdate();

				Assert.IsTrue(zipFile.TestArchive(true));
				Assert.AreEqual(target, zipFile.Count, "Incorrect number of entries stored");
			}
		}

		void Compare(byte[] a, byte[] b)
		{
			Assert.AreEqual(a.Length, b.Length);
			for ( int i = 0; i < a.Length; ++i )
			{
				Assert.AreEqual(a[i], b[i]);
			}
		}

		[Test]
		[Category("Zip")]
		public void EmbeddedArchive()
		{
			MemoryStream memStream = new MemoryStream();
			using (ZipFile f = new ZipFile(memStream))
			{
				f.IsStreamOwner = false;

				StringMemoryDataSource m = new StringMemoryDataSource("0000000");
				f.BeginUpdate(new MemoryArchiveStorage());
				f.Add(m, "a.dat");
				f.Add(m, "b.dat");
				f.CommitUpdate();
				Assert.IsTrue(f.TestArchive(true));
			}

			byte[] rawArchive = memStream.ToArray();
			byte[] pseudoSfx = new byte[1049 + rawArchive.Length];
			Array.Copy(rawArchive, 0, pseudoSfx, 1049, rawArchive.Length);

			memStream = new MemoryStream(pseudoSfx);
			using (ZipFile f = new ZipFile(memStream))
			{
				for ( int index = 0; index < f.Count; ++index)
				{
					Stream entryStream = f.GetInputStream(index);
					MemoryStream data = new MemoryStream();
					StreamUtils.Copy(entryStream, data, new byte[128]);
					string contents = Encoding.ASCII.GetString(data.ToArray());
					Assert.AreEqual("0000000", contents);
				}
			}
		}

		[Test]
		[Category("Zip")]
		public void Zip64Useage()
		{
			MemoryStream memStream = new MemoryStream();
			using (ZipFile f = new ZipFile(memStream))
			{
				f.IsStreamOwner = false;
				f.UseZip64 = UseZip64.On;
				
				StringMemoryDataSource m = new StringMemoryDataSource("0000000");
				f.BeginUpdate(new MemoryArchiveStorage());
				f.Add(m, "a.dat");
				f.Add(m, "b.dat");
				f.CommitUpdate();
				Assert.IsTrue(f.TestArchive(true));
			}

			byte[] rawArchive = memStream.ToArray();
			byte[] pseudoSfx = new byte[1049 + rawArchive.Length];
			Array.Copy(rawArchive, 0, pseudoSfx, 1049, rawArchive.Length);

			memStream = new MemoryStream(pseudoSfx);
			using (ZipFile f = new ZipFile(memStream))
			{
				for ( int index = 0; index < f.Count; ++index)
				{
					Stream entryStream = f.GetInputStream(index);
					MemoryStream data = new MemoryStream();
					StreamUtils.Copy(entryStream, data, new byte[128]);
					string contents = Encoding.ASCII.GetString(data.ToArray());
					Assert.AreEqual("0000000", contents);
				}
			}
		}


		[Test]
		public void BasicEncryption()
		{
			const string TestValue = "0001000";
			MemoryStream memStream = new MemoryStream();
			using (ZipFile f = new ZipFile(memStream))
			{
				f.IsStreamOwner = false;
				f.Password = "Hello";

				StringMemoryDataSource m = new StringMemoryDataSource(TestValue);
				f.BeginUpdate(new MemoryArchiveStorage());
				f.Add(m, "a.dat");
				f.CommitUpdate();
				Assert.IsTrue(f.TestArchive(true), "Archive test should pass");
			}
		
			using (ZipFile g = new ZipFile(memStream))
			{
				g.Password = "Hello";
				ZipEntry ze = g[0];

				Assert.IsTrue(ze.IsCrypted, "Entry should be encrypted");
				using (StreamReader r = new StreamReader(g.GetInputStream(0))) {
					string data = r.ReadToEnd();
					Assert.AreEqual(TestValue, data);
				}
			}
		}
		
		void TryDeleting(byte[] master, int totalEntries, int additions, params string[] toDelete)
		{
			MemoryStream ms = new MemoryStream();
			ms.Write(master, 0, master.Length);

			using (ZipFile f = new ZipFile(ms))
			{
				f.IsStreamOwner = false;
				Assert.AreEqual(totalEntries, f.Count);
				Assert.IsTrue(f.TestArchive(true));
				f.BeginUpdate(new MemoryArchiveStorage());

				for (int i = 0; i < additions; ++i)
				{
					f.Add(new StringMemoryDataSource("Another great file"),
						string.Format("Add{0}.dat", i + 1));
				}

				foreach (string name in toDelete)
				{
					f.Delete(name);
				}
				f.CommitUpdate();

				/* write stream to file to assist debugging.
								byte[] data = ms.ToArray();
								using ( FileStream fs = File.Open(@"c:\aha.zip", FileMode.Create, FileAccess.ReadWrite, FileShare.Read) )
								{
									fs.Write(data, 0, data.Length);
								}
				*/
				int newTotal = totalEntries + additions - toDelete.Length;
				Assert.AreEqual(newTotal, f.Count,
					string.Format("Expected {0} entries after update found {1}", newTotal, f.Count));
				Assert.IsTrue(f.TestArchive(true), "Archive test should pass");
			}
		}

		void TryDeleting(byte[] master, int totalEntries, int additions, params int[] toDelete)
		{
			MemoryStream ms = new MemoryStream();
			ms.Write(master, 0, master.Length);

			using ( ZipFile f = new ZipFile(ms) )
			{
				f.IsStreamOwner = false;
				Assert.AreEqual(totalEntries, f.Count);
				Assert.IsTrue(f.TestArchive(true));
				f.BeginUpdate(new MemoryArchiveStorage());

				for ( int i = 0; i < additions; ++i )
				{
					f.Add(new StringMemoryDataSource("Another great file"),
						string.Format("Add{0}.dat", i + 1));
				}

				foreach ( int i in toDelete )
				{
					f.Delete(f[i]);
				}
				f.CommitUpdate();

/* write stream to file to assist debugging.
				byte[] data = ms.ToArray();
				using ( FileStream fs = File.Open(@"c:\aha.zip", FileMode.Create, FileAccess.ReadWrite, FileShare.Read) )
				{
					fs.Write(data, 0, data.Length);
				}
*/
				int newTotal = totalEntries + additions - toDelete.Length;
				Assert.AreEqual(newTotal, f.Count,
					string.Format("Expected {0} entries after update found {1}", newTotal, f.Count));
				Assert.IsTrue(f.TestArchive(true), "Archive test should pass");
			}
		}

		[Test]
		[Category("Zip")]
		public void AddAndDeleteEntriesMemory()
		{
			MemoryStream memStream = new MemoryStream();

			using (ZipFile f = new ZipFile(memStream))
			{
				f.IsStreamOwner = false;

				f.BeginUpdate(new MemoryArchiveStorage());
				f.Add(new StringMemoryDataSource("Hello world"), @"z:\a\a.dat");
				f.Add(new StringMemoryDataSource("Another"), @"\b\b.dat");
				f.Add(new StringMemoryDataSource("Mr C"), @"c\c.dat");
				f.Add(new StringMemoryDataSource("Mrs D was a star"), @"d\d.dat");
				f.CommitUpdate();
				Assert.IsTrue(f.TestArchive(true));
			}

			byte[] master = memStream.ToArray();

			TryDeleting(master, 4, 1, @"z:\a\a.dat");
			TryDeleting(master, 4, 1, @"\a\a.dat");
			TryDeleting(master, 4, 1, @"a/a.dat");

			TryDeleting(master, 4, 0, 0);
			TryDeleting(master, 4, 0, 1);
			TryDeleting(master, 4, 0, 2);
			TryDeleting(master, 4, 0, 3);
			TryDeleting(master, 4, 0, 0, 1);
			TryDeleting(master, 4, 0, 0, 2);
			TryDeleting(master, 4, 0, 0, 3);
			TryDeleting(master, 4, 0, 1, 2);
			TryDeleting(master, 4, 0, 1, 3);
			TryDeleting(master, 4, 0, 2);

			TryDeleting(master, 4, 1, 0);
			TryDeleting(master, 4, 1, 1);
			TryDeleting(master, 4, 3, 2);
			TryDeleting(master, 4, 4, 3);
			TryDeleting(master, 4, 10, 0, 1);
			TryDeleting(master, 4, 10, 0, 2);
			TryDeleting(master, 4, 10, 0, 3);
			TryDeleting(master, 4, 20, 1, 2);
			TryDeleting(master, 4, 30, 1, 3);
			TryDeleting(master, 4, 40, 2);
		}

		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void AddAndDeleteEntries()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");

			string addFile = Path.Combine(tempFile, "a.dat");
			MakeTempFile(addFile, 1);

			string addFile2 = Path.Combine(tempFile, "b.dat");
			MakeTempFile(addFile2, 259);

			tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");

			using (ZipFile f = ZipFile.Create(tempFile))
			{
				f.BeginUpdate();
				f.Add(addFile);
				f.Add(addFile2);
				f.CommitUpdate();
				Assert.IsTrue(f.TestArchive(true));
			}

			using ( ZipFile f = new ZipFile(tempFile) )
			{
				Assert.AreEqual(2, f.Count);
				Assert.IsTrue(f.TestArchive(true));
				f.BeginUpdate();
				f.Delete(f[0]);
				f.CommitUpdate();
				Assert.AreEqual(1, f.Count);
				Assert.IsTrue(f.TestArchive(true));
			}

			File.Delete(addFile);
			File.Delete(addFile2);
			File.Delete(tempFile);
		}

		/// <summary>
		/// Simple round trip test for ZipFile class
		/// </summary>
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void RoundTrip()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");
			
			if (tempFile != null) 
			{
				tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
				MakeZipFile(tempFile, "", 10, 1024, "");
				
				using ( ZipFile zipFile = new ZipFile(tempFile))
				{
					foreach (ZipEntry e in zipFile) 
					{
						Stream instream = zipFile.GetInputStream(e);
						CheckKnownEntry(instream, 1024);
					}
					zipFile.Close();
				}

				File.Delete(tempFile);
			}
		}
		
		/// <summary>
		/// Simple round trip test for ZipFile class
		/// </summary>
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void RoundTripInMemory()
		{
			MemoryStream storage = new MemoryStream();
			MakeZipFile(storage, false, "", 10, 1024, "");
			
			using ( ZipFile zipFile = new ZipFile(storage))
			{
				foreach (ZipEntry e in zipFile) 
				{
					Stream instream = zipFile.GetInputStream(e);
					CheckKnownEntry(instream, 1024);
				}
				zipFile.Close();
			}
		}

		[Test]
		[Category("Zip")]
		public void AddToEmptyArchive()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");
			if (tempFile != null) 
			{
				string addFile = Path.Combine(tempFile, "a.dat");
				MakeTempFile(addFile, 1);
			
				tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
			
				using ( ZipFile f = ZipFile.Create(tempFile) )
				{
					f.BeginUpdate();
					f.Add(addFile);
					f.CommitUpdate();
					Assert.AreEqual(1, f.Count);
					Assert.IsTrue(f.TestArchive(true));
				}

				using ( ZipFile f = new ZipFile(tempFile) )
				{
					Assert.AreEqual(1, f.Count);
					f.BeginUpdate();
					f.Delete(f[0]);
					f.CommitUpdate();
					Assert.AreEqual(0, f.Count);
					Assert.IsTrue(f.TestArchive(true));
					f.Close();
				}

				File.Delete(addFile);
				File.Delete(tempFile);
			}
		}

		[Test]
		[Category("Zip")]
		public void CreateEmptyArchive()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");

			if (tempFile != null) 
			{
				tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
			
				using ( ZipFile f = ZipFile.Create(tempFile) )
				{
					f.BeginUpdate();
					f.CommitUpdate();
					Assert.IsTrue(f.TestArchive(true));
					f.Close();
				}

				using ( ZipFile f = new ZipFile(tempFile) )
				{
					Assert.AreEqual(0, f.Count);
				}
			}
		}
		/// <summary>
		/// Check that ZipFile finds entries when its got a long comment
		/// </summary>
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void FindEntriesInArchiveWithLongComment()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");
			
			if (tempFile != null) 
			{
				tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
				string longComment = new String('A', 65535);
				MakeZipFile(tempFile, "", 1, 1, longComment);
				using ( ZipFile zipFile = new ZipFile(tempFile) )
				{
					foreach (ZipEntry e in zipFile) 
					{
						Stream instream = zipFile.GetInputStream(e);
						CheckKnownEntry(instream, 1);
					}
					zipFile.Close();
				}
				File.Delete(tempFile);
			}
		}

		/// <summary>
		/// Test ZipFile Find method operation
		/// </summary>
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void FindEntry()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");
			
			if (tempFile != null) 
			{
				tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
				MakeZipFile(tempFile, new String[] {"Farriera", "Champagne", "Urban myth" }, 10, "Aha");
				
				using ( ZipFile zipFile = new ZipFile(tempFile) )
				{
					Assert.AreEqual(3, zipFile.Count, "Expected 1 entry");
				
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
				}
				File.Delete(tempFile);
			}
		}

		/// <summary>
		/// Check that ZipFile class handles no entries in zip file
		/// </summary>
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void HandlesNoEntries()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");
			
			if (tempFile != null) 
			{
				tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
				MakeZipFile(tempFile, "", 0, 1, "Aha");
				
				using ( ZipFile zipFile = new ZipFile(tempFile) )
				{
					Assert.AreEqual(0, zipFile.Count);
					zipFile.Close();
				}

				File.Delete(tempFile);
			}
			
		}

		[Test]
		[Category("Zip")]
		public void ArchiveTesting()
		{
			byte[] originalData = null;
			byte[] compressedData = MakeInMemoryZip(ref originalData, CompressionMethod.Deflated,
				6, 1024, null, true);
			
			MemoryStream ms = new MemoryStream(compressedData);
			ms.Seek(0, SeekOrigin.Begin);
			
			using ( ZipFile testFile = new ZipFile(ms) )
			{
			
				Assert.IsTrue(testFile.TestArchive(true), "Unexpected error in archive detected");

				byte[] corrupted = new byte[compressedData.Length];
				Array.Copy(compressedData, corrupted, compressedData.Length);

				corrupted[123] = (byte)(~corrupted[123] & 0xff);
				ms = new MemoryStream(corrupted);
			}

			using ( ZipFile testFile = new ZipFile(ms) )
			{
				Assert.IsFalse(testFile.TestArchive(true), "Error in archive not detected");
			}
		}

		[Test]
		public void Crypto_AddEncryptedEntryToExistingArchiveSafe()
		{
			MemoryStream ms = new MemoryStream();
			
			byte[] rawData;
			
			using ( ZipFile testFile = new ZipFile(ms) )
			{
				testFile.IsStreamOwner = false;
				testFile.BeginUpdate();
				testFile.Add(new StringMemoryDataSource("Aha"), "No1", CompressionMethod.Stored);
				testFile.Add(new StringMemoryDataSource("And so it goes"), "No2", CompressionMethod.Stored);
				testFile.Add(new StringMemoryDataSource("No3"), "No3", CompressionMethod.Stored);
				testFile.CommitUpdate();
				
				Assert.IsTrue(testFile.TestArchive(true));
				rawData = ms.ToArray();
			}
			
			ms = new MemoryStream(rawData);
			
			using (ZipFile testFile = new ZipFile(ms) ) {
				Assert.IsTrue(testFile.TestArchive(true));

				testFile.BeginUpdate(new MemoryArchiveStorage(FileUpdateMode.Safe));
				testFile.Password = "pwd";
				testFile.Add(new StringMemoryDataSource("Zapata!"), "encrypttest.xml");
				testFile.CommitUpdate();

				Assert.IsTrue(testFile.TestArchive(true));
				
				int entryIndex = testFile.FindEntry("encrypttest.xml", true);
				Assert.IsNotNull(entryIndex >= 0);
				Assert.IsTrue(testFile[entryIndex].IsCrypted);
			}
		}

		[Test]
		public void Crypto_AddEncryptedEntryToExistingArchiveDirect()
		{
			MemoryStream ms = new MemoryStream();

			byte[] rawData;

			using (ZipFile testFile = new ZipFile(ms))
			{
				testFile.IsStreamOwner = false;
				testFile.BeginUpdate();
				testFile.Add(new StringMemoryDataSource("Aha"), "No1", CompressionMethod.Stored);
				testFile.Add(new StringMemoryDataSource("And so it goes"), "No2", CompressionMethod.Stored);
				testFile.Add(new StringMemoryDataSource("No3"), "No3", CompressionMethod.Stored);
				testFile.CommitUpdate();

				Assert.IsTrue(testFile.TestArchive(true));
				rawData = ms.ToArray();
			}

			using (ZipFile testFile = new ZipFile(ms))
			{
				Assert.IsTrue(testFile.TestArchive(true));
				testFile.IsStreamOwner = false;

				testFile.BeginUpdate();
				testFile.Password = "pwd";
				testFile.Add(new StringMemoryDataSource("Zapata!"), "encrypttest.xml");
				testFile.CommitUpdate();

				Assert.IsTrue(testFile.TestArchive(true));

				int entryIndex = testFile.FindEntry("encrypttest.xml", true);
				Assert.IsNotNull(entryIndex >= 0);
				Assert.IsTrue(testFile[entryIndex].IsCrypted);
			}
		}
		
		[Test]
		[Category("Zip")]
		public void UnicodeNames()
		{
			MemoryStream memStream = new MemoryStream();
			using (ZipFile f = new ZipFile(memStream))
			{
				f.IsStreamOwner = false;

				f.BeginUpdate(new MemoryArchiveStorage());

				string[] names = new string[] 
				{
					"\u030A\u03B0",     // Greek
					"\u0680\u0685",     // Arabic
				};

				foreach ( string name in names )
				{
					f.Add(new StringMemoryDataSource("Hello world"), name,
						  CompressionMethod.Deflated, true);
				}
				f.CommitUpdate();
				Assert.IsTrue(f.TestArchive(true));

				foreach ( string name in names )
				{
					int index = f.FindEntry(name, true);

					Assert.IsTrue(index >= 0);
					ZipEntry found = f[index];
					Assert.AreEqual(name, found.Name);
				}
			}

		}

		[Test]
		public void UpdateCommentOnlyInMemory()
		{
			MemoryStream ms = new MemoryStream();

			using (ZipFile testFile = new ZipFile(ms))
			{
				testFile.IsStreamOwner = false;
				testFile.BeginUpdate();
				testFile.Add(new StringMemoryDataSource("Aha"), "No1", CompressionMethod.Stored);
				testFile.Add(new StringMemoryDataSource("And so it goes"), "No2", CompressionMethod.Stored);
				testFile.Add(new StringMemoryDataSource("No3"), "No3", CompressionMethod.Stored);
				testFile.CommitUpdate();

				Assert.IsTrue(testFile.TestArchive(true));
			}

			using (ZipFile testFile = new ZipFile(ms))
			{
				Assert.IsTrue(testFile.TestArchive(true));
				Assert.AreEqual("", testFile.ZipFileComment);
				testFile.IsStreamOwner = false;

				testFile.BeginUpdate();
				testFile.SetComment("Here is my comment");
				testFile.CommitUpdate();

				Assert.IsTrue(testFile.TestArchive(true));
			}

			using (ZipFile testFile = new ZipFile(ms))
			{
				Assert.IsTrue(testFile.TestArchive(true));
				Assert.AreEqual("Here is my comment", testFile.ZipFileComment);
			}
		}

		[Test]
		[Category("CreatesTempFile")]
		public void UpdateCommentOnlyOnDisk()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");

			tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
			if ( File.Exists(tempFile) ) {
				File.Delete(tempFile);
			}

			using (ZipFile testFile = ZipFile.Create(tempFile)) {
				testFile.BeginUpdate();
				testFile.Add(new StringMemoryDataSource("Aha"), "No1", CompressionMethod.Stored);
				testFile.Add(new StringMemoryDataSource("And so it goes"), "No2", CompressionMethod.Stored);
				testFile.Add(new StringMemoryDataSource("No3"), "No3", CompressionMethod.Stored);
				testFile.CommitUpdate();

				Assert.IsTrue(testFile.TestArchive(true));
			}

			using (ZipFile testFile = new ZipFile(tempFile)) {
				Assert.IsTrue(testFile.TestArchive(true));
				Assert.AreEqual("", testFile.ZipFileComment);

				testFile.BeginUpdate(new DiskArchiveStorage(testFile, FileUpdateMode.Direct));
				testFile.SetComment("Here is my comment");
				testFile.CommitUpdate();

				Assert.IsTrue(testFile.TestArchive(true));
			}

			using (ZipFile testFile = new ZipFile(tempFile)) {
				Assert.IsTrue(testFile.TestArchive(true));
				Assert.AreEqual("Here is my comment", testFile.ZipFileComment);
			}
			File.Delete(tempFile);

			// Variant using indirect updating.
			using (ZipFile testFile = ZipFile.Create(tempFile)) {
				testFile.BeginUpdate();
				testFile.Add(new StringMemoryDataSource("Aha"), "No1", CompressionMethod.Stored);
				testFile.Add(new StringMemoryDataSource("And so it goes"), "No2", CompressionMethod.Stored);
				testFile.Add(new StringMemoryDataSource("No3"), "No3", CompressionMethod.Stored);
				testFile.CommitUpdate();

				Assert.IsTrue(testFile.TestArchive(true));
			}

			using (ZipFile testFile = new ZipFile(tempFile)) {
				Assert.IsTrue(testFile.TestArchive(true));
				Assert.AreEqual("", testFile.ZipFileComment);

				testFile.BeginUpdate();
				testFile.SetComment("Here is my comment");
				testFile.CommitUpdate();

				Assert.IsTrue(testFile.TestArchive(true));
			}

			using (ZipFile testFile = new ZipFile(tempFile)) {
				Assert.IsTrue(testFile.TestArchive(true));
				Assert.AreEqual("Here is my comment", testFile.ZipFileComment);
			}
			File.Delete(tempFile);
		}
	}
}
