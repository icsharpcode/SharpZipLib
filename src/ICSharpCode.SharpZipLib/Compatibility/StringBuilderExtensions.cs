#if NET35
namespace System.Text
{
	static class StringBuilderExtensions
	{
		public static void Clear(this StringBuilder sb)
		{
			sb.Length = 0;
		}
	}
}
#endif
