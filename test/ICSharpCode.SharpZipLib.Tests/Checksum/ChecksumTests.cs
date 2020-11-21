using ICSharpCode.SharpZipLib.Checksum;
using NUnit.Framework;
using System;
using System.Diagnostics;

namespace ICSharpCode.SharpZipLib.Tests.Checksum
{
	[TestFixture]
	[Category("Checksum")]
	public class ChecksumTests
	{
		private readonly
				// Represents ASCII string of "123456789"
				byte[] check = { 49, 50, 51, 52, 53, 54, 55, 56, 57 };

		// Represents string "123456789123456789123456789123456789"
		private readonly byte[] longCheck = {
			49, 50, 51, 52, 53, 54, 55, 56, 57,
			49, 50, 51, 52, 53, 54, 55, 56, 57,
			49, 50, 51, 52, 53, 54, 55, 56, 57,
			49, 50, 51, 52, 53, 54, 55, 56, 57
		};

		[Test]
		public void Adler_32()
		{
			var underTestAdler32 = new Adler32();
			Assert.AreEqual(0x00000001, underTestAdler32.Value);

			underTestAdler32.Update(check);
			Assert.AreEqual(0x091E01DE, underTestAdler32.Value);

			underTestAdler32.Reset();
			Assert.AreEqual(0x00000001, underTestAdler32.Value);

			exceptionTesting(underTestAdler32);
		}

		const long BufferSize = 256 * 1024 * 1024;

		[Test]
		public void Adler_32_Performance()
		{
			var rand = new Random(1);

			var buffer = new byte[BufferSize];
			rand.NextBytes(buffer);

			var adler = new Adler32();
			Assert.AreEqual(0x00000001, adler.Value);

			var sw = new Stopwatch();
			sw.Start();

			adler.Update(buffer);

			sw.Stop();
			Console.WriteLine($"Adler32 Hashing of 256 MiB: {sw.Elapsed.TotalSeconds:f4} second(s)");

			adler.Update(check);
			Assert.AreEqual(0xD4897DA3, adler.Value);

			exceptionTesting(adler);
		}

		[Test]
		public void CRC_32_BZip2()
		{
			var underTestBZip2Crc = new BZip2Crc();
			Assert.AreEqual(0x0, underTestBZip2Crc.Value);

			underTestBZip2Crc.Update(check);
			Assert.AreEqual(0xFC891918, underTestBZip2Crc.Value);

			underTestBZip2Crc.Reset();
			Assert.AreEqual(0x0, underTestBZip2Crc.Value);

			exceptionTesting(underTestBZip2Crc);
		}

		[Test]
		public void CRC_32_BZip2_Long()
		{
			var underTestCrc32 = new BZip2Crc();
			underTestCrc32.Update(longCheck);
			Assert.AreEqual(0xEE53D2B2, underTestCrc32.Value);
		}

		[Test]
		public void CRC_32_BZip2_Unaligned()
		{
			// Extract "456" and CRC
			var underTestCrc32 = new BZip2Crc();
			underTestCrc32.Update(new ArraySegment<byte>(check, 3, 3));
			Assert.AreEqual(0x001D0511, underTestCrc32.Value);
		}

		[Test]
		public void CRC_32_BZip2_Long_Unaligned()
		{
			// Extract "789123456789123456" and CRC
			var underTestCrc32 = new BZip2Crc();
			underTestCrc32.Update(new ArraySegment<byte>(longCheck, 15, 18));
			Assert.AreEqual(0x025846E0, underTestCrc32.Value);
		}

		[Test]
		public void CRC_32()
		{
			var underTestCrc32 = new Crc32();
			Assert.AreEqual(0x0, underTestCrc32.Value);

			underTestCrc32.Update(check);
			Assert.AreEqual(0xCBF43926, underTestCrc32.Value);

			underTestCrc32.Reset();
			Assert.AreEqual(0x0, underTestCrc32.Value);

			exceptionTesting(underTestCrc32);
		}

		[Test]
		public void CRC_32_Long()
		{
			var underTestCrc32 = new Crc32();
			underTestCrc32.Update(longCheck);
			Assert.AreEqual(0x3E29169C, underTestCrc32.Value);
		}

		[Test]
		public void CRC_32_Unaligned()
		{
			// Extract "456" and CRC
			var underTestCrc32 = new Crc32();
			underTestCrc32.Update(new ArraySegment<byte>(check, 3, 3));
			Assert.AreEqual(0xB1A8C371, underTestCrc32.Value);
		}

		[Test]
		public void CRC_32_Long_Unaligned()
		{
			// Extract "789123456789123456" and CRC
			var underTestCrc32 = new Crc32();
			underTestCrc32.Update(new ArraySegment<byte>(longCheck, 15, 18));
			Assert.AreEqual(0x31CA9A2E, underTestCrc32.Value);
		}

		private void exceptionTesting(IChecksum crcUnderTest)
		{
			bool exception = false;

			try
			{
				crcUnderTest.Update(null);
			}
			catch (ArgumentNullException)
			{
				exception = true;
			}
			Assert.IsTrue(exception, "Passing a null buffer should cause an ArgumentNullException");

			// reset exception
			exception = false;
			try
			{
				crcUnderTest.Update(new ArraySegment<byte>(null, 0, 0));
			}
			catch (ArgumentNullException)
			{
				exception = true;
			}
			Assert.IsTrue(exception, "Passing a null buffer should cause an ArgumentNullException");

			// reset exception
			exception = false;
			try
			{
				crcUnderTest.Update(new ArraySegment<byte>(check, -1, 9));
			}
			catch (ArgumentOutOfRangeException)
			{
				exception = true;
			}
			Assert.IsTrue(exception, "Passing a negative offset should cause an ArgumentOutOfRangeException");

			// reset exception
			exception = false;
			try
			{
				crcUnderTest.Update(new ArraySegment<byte>(check, 10, 0));
			}
			catch (ArgumentException)
			{
				exception = true;
			}
			Assert.IsTrue(exception, "Passing an offset greater than buffer.Length should cause an ArgumentException");

			// reset exception
			exception = false;
			try
			{
				crcUnderTest.Update(new ArraySegment<byte>(check, 0, -1));
			}
			catch (ArgumentOutOfRangeException)
			{
				exception = true;
			}
			Assert.IsTrue(exception, "Passing a negative count should cause an ArgumentOutOfRangeException");

			// reset exception
			exception = false;
			try
			{
				crcUnderTest.Update(new ArraySegment<byte>(check, 0, 10));
			}
			catch (ArgumentException)
			{
				exception = true;
			}
			Assert.IsTrue(exception, "Passing a count + offset greater than buffer.Length should cause an ArgumentException");
		}
	}
}
