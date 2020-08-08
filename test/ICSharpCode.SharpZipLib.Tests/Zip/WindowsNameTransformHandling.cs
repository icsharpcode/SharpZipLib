using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ICSharpCode.SharpZipLib.Tests.Zip
{
	[TestFixture]
	public class WindowsNameTransformHandling : TransformBase
	{
		[OneTimeSetUp]
		public void TestInit() {
			if (Path.DirectorySeparatorChar != '\\') {
				Assert.Inconclusive("WindowsNameTransform will not work on platforms not using '\\' directory separators");
			}
		}

		[Test]
		public void BasicFiles()
		{
			var wnt = new WindowsNameTransform();
			wnt.TrimIncomingPaths = false;

			TestFile(wnt, "Bogan", "Bogan");
			TestFile(wnt, "absolute/file2", Path.Combine("absolute", "file2"));
			TestFile(wnt, "C:/base/////////t", Path.Combine("base", "t"));
			TestFile(wnt, "//unc/share/zebidi/and/dylan", Path.Combine("zebidi", "and", "dylan"));
			TestFile(wnt, @"\\unc\share\/zebidi\/and\/dylan", Path.Combine("zebidi", "and", "dylan"));
		}

		[Test]
		public void Replacement()
		{
			var wnt = new WindowsNameTransform();
			wnt.TrimIncomingPaths = false;

			TestFile(wnt, "c::", "_");
			TestFile(wnt, "c\\/>", Path.Combine("c", "_"));
		}

		[Test]
		public void NameTooLong()
		{
			var wnt = new WindowsNameTransform();
			var veryLong = new string('x', 261);
			try
			{
				wnt.TransformDirectory(veryLong);
				Assert.Fail("Expected an exception");
			}
			catch (PathTooLongException)
			{
			}
		}

		[Test]
		public void LengthBoundaryOk()
		{
			var wnt = new WindowsNameTransform();
			string veryLong = "c:\\" + new string('x', 260);
			try
			{
				string transformed = wnt.TransformDirectory(veryLong);
			}
			catch
			{
				Assert.Fail("Expected no exception");
			}
		}

		[Test]
		public void ReplacementChecking()
		{
			var wnt = new WindowsNameTransform();
			try
			{
				wnt.Replacement = '*';
				Assert.Fail("Expected an exception");
			}
			catch (ArgumentException)
			{
			}

			try
			{
				wnt.Replacement = '?';
				Assert.Fail("Expected an exception");
			}
			catch (ArgumentException)
			{
			}

			try
			{
				wnt.Replacement = ':';
				Assert.Fail("Expected an exception");
			}
			catch (ArgumentException)
			{
			}

			try
			{
				wnt.Replacement = '/';
				Assert.Fail("Expected an exception");
			}
			catch (ArgumentException)
			{
			}

			try
			{
				wnt.Replacement = '\\';
				Assert.Fail("Expected an exception");
			}
			catch (ArgumentException)
			{
			}
		}

		[Test]
		public void BasicDirectories()
		{
			var wnt = new WindowsNameTransform();
			wnt.TrimIncomingPaths = false;

			string tutu = Path.GetDirectoryName("\\bogan\\ping.txt");
			TestDirectory(wnt, "d/", "d");
			TestDirectory(wnt, "d", "d");
			TestDirectory(wnt, "absolute/file2", @"absolute\file2");

			string BaseDir1 = Path.Combine("C:\\", "Dir");
			wnt.BaseDirectory = BaseDir1;

			TestDirectory(wnt, "talofa", Path.Combine(BaseDir1, "talofa"));

			string BaseDir2 = string.Format(@"C:{0}Dir{0}", Path.DirectorySeparatorChar);
			wnt.BaseDirectory = BaseDir2;

			TestDirectory(wnt, "talofa", Path.Combine(BaseDir2, "talofa"));
		}
	}
}
