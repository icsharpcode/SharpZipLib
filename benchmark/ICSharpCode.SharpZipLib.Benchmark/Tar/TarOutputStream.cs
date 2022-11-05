using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using ICSharpCode.SharpZipLib.Tar;

namespace ICSharpCode.SharpZipLib.Benchmark.Tar
{
	[MemoryDiagnoser]
	[Config(typeof(MultipleRuntimes))]
	public class TarOutputStream
	{
		private readonly byte[] backingArray = new byte[1024 * 1024 + (6 * 1024)];
		private readonly byte[] inputBuffer = new byte[1024];
		private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

		[Benchmark]
		public void WriteTarOutputStream()
		{
			using (var outputMemoryStream = new MemoryStream(backingArray))
			{
				using (var tarOutputStream =
				       new ICSharpCode.SharpZipLib.Tar.TarOutputStream(outputMemoryStream, Encoding.UTF8))
				{
					var tarEntry = TarEntry.CreateTarEntry("some file");
					tarEntry.Size = 1024 * 1024;
					tarOutputStream.PutNextEntry(tarEntry);

					_rng.GetBytes(inputBuffer);

					for (int i = 0; i < 1024; i++)
					{
						tarOutputStream.Write(inputBuffer, 0, inputBuffer.Length);
					}
				}
			}
		}

		[Benchmark]
		public async Task WriteTarOutputStreamAsync()
		{
			using (var outputMemoryStream = new MemoryStream(backingArray))
			{
				using (var tarOutputStream =
				       new ICSharpCode.SharpZipLib.Tar.TarOutputStream(outputMemoryStream, Encoding.UTF8))
				{
					var tarEntry = TarEntry.CreateTarEntry("some file");
					tarEntry.Size = 1024 * 1024;

					await tarOutputStream.PutNextEntryAsync(tarEntry, CancellationToken.None);

					_rng.GetBytes(inputBuffer);

					for (int i = 0; i < 1024; i++)
					{
						await tarOutputStream.WriteAsync(inputBuffer, 0, inputBuffer.Length);
					}
				}
			}
		}
	}
}
