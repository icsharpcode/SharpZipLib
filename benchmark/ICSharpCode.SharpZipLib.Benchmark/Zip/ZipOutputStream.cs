using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace ICSharpCode.SharpZipLib.Benchmark.Zip
{
	[MemoryDiagnoser]
	[Config(typeof(MultipleRuntimes))]
	public class ZipOutputStream
	{
		private const int ChunkCount = 64;
		private const int ChunkSize = 1024 * 1024;
		private const int N = ChunkCount * ChunkSize;

		byte[] outputBuffer;
		byte[] inputBuffer;

		[GlobalSetup]
		public void GlobalSetup()
		{
			inputBuffer = new byte[ChunkSize];
			outputBuffer = new byte[N];
		}

		[Benchmark]
		public long WriteZipOutputStream()
		{
			using (var memoryStream = new MemoryStream(outputBuffer))
			{
				var zipOutputStream = new SharpZipLib.Zip.ZipOutputStream(memoryStream);
				zipOutputStream.PutNextEntry(new SharpZipLib.Zip.ZipEntry("0"));

				for (int i = 0; i < ChunkCount; i++)
				{
					zipOutputStream.Write(inputBuffer, 0, inputBuffer.Length);
				}

				return memoryStream.Position;
			}
		}

		[Benchmark]
		public async Task<long> WriteZipOutputStreamAsync()
		{
			using (var memoryStream = new MemoryStream(outputBuffer))
			{
				using (var zipOutputStream = new SharpZipLib.Zip.ZipOutputStream(memoryStream))
				{
					zipOutputStream.IsStreamOwner = false;
					zipOutputStream.PutNextEntry(new SharpZipLib.Zip.ZipEntry("0"));

					for (int i = 0; i < ChunkCount; i++)
					{
						await zipOutputStream.WriteAsync(inputBuffer, 0, inputBuffer.Length);
					}
				}

				return memoryStream.Position;
			}
		}
	}
}
