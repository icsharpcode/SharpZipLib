using System.Linq;
using System.Collections.Generic;

namespace System
{
	class ArraySegmentWrapper<T> : IList<T>
	{
		private readonly ArraySegment<T> segment;

		public ArraySegmentWrapper(ArraySegment<T> segment)
		{
			this.segment = segment;
		}

		public ArraySegmentWrapper(T[] array, int offset, int count)
			: this(new ArraySegment<T>(array, offset, count))
		{
		}

		public int IndexOf(T item)
		{
			for (int i = segment.Offset; i < segment.Offset + segment.Count; i++)
				if (Equals(segment.Array[i], item))
					return i;
			return -1;
		}

		public void Insert(int index, T item)
		{
			throw new NotSupportedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		public T this[int index]
		{
			get
			{
				if (index >= this.Count)
					throw new IndexOutOfRangeException();
				return this.segment.Array[index + this.segment.Offset];
			}
			set
			{
				if (index >= this.Count)
					throw new IndexOutOfRangeException();
				this.segment.Array[index + this.segment.Offset] = value;
			}
		}

		public void Add(T item)
		{
			throw new NotSupportedException();
		}

		public void Clear()
		{
			throw new NotSupportedException();
		}

		public bool Contains(T item)
		{
			return this.IndexOf(item) != -1;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			for (int i = segment.Offset; i < segment.Offset + segment.Count; i++)
			{
				array[arrayIndex] = segment.Array[i];
				arrayIndex++;
			}
		}

		public int Count
		{
			get { return this.segment.Count; }
		}

		public bool IsReadOnly => false;

		public bool Remove(T item) => throw new NotSupportedException();

		public IEnumerator<T> GetEnumerator()
		{
			for (int i = segment.Offset; i < segment.Offset + segment.Count; i++)
				yield return segment.Array[i];
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	static class ArraySegmentWrapper
	{
		public static ArraySegment<T> GetSegment<T>(this T[] array, int from, int count)
		{
			return new ArraySegment<T>(array, from, count);
		}

		public static ArraySegment<T> GetSegment<T>(this T[] array, int from)
		{
			return GetSegment(array, from, array.Length - from);
		}

		public static ArraySegment<T> GetSegment<T>(this T[] array)
		{
			return new ArraySegment<T>(array);
		}

		public static IList<T> ToList<T>(this ArraySegment<T> arraySegment)
		{
#if NET35
			return new ArraySegmentWrapper<T>(arraySegment);
#else
			return arraySegment;
#endif
		}

		public static IEnumerable<T> AsEnumerable<T>(this ArraySegment<T> arraySegment)
		{
			return arraySegment.Array.Skip(arraySegment.Offset).Take(arraySegment.Count);
		}

		public static T[] ToArray<T>(this ArraySegment<T> arraySegment)
		{
			T[] array = new T[arraySegment.Count];
			Array.Copy(arraySegment.Array, arraySegment.Offset, array, 0, arraySegment.Count);
			return array;
		}
	}
}

