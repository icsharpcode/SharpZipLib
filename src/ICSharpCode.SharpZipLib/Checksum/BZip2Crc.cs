using ICSharpCode.SharpZipLib.Checksum.Proxy;
using System;

namespace ICSharpCode.SharpZipLib.Checksum
{
	/// <summary>
	/// CRC-32 with unreversed data and reversed output
	/// </summary>
	public sealed class BZip2Crc : Crc32Base
	{
		private static readonly NormalCrc32Proxy _proxy = new NormalCrc32Proxy();

		internal override Crc32ProxyBase Proxy => _proxy;
	}
}
