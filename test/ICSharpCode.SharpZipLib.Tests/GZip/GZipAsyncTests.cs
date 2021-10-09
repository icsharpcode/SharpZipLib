using System.IO;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tests.TestSupport;
using NUnit.Framework;

namespace ICSharpCode.SharpZipLib.Tests.GZip
{
	
#if NETCOREAPP3_1_OR_GREATER
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

			await using var ms = new MemoryStream();
			await using (var outStream = new GZipOutputStream(ms) { IsStreamOwner = false })
			{
				outStream.FileName = "/path/to/file.ext";
				outStream.Write(Encoding.ASCII.GetBytes(content));
			}

			ms.Seek(0, SeekOrigin.Begin);

			using (var inStream = new GZipInputStream(ms))
			{
				var readBuffer = new byte[content.Length];
				inStream.Read(readBuffer, 0, readBuffer.Length);
				Assert.AreEqual(content, Encoding.ASCII.GetString(readBuffer));
				Assert.AreEqual("file.ext", inStream.GetFilename());
			}
		}
	}
#endif
}
