using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using ICSharpCode.SharpZipLib.Zip;

namespace ICSharpCode.SharpZipLib.Tests.TestSupport
{
	/// <summary>
	/// Provides support for testing in memory zip archives.
	/// </summary>
	static class ZipTesting
	{
		/// <summary>
		/// Tests the archive.
		/// </summary>
		/// <param name="data">The data.</param>
		/// <returns></returns>
		public static bool TestArchive(byte[] data)
		{
			return TestArchive(data, null);
		}

		/// <summary>
		/// Tests the archive.
		/// </summary>
		/// <param name="data">The data.</param>
		/// <param name="password">The password.</param>
		/// <returns>true if archive tests ok; false otherwise.</returns>
		public static bool TestArchive(byte[] data, string password)
		{
			using (MemoryStream ms = new MemoryStream(data))
			using (ZipFile zipFile = new ZipFile(ms)) {
#if !PCL
				zipFile.Password = password;
#else
                if (!String.IsNullOrWhiteSpace(password))
                    throw new InvalidOperationException("Password not supported in PCL");
#endif
				return zipFile.TestArchive(true);
			}
		}

	}
}
