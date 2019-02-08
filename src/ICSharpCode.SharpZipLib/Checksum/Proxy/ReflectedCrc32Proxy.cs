using System;
using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.SharpZipLib.Checksum.Proxy
{
	internal class ReflectedCrc32Proxy : Crc32ProxyBase
	{

		private readonly uint _poly = 0xEDB88320u;


		protected override uint CalculateCrc(uint crc, byte input, uint[] lookupTable)
		{
			return lookupTable[(byte)(crc ^ input)] ^ (crc >> 8);
		}

		protected override uint CalculateLookupValue(uint lookupValue)
		{
			return (lookupValue & 1) == 1 ? _poly ^ (lookupValue >> 1) : (lookupValue >> 1);
		}

		protected override uint InitLookupValue(uint index) => index;
	}
}
