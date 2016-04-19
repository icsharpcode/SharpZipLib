using System;
using NUnit.Framework;

namespace ICSharpCode.SharpZipLib.Tests.TestSupport
{
	/// <summary>
	/// Miscellaneous test utilities.
	/// </summary>
	public static class Utils
	{

		static void Compare(byte[] a, byte[] b)
		{
			if (a == null) {
				throw new ArgumentNullException(nameof(a));
			}

			if (b == null) {
				throw new ArgumentNullException(nameof(b));
			}

			Assert.AreEqual(a.Length, b.Length);
			for (int i = 0; i < a.Length; ++i) {
				Assert.AreEqual(a[i], b[i]);
			}
		}

	}
}
