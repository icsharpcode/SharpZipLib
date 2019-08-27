using ICSharpCode.SharpZipLib.Checksum;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.SharpZipLib.Tests.Checksum
{

	[TestFixture]
	public class Crc32Tests : ChecksumTestBase
	{

		[Test]
		public void CRC_32()
		{
			var underTestCrc32 = new Crc32();
			Assert.AreEqual(0x0, underTestCrc32.Value);

			underTestCrc32.Update(check);
			Assert.AreEqual(0xCBF43926, underTestCrc32.Value);

			underTestCrc32.Reset();
			Assert.AreEqual(0x0, underTestCrc32.Value);

			underTestCrc32.Update(longcheck);
			Assert.AreEqual(0x4DDF6E59, underTestCrc32.Value);

			underTestCrc32.Reset();
			Assert.AreEqual(0x0, underTestCrc32.Value);

			exceptionTesting(underTestCrc32);
		}


		[Test]
		public void CRC_32_Byte_For_Byte()
		{
			var underTestCrc32 = new Crc32();
			Assert.AreEqual(0x0, underTestCrc32.Value);

			foreach (byte b in check)
			{
				underTestCrc32.Update((int)b);
			}
			Assert.AreEqual(0xCBF43926, underTestCrc32.Value);

			underTestCrc32.Reset();
			Assert.AreEqual(0x0, underTestCrc32.Value);

			underTestCrc32.Update(longcheck);
			Assert.AreEqual(0x4DDF6E59, underTestCrc32.Value);

			underTestCrc32.Reset();
			Assert.AreEqual(0x0, underTestCrc32.Value);

			exceptionTesting(underTestCrc32);
		}


		[Test]
		public void CRC32_ComputeCrc32_Produces_Correct_Result()
		{
			Assert.AreEqual(0x19F6D6AB, Crc32.ComputeCrc32(0x9AE0DAAF, 57));
			Assert.AreEqual(0xAA0D1792, Crc32.ComputeCrc32(0x75BCD15, 123));
			Assert.AreEqual(0xF6280B5B, Crc32.ComputeCrc32(0x912D00C0, 21));
		}
	}
}
