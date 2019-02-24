using System;
using BenchmarkDotNet.Attributes;

namespace ICSharpCode.SharpZipLib.Benchmark.Checksum
{
	[Config(typeof(MultipleRuntimes))]
	public class BZip2Crc
	{
		private const int ChunkCount = 256;
		private const int ChunkSize = 1024 * 1024;
		private const int N = ChunkCount * ChunkSize;
		private readonly byte[] data;

		public BZip2Crc()
		{
			data = new byte[N];
			new Random(1).NextBytes(data);
		}

		[Benchmark]
		public long BZip2CrcLargeUpdate()
		{
			var bzipCrc = new ICSharpCode.SharpZipLib.Checksum.BZip2Crc();
			bzipCrc.Update(data);
			return bzipCrc.Value;
		}

		/*
		[Benchmark]
		public long BZip2CrcChunkedUpdate()
		{
			var bzipCrc = new ICSharpCode.SharpZipLib.Checksum.BZip2Crc();

			for (int i = 0; i < ChunkCount; i++)
			{
				var segment = new ArraySegment<byte>(data, ChunkSize * i, ChunkSize);
				bzipCrc.Update(segment);
			}

			return bzipCrc.Value;
		}
		*/
	}
}
