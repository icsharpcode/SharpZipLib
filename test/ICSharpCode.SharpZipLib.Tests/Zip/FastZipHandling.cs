using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Tests.TestSupport;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;

namespace ICSharpCode.SharpZipLib.Tests.Zip
{
	[TestFixture]
	public class FastZipHandling : ZipBase
	{
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void Basics()
		{
			const string tempName1 = "a(1).dat";

			var target = new MemoryStream();

			string tempFilePath = GetTempFilePath();
			Assert.IsNotNull(tempFilePath, "No permission to execute this test?");

			string addFile = Path.Combine(tempFilePath, tempName1);
			MakeTempFile(addFile, 1);

			try {
				var fastZip = new FastZip();
				fastZip.CreateZip(target, tempFilePath, false, @"a\(1\)\.dat", null);

				var archive = new MemoryStream(target.ToArray());
				using (ZipFile zf = new ZipFile(archive)) {
					Assert.AreEqual(1, zf.Count);
					ZipEntry entry = zf[0];
					Assert.AreEqual(tempName1, entry.Name);
					Assert.AreEqual(1, entry.Size);
					Assert.IsTrue(zf.TestArchive(true));

					zf.Close();
				}
			} finally {
				File.Delete(tempName1);
			}
		}

		const string ZipTempDir = "SharpZipLibTest";

		void EnsureTestDirectoryIsEmpty(string baseDir)
		{
			string name = Path.Combine(baseDir, ZipTempDir);

			if (Directory.Exists(name)) {
				Directory.Delete(name, true);
			}

			Directory.CreateDirectory(name);
		}

		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void ExtractEmptyDirectories()
		{
			string tempFilePath = GetTempFilePath();
			Assert.IsNotNull(tempFilePath, "No permission to execute this test?");

			string name = Path.Combine(tempFilePath, "x.zip");

			EnsureTestDirectoryIsEmpty(tempFilePath);

			string targetDir = Path.Combine(tempFilePath, ZipTempDir + @"\floyd");
			using (FileStream fs = File.Create(name)) {
				using (ZipOutputStream zOut = new ZipOutputStream(fs)) {
					zOut.PutNextEntry(new ZipEntry("floyd/"));
				}
			}

			var fastZip = new FastZip();
			fastZip.CreateEmptyDirectories = true;
			fastZip.ExtractZip(name, targetDir, "zz");

			File.Delete(name);
			Assert.IsTrue(Directory.Exists(targetDir), "Empty directory should be created");
		}

		[Test]
		[Category("Zip")]
		public void Encryption()
		{
			const string tempName1 = "a.dat";

			var target = new MemoryStream();

			string tempFilePath = GetTempFilePath();
			Assert.IsNotNull(tempFilePath, "No permission to execute this test?");

			string addFile = Path.Combine(tempFilePath, tempName1);
			MakeTempFile(addFile, 1);

			try {
				var fastZip = new FastZip();
				fastZip.Password = "Ahoy";

				fastZip.CreateZip(target, tempFilePath, false, @"a\.dat", null);

				var archive = new MemoryStream(target.ToArray());
				using (ZipFile zf = new ZipFile(archive)) {
					zf.Password = "Ahoy";
					Assert.AreEqual(1, zf.Count);
					ZipEntry entry = zf[0];
					Assert.AreEqual(tempName1, entry.Name);
					Assert.AreEqual(1, entry.Size);
					Assert.IsTrue(zf.TestArchive(true));
					Assert.IsTrue(entry.IsCrypted);
				}
			} finally {
				File.Delete(tempName1);
			}
		}

		[Test]
		[Category("Zip")]
		public void CreateExceptions()
		{
			var fastZip = new FastZip();
			string tempFilePath = GetTempFilePath();
			Assert.IsNotNull(tempFilePath, "No permission to execute this test?");

			Assert.Throws<DirectoryNotFoundException>(() => 
			{
				string addFile = Path.Combine(tempFilePath, "test.zip");
				try {
					fastZip.CreateZip(addFile, @"z:\doesnt exist", false, null);
				} finally {
					File.Delete(addFile);
				}
			});
		}

		#region String testing helper

		private void TestFileNames(params string[] names)
			=> TestFileNames((IEnumerable<string>)names);

