using System;
using System.IO;
using System.Text;
using System.Security;

using NUnit.Framework;

using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.GZip;

using ICSharpCode.SharpZipLib.Tests.TestSupport;

namespace ICSharpCode.SharpZipLib.Tests.Base
{
	/// <summary>
	/// This class contains test cases for Deflater/Inflater streams.
	/// </summary>
	[TestFixture]
	public class InflaterDeflaterTestSuite
	{
		void Inflate(MemoryStream ms, byte[] original, int level, bool zlib)
		{
			ms.Seek(0, SeekOrigin.Begin);
			
			Inflater inflater = new Inflater(!zlib);
			InflaterInputStream inStream = new InflaterInputStream(ms, inflater);
			byte[] buf2 = new byte[original.Length];

			int currentIndex  = 0;
			int count = buf2.Length;

			try
			{
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
			}
			catch(Exception ex)
			{
				Console.WriteLine("Unexpected exception - '{0}'", ex.Message);
				throw;
			}

			if ( currentIndex != original.Length )
			{
				Console.WriteLine("Original {0}, new {1}", original.Length, currentIndex);
				Assert.Fail("Lengths different");
			}

			for (int i = 0; i < original.Length; ++i) 
			{
				if ( buf2[i] != original[i] )
				{
					string description = string.Format("Difference at {0} level {1} zlib {2} ", i, level, zlib);
					if ( original.Length < 2048 )
					{
						StringBuilder builder = new StringBuilder(description);
						for (int d = 0; d < original.Length; ++d)
						{
							builder.AppendFormat("{0} ", original[d]);
						}

						Assert.Fail(builder.ToString());
					}
					else
					{
						Assert.Fail(description);
					}
				}
			}
		}

		MemoryStream Deflate(byte[] data, int level, bool zlib)
		{
			MemoryStream memoryStream = new MemoryStream();
			
			Deflater deflater = new Deflater(level, !zlib);
			using ( DeflaterOutputStream outStream = new DeflaterOutputStream(memoryStream, deflater) )
			{
				outStream.IsStreamOwner = false;
				outStream.Write(data, 0, data.Length);
				outStream.Flush();
				outStream.Finish();
			}
			return memoryStream;
		}

		void RandomDeflateInflate(int size, int level, bool zlib)
		{
			byte[] buffer = new byte[size];
			System.Random rnd = new Random();
			rnd.NextBytes(buffer);
			
			MemoryStream ms = Deflate(buffer, level, zlib);
			Inflate(ms, buffer, level, zlib);
		}

		/// <summary>
		/// Basic inflate/deflate test
		/// </summary>
		[Test]
		[Category("Base")]
		public void InflateDeflateZlib()
		{
			for (int level = 0; level < 10; ++level)
			{
				RandomDeflateInflate(100000, level, true);
			}
		}

		delegate void RunCompress(byte[] buffer);

		int runLevel;
		bool runZlib;
		long runCount = 0;
		Random runRandom = new Random(5);

		void DeflateAndInflate(byte[] buffer)
		{
			++runCount;
			MemoryStream ms = Deflate(buffer, runLevel, runZlib);
			Inflate(ms, buffer, runLevel, runZlib);
		}

		void TryVariants(RunCompress test, byte[] buffer, int index)
		{
			int worker = 0;
			while ( worker <= 255 )
			{
				buffer[index] = (byte)worker;
				if ( index < buffer.Length - 1)
				{
					TryVariants(test, buffer, index + 1);
				}
				else
				{
					test(buffer);
				}

				worker += runRandom.Next(256);
			}
		}

		void TryManyVariants(int level, bool zlib, RunCompress test, byte[] buffer)
		{
			runLevel = level;
			runZlib = zlib;
			TryVariants(test, buffer, 0);
		}


		[Test]
		[Category("Base")]
		public void SmallBlocks()
		{
			byte[] buffer = new byte[10];
			Array.Clear(buffer, 0, buffer.Length);
			TryManyVariants(0, false, new RunCompress(DeflateAndInflate), buffer);
		}

		[Test]
		[Explicit]
		[Category("Base")]
		[Category("ExcludeFromAutoBuild")]
		public void FindBug()
		{
			using ( FileStream fs = File.OpenRead("c:\\tmp\\original.dat") )
			{
				long readOffset =  0;
				long readLength = fs.Length - readOffset;
//				readLength -= 5567; // 5568 works 5567 doesnt....

				fs.Seek(readOffset, SeekOrigin.Begin);

				byte[] original = new byte[readLength];

				int bytesRead = fs.Read(original, 0, original.Length);
				Assert.AreEqual(bytesRead, original.Length);

				MemoryStream ms = Deflate(original, Deflater.BEST_SPEED, true);
				Inflate(ms, original, Deflater.BEST_SPEED, true);
			}
		}

