using NUnit.Framework;
using ICSharpCode.SharpZipLib.Checksum;
using System;

namespace ICSharpCode.SharpZipLib.Tests.Checksum
{
	[TestFixture]
	[Category("Checksum")]
	public class ChecksumTests
	{
		readonly
				// Represents ASCII string of "123456789"
				byte[] check = { 49, 50, 51, 52, 53, 54, 55, 56, 57 };

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

		private void exceptionTesting(IChecksum crcUnderTest)
		{

			bool exception = false;

			try {
				crcUnderTest.Update(null);
			} catch (ArgumentNullException) {
				exception = true;
			}
			Assert.IsTrue(exception, "Passing a null buffer should cause an ArgumentNullException");

			// reset exception
			exception = false;
			try {
				crcUnderTest.Update(null, 0, 0);
			} catch (ArgumentNullException) {
				exception = true;
			}
			Assert.IsTrue(exception, "Passing a null buffer should cause an ArgumentNullException");

			// reset exception
			exception = false;
			try {
				crcUnderTest.Update(check, -1, 9);
			} catch (ArgumentOutOfRangeException) {
				exception = true;
			}
			Assert.IsTrue(exception, "Passing a negative offset should cause an ArgumentOutOfRangeException");

			// reset exception
			exception = false;
			try {
				crcUnderTest.Update(check, 9, 0);
			} catch (ArgumentOutOfRangeException) {
				exception = true;
			}
			Assert.IsTrue(exception, "Passing an offset greater than or equal to buffer.Length should cause an ArgumentOutOfRangeException");

			// reset exception
			exception = false;
			try {
				crcUnderTest.Update(check, 0, -1);
			} catch (ArgumentOutOfRangeException) {
				exception = true;
			}
			Assert.IsTrue(exception, "Passing a negative count should cause an ArgumentOutOfRangeException");

			// reset exception
			exception = false;
			try {
				crcUnderTest.Update(check, 0, 10);
			} catch (ArgumentOutOfRangeException) {
				exception = true;
			}
			Assert.IsTrue(exception, "Passing a count + offset greater than buffer.Length should cause an ArgumentOutOfRangeException");
		}
	}
}