		private void TestFileNames(IEnumerable<string> names)
		{
			var zippy = new FastZip();

			using (var tempDir = new Utils.TempDir())
			using (var tempZip = new Utils.TempFile())
			{
				int nameCount = 0;
				foreach (var name in names)
				{
					tempDir.CreateDummyFile(name);
					nameCount++;
				}

				zippy.CreateZip(tempZip.Filename, tempDir.Fullpath, true, null, null);

				using (ZipFile z = new ZipFile(tempZip.Filename))
				{
					Assert.AreEqual(nameCount, z.Count);
					foreach (var name in names)
					{
						var index = z.FindEntry(name, true);

						Assert.AreNotEqual(index, -1, "Zip entry \"{0}\" not found", name);

						var entry = z[index];

						if (ZipStrings.UseUnicode)
						{
							Assert.IsTrue(entry.IsUnicodeText, "Zip entry #{0} not marked as unicode", index);
						}
						else
						{
							Assert.IsFalse(entry.IsUnicodeText, "Zip entry #{0} marked as unicode", index);
						}

						Assert.AreEqual(name, entry.Name);

						var nameBytes = string.Join(' ', Encoding.BigEndianUnicode.GetBytes(entry.Name).Select(b => b.ToString("x2")));

						Console.WriteLine($" - Zip entry: {entry.Name} ({nameBytes})");
					}
				}

			}
		}

#endregion

		[Test]
		[Category("Zip")]
		[Category("Unicode")]
		public void UnicodeText()
		{
			var preCp = ZipStrings.CodePage;
			try
			{
				TestFileNames(StringTesting.Filenames);
			}
			finally
			{
				ZipStrings.CodePage = preCp;
			}
		}

		[Test]
		[Category("Zip")]
		[Category("Unicode")]
		public void NonUnicodeText()
		{
			var preCp = ZipStrings.CodePage;
			try
			{
				Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

				foreach((string language, string filename, string encoding) in StringTesting.GetTestSamples())
				{
					Console.WriteLine($"{language} filename \"{filename}\" using \"{encoding}\":");
					ZipStrings.CodePage = Encoding.GetEncoding(encoding).CodePage;
					TestFileNames(filename);
				}

			}
			finally
			{
				ZipStrings.CodePage = preCp;
			}
		}

		[Test]
		[Category("Zip")]
		public void ExtractExceptions()
		{
			var fastZip = new FastZip();
			string tempFilePath = GetTempFilePath();
			Assert.IsNotNull(tempFilePath, "No permission to execute this test?");

			string addFile = Path.Combine(tempFilePath, "test.zip");
			try {
				Assert.Throws<FileNotFoundException>(() => fastZip.ExtractZip(addFile, @"z:\doesnt exist", null));
			} finally {
				File.Delete(addFile);
			}
		}

		[Test]
		[Category("Zip")]
		[Ignore("see comments below")]
		/*
		 * This test is somewhat strange:
		 * a) It tries to simulate a locked file by opening it on the same thread using FileShare.
		 *    However the FileShare value is not meant for cross-process file locking, but only to
		 *    allow other threads in the same process to access the same file.
		 *    This is not the intended behavior, you would need a second process locking the file
		 *    when running this test.
		 * b) It would require to change the file operation in FastZip.ProcessFile to use FileShare.ReadWrite
		 *    but doing so would make FastZip work with locked files (that are potentially written to by others)
		 *    and silently ignoring any locks. HOWEVER: This can lead to corrupt/incomplete files, which is why it
		 *    should not be the default behavior.
		 *    
		 * Therefore I would remove this test.
		 **/
		public void ReadingOfLockedDataFiles()
		{
			const string tempName1 = "a.dat";

			var target = new MemoryStream();

			string tempFilePath = GetTempFilePath();
			Assert.IsNotNull(tempFilePath, "No permission to execute this test?");

			string addFile = Path.Combine(tempFilePath, tempName1);
			MakeTempFile(addFile, 1);

			try {
				var fastZip = new FastZip();

				using (File.Open(addFile, FileMode.Open, FileAccess.Write, FileShare.ReadWrite)) {
					fastZip.CreateZip(target, tempFilePath, false, @"a\.dat", null);

					var archive = new MemoryStream(target.ToArray());
					using (ZipFile zf = new ZipFile(archive)) {
						Assert.AreEqual(1, zf.Count);
						ZipEntry entry = zf[0];
						Assert.AreEqual(tempName1, entry.Name);
						Assert.AreEqual(1, entry.Size);
						Assert.IsTrue(zf.TestArchive(true));

						zf.Close();
					}
				}
			} finally {
				File.Delete(tempName1);
			}
		}

