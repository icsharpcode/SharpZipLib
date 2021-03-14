using System;
using System.IO;
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
		
		[Test]
		[Category("Core")]
		public void DropPathRoot()
		{
			string TestPath(string s) => PathUtils.DropPathRoot(s, replaceInvalidChars: false);
			Assert.AreEqual("file.txt", TestPath(@"\\server\share\file.txt"));
			Assert.AreEqual("file.txt", TestPath(@"c:\file.txt"));
			Assert.AreEqual(@"subdir with spaces\file.txt", TestPath(@"z:\subdir with spaces\file.txt"));
			Assert.AreEqual("", TestPath(@"\\server\share\"));
			Assert.AreEqual(@"server\share\file.txt", TestPath(@"\server\share\file.txt"));
			Assert.AreEqual(@"path\file.txt", TestPath(@"\\server\share\\path\file.txt"));
			Assert.DoesNotThrow(() => Console.WriteLine(TestPath(@"c:\file:+/")));
			Assert.DoesNotThrow(() => Console.WriteLine(TestPath(@"c:\file*?")));
			Assert.DoesNotThrow(() => Console.WriteLine(TestPath("c:\\file|\"")));
			Assert.DoesNotThrow(() => Console.WriteLine(TestPath(@"c:\file<>")));
		}

		[Test]
		[TestCase(@"c:\file:+/")]
		[TestCase(@"c:\file*?")]
		[TestCase("c:\\file|\"")]
		[TestCase(@"c:\file<>")]
		[Category("Core")]
		public void DropPathRoot_DoesNotThrowForInvalidPath(string path)
		{
			Assert.DoesNotThrow(() => Console.WriteLine(PathUtils.DropPathRoot(path)));
		}
	}
}
