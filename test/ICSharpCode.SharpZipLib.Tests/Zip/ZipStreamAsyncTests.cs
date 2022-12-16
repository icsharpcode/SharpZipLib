using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Tests.TestSupport;
using NUnit.Framework;

namespace ICSharpCode.SharpZipLib.Tests.Zip
{
	[TestFixture]
	public class ZipStreamAsyncTests
	{
		[Test]
		[Category("Zip")]
		[Category("Async")]
		public async Task WriteZipStreamUsingAsync()
		{
#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
			await using var ms = new MemoryStream();
			
			await using (var outStream = new ZipOutputStream(ms){IsStreamOwner = false})
			{
				await outStream.PutNextEntryAsync(new ZipEntry("FirstFile"));
				await Utils.WriteDummyDataAsync(outStream, 12);

				await outStream.PutNextEntryAsync(new ZipEntry("SecondFile"));
				await Utils.WriteDummyDataAsync(outStream, 12);
			}

			ZipTesting.AssertValidZip(ms);
#else
			await Task.CompletedTask;
			Assert.Ignore("Async Using is not supported");
#endif
		}

		[Test]
		[Category("Zip")]
		[Category("Async")]
		public async Task WriteZipStreamAsync ()
		{
			using var ms = new MemoryStream();

			using(var outStream = new ZipOutputStream(ms) { IsStreamOwner = false })
			{
				await outStream.PutNextEntryAsync(new ZipEntry("FirstFile"));
				await Utils.WriteDummyDataAsync(outStream, 12);

				await outStream.PutNextEntryAsync(new ZipEntry("SecondFile"));
				await Utils.WriteDummyDataAsync(outStream, 12);

				await outStream.FinishAsync(CancellationToken.None);
			}

			ZipTesting.AssertValidZip(ms);
		}
		
		
		[Test]
		[Category("Zip")]
		[Category("Async")]
		public async Task WriteZipStreamWithAesAsync()
		{
			using var ms = new MemoryStream();
			var password = "f4ls3p0s1t1v3";
			
			using (var outStream = new ZipOutputStream(ms){IsStreamOwner = false, Password = password})
			{
				await outStream.PutNextEntryAsync(new ZipEntry("FirstFile"){AESKeySize = 256});
				await Utils.WriteDummyDataAsync(outStream, 12);

				await outStream.PutNextEntryAsync(new ZipEntry("SecondFile"){AESKeySize = 256});
				await Utils.WriteDummyDataAsync(outStream, 12);
				
				await outStream.FinishAsync(CancellationToken.None);
			}
			
			ZipTesting.AssertValidZip(ms, password);
		}
		
		[Test]
		[Category("Zip")]
		[Category("Async")]
		public async Task WriteZipStreamWithZipCryptoAsync()
		{
			using var ms = new MemoryStream();
			var password = "f4ls3p0s1t1v3";
			
			using (var outStream = new ZipOutputStream(ms){IsStreamOwner = false, Password = password})
			{
				await outStream.PutNextEntryAsync(new ZipEntry("FirstFile"){AESKeySize = 0});
				await Utils.WriteDummyDataAsync(outStream, 12);

				await outStream.PutNextEntryAsync(new ZipEntry("SecondFile"){AESKeySize = 0});
				await Utils.WriteDummyDataAsync(outStream, 12);
				
				await outStream.FinishAsync(CancellationToken.None);
			}
			
			ZipTesting.AssertValidZip(ms, password, false);
		}

		[Test]
		[Category("Zip")]
		[Category("Async")]
		public async Task WriteReadOnlyZipStreamAsync ()
		{
			using var ms = new MemoryStreamWithoutSeek();

			using(var outStream = new ZipOutputStream(ms) { IsStreamOwner = false })
			{
				await outStream.PutNextEntryAsync(new ZipEntry("FirstFile"));
				await Utils.WriteDummyDataAsync(outStream, 12);

				await outStream.PutNextEntryAsync(new ZipEntry("SecondFile"));
				await Utils.WriteDummyDataAsync(outStream, 12);

				await outStream.FinishAsync(CancellationToken.None);
			}

			ZipTesting.AssertValidZip(new MemoryStream(ms.ToArray()));
		}

		[Test]
		[Category("Zip")]
		[Category("Async")]
		[TestCase(12, Description = "Small files")]
		[TestCase(12000, Description = "Large files")]
        public async Task WriteZipStreamToAsyncOnlyStream (int fileSize)
		{
#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
			await using(var ms = new MemoryStreamWithoutSync()){
				await using(var outStream = new ZipOutputStream(ms) { IsStreamOwner = false })
				{
					await outStream.PutNextEntryAsync(new ZipEntry("FirstFile"));
					await Utils.WriteDummyDataAsync(outStream, fileSize);

					await outStream.PutNextEntryAsync(new ZipEntry("SecondFile"));
					await Utils.WriteDummyDataAsync(outStream, fileSize);

					await outStream.FinishAsync(CancellationToken.None);
					await outStream.DisposeAsync();
				}

				ZipTesting.AssertValidZip(new MemoryStream(ms.ToArray()));
			}
#else
			await Task.CompletedTask;
			Assert.Ignore("AsyncDispose is not supported");
#endif
		}

	}
}