using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using System;
using System.IO;

namespace ICSharpCode.SharpZipLib.Tests.Zip
{
	[TestFixture]
	public class ZipEntryFactoryHandling : ZipBase
	{
		// TODO: Complete testing for ZipEntryFactory

		// FileEntry creation and retrieval of information
		// DirectoryEntry creation and retrieval of information.

		[Test]
		[Category("Zip")]
		public void Defaults()
		{
			DateTime testStart = DateTime.Now;
			var f = new ZipEntryFactory();
			Assert.IsNotNull(f.NameTransform);
			Assert.AreEqual(-1, f.GetAttributes);
			Assert.AreEqual(0, f.SetAttributes);
			Assert.AreEqual(ZipEntryFactory.TimeSetting.LastWriteTime, f.Setting);

			Assert.LessOrEqual(testStart, f.FixedDateTime);
			Assert.GreaterOrEqual(DateTime.Now, f.FixedDateTime);

			f = new ZipEntryFactory(ZipEntryFactory.TimeSetting.LastAccessTimeUtc);
			Assert.IsNotNull(f.NameTransform);
			Assert.AreEqual(-1, f.GetAttributes);
			Assert.AreEqual(0, f.SetAttributes);
			Assert.AreEqual(ZipEntryFactory.TimeSetting.LastAccessTimeUtc, f.Setting);
			Assert.LessOrEqual(testStart, f.FixedDateTime);
			Assert.GreaterOrEqual(DateTime.Now, f.FixedDateTime);

			var fixedDate = new DateTime(1999, 1, 2);
			f = new ZipEntryFactory(fixedDate);
			Assert.IsNotNull(f.NameTransform);
			Assert.AreEqual(-1, f.GetAttributes);
			Assert.AreEqual(0, f.SetAttributes);
			Assert.AreEqual(ZipEntryFactory.TimeSetting.Fixed, f.Setting);
			Assert.AreEqual(fixedDate, f.FixedDateTime);
		}