		/// <summary>
		/// Basic inflate/deflate test
		/// </summary>
		[Test]
		[Category("Base")]
		public void InflateDeflateNonZlib()
		{
			for (int level = 0; level < 10; ++level)
			{
				RandomDeflateInflate(100000, level, false);
			}
		}

		[Test]
		[Category("Base")]
		public void CloseDeflatorWithNestedUsing()
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
			if (tempFile != null) 
			{
				tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
				using (FileStream diskFile = File.Create(tempFile))
				using (DeflaterOutputStream deflator = new DeflaterOutputStream(diskFile))
				using (StreamWriter txtFile = new StreamWriter(deflator))
				{
					txtFile.Write("Hello");
					txtFile.Flush();
				}

				File.Delete(tempFile);
			}
		}

		[Test]
		[Category("Base")]
		public void DeflatorStreamOwnership()
		{
			MemoryStreamEx memStream = new MemoryStreamEx();
			DeflaterOutputStream s = new DeflaterOutputStream(memStream);
			
			Assert.IsFalse(memStream.IsClosed, "Shouldnt be closed initially");
			Assert.IsFalse(memStream.IsDisposed, "Shouldnt be disposed initially");
			
			s.Close();
			
			Assert.IsTrue(memStream.IsClosed, "Should be closed after parent owner close");
			Assert.IsTrue(memStream.IsDisposed, "Should be disposed after parent owner close");
			
			memStream = new MemoryStreamEx();
			s = new DeflaterOutputStream(memStream);
			
			Assert.IsFalse(memStream.IsClosed, "Shouldnt be closed initially");
			Assert.IsFalse(memStream.IsDisposed, "Shouldnt be disposed initially");
			
			s.IsStreamOwner = false;
			s.Close();
			
			Assert.IsFalse(memStream.IsClosed, "Should not be closed after parent owner close");
			Assert.IsFalse(memStream.IsDisposed, "Should not be disposed after parent owner close");
			
		}

		[Test]
		[Category("Base")]
		public void InflatorStreamOwnership()
		{
			MemoryStreamEx memStream = new MemoryStreamEx();
			InflaterInputStream s = new InflaterInputStream(memStream);

			Assert.IsFalse(memStream.IsClosed, "Shouldnt be closed initially");
			Assert.IsFalse(memStream.IsDisposed, "Shouldnt be disposed initially");

			s.Close();

			Assert.IsTrue(memStream.IsClosed, "Should be closed after parent owner close");
			Assert.IsTrue(memStream.IsDisposed, "Should be disposed after parent owner close");

			memStream = new MemoryStreamEx();
			s = new InflaterInputStream(memStream);

			Assert.IsFalse(memStream.IsClosed, "Shouldnt be closed initially");
			Assert.IsFalse(memStream.IsDisposed, "Shouldnt be disposed initially");

			s.IsStreamOwner = false;
			s.Close();

			Assert.IsFalse(memStream.IsClosed, "Should not be closed after parent owner close");
			Assert.IsFalse(memStream.IsDisposed, "Should not be disposed after parent owner close");

		}
		
		[Test]
		[Category("Base")]
		public void CloseInflatorWithNestedUsing()
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

			if (tempFile != null) 
			{
				tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
				using (FileStream diskFile = File.Create(tempFile))
				using (DeflaterOutputStream deflator = new DeflaterOutputStream(diskFile))
				using (StreamWriter textWriter = new StreamWriter(deflator))
				{
					textWriter.Write("Hello");
					textWriter.Flush();
				}

				using (FileStream diskFile = File.OpenRead(tempFile))
				using (InflaterInputStream deflator = new InflaterInputStream(diskFile))
				using (StreamReader textReader = new StreamReader(deflator))
				{
					char[] buffer = new char[5];
					int readCount = textReader.Read(buffer, 0, 5);
					Assert.AreEqual(5, readCount);

					StringBuilder b = new StringBuilder();
					b.Append(buffer);
					Assert.AreEqual("Hello", b.ToString());

				}

				File.Delete(tempFile);
			}
		}
	}
}
