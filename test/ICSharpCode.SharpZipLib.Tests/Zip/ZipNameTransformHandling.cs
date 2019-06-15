using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using System;
using System.IO;

namespace ICSharpCode.SharpZipLib.Tests.Zip
{
	[TestFixture]
	public class ZipNameTransformHandling : TransformBase
	{
		[Test]
		[Category("Zip")]
		public void Basic()
		{
			var t = new ZipNameTransform();

			TestFile(t, "abcdef", "abcdef");
			TestFile(t, @"\\uncpath\d1\file1", "file1");
			TestFile(t, @"C:\absolute\file2", "absolute/file2");

			// This is ignored but could be converted to 'file3'
			TestFile(t, @"./file3", "./file3");

			// The following relative paths cant be handled and are ignored
			TestFile(t, @"../file3", "../file3");
			TestFile(t, @".../file3", ".../file3");

			// Trick filenames.
			TestFile(t, @".....file3", ".....file3");
			TestFile(t, @"c::file", "_file");
		}

		[Test]
		public void TooLong()
		{
			var zt = new ZipNameTransform();
			var veryLong = new string('x', 65536);
			try
			{
				zt.TransformDirectory(veryLong);
				Assert.Fail("Expected an exception");
			}
			catch (PathTooLongException)
			{
			}
		}

		[Test]
		public void LengthBoundaryOk()
		{
			var zt = new ZipNameTransform();
			string veryLong = "c:\\" + new string('x', 65535);
			try
			{
				zt.TransformDirectory(veryLong);
			}
			catch
			{
				Assert.Fail("Expected no exception");
			}
		}

		[Test]
		[Category("Zip")]
		public void NameTransforms()
		{
			INameTransform t = new ZipNameTransform(@"C:\Slippery");
			Assert.AreEqual("Pongo/Directory/", t.TransformDirectory(@"C:\Slippery\Pongo\Directory"), "Value should be trimmed and converted");
			Assert.AreEqual("PoNgo/Directory/", t.TransformDirectory(@"c:\slipperY\PoNgo\Directory"), "Trimming should be case insensitive");
			Assert.AreEqual("slippery/Pongo/Directory/", t.TransformDirectory(@"d:\slippery\Pongo\Directory"), "Trimming should be case insensitive");

			Assert.AreEqual("Pongo/File", t.TransformFile(@"C:\Slippery\Pongo\File"), "Value should be trimmed and converted");
		}

		/// <summary>
		/// Test ZipEntry static file name cleaning methods
		/// </summary>
		[Test]
		[Category("Zip")]
		public void FilenameCleaning()
		{
			Assert.AreEqual(0, string.Compare(ZipEntry.CleanName("hello"), "hello", StringComparison.Ordinal));
			Assert.AreEqual(0, string.Compare(ZipEntry.CleanName(@"z:\eccles"), "eccles", StringComparison.Ordinal));
			Assert.AreEqual(0, string.Compare(ZipEntry.CleanName(@"\\server\share\eccles"), "eccles", StringComparison.Ordinal));
			Assert.AreEqual(0, string.Compare(ZipEntry.CleanName(@"\\server\share\dir\eccles"), "dir/eccles", StringComparison.Ordinal));
		}

		[Test]
		[Category("Zip")]
		public void PathalogicalNames()
		{
			string badName = ".*:\\zy3$";

			Assert.IsFalse(ZipNameTransform.IsValidName(badName));

			var t = new ZipNameTransform();
			string result = t.TransformFile(badName);

			Assert.IsTrue(ZipNameTransform.IsValidName(result));
		}
	}
}