		[Test]
		[Category("Zip")]
		public void CreateInMemoryValues()
		{
			string tempFile = "bingo:";

			// Note the seconds returned will be even!
			var epochTime = new DateTime(1980, 1, 1);
			var createTime = new DateTime(2100, 2, 27, 11, 07, 56);
			var lastWriteTime = new DateTime(2050, 11, 3, 7, 23, 32);
			var lastAccessTime = new DateTime(2050, 11, 3, 0, 42, 12);

			var factory = new ZipEntryFactory();
			ZipEntry entry;
			int combinedAttributes;

			DateTime startTime = DateTime.Now;

			factory.Setting = ZipEntryFactory.TimeSetting.CreateTime;
			factory.GetAttributes = ~((int)FileAttributes.ReadOnly);
			factory.SetAttributes = (int)FileAttributes.ReadOnly;
			combinedAttributes = (int)FileAttributes.ReadOnly;

			entry = factory.MakeFileEntry(tempFile, false);
			Assert.IsTrue(TestHelper.CompareDosDateTimes(startTime, entry.DateTime) <= 0, "Create time failure");
			Assert.AreEqual(entry.ExternalFileAttributes, combinedAttributes);
			Assert.AreEqual(-1, entry.Size);

			factory.FixedDateTime = startTime;
			factory.Setting = ZipEntryFactory.TimeSetting.Fixed;
			entry = factory.MakeFileEntry(tempFile, false);
			Assert.AreEqual(0, TestHelper.CompareDosDateTimes(startTime, entry.DateTime), "Access time failure");
			Assert.AreEqual(-1, entry.Size);

			factory.Setting = ZipEntryFactory.TimeSetting.LastWriteTime;
			entry = factory.MakeFileEntry(tempFile, false);
			Assert.IsTrue(TestHelper.CompareDosDateTimes(startTime, entry.DateTime) <= 0, "Write time failure");
			Assert.AreEqual(-1, entry.Size);
		}

		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		[Platform("Win32NT")]
		public void CreatedFileEntriesUsesExpectedAttributes()
		{
			string tempDir = GetTempFilePath();
			if (tempDir == null) Assert.Inconclusive("No permission to execute this test?");

			tempDir = Path.Combine(tempDir, "SharpZipTest");
			Directory.CreateDirectory(tempDir);

			try
			{
				string tempFile = Path.Combine(tempDir, "SharpZipTest.Zip");
				
				using (FileStream f = File.Create(tempFile, 1024))
				{
					f.WriteByte(0);
				}

				FileAttributes attributes = FileAttributes.Hidden;

				File.SetAttributes(tempFile, attributes);
				ZipEntryFactory factory = null;
				ZipEntry entry;
				int combinedAttributes = 0;

				try
				{
					factory = new ZipEntryFactory();

					factory.GetAttributes = ~((int)FileAttributes.ReadOnly);
					factory.SetAttributes = (int)FileAttributes.ReadOnly;
					combinedAttributes = (int)(FileAttributes.ReadOnly | FileAttributes.Hidden);

					entry = factory.MakeFileEntry(tempFile);
					Assert.AreEqual(entry.ExternalFileAttributes, combinedAttributes);
					Assert.AreEqual(1, entry.Size);
				}
				finally
				{
					File.Delete(tempFile);
				}
			}
			finally
			{
				Directory.Delete(tempDir, true);
			}
			
		}

		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		[TestCase(ZipEntryFactory.TimeSetting.CreateTime)]
		[TestCase(ZipEntryFactory.TimeSetting.LastAccessTime)]
		[TestCase(ZipEntryFactory.TimeSetting.LastWriteTime)]
		public void CreatedFileEntriesUsesExpectedTime(ZipEntryFactory.TimeSetting timeSetting)
		{
			string tempDir = GetTempFilePath();
			if (tempDir == null) Assert.Inconclusive("No permission to execute this test?");

			tempDir = Path.Combine(tempDir, "SharpZipTest");

			// Note the seconds returned will be even!
			var expectedTime = new DateTime(2100, 2, 27, 11, 07, 56);

			Directory.CreateDirectory(tempDir);

			try
			{

				string tempFile = Path.Combine(tempDir, "SharpZipTest.Zip");
				
				using (FileStream f = File.Create(tempFile, 1024))
				{
					f.WriteByte(0);
				}

				DateTime fileTime = DateTime.MinValue;

				if (timeSetting == ZipEntryFactory.TimeSetting.CreateTime) {
					File.SetCreationTime(tempFile, expectedTime);
					fileTime = File.GetCreationTime(tempFile);
				}

				if (timeSetting == ZipEntryFactory.TimeSetting.LastAccessTime){
					File.SetLastAccessTime(tempFile, expectedTime);
					fileTime = File.GetLastAccessTime(tempFile);
				}

				if (timeSetting == ZipEntryFactory.TimeSetting.LastWriteTime) {
					File.SetLastWriteTime(tempFile, expectedTime);
					fileTime = File.GetLastWriteTime(tempFile);
				}

				if(fileTime != expectedTime) {
					Assert.Inconclusive("File time could not be altered");
				}

				var factory = new ZipEntryFactory();

				factory.Setting = timeSetting;

				var entry = factory.MakeFileEntry(tempFile);
				Assert.AreEqual(expectedTime, entry.DateTime);
				Assert.AreEqual(1, entry.Size);

			}
			finally
			{
				Directory.Delete(tempDir, true);
			}
			
		}

		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		[TestCase(ZipEntryFactory.TimeSetting.CreateTime)]
		[TestCase(ZipEntryFactory.TimeSetting.LastAccessTime)]
		[TestCase(ZipEntryFactory.TimeSetting.LastWriteTime)]
		public void CreatedDirectoryEntriesUsesExpectedTime(ZipEntryFactory.TimeSetting timeSetting)
		{
			string tempDir = GetTempFilePath();
			if (tempDir == null) Assert.Inconclusive("No permission to execute this test?");

			tempDir = Path.Combine(tempDir, "SharpZipTest");

			// Note the seconds returned will be even!
			var expectedTime = new DateTime(2100, 2, 27, 11, 07, 56);

			Directory.CreateDirectory(tempDir);

			try
			{

				string tempFile = Path.Combine(tempDir, "SharpZipTest.Zip");
				
				using (FileStream f = File.Create(tempFile, 1024))
				{
					f.WriteByte(0);
				}

				DateTime dirTime = DateTime.MinValue;

				if (timeSetting == ZipEntryFactory.TimeSetting.CreateTime) {
					Directory.SetCreationTime(tempFile, expectedTime);
					dirTime = Directory.GetCreationTime(tempDir);
				}

				if (timeSetting == ZipEntryFactory.TimeSetting.LastAccessTime){
					Directory.SetLastAccessTime(tempDir, expectedTime);
					dirTime = Directory.GetLastAccessTime(tempDir);
				}

				if (timeSetting == ZipEntryFactory.TimeSetting.LastWriteTime) {
					Directory.SetLastWriteTime(tempDir, expectedTime);
					dirTime = Directory.GetLastWriteTime(tempDir);
				}

				if(dirTime != expectedTime) {
					Assert.Inconclusive("Directory time could not be altered");
				}

				var factory = new ZipEntryFactory();

				factory.Setting = timeSetting;

				var entry = factory.MakeDirectoryEntry(tempDir);
				Assert.AreEqual(expectedTime, entry.DateTime);
			}
			finally
			{
				Directory.Delete(tempDir, true);
			}
			
		}
	}
}
