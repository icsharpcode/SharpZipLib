using ICSharpCode.SharpZipLib.Checksum.Proxy;
using System;

namespace ICSharpCode.SharpZipLib.Checksum
{
	/// <summary>
	/// CRC-32 with reversed data and unreversed output
	/// </summary>
	public sealed class Crc32 : Crc32Base
	{
		private static readonly ReflectedCrc32Proxy _proxy = new ReflectedCrc32Proxy();
		
		internal override Crc32ProxyBase Proxy => _proxy;


		internal static uint ComputeCrc32(uint oldCrc, byte bval)
		{
			return ~_proxy.Append(~oldCrc, new byte[] { (byte)bval }, 0, 1);
		}


	}
}
