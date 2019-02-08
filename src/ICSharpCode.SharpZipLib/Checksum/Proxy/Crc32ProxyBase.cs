using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ICSharpCode.SharpZipLib.Checksum.Proxy
{
	/* 
	 * This is heavily based on Crc32.NET: https://github.com/force-net/Crc32.NET/blob/develop/Crc32.NET/SafeProxy.cs
	 * Commit hash: fbc1061b0cb53df2322d5aed33167a2e6335970b: https://github.com/force-net/Crc32.NET/tree/fbc1061b0cb53df2322d5aed33167a2e6335970b
	 * 
	 * Then modified for BZIP2 CRC using the excellent CRC32 description from Michaelangel007: https://github.com/Michaelangel007/crc32
	 * 
	 * Original comment from CRC32.NET:
	 * This is .NET safe implementation of Crc32 algorithm.
	 * This implementation was investigated as fastest from different variants. It based on Robert Vazan native implementations of Crc32C
	 * Also, it is good for x64 and for x86, so, it seems, there is no sense to do 2 different realizations.
	 * 
	 * Addition: some speed increase was found with splitting xor to 4 independent blocks. Also, some attempts to optimize unaligned tails was unsuccessfull (JIT limitations?).
	 * 
	 * 
	 * Max Vysokikh, 2016-2017
	 */
	internal abstract class Crc32ProxyBase
	{
		private readonly uint[] _table = new uint[16 * 256];

		public Crc32ProxyBase()
		{
			Init();
		}

		protected abstract uint InitLookupValue(uint index);
		protected abstract uint CalculateLookupValue(uint lookupValue);
		protected abstract uint CalculateCrc(uint crc, byte input, uint[] lookupTable);



		private void Init()
		{
			var table = _table;
			for (uint i = 0; i < 256; i++)
			{
				uint lookupVal = InitLookupValue(i);
				for (int t = 0; t < 16; t++)
				{
					for (int k = 0; k < 8; k++) lookupVal = CalculateLookupValue(lookupVal);
					table[(t * 256) + i] = lookupVal;
				}
			}
		}



		public uint Append(uint crc, byte[] input, int offset, int length)
		{
			uint crcLocal = uint.MaxValue ^ crc;
			uint[] table = _table;

			while (length >= 16)
			{
				var a = table[(3 * 256) + input[offset + 12]]
					^ table[(2 * 256) + input[offset + 13]]
					^ table[(1 * 256) + input[offset + 14]]
					^ table[(0 * 256) + input[offset + 15]];

				var b = table[(7 * 256) + input[offset + 8]]
					^ table[(6 * 256) + input[offset + 9]]
					^ table[(5 * 256) + input[offset + 10]]
					^ table[(4 * 256) + input[offset + 11]];

				var c = table[(11 * 256) + input[offset + 4]]
					^ table[(10 * 256) + input[offset + 5]]
					^ table[(9 * 256) + input[offset + 6]]
					^ table[(8 * 256) + input[offset + 7]];

				var d = table[(15 * 256) + ((byte)crcLocal ^ input[offset])]
					^ table[(14 * 256) + ((byte)(crcLocal >> 8) ^ input[offset + 1])]
					^ table[(13 * 256) + ((byte)(crcLocal >> 16) ^ input[offset + 2])]
					^ table[(12 * 256) + ((crcLocal >> 24) ^ input[offset + 3])];

				crcLocal = d ^ c ^ b ^ a;
				offset += 16;
				length -= 16;
			}

			while (--length >= 0)
				crcLocal = CalculateCrc(crcLocal, input[offset++], table);

			return crcLocal ^ uint.MaxValue;
		}

	}
}
