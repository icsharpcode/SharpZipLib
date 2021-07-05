using System;
using System.IO;
using BenchmarkDotNet.Attributes;

namespace ICSharpCode.SharpZipLib.Benchmark.BZip2
{
	[Config(typeof(MultipleRuntimes))]
	public class BZip2InputStream
	{
		private byte[] compressedData;

		public BZip2InputStream()
		{
			var outputMemoryStream = new MemoryStream();
			using (var outputStream = new SharpZipLib.BZip2.BZip2OutputStream(outputMemoryStream))
			{
				var random = new Random(1234);
				var inputData = new byte[1024 * 1024 * 30];
				random.NextBytes(inputData);
				var inputMemoryStream = new MemoryStream(inputData);
				inputMemoryStream.CopyTo(outputStream);
			}

			compressedData = outputMemoryStream.ToArray();
		}

		[Benchmark]
		public void DecompressData()
		{
			var memoryStream = new MemoryStream(compressedData);
			using (var inputStream = new SharpZipLib.BZip2.BZip2InputStream(memoryStream))
			{
				inputStream.CopyTo(Stream.Null);
			}
		}
	}
}
