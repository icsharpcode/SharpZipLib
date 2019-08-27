using System;
using BenchmarkDotNet.Attributes;

namespace ICSharpCode.SharpZipLib.Benchmark.Checksum
{
	[Config(typeof(MultipleRuntimes))]
	public class Crc32
	{
		private const int ChunkCount = 256;
		private const int ChunkSize = 1024 * 1024;
		private const int N = ChunkCount * ChunkSize;
		private readonly byte[] data;

		public Crc32()
		{
			data = new byte[N];
			new Random(1).NextBytes(data);
		}

		[Benchmark]
		public long Crc32LargeUpdate()
		{
			var crc32 = new ICSharpCode.SharpZipLib.Checksum.Crc32();
			crc32.Update(data);
			return crc32.Value;
		}

		/*
		[Benchmark]
		public long Crc32ChunkedUpdate()
		{
			var crc32 = new ICSharpCode.SharpZipLib.Checksum.Crc32();

			for (int i = 0; i < ChunkCount; i++)
			{
				var segment = new ArraySegment<byte>(data, ChunkSize * i, ChunkSize);
				crc32.Update(segment);
			}

			return crc32.Value;
		}
		*/
	}
}
