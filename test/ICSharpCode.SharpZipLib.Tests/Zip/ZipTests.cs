using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Tests.TestSupport;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using System;
using System.IO;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace ICSharpCode.SharpZipLib.Tests.Zip
{
	internal class RuntimeInfo
	{
		public RuntimeInfo(CompressionMethod method, int compressionLevel,
			int size, string password, bool getCrc)
		{
			this.method = method;
			this.compressionLevel = compressionLevel;
			this.password = password;
			this.size = size;
			this.random = false;

			original = new byte[Size];
			if (random)
			{
				var rnd = new Random();
				rnd.NextBytes(original);
			}
			else
			{
				for (int i = 0; i < size; ++i)
				{
					original[i] = (byte)'A';
				}
			}

			if (getCrc)
			{
				var crc32 = new Crc32();
				crc32.Update(new ArraySegment<byte>(original, 0, size));
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

		private bool Random
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

		private readonly byte[] original;
		private readonly CompressionMethod method;
		private int compressionLevel;
		private int size;
		private string password;
		private bool random;
		private bool isDirectory_;
		private long crc = -1;

		#endregion Instance Fields
	}

	internal class MemoryDataSource : IStaticDataSource
	{
		#region Constructors

		/// <summary>
		/// Initialise a new instance.
		/// </summary>
		/// <param name="data">The data to provide.</param>
		public MemoryDataSource(byte[] data)
		{
			data_ = data;
		}

		#endregion Constructors

		#region IDataSource Members

		/// <summary>
		/// Get a Stream for this <see cref="IStaticDataSource"/>
		/// </summary>
		/// <returns>Returns a <see cref="Stream"/></returns>
		public Stream GetSource()
		{
			return new MemoryStream(data_);
		}

		#endregion IDataSource Members

		#region Instance Fields

		private readonly byte[] data_;

		#endregion Instance Fields
	}

	internal class StringMemoryDataSource : MemoryDataSource
	{
		public StringMemoryDataSource(string data)
			: base(Encoding.ASCII.GetBytes(data))
		{
		}
	}

	public class ZipBase
	{
		static protected string GetTempFilePath()
		{
			string result = null;
			try
			{
				result = Path.GetTempPath();
			}
			catch (SecurityException)
			{
			}
			return result;
		}

		protected byte[] MakeInMemoryZip(bool withSeek, params object[] createSpecs)
		{
			MemoryStream ms;

			if (withSeek)
			{
				ms = new MemoryStream();
			}
			else
			{
				ms = new MemoryStreamWithoutSeek();
			}

			using (ZipOutputStream outStream = new ZipOutputStream(ms))
			{
				for (int counter = 0; counter < createSpecs.Length; ++counter)
				{
					var info = createSpecs[counter] as RuntimeInfo;
					outStream.Password = info.Password;

					if (info.Method != CompressionMethod.Stored)
					{
						outStream.SetLevel(info.CompressionLevel); // 0 - store only to 9 - means best compression
					}

					string entryName;

					if (info.IsDirectory)
					{
						entryName = "dir" + counter + "/";
					}
					else
					{
						entryName = "entry" + counter + ".tst";
					}

					var entry = new ZipEntry(entryName);
					entry.CompressionMethod = info.Method;
					if (info.Crc >= 0)
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

			if (withSeek)
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

				var entry = new ZipEntry("dummyfile.tst");
				entry.CompressionMethod = method;

				outStream.PutNextEntry(entry);

				if (size > 0)
				{
					var rnd = new Random();
					original = new byte[size];
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
			}
			return ms.ToArray();
		}

		protected static void MakeTempFile(string name, int size)
		{
			using (FileStream fs = File.Create(name))
			{
				byte[] buffer = new byte[4096];
				while (size > 0)
				{
					fs.Write(buffer, 0, Math.Min(size, buffer.Length));
					size -= buffer.Length;
				}
			}
		}

		protected static byte ScatterValue(byte rhs)
		{
			return (byte)((rhs * 253 + 7) & 0xff);
		}

		private static void AddKnownDataToEntry(Stream zipStream, int size)
		{
			if (size > 0)
			{
				byte nextValue = 0;
				int bufferSize = Math.Min(size, 65536);
				byte[] data = new byte[bufferSize];
				int currentIndex = 0;
				for (int i = 0; i < size; ++i)
				{
					data[currentIndex] = nextValue;
					nextValue = ScatterValue(nextValue);

					currentIndex += 1;
					if ((currentIndex >= data.Length) || (i + 1 == size))
					{
						zipStream.Write(data, 0, currentIndex);
						currentIndex = 0;
					}
				}
			}
		}

		public void WriteToFile(string fileName, byte[] data)
		{
			using (FileStream fs = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
			{
				fs.Write(data, 0, data.Length);
			}
		}

		#region MakeZipFile

		protected void MakeZipFile(Stream storage, bool isOwner, string[] names, int size, string comment)
		{
			using (ZipOutputStream zOut = new ZipOutputStream(storage))
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
				using (ZipOutputStream zOut = new ZipOutputStream(fs))
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

		#endregion MakeZipFile

		#region MakeZipFile Entries

		protected void MakeZipFile(string name, string entryNamePrefix, int entries, int size, string comment)
		{
			using (FileStream fs = File.Create(name))
			using (ZipOutputStream zOut = new ZipOutputStream(fs))
			{
				zOut.SetComment(comment);
				for (int i = 0; i < entries; ++i)
				{
					zOut.PutNextEntry(new ZipEntry(entryNamePrefix + (i + 1)));
					AddKnownDataToEntry(zOut, size);
				}
			}
		}

		protected void MakeZipFile(Stream storage, bool isOwner,
			string entryNamePrefix, int entries, int size, string comment)
		{
			using (ZipOutputStream zOut = new ZipOutputStream(storage))
			{
				zOut.IsStreamOwner = isOwner;
				zOut.SetComment(comment);
				for (int i = 0; i < entries; ++i)
				{
					zOut.PutNextEntry(new ZipEntry(entryNamePrefix + (i + 1)));
					AddKnownDataToEntry(zOut, size);
				}
			}
		}

		protected void MakeZipFile(Stream storage, CompressionMethod compressionMethod,  bool isOwner,
			string entryNamePrefix, int entries, int size, string comment)
		{
			using (ZipFile f = new ZipFile(storage, leaveOpen: !isOwner))
			{
				f.BeginUpdate();
				f.SetComment(comment);

				for (int i = 0; i < entries; ++i)
				{
					var data = new MemoryStream();
					AddKnownDataToEntry(data, size);

					var m = new MemoryDataSource(data.ToArray());
					f.Add(m, entryNamePrefix + (i + 1), compressionMethod);
				}

				f.CommitUpdate();
			}
		}

		#endregion MakeZipFile Entries

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

		protected static async Task CheckKnownEntryAsync(Stream inStream, int expectedCount)
		{
			byte[] buffer = new byte[1024];

			int bytesRead;
			int total = 0;
			byte nextValue = 0;
			while ((bytesRead = await inStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
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

	internal class TestHelper
	{
		static public void SaveMemoryStream(MemoryStream ms, string fileName)
		{
			byte[] data = ms.ToArray();
			using (FileStream fs = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
			{
				fs.Write(data, 0, data.Length);
			}
		}

		static public int CompareDosDateTimes(DateTime l, DateTime r)
		{
			// Compare dates to dos accuracy...
			// Ticks can be different yet all these values are still the same!
			int result = l.Year - r.Year;
			if (result == 0)
			{
				result = l.Month - r.Month;
				if (result == 0)
				{
					result = l.Day - r.Day;
					if (result == 0)
					{
						result = l.Hour - r.Hour;
						if (result == 0)
						{
							result = l.Minute - r.Minute;
							if (result == 0)
							{
								result = (l.Second / 2) - (r.Second / 2);
							}
						}
					}
				}
			}
			return result;
		}
	}

	public class TransformBase : ZipBase
	{
		protected void TestFile(INameTransform t, string original, string expected)
		{
			string transformed = t.TransformFile(original);
			Assert.AreEqual(expected, transformed, "Should be equal");
		}

		protected void TestDirectory(INameTransform t, string original, string expected)
		{
			string transformed = t.TransformDirectory(original);
			Assert.AreEqual(expected, transformed, "Should be equal");
		}
	}
}
