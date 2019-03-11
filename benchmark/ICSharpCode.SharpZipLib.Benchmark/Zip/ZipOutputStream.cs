﻿using System;
using System.IO;
using BenchmarkDotNet.Attributes;

namespace ICSharpCode.SharpZipLib.Benchmark.Zip
{
	[Config(typeof(MultipleRuntimes))]
	public class ZipOutputStream
	{
		private const int ChunkCount = 64;
		private const int ChunkSize = 1024 * 1024;
		private const int N = ChunkCount * ChunkSize;

		byte[] outputBuffer;
		byte[] inputBuffer;

		public ZipOutputStream()
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
	}
}
