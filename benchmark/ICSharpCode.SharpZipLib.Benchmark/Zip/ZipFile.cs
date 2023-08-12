using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using ICSharpCode.SharpZipLib.Zip;

namespace ICSharpCode.SharpZipLib.Benchmark.Zip
{
	[MemoryDiagnoser]
	[Config(typeof(MultipleRuntimes))]
	public class ZipFile
	{
		private readonly byte[] readBuffer = new byte[4096];
		private string zipFileWithLargeAmountOfEntriesPath;

		[GlobalSetup]
		public async Task GlobalSetup()
		{
			SharpZipLibOptions.InflaterPoolSize = 4;

			// large real-world test file from test262 repository
			string commitSha = "2e4e0e6b8ebe3348a207144204cb6d7a5571c863";
			zipFileWithLargeAmountOfEntriesPath = Path.Combine(Path.GetTempPath(), $"{commitSha}.zip");
			if (!File.Exists(zipFileWithLargeAmountOfEntriesPath))
			{
				var uri = $"https://github.com/tc39/test262/archive/{commitSha}.zip";

				Console.WriteLine("Loading test262 repository archive from {0}", uri);

				using (var client = new HttpClient())
				{
					using (var downloadStream = await client.GetStreamAsync(uri))
					{
						using (var writeStream = File.OpenWrite(zipFileWithLargeAmountOfEntriesPath))
						{
							await downloadStream.CopyToAsync(writeStream);
							Console.WriteLine("File downloaded and saved to {0}", zipFileWithLargeAmountOfEntriesPath);
						}
					}
				}
			}

		}

		[Benchmark]
		public void ReadLargeZipFile()
		{
			using (var file = new SharpZipLib.Zip.ZipFile(zipFileWithLargeAmountOfEntriesPath))
			{
				foreach (ZipEntry entry in file)
				{
					using (var stream = file.GetInputStream(entry))
					{
						while (stream.Read(readBuffer, 0, readBuffer.Length) > 0)
						{
						}
					}
				}
			}
		}
	}
}
