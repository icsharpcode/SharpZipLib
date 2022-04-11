using System;
using System.Buffers;

namespace ICSharpCode.SharpZipLib.Core
{
	/// <summary>
	/// A MemoryPool that will return a Memory which is exactly the length asked for using the bufferSize parameter.
	/// This is in contrast to the default ArrayMemoryPool which will return a Memory of equal size to the underlying
	/// array which at least as long as the minBufferSize parameter.
	/// Note: The underlying array may be larger than the slice of Memory
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal sealed class ExactMemoryPool<T> : MemoryPool<T>
	{
		public new static readonly MemoryPool<T> Shared = new ExactMemoryPool<T>();

		public override IMemoryOwner<T> Rent(int bufferSize = -1)
		{
			if ((uint)bufferSize > int.MaxValue || bufferSize < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(bufferSize));
			}

			return new ExactMemoryPoolBuffer(bufferSize);
		}

		protected override void Dispose(bool disposing)
		{
		}

		public override int MaxBufferSize => int.MaxValue;

		private sealed class ExactMemoryPoolBuffer : IMemoryOwner<T>, IDisposable
		{
			private T[] array;
			private readonly int size;

			public ExactMemoryPoolBuffer(int size)
			{
				this.size = size;
				this.array = ArrayPool<T>.Shared.Rent(size);
			}

			public Memory<T> Memory
			{
				get
				{
					T[] array = this.array;
					if (array == null)
					{
						throw new ObjectDisposedException(nameof(ExactMemoryPoolBuffer));
					}

					return new Memory<T>(array).Slice(0, size);
				}
			}

			public void Dispose()
			{
				T[] array = this.array;
				if (array == null)
				{
					return;
				}

				this.array = null;
				ArrayPool<T>.Shared.Return(array);
			}
		}
	}
}
