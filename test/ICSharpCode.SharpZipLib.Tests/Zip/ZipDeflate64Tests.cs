using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Tests.TestSupport;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;

namespace ICSharpCode.SharpZipLib.Tests.Zip
{
	[TestFixture]
	public class ZipDeflate64Tests
	{
		[Test]
		[Category("Zip"), Category("Deflate64")]
		public void WriteZipStreamWithDeflate64()
		{
			var contentLength = 2 * 1024 * 1024;

			// Using different seeds so that we can verify that the contents have not been swapped
			var seed = 5;
			var seed64 = 6;

			using var ms = new MemoryStream();

			using (var zipOutputStream = new ZipOutputStream(ms) { IsStreamOwner = false })
			{
				zipOutputStream.PutNextEntry(new ZipEntry("deflate64.file")
				{
					CompressionMethod = CompressionMethod.Deflate64,
				});

				Utils.WriteDummyData(zipOutputStream, contentLength, seed64);

				zipOutputStream.PutNextEntry(new ZipEntry("deflate.file")
				{
					CompressionMethod = CompressionMethod.Deflated,
				});

				Utils.WriteDummyData(zipOutputStream, contentLength, seed);
			}

			SevenZipHelper.VerifyZipWith7Zip(ms, null);
			foreach (var (name, content) in SevenZipHelper.GetZipContentsWith7Zip(ms, null))
			{
				switch (name)
				{
					case "deflate.file":
						Assert.That(content, Is.EqualTo(Utils.GetDummyBytes(contentLength, seed)));
						break;
					case "deflate64.file":
						Assert.That(content, Is.EqualTo(Utils.GetDummyBytes(contentLength, seed64)));
						break;
					default:
						Assert.Fail($"Unexpected file name {name}");
						break;
				};
			}
		}
	}
}
