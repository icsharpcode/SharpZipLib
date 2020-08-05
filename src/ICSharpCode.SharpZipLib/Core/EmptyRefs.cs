using System;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace ICSharpCode.SharpZipLib.Core
{
	internal static class EmptyRefs
	{
#if NET45
		public static byte[] ByteArray { get; } = new byte[0];
		public static int[] Int32Array { get; } = new int[0];
		public static short[] Int16Array {get; } = new short[0];
		public static ZipEntry[] ZipEntryArray { get; } = new ZipEntry[0];
#else
		public static byte[] ByteArray => Array.Empty<byte>(); 
		public static int[] Int32Array => Array.Empty<int>();
		public static short[] Int16Array => Array.Empty<short>();
		public static ZipEntry[] ZipEntryArray => Array.Empty<ZipEntry>();
#endif
	}
}
