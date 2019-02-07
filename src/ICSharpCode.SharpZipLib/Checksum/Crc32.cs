using System;

namespace ICSharpCode.SharpZipLib.Checksum
{
	/// <summary>
	/// CRC-32 with reversed data and unreversed output
	/// </summary>
	public sealed class Crc32 : Crc32Base
	{
		private static readonly Crc32Proxy _proxy = new Crc32Proxy(reflected: true);
		
		internal override Crc32Proxy Proxy => _proxy;


		internal static uint ComputeCrc32(uint oldCrc, byte bval)
		{
			return ~_proxy.Append(~oldCrc, new byte[] { (byte)bval }, 0, 1);
		}


	}
}
