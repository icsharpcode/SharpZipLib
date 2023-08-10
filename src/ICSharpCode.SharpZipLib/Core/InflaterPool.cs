using System;
using System.Collections.Concurrent;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace ICSharpCode.SharpZipLib.Core
{
	/// <summary>
	/// Pool Inflator instances as they can be costly due to byte array allocations.
	/// </summary>
	internal sealed class InflaterPool
	{
		public static InflaterPool Instance { get; } = new InflaterPool();
		private readonly ConcurrentQueue<PooledInflater> noHeaderPool = new ConcurrentQueue<PooledInflater>();
		private readonly ConcurrentQueue<PooledInflater> headerPool = new ConcurrentQueue<PooledInflater>();

		private InflaterPool()
		{
		}

		public Inflater Rent(bool noHeader = false)
		{
			var pool = GetPool(noHeader);
			var inf = pool.TryDequeue(out var inflater) ? inflater : new PooledInflater(noHeader);
			inf.Reset();
			return inf;
		}

		public void Return(Inflater inflater)
		{
			if (!(inflater is PooledInflater pooledInflater))
			{
				throw new ArgumentException("Returned inflater was not a pooled one");
			}

			var pool = GetPool(inflater.noHeader);
			if (pool.Count < 10)
			{
				pooledInflater.Reset();
				pool.Enqueue(pooledInflater);
			}
		}

		private ConcurrentQueue<PooledInflater> GetPool(bool noHeader) => noHeader ? noHeaderPool : headerPool;
	}
}
