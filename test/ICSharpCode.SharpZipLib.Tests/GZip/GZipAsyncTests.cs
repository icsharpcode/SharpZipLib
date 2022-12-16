﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tests.TestSupport;
using NUnit.Framework;

namespace ICSharpCode.SharpZipLib.Tests.GZip
{
	[TestFixture]
	public class GZipAsyncTests
	{
		[Test]
		[Category("GZip")]
		[Category("Async")]
		public async Task SmallBufferDecompressionAsync([Values(0, 1, 3)] int seed)
		{
			var outputBufferSize = 100000;
			var outputBuffer = new byte[outputBufferSize];
			var inputBuffer = Utils.GetDummyBytes(outputBufferSize * 4, seed);
			
#if NETCOREAPP3_1_OR_GREATER
			await using var msGzip = new MemoryStream();
			await using (var gzos = new GZipOutputStream(msGzip){IsStreamOwner = false})
			{
				await gzos.WriteAsync(inputBuffer, 0, inputBuffer.Length);
			}

			msGzip.Seek(0, SeekOrigin.Begin);

			using (var gzis = new GZipInputStream(msGzip))
			await using (var msRaw = new MemoryStream())
			{
				int readOut;
				while ((readOut = gzis.Read(outputBuffer, 0, outputBuffer.Length)) > 0)
				{
					await msRaw.WriteAsync(outputBuffer, 0, readOut);
				}

				var resultBuffer = msRaw.ToArray();
				for (var i = 0; i < resultBuffer.Length; i++)
				{
					Assert.AreEqual(inputBuffer[i], resultBuffer[i]);
				}
			}
#else
			using var msGzip = new MemoryStream();
			using (var gzos = new GZipOutputStream(msGzip){IsStreamOwner = false})
			{
				await gzos.WriteAsync(inputBuffer, 0, inputBuffer.Length);
			}

			msGzip.Seek(0, SeekOrigin.Begin);

			using (var gzis = new GZipInputStream(msGzip))
			using (var msRaw = new MemoryStream())
			{
				int readOut;
				while ((readOut = gzis.Read(outputBuffer, 0, outputBuffer.Length)) > 0)
				{
					await msRaw.WriteAsync(outputBuffer, 0, readOut);
				}

				var resultBuffer = msRaw.ToArray();
				for (var i = 0; i < resultBuffer.Length; i++)
				{
					Assert.AreEqual(inputBuffer[i], resultBuffer[i]);
				}
			}
#endif
		}
		
		/// <summary>
		/// Basic compress/decompress test
		/// </summary>
		[Test]
		[Category("GZip")]
		[Category("Async")]
		public async Task OriginalFilenameAsync()
		{
			var content = "FileContents";

#if NETCOREAPP3_1_OR_GREATER
			await using var ms = new MemoryStream();
			await using (var outStream = new GZipOutputStream(ms) { IsStreamOwner = false })
			{
				outStream.FileName = "/path/to/file.ext";
				outStream.Write(Encoding.ASCII.GetBytes(content));
			}
#else
			var ms = new MemoryStream();
			var outStream = new GZipOutputStream(ms){ IsStreamOwner = false };
			outStream.FileName = "/path/to/file.ext";
			var bytes = Encoding.ASCII.GetBytes(content);
			outStream.Write(bytes, 0, bytes.Length);
			await outStream.FinishAsync(System.Threading.CancellationToken.None);
			outStream.Dispose();
			
#endif
			ms.Seek(0, SeekOrigin.Begin);

			using (var inStream = new GZipInputStream(ms))
			{
				var readBuffer = new byte[content.Length];
				inStream.Read(readBuffer, 0, readBuffer.Length);
				Assert.AreEqual(content, Encoding.ASCII.GetString(readBuffer));
				Assert.AreEqual("file.ext", inStream.GetFilename());
			}
		}

		/// <summary>
		/// Test creating an empty gzip stream using async
		/// </summary>
		[Test]
		[Category("GZip")]
		[Category("Async")]
		public async Task EmptyGZipStreamAsync()
		{
#if NETCOREAPP3_1_OR_GREATER
			await using var ms = new MemoryStream();
			await using (var outStream = new GZipOutputStream(ms) { IsStreamOwner = false })
			{
				// No content
			}
#else
			var ms = new MemoryStream();
			var outStream = new GZipOutputStream(ms){ IsStreamOwner = false };
			await outStream.FinishAsync(System.Threading.CancellationToken.None);
			outStream.Dispose();

#endif
			ms.Seek(0, SeekOrigin.Begin);

			using (var inStream = new GZipInputStream(ms))
			using (var reader = new StreamReader(inStream))
			{
				var content = await reader.ReadToEndAsync();
				Assert.IsEmpty(content);
			}
		}

		[Test]
		[Category("GZip")]
		[Category("Async")]
		public async Task WriteGZipStreamToAsyncOnlyStream()
		{
#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
			var content = Encoding.ASCII.GetBytes("a");
			var modTime = DateTime.UtcNow;

            await using (var msAsync = new MemoryStreamWithoutSync())
			{
				await using (var outStream = new GZipOutputStream(msAsync) { IsStreamOwner = false })
				{
					outStream.ModifiedTime = modTime;
					await outStream.WriteAsync(content);
				}

				using var msSync = new MemoryStream();
                using (var outStream = new GZipOutputStream(msSync) { IsStreamOwner = false })
                {
                    outStream.ModifiedTime = modTime;
                    outStream.Write(content);
                }

				var syncBytes = string.Join(' ', msSync.ToArray());
				var asyncBytes = string.Join(' ', msAsync.ToArray());

                Assert.AreEqual(syncBytes, asyncBytes, "Sync and Async compressed streams are not equal");

                // Since GZipInputStream isn't async yet we need to read from it from a regular MemoryStream
                using (var readStream = new MemoryStream(msAsync.ToArray()))
				using (var inStream = new GZipInputStream(readStream))
				using (var reader = new StreamReader(inStream))
				{
					Assert.AreEqual(content, await reader.ReadToEndAsync());
				}
			}
#else
			await Task.CompletedTask;
			Assert.Ignore("AsyncDispose is not supported");
#endif
		}
	}
}