		[Test]
		[Category("Zip")]
		public void NonAsciiPasswords()
		{
			const string tempName1 = "a.dat";

			var target = new MemoryStream();

			string tempFilePath = GetTempFilePath();
			Assert.IsNotNull(tempFilePath, "No permission to execute this test?");

			string addFile = Path.Combine(tempFilePath, tempName1);
			MakeTempFile(addFile, 1);

			string password = "abc\u0066\u0393";
			try {
				var fastZip = new FastZip();
				fastZip.Password = password;

				fastZip.CreateZip(target, tempFilePath, false, @"a\.dat", null);

				var archive = new MemoryStream(target.ToArray());
				using (ZipFile zf = new ZipFile(archive)) {
					zf.Password = password;
					Assert.AreEqual(1, zf.Count);
					ZipEntry entry = zf[0];
					Assert.AreEqual(tempName1, entry.Name);
					Assert.AreEqual(1, entry.Size);
					Assert.IsTrue(zf.TestArchive(true));
					Assert.IsTrue(entry.IsCrypted);
				}
			} finally {
				File.Delete(tempName1);
			}
		}


		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void LimitExtractPath()
		{
			string tempPath = GetTempFilePath();
			Assert.IsNotNull(tempPath, "No permission to execute this test?");

			var uniqueName = "SharpZipLib.Test_" + DateTime.Now.Ticks.ToString("x");

			tempPath = Path.Combine(tempPath, uniqueName);
			var extractPath = Path.Combine(tempPath, "output");

			const string contentFile = "content.txt";

			var contentFilePathBad = Path.Combine("..", contentFile);
			var extractFilePathBad = Path.Combine(tempPath, contentFile);
			var archiveFileBad = Path.Combine(tempPath, "test-good.zip");

			var contentFilePathGood = Path.Combine("childDir", contentFile);
			var extractFilePathGood = Path.Combine(extractPath, contentFilePathGood);
			var archiveFileGood = Path.Combine(tempPath, "test-bad.zip");

			try
			{
				Directory.CreateDirectory(extractPath);

				// Create test input
				void CreateTestFile(string archiveFile, string contentPath)
				{
					using (var zf = ZipFile.Create(archiveFile))
					{
						zf.BeginUpdate();
						zf.Add(new StringMemoryDataSource($"Content of {archiveFile}"), contentPath);
						zf.CommitUpdate();
					}
				}

				CreateTestFile(archiveFileGood, contentFilePathGood);
				CreateTestFile(archiveFileBad, contentFilePathBad);

				Assert.IsTrue(File.Exists(archiveFileGood), "Good test archive was not created");
				Assert.IsTrue(File.Exists(archiveFileBad), "Bad test archive was not created");

				var fastZip = new FastZip();

				Assert.DoesNotThrow(() => {
					fastZip.ExtractZip(archiveFileGood, extractPath, "");
				}, "Threw exception on good file name");

				Assert.IsTrue(File.Exists(extractFilePathGood), "Good output file not created");

				Assert.Throws<SharpZipLib.Core.InvalidNameException>(() => {
					fastZip.ExtractZip(archiveFileBad, extractPath, "");
				}, "No exception was thrown for bad file name");

				Assert.IsFalse(File.Exists(extractFilePathBad), "Bad output file created");

				Assert.DoesNotThrow(() => {
					fastZip.ExtractZip(archiveFileBad, extractPath, FastZip.Overwrite.Never, null, "", "", true, true);
				}, "Threw exception on bad file name when traversal explicitly allowed");

				Assert.IsTrue(File.Exists(extractFilePathBad), "Bad output file not created when traversal explicitly allowed");

			}
			finally
			{
				Directory.Delete(tempPath, true);
			}
		}

	}
}
