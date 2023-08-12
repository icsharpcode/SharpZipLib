using System.IO;
using BenchmarkDotNet.Attributes;

namespace ICSharpCode.SharpZipLib.Benchmark.Zip
{
	[MemoryDiagnoser]
	[Config(typeof(MultipleRuntimes))]
	public class ZipInputStream
	{
		private const int ChunkCount = 64;
		private const int ChunkSize = 1024 * 1024;
		private const int N = ChunkCount * ChunkSize;

		byte[] zippedData;
		byte[] readBuffer = new byte[4096];

		[GlobalSetup]
		public void GlobalSetup()
		{
			using (var memoryStream = new MemoryStream())
			{
				using (var zipOutputStream = new SharpZipLib.Zip.ZipOutputStream(memoryStream))
				{
					zipOutputStream.PutNextEntry(new SharpZipLib.Zip.ZipEntry("0"));

					var inputBuffer = new byte[ChunkSize];

					for (int i = 0; i < ChunkCount; i++)
					{
						zipOutputStream.Write(inputBuffer, 0, inputBuffer.Length);
					}
				}

				zippedData = memoryStream.ToArray();
			}
		}

		[Benchmark]
		public long ReadZipInputStream()
		{
			using (var memoryStream = new MemoryStream(zippedData))
			{
				using (var zipInputStream = new SharpZipLib.Zip.ZipInputStream(memoryStream))
				{
					var entry = zipInputStream.GetNextEntry();

					while (zipInputStream.Read(readBuffer, 0, readBuffer.Length) > 0)
					{

					}

					return entry.Size;
				}
			}
		}
	}
}
