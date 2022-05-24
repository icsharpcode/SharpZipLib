using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Tar;
using NUnit.Framework;

namespace ICSharpCode.SharpZipLib.Tests.Tar
{
	public class TarInputStreamTests
	{
		[Test]
		public void TestRead()
		{
			var entryBytes = new byte[2000];
			var r = new Random();
			r.NextBytes(entryBytes);
			using var ms = new MemoryStream();
			using (var tos = new TarOutputStream(ms, Encoding.UTF8) { IsStreamOwner = false })
			{
				var e = TarEntry.CreateTarEntry("some entry");
				e.Size = entryBytes.Length;
				tos.PutNextEntry(e);
				tos.Write(entryBytes, 0, entryBytes.Length);
				tos.CloseEntry();
			}

			ms.Seek(0, SeekOrigin.Begin);

			using var tis = new TarInputStream(ms, Encoding.UTF8);
			var entry = tis.GetNextEntry();
			Assert.AreEqual("some entry", entry.Name);
			var buffer = new byte[1000]; // smaller than 2 blocks
			var read0 = tis.Read(buffer, 0, buffer.Length);
			Assert.AreEqual(1000, read0);
			Assert.AreEqual(entryBytes.AsSpan(0, 1000).ToArray(), buffer);

			var read1 = tis.Read(buffer, 0, 5);
			Assert.AreEqual(5, read1);
			Assert.AreEqual(entryBytes.AsSpan(1000, 5).ToArray(), buffer.AsSpan().Slice(0, 5).ToArray());

			var read2 = tis.Read(buffer, 0, 20);
			Assert.AreEqual(20, read2);
			Assert.AreEqual(entryBytes.AsSpan(1005, 20).ToArray(), buffer.AsSpan().Slice(0, 20).ToArray());

			var read3 = tis.Read(buffer, 0, 975);
			Assert.AreEqual(975, read3);
			Assert.AreEqual(entryBytes.AsSpan(1025, 975).ToArray(), buffer.AsSpan().Slice(0, 975).ToArray());
		}

		[Test]
		public async Task TestReadAsync()
		{
			var entryBytes = new byte[2000];
			var r = new Random();
			r.NextBytes(entryBytes);
			using var ms = new MemoryStream();
			using (var tos = new TarOutputStream(ms, Encoding.UTF8) { IsStreamOwner = false })
			{
				var e = TarEntry.CreateTarEntry("some entry");
				e.Size = entryBytes.Length;
				await tos.PutNextEntryAsync(e, CancellationToken.None);
				await tos.WriteAsync(entryBytes, 0, entryBytes.Length);
				await tos.CloseEntryAsync(CancellationToken.None);
			}

			ms.Seek(0, SeekOrigin.Begin);

			using var tis = new TarInputStream(ms, Encoding.UTF8);
			var entry = await tis.GetNextEntryAsync(CancellationToken.None);
			Assert.AreEqual("some entry", entry.Name);
			var buffer = new byte[1000]; // smaller than 2 blocks
			var read0 = await tis.ReadAsync(buffer, 0, buffer.Length);
			Assert.AreEqual(1000, read0);
			Assert.AreEqual(entryBytes.AsSpan(0, 1000).ToArray(), buffer);

			var read1 = await tis.ReadAsync(buffer, 0, 5);
			Assert.AreEqual(5, read1);
			Assert.AreEqual(entryBytes.AsSpan(1000, 5).ToArray(), buffer.AsSpan().Slice(0, 5).ToArray());

			var read2 = await tis.ReadAsync(buffer, 0, 20);
			Assert.AreEqual(20, read2);
			Assert.AreEqual(entryBytes.AsSpan(1005, 20).ToArray(), buffer.AsSpan().Slice(0, 20).ToArray());

			var read3 = await tis.ReadAsync(buffer, 0, 975);
			Assert.AreEqual(975, read3);
			Assert.AreEqual(entryBytes.AsSpan(1025, 975).ToArray(), buffer.AsSpan().Slice(0, 975).ToArray());
		}
	}
}
