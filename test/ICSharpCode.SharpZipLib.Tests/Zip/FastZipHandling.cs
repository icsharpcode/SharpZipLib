using System.IO;
using System.Text.RegularExpressions;
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

		[Test]
		[Category("Zip")]
		public void UnicodeText()
		{
			var zippy = new FastZip();
			var factory = new ZipEntryFactory();
			factory.IsUnicodeText = true;
			zippy.EntryFactory = factory;

			string tempFilePath = GetTempFilePath();
			Assert.IsNotNull(tempFilePath, "No permission to execute this test?");

			const string tempName1 = "a.dat";
			string addFile = Path.Combine(tempFilePath, tempName1);
			MakeTempFile(addFile, 1);

			try {
				var target = new MemoryStream();
				zippy.CreateZip(target, tempFilePath, false, Regex.Escape(tempName1), null);

				var archive = new MemoryStream(target.ToArray());

				using (ZipFile z = new ZipFile(archive)) {
					Assert.AreEqual(1, z.Count);
					Assert.IsTrue(z[0].IsUnicodeText);
				}
			} finally {
				File.Delete(addFile);
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
	}
}
