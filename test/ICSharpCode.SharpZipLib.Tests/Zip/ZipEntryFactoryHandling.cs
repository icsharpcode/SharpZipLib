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
		public void CreatedValues()
		{
			string tempDir = GetTempFilePath();
			Assert.IsNotNull(tempDir, "No permission to execute this test?");

			tempDir = Path.Combine(tempDir, "SharpZipTest");

			if (tempDir != null)
			{
				Directory.CreateDirectory(tempDir);

				try
				{
					// Note the seconds returned will be even!
					var createTime = new DateTime(2100, 2, 27, 11, 07, 56);
					var lastWriteTime = new DateTime(2050, 11, 3, 7, 23, 32);
					var lastAccessTime = new DateTime(2050, 11, 3, 0, 42, 12);

					string tempFile = Path.Combine(tempDir, "SharpZipTest.Zip");
					using (FileStream f = File.Create(tempFile, 1024))
					{
						f.WriteByte(0);
					}

					File.SetCreationTime(tempFile, createTime);
					File.SetLastWriteTime(tempFile, lastWriteTime);
					File.SetLastAccessTime(tempFile, lastAccessTime);

					FileAttributes attributes = FileAttributes.Hidden;

					File.SetAttributes(tempFile, attributes);
					ZipEntryFactory factory = null;
					ZipEntry entry;
					int combinedAttributes = 0;

					try
					{
						factory = new ZipEntryFactory();

						factory.Setting = ZipEntryFactory.TimeSetting.CreateTime;
						factory.GetAttributes = ~((int)FileAttributes.ReadOnly);
						factory.SetAttributes = (int)FileAttributes.ReadOnly;
						combinedAttributes = (int)(FileAttributes.ReadOnly | FileAttributes.Hidden);

						entry = factory.MakeFileEntry(tempFile);
						Assert.AreEqual(createTime, entry.DateTime, "Create time failure");
						Assert.AreEqual(entry.ExternalFileAttributes, combinedAttributes);
						Assert.AreEqual(1, entry.Size);

						factory.Setting = ZipEntryFactory.TimeSetting.LastAccessTime;
						entry = factory.MakeFileEntry(tempFile);
						Assert.AreEqual(lastAccessTime, entry.DateTime, "Access time failure");
						Assert.AreEqual(1, entry.Size);

						factory.Setting = ZipEntryFactory.TimeSetting.LastWriteTime;
						entry = factory.MakeFileEntry(tempFile);
						Assert.AreEqual(lastWriteTime, entry.DateTime, "Write time failure");
						Assert.AreEqual(1, entry.Size);
					}
					finally
					{
						File.Delete(tempFile);
					}

					// Do the same for directories
					// Note the seconds returned will be even!
					createTime = new DateTime(2090, 2, 27, 11, 7, 56);
					lastWriteTime = new DateTime(2107, 12, 31, 23, 59, 58);
					lastAccessTime = new DateTime(1980, 1, 1, 1, 0, 0);

					Directory.SetCreationTime(tempDir, createTime);
					Directory.SetLastWriteTime(tempDir, lastWriteTime);
					Directory.SetLastAccessTime(tempDir, lastAccessTime);

					factory.Setting = ZipEntryFactory.TimeSetting.CreateTime;
					entry = factory.MakeDirectoryEntry(tempDir);
					Assert.AreEqual(createTime, entry.DateTime, "Directory create time failure");
					Assert.IsTrue((entry.ExternalFileAttributes & (int)FileAttributes.Directory) == (int)FileAttributes.Directory);

					factory.Setting = ZipEntryFactory.TimeSetting.LastAccessTime;
					entry = factory.MakeDirectoryEntry(tempDir);
					Assert.AreEqual(lastAccessTime, entry.DateTime, "Directory access time failure");

					factory.Setting = ZipEntryFactory.TimeSetting.LastWriteTime;
					entry = factory.MakeDirectoryEntry(tempDir);
					Assert.AreEqual(lastWriteTime, entry.DateTime, "Directory write time failure");
				}
				finally
				{
					Directory.Delete(tempDir, true);
				}
			}
		}
	}
}
