using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using System;
using System.IO;
using ICSharpCode.SharpZipLib.Tests.TestSupport;

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

			// This is ignored but could be converted to 'file3'
			TestFile(t, @"./file3", "./file3");

			// The following relative paths cant be handled and are ignored
			TestFile(t, @"../file3", "../file3");
			TestFile(t, @".../file3", ".../file3");

			// Trick filenames.
			TestFile(t, @".....file3", ".....file3");
		}

		[Test]
		[Category("Zip")]
		[Platform("Win")]
		public void Basic_Windows()
		{
			var t = new ZipNameTransform();
			TestFile(t, @"\\uncpath\d1\file1", "file1");
			TestFile(t, @"C:\absolute\file2", "absolute/file2");
			
			TestFile(t, @"c::file", "_file");
		}
		
		[Test]
		[Category("Zip")]
		[Platform(Exclude="Win")]
		public void Basic_Posix()
		{
			var t = new ZipNameTransform();
			TestFile(t, @"backslash_path\file1", "backslash_path/file1");
			TestFile(t, "/absolute/file2", "absolute/file2");
			
			TestFile(t, @"////////:file", "_file");
		}

		[Test]
		public void TooLong()
		{
			var zt = new ZipNameTransform();
			var tooLong = new string('x', 65536);
			Assert.Throws<PathTooLongException>(() => zt.TransformDirectory(tooLong));
		}

		[Test]
		public void LengthBoundaryOk()
		{
			var zt = new ZipNameTransform();
			var tooLongWithRoot = Utils.SystemRoot + new string('x', 65535);
			Assert.DoesNotThrow(() => zt.TransformDirectory(tooLongWithRoot));
		}

		[Test]
		[Category("Zip")]
		[Platform("Win")]
		public void NameTransforms_Windows()
		{
			INameTransform t = new ZipNameTransform(@"C:\Slippery");
			Assert.AreEqual("Pongo/Directory/", t.TransformDirectory(@"C:\Slippery\Pongo\Directory"), "Value should be trimmed and converted");
			Assert.AreEqual("PoNgo/Directory/", t.TransformDirectory(@"c:\slipperY\PoNgo\Directory"), "Trimming should be case insensitive");
			Assert.AreEqual("slippery/Pongo/Directory/", t.TransformDirectory(@"d:\slippery\Pongo\Directory"), "Trimming should account for root");

			Assert.AreEqual("Pongo/File", t.TransformFile(@"C:\Slippery\Pongo\File"), "Value should be trimmed and converted");
		}
		
		[Test]
		[Category("Zip")]
		[Platform(Exclude="Win")]
		public void NameTransforms_Posix()
		{
			INameTransform t = new ZipNameTransform(@"/Slippery");
			Assert.AreEqual("Pongo/Directory/", t.TransformDirectory(@"/Slippery\Pongo\Directory"), "Value should be trimmed and converted");
			Assert.AreEqual("PoNgo/Directory/", t.TransformDirectory(@"/slipperY\PoNgo\Directory"), "Trimming should be case insensitive");
			Assert.AreEqual("slippery/Pongo/Directory/", t.TransformDirectory(@"/slippery/slippery/Pongo/Directory"), "Trimming should account for root");

			Assert.AreEqual("Pongo/File", t.TransformFile(@"/Slippery/Pongo/File"), "Value should be trimmed and converted");
		}

		/// <summary>
		/// Test ZipEntry static file name cleaning methods
		/// </summary>
		[Test]
		[Category("Zip")]
		public void FilenameCleaning()
		{
			Assert.AreEqual("hello", ZipEntry.CleanName("hello"));
			if(Environment.OSVersion.Platform == PlatformID.Win32NT) 
			{
				Assert.AreEqual("eccles", ZipEntry.CleanName(@"z:\eccles"));
				Assert.AreEqual("eccles", ZipEntry.CleanName(@"\\server\share\eccles"));
				Assert.AreEqual("dir/eccles", ZipEntry.CleanName(@"\\server\share\dir\eccles"));
			}
			else {
				Assert.AreEqual("eccles", ZipEntry.CleanName(@"/eccles"));
			}
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
