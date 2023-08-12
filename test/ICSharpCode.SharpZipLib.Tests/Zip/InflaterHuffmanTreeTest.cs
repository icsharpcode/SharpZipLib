using System;
using NUnit.Framework;

namespace ICSharpCode.SharpZipLib.Tests.Zip
{
	public class InflaterHuffmanTreeTest
	{
		/// <summary>
		/// Generates code based on optimization described in https://github.com/dotnet/csharplang/issues/5295#issue-1028421234
		/// </summary>
		[Test]
		[Explicit]
		public void GenerateTrees()
		{
			// generates the byte arrays needed by InflaterHuffmanTree
			var defLitLenTreeBytes = new byte[288];
			int i = 0;
			while (i < 144)
			{
				defLitLenTreeBytes[i++] = 8;
			}

			while (i < 256)
			{
				defLitLenTreeBytes[i++] = 9;
			}

			while (i < 280)
			{
				defLitLenTreeBytes[i++] = 7;
			}

			while (i < 288)
			{
				defLitLenTreeBytes[i++] = 8;
			}

			Console.WriteLine($"private static ReadOnlySpan<byte> defLitLenTreeBytes => new byte[] {{ { string.Join(", ",  defLitLenTreeBytes) } }};");


			var defDistTreeBytes = new byte[32];
			i = 0;
			while (i < 32)
			{
				defDistTreeBytes[i++] = 5;
			}

			Console.WriteLine($"private static ReadOnlySpan<byte> defDistTreeBytes => new byte[] {{ { string.Join(", ",  defDistTreeBytes) } }};");
		}
	}
}
