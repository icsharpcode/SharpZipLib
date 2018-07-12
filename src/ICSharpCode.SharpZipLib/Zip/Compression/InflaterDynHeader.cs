using System;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace ICSharpCode.SharpZipLib.Zip.Compression
{
	class InflaterDynHeader
	{
		#region Constants

		static readonly int[] BL_ORDER =
		{ 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };
		#endregion

		public bool Decode(StreamManipulator input)
		{
			try
			{
				lnum = input.GrabBits(5) + 257;
				dnum = input.GrabBits(5) + 1;
				blnum = input.GrabBits(4) + 4;
				num = lnum + dnum;

				lengths = new byte[19];

				for (int i = 0; i < blnum; i++)
				{
					lengths[BL_ORDER[i]] = (byte)input.GrabBits(3, true);
				}
				blTree = new InflaterHuffmanTree(lengths);
				lengths = new byte[num];

				int index = 0;
				while (index < lnum + dnum)
				{
					byte len;

					int symbol = blTree.GetSymbol(input);
					if (symbol < 0)
						return false;
					if (symbol < 16)
						lengths[index++] = (byte)symbol;
					else
					{
						len = 0;
						if (symbol == 16)
						{
							if (index == 0)
								return false;   // No last length!
							len = lengths[index - 1];
							symbol = input.GrabBits(2, true) + 3;
						}
						else if (symbol == 17)
						{
							// repeat zero 3..10 times
							symbol = input.GrabBits(3, true) + 3;
						}
						else
						{
							// (symbol == 18), repeat zero 11..138 times
							symbol = input.GrabBits(7, true) + 11;
						}

						if (index + symbol > lnum + dnum)
							return false; // too many lengths!

						// repeat last or zero symbol times
						while (symbol-- > 0)
							lengths[index++] = len;
					}
				}

				if (lengths[256] == 0)
					return false; // No end-of-block code!

				return true;
			}
			catch (Exception x)
			{
				return false;
			}
		}

		public InflaterHuffmanTree BuildLitLenTree()
		{
			byte[] litlenLens = new byte[lnum];
			Array.Copy(lengths, 0, litlenLens, 0, lnum);
			return new InflaterHuffmanTree(litlenLens);
		}

		public InflaterHuffmanTree BuildDistTree()
		{
			byte[] distLens = new byte[dnum];
			Array.Copy(lengths, lnum, distLens, 0, dnum);
			return new InflaterHuffmanTree(distLens);
		}

		#region Instance Fields
		byte[] lengths;

		InflaterHuffmanTree blTree;

		int lnum, dnum, blnum, num;
		#endregion

	}
}
