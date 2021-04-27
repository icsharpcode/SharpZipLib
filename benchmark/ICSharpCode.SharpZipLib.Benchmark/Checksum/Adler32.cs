using System;
using BenchmarkDotNet.Attributes;

namespace ICSharpCode.SharpZipLib.Benchmark.Checksum
{
	[Config(typeof(MultipleRuntimes))]
	public class Adler32
	{
		private const int ChunkCount = 256;
		private const int ChunkSize = 1024 * 1024;
		private const int N = ChunkCount * ChunkSize;
		private readonly byte[] data;

		public Adler32()
		{
			data = new byte[N];
			new Random(1).NextBytes(data);
		}

		[Benchmark]
		public long Adler32LargeUpdate()
		{
			var adler32 = new ICSharpCode.SharpZipLib.Checksum.Adler32();
			adler32.Update(data);
			return adler32.Value;
		}

		/*
		[Benchmark]
		public long Adler32ChunkedUpdate()
		{
			var adler32 = new ICSharpCode.SharpZipLib.Checksum.Adler32();

			for (int i = 0; i < ChunkCount; i++)
			{
				var segment = new ArraySegment<byte>(data, ChunkSize * i, ChunkSize);
				adler32.Update(segment);
			}

			return adler32.Value;
		}
		*/
	}
}
