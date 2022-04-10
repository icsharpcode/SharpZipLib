using System.IO;
using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Attributes;
using ICSharpCode.SharpZipLib.Tar;

namespace ICSharpCode.SharpZipLib.Benchmark.Tar
{
	[MemoryDiagnoser]
	[Config(typeof(MultipleRuntimes))]
	public class TarInputStream
	{
		private readonly byte[] archivedData;
		private readonly byte[] readBuffer = new byte[1024];

		public TarInputStream()
		{
			using (var outputMemoryStream = new MemoryStream())
			{
				using (var zipOutputStream = new ICSharpCode.SharpZipLib.Tar.TarOutputStream(outputMemoryStream, Encoding.UTF8))
				{
					var tarEntry = TarEntry.CreateTarEntry("some file");
					tarEntry.Size = 1024 * 1024;
					zipOutputStream.PutNextEntry(tarEntry);

					var rng = RandomNumberGenerator.Create();
					var inputBuffer = new byte[1024];
					rng.GetBytes(inputBuffer);

					for (int i = 0; i < 1024; i++)
					{
						zipOutputStream.Write(inputBuffer, 0, inputBuffer.Length);
					}
				}

				archivedData = outputMemoryStream.ToArray();
			}
		}

		[Benchmark]
		public long ReadTarInputStream()
		{
			using (var memoryStream = new MemoryStream(archivedData))
			using(var zipInputStream = new ICSharpCode.SharpZipLib.Tar.TarInputStream(memoryStream, Encoding.UTF8))
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
