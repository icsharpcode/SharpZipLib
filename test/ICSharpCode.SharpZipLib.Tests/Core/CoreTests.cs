using ICSharpCode.SharpZipLib.Core;
using NUnit.Framework;

namespace ICSharpCode.SharpZipLib.Tests.Core
{
	[TestFixture]
	public class CoreTestSuite
	{
		[Test]
		[Category("Core")]
		public void FilterQuoting()
		{
			string[] filters = NameFilter.SplitQuoted("");
			Assert.AreEqual(0, filters.Length);

			filters = NameFilter.SplitQuoted(";;;");
			Assert.AreEqual(4, filters.Length);
			foreach (string filter in filters)
			{
				Assert.AreEqual("", filter);
			}

			filters = NameFilter.SplitQuoted("a;a;a;a;a");
			Assert.AreEqual(5, filters.Length);
			foreach (string filter in filters)
			{
				Assert.AreEqual("a", filter);
			}

			filters = NameFilter.SplitQuoted(@"a\;;a\;;a\;;a\;;a\;");
			Assert.AreEqual(5, filters.Length);
			foreach (string filter in filters)
			{
				Assert.AreEqual("a;", filter);
			}
		}

		[Test]
		[Category("Core")]
		public void NullFilter()
		{
			var nf = new NameFilter(null);
			Assert.IsTrue(nf.IsIncluded("o78i6bgv5rvu\\kj//&*"));
		}

		[Test]
		[Category("Core")]
		public void ValidFilter()
		{
			Assert.IsTrue(NameFilter.IsValidFilterExpression(null));
			Assert.IsTrue(NameFilter.IsValidFilterExpression(string.Empty));
			Assert.IsTrue(NameFilter.IsValidFilterExpression("a"));

			Assert.IsFalse(NameFilter.IsValidFilterExpression(@"\,)"));
			Assert.IsFalse(NameFilter.IsValidFilterExpression(@"[]"));
		}
	}
}
