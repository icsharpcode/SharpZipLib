using System;

using NUnit.Framework;

namespace SharpZipLibTests.TestSupport
{
	/// <summary>
	/// Miscellaneous test utilities.
	/// </summary>
	public class Utils
	{
		public Utils()
		{
		}
		
		static void Compare(byte[] a, byte[] b)
		{
			if ( a == null ) {
				throw new ArgumentNullException("a");
			}

			if ( b == null ) {
				throw new ArgumentNullException("b");
			}
			
			Assert.AreEqual(a.Length, b.Length);
			for (int i = 0; i < a.Length; ++i) {
				Assert.AreEqual(a[i], b[i]);
			}
		}
		
	}
}
