using System;
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

		// Use a shorter name wrapper to make tests more legible
		private static string DropRoot(string s) => PathUtils.DropPathRoot(s);
		
		[Test]
		[Category("Core")]
		[Platform("Win")]
		public void DropPathRoot_Windows()
		{
			Assert.AreEqual("file.txt", DropRoot(@"\\server\share\file.txt"));
			Assert.AreEqual("file.txt", DropRoot(@"c:\file.txt"));
			Assert.AreEqual(@"subdir with spaces\file.txt", DropRoot(@"z:\subdir with spaces\file.txt"));
			Assert.AreEqual("", DropRoot(@"\\server\share\"));
			Assert.AreEqual(@"server\share\file.txt", DropRoot(@"\server\share\file.txt"));
			Assert.AreEqual(@"path\file.txt", DropRoot(@"\\server\share\\path\file.txt"));
		}

		[Test]
		[Category("Core")]
		[Platform(Exclude="Win")]
		public void DropPathRoot_Posix()
		{
			Assert.AreEqual("file.txt", DropRoot("/file.txt"));
			Assert.AreEqual(@"tmp/file.txt", DropRoot(@"/tmp/file.txt"));
			Assert.AreEqual(@"tmp\file.txt", DropRoot(@"\tmp\file.txt"));
			Assert.AreEqual(@"tmp/file.txt", DropRoot(@"\tmp/file.txt"));
			Assert.AreEqual(@"tmp\file.txt", DropRoot(@"/tmp\file.txt"));
			Assert.AreEqual("", DropRoot("/"));

		}

		[Test]
		[TestCase(@"c:\file:+/")]
		[TestCase(@"c:\file*?")]
		[TestCase("c:\\file|\"")]
		[TestCase(@"c:\file<>")]
		[TestCase(@"c:file")]
		[TestCase(@"c::file")]
		[TestCase(@"c:?file")]
		[TestCase(@"c:+file")]
		[TestCase(@"cc:file")]
		[Category("Core")]
		public void DropPathRoot_DoesNotThrowForInvalidPath(string path)
		{
			Assert.DoesNotThrow(() => Console.WriteLine(PathUtils.DropPathRoot(path)));
		}
	}
}
