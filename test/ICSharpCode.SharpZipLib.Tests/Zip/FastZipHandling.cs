using ICSharpCode.SharpZipLib.Tests.TestSupport;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

			try
			{
				var fastZip = new FastZip();
				fastZip.CreateZip(target, tempFilePath, false, @"a\(1\)\.dat", null);

				var archive = new MemoryStream(target.ToArray());
				using (ZipFile zf = new ZipFile(archive))
				{
					Assert.AreEqual(1, zf.Count);
					ZipEntry entry = zf[0];
					Assert.AreEqual(tempName1, entry.Name);
					Assert.AreEqual(1, entry.Size);
					Assert.IsTrue(zf.TestArchive(true));

					zf.Close();
				}
			}
			finally
			{
				File.Delete(tempName1);
			}
		}

		private const string ZipTempDir = "SharpZipLibTest";

		private void EnsureTestDirectoryIsEmpty(string baseDir)
		{
			string name = Path.Combine(baseDir, ZipTempDir);

			if (Directory.Exists(name))
			{
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
			using (FileStream fs = File.Create(name))
			{
				using (ZipOutputStream zOut = new ZipOutputStream(fs))
				{
					zOut.PutNextEntry(new ZipEntry("floyd/"));
				}
			}

			var fastZip = new FastZip();
			fastZip.CreateEmptyDirectories = true;
			fastZip.ExtractZip(name, targetDir, "zz");

			File.Delete(name);
			Assert.IsTrue(Directory.Exists(targetDir), "Empty directory should be created");
		}

		/// <summary>
		/// Test that FastZip can create empty directory entries in archives.
		/// </summary>
		[TestCase(null)]
		[TestCase("password")]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void CreateEmptyDirectories(string password)
		{
			using (var tempFilePath = new Utils.TempDir())
			{
				string name = Path.Combine(tempFilePath.Fullpath, "x.zip");

				// Create empty test folders (The folder that we'll zip, and the test sub folder).
				string archiveRootDir = Path.Combine(tempFilePath.Fullpath, ZipTempDir);
				string targetDir = Path.Combine(archiveRootDir, "floyd");
				Directory.CreateDirectory(targetDir);

				// Create the archive with FastZip
				var fastZip = new FastZip
				{
					CreateEmptyDirectories = true,
					Password = password,
				};
				fastZip.CreateZip(name, archiveRootDir, true, null);

				// Test that the archive contains the empty folder entry
				using (var zipFile = new ZipFile(name))
				{
					Assert.That(zipFile.Count, Is.EqualTo(1), "Should only be one entry in the file");

					var folderEntry = zipFile.GetEntry("floyd/");
					Assert.That(folderEntry.IsDirectory, Is.True, "The entry must be a folder");

					Assert.IsTrue(zipFile.TestArchive(true));
				}
			}
		}

		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void ContentEqualAfterAfterArchived([Values(0, 1, 64)]int contentSize)
		{
			using(var sourceDir = new Utils.TempDir())
			using(var targetDir = new Utils.TempDir())
			using(var zipFile = Utils.GetDummyFile(0))
			{
				var sourceFile = sourceDir.CreateDummyFile(contentSize);
				var sourceContent = File.ReadAllBytes(sourceFile);
				new FastZip().CreateZip(zipFile.Filename, sourceDir.Fullpath, true, null);

				Assert.DoesNotThrow(() =>
				{
					new FastZip().ExtractZip(zipFile.Filename, targetDir.Fullpath, null);
				}, "Exception during extraction of test archive");
				
				var targetFile = Path.Combine(targetDir.Fullpath, Path.GetFileName(sourceFile));
				var targetContent = File.ReadAllBytes(targetFile);

				Assert.AreEqual(sourceContent.Length, targetContent.Length, "Extracted file size does not match source file size");
				Assert.AreEqual(sourceContent, targetContent, "Extracted content does not match source content");
			}
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

			try
			{
				var fastZip = new FastZip();
				fastZip.Password = "Ahoy";

				fastZip.CreateZip(target, tempFilePath, false, @"a\.dat", null);

				var archive = new MemoryStream(target.ToArray());
				using (ZipFile zf = new ZipFile(archive))
				{
					zf.Password = "Ahoy";
					Assert.AreEqual(1, zf.Count);
					ZipEntry entry = zf[0];
					Assert.AreEqual(tempName1, entry.Name);
					Assert.AreEqual(1, entry.Size);
					Assert.IsTrue(zf.TestArchive(true));
					Assert.IsTrue(entry.IsCrypted);
				}
			}
			finally
			{
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
				try
				{
					fastZip.CreateZip(addFile, @"z:\doesnt exist", false, null);
				}
				finally
				{
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

						var nameBytes = string.Join(" ", Encoding.BigEndianUnicode.GetBytes(entry.Name).Select(b => b.ToString("x2")));

						Console.WriteLine($" - Zip entry: {entry.Name} ({nameBytes})");
					}
				}
			}
		}

		#endregion String testing helper

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

				foreach ((string language, string filename, string encoding) in StringTesting.GetTestSamples())
				{
					Console.WriteLine($"{language} filename \"{filename}\" using \"{encoding}\":");

					// TODO: samples of this test must be reversible
					// Some samples can't be restored back with their encoding.
					// test wasn't failing only because SystemDefaultCodepage is 65001 on Net.Core and
					// old behaviour actually was using Unicode instead of user's passed codepage
					var encoder = Encoding.GetEncoding(encoding);
					var bytes = encoder.GetBytes(filename);
					var restoredString = encoder.GetString(bytes);
					if(string.CompareOrdinal(filename, restoredString) != 0)
					{
						Console.WriteLine($"Sample for language {language} with value of {filename} is skipped, because it's irreversable");
						continue;
					}

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
			try
			{
				Assert.Throws<FileNotFoundException>(() => fastZip.ExtractZip(addFile, @"z:\doesnt exist", null));
			}
			finally
			{
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

			try
			{
				var fastZip = new FastZip();

				using (File.Open(addFile, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
				{
					fastZip.CreateZip(target, tempFilePath, false, @"a\.dat", null);

					var archive = new MemoryStream(target.ToArray());
					using (ZipFile zf = new ZipFile(archive))
					{
						Assert.AreEqual(1, zf.Count);
						ZipEntry entry = zf[0];
						Assert.AreEqual(tempName1, entry.Name);
						Assert.AreEqual(1, entry.Size);
						Assert.IsTrue(zf.TestArchive(true));

						zf.Close();
					}
				}
			}
			finally
			{
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
			try
			{
				var fastZip = new FastZip();
				fastZip.Password = password;

				fastZip.CreateZip(target, tempFilePath, false, @"a\.dat", null);

				var archive = new MemoryStream(target.ToArray());
				using (ZipFile zf = new ZipFile(archive))
				{
					zf.Password = password;
					Assert.AreEqual(1, zf.Count);
					ZipEntry entry = zf[0];
					Assert.AreEqual(tempName1, entry.Name);
					Assert.AreEqual(1, entry.Size);
					Assert.IsTrue(zf.TestArchive(true));
					Assert.IsTrue(entry.IsCrypted);
				}
			}
			finally
			{
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

				Assert.DoesNotThrow(() =>
				{
					fastZip.ExtractZip(archiveFileGood, extractPath, "");
				}, "Threw exception on good file name");

				Assert.IsTrue(File.Exists(extractFilePathGood), "Good output file not created");

				Assert.Throws<SharpZipLib.Core.InvalidNameException>(() =>
				{
					fastZip.ExtractZip(archiveFileBad, extractPath, "");
				}, "No exception was thrown for bad file name");

				Assert.IsFalse(File.Exists(extractFilePathBad), "Bad output file created");

				Assert.DoesNotThrow(() =>
				{
					fastZip.ExtractZip(archiveFileBad, extractPath, FastZip.Overwrite.Never, null, "", "", true, true);
				}, "Threw exception on bad file name when traversal explicitly allowed");

				Assert.IsTrue(File.Exists(extractFilePathBad), "Bad output file not created when traversal explicitly allowed");
			}
			finally
			{
				Directory.Delete(tempPath, true);
			}
		}

		/// <summary>
		/// Check that the input stream is not closed on error when isStreamOwner is false
		/// </summary>
		[Test]
		public void StreamNotClosedOnError()
		{
			// test paths
			string tempFilePath = GetTempFilePath();
			Assert.IsNotNull(tempFilePath, "No permission to execute this test?");

			var tempFolderPath = Path.Combine(tempFilePath, Path.GetRandomFileName());
			Assert.That(Directory.Exists(tempFolderPath), Is.False, "Temp folder path should not exist");

			// memory that isn't a valid zip
			var ms = new TrackedMemoryStream(new byte[32]);
			Assert.IsFalse(ms.IsClosed, "Underlying stream should NOT be closed initially");

			// Try to extract
			var fastZip = new FastZip();
			fastZip.CreateEmptyDirectories = true;

			Assert.Throws<ZipException>(() => fastZip.ExtractZip(ms, tempFolderPath, FastZip.Overwrite.Always, null, "a", "b", false, false), "Should throw when extracting an invalid file");
			Assert.IsFalse(ms.IsClosed, "inputStream stream should NOT be closed when isStreamOwner is false");

			// test folder should not have been created on error
			Assert.That(Directory.Exists(tempFolderPath), Is.False, "Temp folder path should still not exist");
		}

		/// <summary>
		/// Check that the input stream is closed on error when isStreamOwner is true
		/// </summary>
		[Test]
		public void StreamClosedOnError()
		{
			// test paths
			string tempFilePath = GetTempFilePath();
			Assert.IsNotNull(tempFilePath, "No permission to execute this test?");

			var tempFolderPath = Path.Combine(tempFilePath, Path.GetRandomFileName());
			Assert.That(Directory.Exists(tempFolderPath), Is.False, "Temp folder path should not exist");

			// memory that isn't a valid zip
			var ms = new TrackedMemoryStream(new byte[32]);
			Assert.IsFalse(ms.IsClosed, "Underlying stream should NOT be closed initially");

			// Try to extract
			var fastZip = new FastZip();
			fastZip.CreateEmptyDirectories = true;

			Assert.Throws<ZipException>(() => fastZip.ExtractZip(ms, tempFolderPath, FastZip.Overwrite.Always, null, "a", "b", false, true), "Should throw when extracting an invalid file");
			Assert.IsTrue(ms.IsClosed, "inputStream stream should be closed when isStreamOwner is true");

			// test folder should not have been created on error
			Assert.That(Directory.Exists(tempFolderPath), Is.False, "Temp folder path should still not exist");
		}

		/// <summary>
		/// #426 - set the modified date for created directory entries if the RestoreDateTimeOnExtract option is enabled
		/// </summary>
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void SetDirectoryModifiedDate()
		{
			string tempFilePath = GetTempFilePath();
			Assert.IsNotNull(tempFilePath, "No permission to execute this test?");

			string zipName = Path.Combine(tempFilePath, $"{nameof(SetDirectoryModifiedDate)}.zip");

			EnsureTestDirectoryIsEmpty(tempFilePath);

			var modifiedTime = new DateTime(2001, 1, 2);
			string targetDir = Path.Combine(tempFilePath, ZipTempDir, nameof(SetDirectoryModifiedDate));
			using (FileStream fs = File.Create(zipName))
			{
				using (ZipOutputStream zOut = new ZipOutputStream(fs))
				{
					// Add an empty directory entry, with a specified time field
					var entry = new ZipEntry("emptyFolder/")
					{
						DateTime = modifiedTime
					};
					zOut.PutNextEntry(entry);
				}
			}

			try
			{
				// extract the zip
				var fastZip = new FastZip
				{
					CreateEmptyDirectories = true,
					RestoreDateTimeOnExtract = true
				};
				fastZip.ExtractZip(zipName, targetDir, "zz");

				File.Delete(zipName);

				// Check that the empty sub folder exists and has the expected modlfied date
				string emptyTargetDir = Path.Combine(targetDir, "emptyFolder");

				Assert.That(Directory.Exists(emptyTargetDir), Is.True, "Empty directory should be created");

				var extractedFolderTime = Directory.GetLastWriteTime(emptyTargetDir);
				Assert.That(extractedFolderTime, Is.EqualTo(modifiedTime));
			}
			finally
			{
				// Tidy up
				Directory.Delete(targetDir, true);
			}
		}
	}
}
