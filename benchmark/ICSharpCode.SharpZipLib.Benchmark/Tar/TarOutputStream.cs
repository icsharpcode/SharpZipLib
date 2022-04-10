using System.IO;
using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Attributes;
using ICSharpCode.SharpZipLib.Tar;

namespace ICSharpCode.SharpZipLib.Benchmark.Tar
{
	[MemoryDiagnoser]
	[Config(typeof(MultipleRuntimes))]
	public class TarOutputStream
	{
		private readonly byte[] _backingArray = new byte[1024 * 1024 + (6 * 1024)];

		[Benchmark]
		public void WriteTarOutputStream()
		{
			using (var outputMemoryStream = new MemoryStream(_backingArray))
			{
				using (var tarOutputStream =
				       new ICSharpCode.SharpZipLib.Tar.TarOutputStream(outputMemoryStream, Encoding.UTF8))
				{
					var tarEntry = TarEntry.CreateTarEntry("some file");
					tarEntry.Size = 1024 * 1024;
					tarOutputStream.PutNextEntry(tarEntry);

					var rng = RandomNumberGenerator.Create();
					var inputBuffer = new byte[1024];
					rng.GetBytes(inputBuffer);

					for (int i = 0; i < 1024; i++)
					{
						tarOutputStream.Write(inputBuffer, 0, inputBuffer.Length);
					}
				}
			}
		}
	}
}
