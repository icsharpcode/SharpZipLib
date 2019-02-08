using ICSharpCode.SharpZipLib.Checksum;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ICSharpCode.SharpZipLib.Tests.Checksum
{

	[TestFixture]
	public class Adler32Tests : ChecksumTestBase
	{
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
	}
}
