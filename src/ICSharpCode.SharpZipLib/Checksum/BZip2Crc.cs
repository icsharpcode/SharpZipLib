using System;

namespace ICSharpCode.SharpZipLib.Checksum
{
	/// <summary>
	/// CRC-32 with unreversed data and reversed output
	/// </summary>
	public sealed class BZip2Crc : Crc32Base
	{
		private static readonly Crc32Proxy _proxy = new Crc32Proxy(reflected: false);

		internal override Crc32Proxy Proxy => _proxy;
	}
}
