using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Tar;
using NUnit.Framework;

namespace ICSharpCode.SharpZipLib.Tests.Tar
{
	[TestFixture]
	public class TarBufferTests
	{
		[Test]
		public void TestSimpleReadWrite()
		{
			var ms = new MemoryStream();
			var reader = TarBuffer.CreateInputTarBuffer(ms, 1);
			var writer = TarBuffer.CreateOutputTarBuffer(ms, 1);
			writer.IsStreamOwner = false;

			var block = new byte[TarBuffer.BlockSize];
			var r = new Random();
			r.NextBytes(block);

			writer.WriteBlock(block);
			writer.WriteBlock(block);
			writer.WriteBlock(block);
			writer.Close();

			ms.Seek(0, SeekOrigin.Begin);

			var block0 = reader.ReadBlock();
			var block1 = reader.ReadBlock();
			var block2 = reader.ReadBlock();
			Assert.AreEqual(block, block0);
			Assert.AreEqual(block, block1);
			Assert.AreEqual(block, block2);
			writer.Close();
		}

		[Test]
		public void TestSkipBlock()
		{
			var ms = new MemoryStream();
			var reader = TarBuffer.CreateInputTarBuffer(ms, 1);
			var writer = TarBuffer.CreateOutputTarBuffer(ms, 1);
			writer.IsStreamOwner = false;

			var block0 = new byte[TarBuffer.BlockSize];
			var block1 = new byte[TarBuffer.BlockSize];
			var r = new Random();
			r.NextBytes(block0);
			r.NextBytes(block1);

			writer.WriteBlock(block0);
			writer.WriteBlock(block1);
			writer.Close();

			ms.Seek(0, SeekOrigin.Begin);

			reader.SkipBlock();
			var block = reader.ReadBlock();
			Assert.AreEqual(block, block1);
			writer.Close();
		}

		[Test]
		public async Task TestSimpleReadWriteAsync()
		{
			var ms = new MemoryStream();
			var reader = TarBuffer.CreateInputTarBuffer(ms, 1);
			var writer = TarBuffer.CreateOutputTarBuffer(ms, 1);
			writer.IsStreamOwner = false;

			var block = new byte[TarBuffer.BlockSize];
			var r = new Random();
			r.NextBytes(block);

			await writer.WriteBlockAsync(block, CancellationToken.None);
			await writer.WriteBlockAsync(block, CancellationToken.None);
			await writer.WriteBlockAsync(block, CancellationToken.None);
			await writer.CloseAsync(CancellationToken.None);

			ms.Seek(0, SeekOrigin.Begin);

			var block0 = new byte[TarBuffer.BlockSize];
			await reader.ReadBlockIntAsync(block0, CancellationToken.None, true);
			var block1 = new byte[TarBuffer.BlockSize];
			await reader.ReadBlockIntAsync(block1, CancellationToken.None, true);
			var block2 = new byte[TarBuffer.BlockSize];
			await reader.ReadBlockIntAsync(block2, CancellationToken.None, true);
			Assert.AreEqual(block, block0);
			Assert.AreEqual(block, block1);
			Assert.AreEqual(block, block2);
			await writer.CloseAsync(CancellationToken.None);
		}

		[Test]
		public async Task TestSkipBlockAsync()
		{
			var ms = new MemoryStream();
			var reader = TarBuffer.CreateInputTarBuffer(ms, 1);
			var writer = TarBuffer.CreateOutputTarBuffer(ms, 1);
			writer.IsStreamOwner = false;

			var block0 = new byte[TarBuffer.BlockSize];
			var block1 = new byte[TarBuffer.BlockSize];
			var r = new Random();
			r.NextBytes(block0);
			r.NextBytes(block1);

			await writer.WriteBlockAsync(block0, CancellationToken.None);
			await writer.WriteBlockAsync(block1, CancellationToken.None);
			await writer.CloseAsync(CancellationToken.None);

			ms.Seek(0, SeekOrigin.Begin);

			await reader.SkipBlockAsync(CancellationToken.None);
			var block = new byte[TarBuffer.BlockSize];
			await reader.ReadBlockIntAsync(block, CancellationToken.None, true);
			Assert.AreEqual(block, block1);
			await writer.CloseAsync(CancellationToken.None);
		}
	}
}
