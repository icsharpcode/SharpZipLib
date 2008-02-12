using NUnit.Framework;

using ICSharpCode.SharpZipLib.Core;

namespace ICSharpCode.SharpZipLib.Tests.Core
{
	[TestFixture]
	public class Core
	{

		[Test]
		public void FilterQuoting()
		{
			string[] filters = NameFilter.SplitQuoted("");
			Assert.AreEqual(0, filters.Length);
			
			filters = NameFilter.SplitQuoted(";;;");
			Assert.AreEqual(4, filters.Length);
			foreach(string filter in filters) {
				Assert.AreEqual("", filter);
			}

			filters = NameFilter.SplitQuoted("a;a;a;a;a");
			Assert.AreEqual(5, filters.Length);
			foreach (string filter in filters) {
				Assert.AreEqual("a", filter);
			}

			filters = NameFilter.SplitQuoted(@"a\;;a\;;a\;;a\;;a\;");
			Assert.AreEqual(5, filters.Length);
			foreach (string filter in filters) {
				Assert.AreEqual("a;", filter);
			}
		}

		[Test]
		public void ValidFilter()
		{
			Assert.IsTrue(NameFilter.IsValidFilterExpression("a"));
			Assert.IsFalse(NameFilter.IsValidFilterExpression(@"\,)"));
		}
	}
}
