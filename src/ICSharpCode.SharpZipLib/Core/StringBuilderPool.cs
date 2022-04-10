using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.SharpZipLib.Core
{
	class StringBuilderPool
	{
		public static StringBuilderPool Instance { get; } = new StringBuilderPool();
		private readonly Queue<StringBuilder> pool = new Queue<StringBuilder>();

		public StringBuilder Rent()
		{
			return pool.Count > 0 ? pool.Dequeue() : new StringBuilder();
		}

		public void Return(StringBuilder builder)
		{
			builder.Clear();
			pool.Enqueue(builder);
		}
	}
}
