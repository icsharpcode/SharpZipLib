using ICSharpCode.SharpZipLib.Checksum;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.SharpZipLib.Tests.Checksum
{
	[TestFixture]
	public class Bzip2CrcTests : ChecksumTestBase
	{


		[Test]
		public void CRC_32_BZip2()
		{
			var underTestBZip2Crc = new BZip2Crc();
			Assert.AreEqual(0x0, underTestBZip2Crc.Value);

			underTestBZip2Crc.Update(check);
			Assert.AreEqual(0xFC891918, underTestBZip2Crc.Value);

			underTestBZip2Crc.Reset();
			Assert.AreEqual(0x0, underTestBZip2Crc.Value);

			underTestBZip2Crc.Update(longcheck);
			Assert.AreEqual(0xA12ADA2B, underTestBZip2Crc.Value);

			underTestBZip2Crc.Reset();
			Assert.AreEqual(0x0, underTestBZip2Crc.Value);

			exceptionTesting(underTestBZip2Crc);
		}

	}
}
