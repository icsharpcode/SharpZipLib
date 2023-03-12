using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Tests.TestSupport;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Does = ICSharpCode.SharpZipLib.Tests.TestSupport.Does;

namespace ICSharpCode.SharpZipLib.Tests.Zip
{
	[TestFixture]
	public class ZipFileHandling : ZipBase
	{
		[Test]
		[Category("Zip")]
		public void NullStreamDetected()
		{
			ZipFile bad = null;
			FileStream nullStream = null;

			bool nullStreamDetected = false;

			try
			{
				// ReSharper disable once ExpressionIsAlwaysNull
				bad = new ZipFile(nullStream);
			}
			catch
			{
				nullStreamDetected = true;
			}

			Assert.IsTrue(nullStreamDetected, "Null stream should be detected in ZipFile constructor");
			Assert.IsNull(bad, "ZipFile instance should not be created");
		}

		/// <summary>
		/// Check that adding too many entries is detected and handled
		/// </summary>
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void Zip64Entries()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");

			const int target = 65537;

			using (ZipFile zipFile = ZipFile.Create(Path.GetTempFileName()))
			{
				zipFile.BeginUpdate();

				for (int i = 0; i < target; ++i)
				{
					var ze = new ZipEntry(i.ToString());
					ze.CompressedSize = 0;
					ze.Size = 0;
					zipFile.Add(ze);
				}
				zipFile.CommitUpdate();

				Assert.That(zipFile, Does.PassTestArchive());
				Assert.AreEqual(target, zipFile.Count, "Incorrect number of entries stored");
			}
		}

		[Test]
		[Category("Zip")]
		public void EmbeddedArchive()
		{
			var memStream = new MemoryStream();
			using (ZipFile f = new ZipFile(memStream))
			{
				f.IsStreamOwner = false;

				var m = new StringMemoryDataSource("0000000");
				f.BeginUpdate(new MemoryArchiveStorage());
				f.Add(m, "a.dat");
				f.Add(m, "b.dat");
				f.CommitUpdate();
				Assert.That(f, Does.PassTestArchive());
			}

			byte[] rawArchive = memStream.ToArray();
			byte[] pseudoSfx = new byte[1049 + rawArchive.Length];
			Array.Copy(rawArchive, 0, pseudoSfx, 1049, rawArchive.Length);

			memStream = new MemoryStream(pseudoSfx);
			using (ZipFile f = new ZipFile(memStream))
			{
				for (int index = 0; index < f.Count; ++index)
				{
					Stream entryStream = f.GetInputStream(index);
					var data = new MemoryStream();
					StreamUtils.Copy(entryStream, data, new byte[128]);
					string contents = Encoding.ASCII.GetString(data.ToArray());
					Assert.AreEqual("0000000", contents);
				}
			}
		}

		[Test]
		[Category("Zip")]
		public void Zip64Useage()
		{
			var memStream = new MemoryStream();
			using (ZipFile f = new ZipFile(memStream))
			{
				f.IsStreamOwner = false;
				f.UseZip64 = UseZip64.On;

				var m = new StringMemoryDataSource("0000000");
				f.BeginUpdate(new MemoryArchiveStorage());
				f.Add(m, "a.dat");
				f.Add(m, "b.dat");
				f.CommitUpdate();
				Assert.That(f, Does.PassTestArchive());
			}

			byte[] rawArchive = memStream.ToArray();

			byte[] pseudoSfx = new byte[1049 + rawArchive.Length];
			Array.Copy(rawArchive, 0, pseudoSfx, 1049, rawArchive.Length);

			memStream = new MemoryStream(pseudoSfx);
			using (ZipFile f = new ZipFile(memStream))
			{
				for (int index = 0; index < f.Count; ++index)
				{
					Stream entryStream = f.GetInputStream(index);
					var data = new MemoryStream();
					StreamUtils.Copy(entryStream, data, new byte[128]);
					string contents = Encoding.ASCII.GetString(data.ToArray());
					Assert.AreEqual("0000000", contents);
				}
			}
		}

		/// <summary>
		/// Test that entries can be removed from a Zip64 file
		/// </summary>
		[Test]
		[Category("Zip")]
		public void Zip64Update()
		{
			using (var memStream = new MemoryStream())
			{
				using (ZipFile f = new ZipFile(memStream, leaveOpen: true))
				{
					f.UseZip64 = UseZip64.On;

					var m = new StringMemoryDataSource("0000000");
					f.BeginUpdate(new MemoryArchiveStorage());
					f.Add(m, "a.dat");
					f.Add(m, "b.dat");
					f.CommitUpdate();
					Assert.That(f.TestArchive(true), Is.True, "initial archive should be valid");
				}

				memStream.Seek(0, SeekOrigin.Begin);

				using (ZipFile f = new ZipFile(memStream, leaveOpen: true))
				{
					Assert.That(f.Count, Is.EqualTo(2), "Archive should have 2 entries");

					f.BeginUpdate(new MemoryArchiveStorage());
					f.Delete("b.dat");
					f.CommitUpdate();
					Assert.That(f.TestArchive(true), Is.True, "modified archive should be valid");
				}

				memStream.Seek(0, SeekOrigin.Begin);

				using (ZipFile f = new ZipFile(memStream, leaveOpen: true))
				{
					Assert.That(f.Count, Is.EqualTo(1), "Archive should have 1 entry");

					for (int index = 0; index < f.Count; ++index)
					{
						Stream entryStream = f.GetInputStream(index);
						var data = new MemoryStream();
						StreamUtils.Copy(entryStream, data, new byte[128]);
						string contents = Encoding.ASCII.GetString(data.ToArray());
						Assert.That(contents, Is.EqualTo("0000000"), "archive member data should be correct");
					}
				}
			}
		}

		/// <summary>
		/// Test for issue #403 - zip64 locator signature bytes being present in a contained file,
		/// when the outer zip file isn't using zip64
		/// </summary>
		[Test]
		[Category("Zip")]
		public void FakeZip64Locator()
		{
			using (var memStream = new MemoryStream())
			{
				// set the file contents to the zip 64 directory locator signature
				var locatorValue = ZipConstants.Zip64CentralDirLocatorSignature;
				var locatorBytes = new byte[] { (byte)(locatorValue & 0xff), (byte)((locatorValue >> 8) & 0xff), (byte)((locatorValue >> 16) & 0xff), (byte)((locatorValue >> 24) & 0xff) };

				using (ZipFile f = new ZipFile(memStream, leaveOpen: true))
				{
					var m = new MemoryDataSource(locatorBytes);

					// Add the entry - set compression method to stored so the signature bytes remain as expected
					f.BeginUpdate(new MemoryArchiveStorage());
					f.Add(m, "a.dat", CompressionMethod.Stored);
					f.CommitUpdate();
					Assert.That(f, Does.PassTestArchive());
				}

				memStream.Seek(0, SeekOrigin.Begin);

				// Check that the archive is readable.
				using (ZipFile f = new ZipFile(memStream, leaveOpen: true))
				{
					Assert.That(f.Count, Is.EqualTo(1), "Archive should have 1 entry");
				}
			}
		}

		[Test]
		[Category("Zip")]
		[Explicit]
		public void Zip64Offset()
		{
			// TODO: Test to check that a zip64 offset value is loaded correctly.
			// Changes in ZipEntry to CentralHeaderRequiresZip64 and LocalHeaderRequiresZip64
			// were not quite correct...
		}

		[Test]
		[Category("Zip")]
		public void BasicEncryption()
		{
			const string testValue = "0001000";
			var memStream = new MemoryStream();
			using (var zf = new ZipFile(memStream))
			{
				zf.IsStreamOwner = false;
				zf.Password = "Hello";

				var m = new StringMemoryDataSource(testValue);
				zf.BeginUpdate(new MemoryArchiveStorage());
				zf.Add(m, "a.dat");
				zf.CommitUpdate();
				Assert.IsTrue(zf.TestArchive(testData: true), "Archive test should pass");
			}

			using (var zf = new ZipFile(memStream))
			{
				zf.Password = "Hello";
				var ze = zf[0];

				Assert.IsTrue(ze.IsCrypted, "Entry should be encrypted");
				using (var r = new StreamReader(zf.GetInputStream(entryIndex: 0)))
				{
					var data = r.ReadToEnd();
					Assert.AreEqual(testValue, data);
				}
			}
		}

		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void BasicEncryptionToDisk()
		{
			const string testValue = "0001000";
			var tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");

			tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");

			using (var zf = ZipFile.Create(tempFile))
			{
				zf.Password = "Hello";

				var m = new StringMemoryDataSource(testValue);
				zf.BeginUpdate();
				zf.Add(m, "a.dat");
				zf.CommitUpdate();
			}

			using (var zf = new ZipFile(tempFile))
			{
				zf.Password = "Hello";
				Assert.IsTrue(zf.TestArchive(testData: true), "Archive test should pass");
			}

			using (var zf = new ZipFile(tempFile))
			{
				zf.Password = "Hello";
				ZipEntry ze = zf[0];

				Assert.IsTrue(ze.IsCrypted, "Entry should be encrypted");
				using (var r = new StreamReader(zf.GetInputStream(entryIndex: 0)))
				{
					var data = r.ReadToEnd();
					Assert.AreEqual(testValue, data);
				}
			}

			File.Delete(tempFile);
		}

		[Test]
		[Category("Zip")]
		public void AddEncryptedEntriesToExistingArchive()
		{
			const string testValue = "0001000";
			var memStream = new MemoryStream();
			using (ZipFile f = new ZipFile(memStream))
			{
				f.IsStreamOwner = false;
				f.UseZip64 = UseZip64.Off;

				var m = new StringMemoryDataSource(testValue);
				f.BeginUpdate(new MemoryArchiveStorage());
				f.Add(m, "a.dat");
				f.CommitUpdate();
				Assert.IsTrue(f.TestArchive(true), "Archive test should pass");
			}

			using (ZipFile g = new ZipFile(memStream))
			{
				ZipEntry ze = g[0];

				Assert.IsFalse(ze.IsCrypted, "Entry should NOT be encrypted");
				using (StreamReader r = new StreamReader(g.GetInputStream(0)))
				{
					string data = r.ReadToEnd();
					Assert.AreEqual(testValue, data);
				}

				var n = new StringMemoryDataSource(testValue);

				g.Password = "Axolotyl";
				g.UseZip64 = UseZip64.Off;
				g.IsStreamOwner = false;
				g.BeginUpdate();
				g.Add(n, "a1.dat");
				g.CommitUpdate();
				Assert.IsTrue(g.TestArchive(true), "Archive test should pass");
				ze = g[1];
				Assert.IsTrue(ze.IsCrypted, "New entry should be encrypted");

				using (StreamReader r = new StreamReader(g.GetInputStream(0)))
				{
					string data = r.ReadToEnd();
					Assert.AreEqual(testValue, data);
				}
			}
		}

		private void TryDeleting(byte[] master, int totalEntries, int additions, params string[] toDelete)
		{
			var ms = new MemoryStream();
			ms.Write(master, 0, master.Length);

			using (ZipFile f = new ZipFile(ms))
			{
				f.IsStreamOwner = false;
				Assert.AreEqual(totalEntries, f.Count);
				Assert.That(f, Does.PassTestArchive());
				f.BeginUpdate(new MemoryArchiveStorage());

				for (int i = 0; i < additions; ++i)
				{
					f.Add(new StringMemoryDataSource("Another great file"),
						string.Format("Add{0}.dat", i + 1));
				}

				foreach (string name in toDelete)
				{
					f.Delete(name);
				}
				f.CommitUpdate();

				// write stream to file to assist debugging.
				// WriteToFile(@"c:\aha.zip", ms.ToArray());

				int newTotal = totalEntries + additions - toDelete.Length;
				Assert.AreEqual(newTotal, f.Count,
					string.Format("Expected {0} entries after update found {1}", newTotal, f.Count));
				Assert.IsTrue(f.TestArchive(true), "Archive test should pass");
			}
		}

		private void TryDeleting(byte[] master, int totalEntries, int additions, params int[] toDelete)
		{
			var ms = new MemoryStream();
			ms.Write(master, 0, master.Length);

			using (ZipFile f = new ZipFile(ms))
			{
				f.IsStreamOwner = false;
				Assert.AreEqual(totalEntries, f.Count);
				Assert.That(f, Does.PassTestArchive());
				f.BeginUpdate(new MemoryArchiveStorage());

				for (int i = 0; i < additions; ++i)
				{
					f.Add(new StringMemoryDataSource("Another great file"),
						string.Format("Add{0}.dat", i + 1));
				}

				foreach (int i in toDelete)
				{
					f.Delete(f[i]);
				}
				f.CommitUpdate();

				/* write stream to file to assist debugging.
								byte[] data = ms.ToArray();
								using ( FileStream fs = File.Open(@"c:\aha.zip", FileMode.Create, FileAccess.ReadWrite, FileShare.Read) ) {
									fs.Write(data, 0, data.Length);
								}
				*/
				int newTotal = totalEntries + additions - toDelete.Length;
				Assert.AreEqual(newTotal, f.Count,
					string.Format("Expected {0} entries after update found {1}", newTotal, f.Count));
				Assert.IsTrue(f.TestArchive(true), "Archive test should pass");
			}
		}

		[Test]
		[Category("Zip")]
		public void AddAndDeleteEntriesMemory()
		{
			var memStream = new MemoryStream();

			using (ZipFile f = new ZipFile(memStream))
			{
				f.IsStreamOwner = false;

				f.BeginUpdate(new MemoryArchiveStorage());
				f.Add(new StringMemoryDataSource("Hello world"), Utils.SystemRoot + @"a\a.dat");
				f.Add(new StringMemoryDataSource("Another"), @"\b\b.dat");
				f.Add(new StringMemoryDataSource("Mr C"), @"c\c.dat");
				f.Add(new StringMemoryDataSource("Mrs D was a star"), @"d\d.dat");
				f.CommitUpdate();
				Assert.That(f, Does.PassTestArchive());
				foreach (ZipEntry entry in f)
				{
					Console.WriteLine($" - {entry.Name}");
				}
			}

			byte[] master = memStream.ToArray();

			TryDeleting(master, 4, 1, Utils.SystemRoot + @"a\a.dat");
			TryDeleting(master, 4, 1, @"\a\a.dat");
			TryDeleting(master, 4, 1, @"a/a.dat");

			TryDeleting(master, 4, 0, 0);
			TryDeleting(master, 4, 0, 1);
			TryDeleting(master, 4, 0, 2);
			TryDeleting(master, 4, 0, 3);
			TryDeleting(master, 4, 0, 0, 1);
			TryDeleting(master, 4, 0, 0, 2);
			TryDeleting(master, 4, 0, 0, 3);
			TryDeleting(master, 4, 0, 1, 2);
			TryDeleting(master, 4, 0, 1, 3);
			TryDeleting(master, 4, 0, 2);

			TryDeleting(master, 4, 1, 0);
			TryDeleting(master, 4, 1, 1);
			TryDeleting(master, 4, 3, 2);
			TryDeleting(master, 4, 4, 3);
			TryDeleting(master, 4, 10, 0, 1);
			TryDeleting(master, 4, 10, 0, 2);
			TryDeleting(master, 4, 10, 0, 3);
			TryDeleting(master, 4, 20, 1, 2);
			TryDeleting(master, 4, 30, 1, 3);
			TryDeleting(master, 4, 40, 2);
		}

		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void AddAndDeleteEntries()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");

			string addFile = Path.Combine(tempFile, "a.dat");
			MakeTempFile(addFile, 1);

			string addFile2 = Path.Combine(tempFile, "b.dat");
			MakeTempFile(addFile2, 259);

			string addDirectory = Path.Combine(tempFile, "dir");

			tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");

			using (ZipFile f = ZipFile.Create(tempFile))
			{
				f.BeginUpdate();
				f.Add(addFile);
				f.Add(addFile2);
				f.AddDirectory(addDirectory);
				f.CommitUpdate();
				Assert.That(f, Does.PassTestArchive());
			}

			using (ZipFile f = new ZipFile(tempFile))
			{
				Assert.AreEqual(3, f.Count);
				Assert.That(f, Does.PassTestArchive());

				// Delete file
				f.BeginUpdate();
				f.Delete(f[0]);
				f.CommitUpdate();
				Assert.AreEqual(2, f.Count);
				Assert.That(f, Does.PassTestArchive());

				// Delete directory
				f.BeginUpdate();
				f.Delete(f[1]);
				f.CommitUpdate();
				Assert.AreEqual(1, f.Count);
				Assert.That(f, Does.PassTestArchive());
			}

			File.Delete(addFile);
			File.Delete(addFile2);
			File.Delete(tempFile);
		}

		/// <summary>
		/// Simple round trip test for ZipFile class
		/// </summary>
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void RoundTrip()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");

			tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");

			try
			{
				MakeZipFile(tempFile, "", 10, 1024, "");

				using (ZipFile zipFile = new ZipFile(tempFile))
				{
					foreach (ZipEntry e in zipFile)
					{
						Stream instream = zipFile.GetInputStream(e);
						CheckKnownEntry(instream, 1024);
					}
					zipFile.Close();
				}
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		/// <summary>
		/// Simple round trip test for ZipFile class
		/// </summary>
		[Test]
		[Category("Zip")]
		public void RoundTripInMemory()
		{
			var storage = new MemoryStream();
			MakeZipFile(storage, false, "", 10, 1024, "");

			using (ZipFile zipFile = new ZipFile(storage))
			{
				foreach (ZipEntry e in zipFile)
				{
					Stream instream = zipFile.GetInputStream(e);
					CheckKnownEntry(instream, 1024);
				}
				zipFile.Close();
			}
		}

		/// <summary>
		/// Simple async round trip test for ZipFile class
		/// </summary>
		[TestCase(CompressionMethod.Stored)]
		[TestCase(CompressionMethod.Deflated)]
		[TestCase(CompressionMethod.BZip2)]
		[Category("Zip")]
		[Category("Async")]
		public async Task RoundTripInMemoryAsync(CompressionMethod compressionMethod)
		{
			var storage = new MemoryStream();
			MakeZipFile(storage, compressionMethod, false, "", 10, 1024, "");

			using (ZipFile zipFile = new ZipFile(storage))
			{
				foreach (ZipEntry e in zipFile)
				{
					Stream instream = zipFile.GetInputStream(e);
					await CheckKnownEntryAsync(instream, 1024);
				}
			}
		}

		[Test]
		[Category("Zip")]
		public void AddToEmptyArchive()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");

			string addFile = Path.Combine(tempFile, "a.dat");

			MakeTempFile(addFile, 1);

			try
			{
				tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");

				using (ZipFile f = ZipFile.Create(tempFile))
				{
					f.BeginUpdate();
					f.Add(addFile);
					f.CommitUpdate();
					Assert.AreEqual(1, f.Count);
					Assert.That(f, Does.PassTestArchive());
				}

				using (ZipFile f = new ZipFile(tempFile))
				{
					Assert.AreEqual(1, f.Count);
					f.BeginUpdate();
					f.Delete(f[0]);
					f.CommitUpdate();
					Assert.AreEqual(0, f.Count);
					Assert.That(f, Does.PassTestArchive());
					f.Close();
				}

				File.Delete(tempFile);
			}
			finally
			{
				File.Delete(addFile);
			}
		}

		[Test]
		[Category("Zip")]
		public void CreateEmptyArchive()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");

			tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");

			using (ZipFile f = ZipFile.Create(tempFile))
			{
				f.BeginUpdate();
				f.CommitUpdate();
				Assert.That(f, Does.PassTestArchive());
				f.Close();
			}

			using (ZipFile f = new ZipFile(tempFile))
			{
				Assert.AreEqual(0, f.Count);
			}

			File.Delete(tempFile);
		}

		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void CreateArchiveWithNoCompression()
		{
			using var sourceFile = Utils.GetDummyFile();
			using var zipFile = Utils.GetDummyFile(0);
			var inputContent = File.ReadAllText(sourceFile);
			using (var zf = ZipFile.Create(zipFile))
			{
				zf.BeginUpdate();
				zf.Add(sourceFile, CompressionMethod.Stored);
				zf.CommitUpdate();
				Assert.That(zf, Does.PassTestArchive());
				zf.Close();
			}

			using (var zf = new ZipFile(zipFile))
			{
				Assert.AreEqual(1, zf.Count);
				using (var sr = new StreamReader(zf.GetInputStream(zf[0])))
				{
					var outputContent = sr.ReadToEnd();
					Assert.AreEqual(inputContent, outputContent, "extracted content does not match source content");
				}
			}
		}

		/// <summary>
		/// Check that ZipFile finds entries when its got a long comment
		/// </summary>
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void FindEntriesInArchiveWithLongComment()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");

			tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
			var longComment = new String('A', 65535);
			MakeZipFile(tempFile, "", 1, 1, longComment);

			try
			{
				using (ZipFile zipFile = new ZipFile(tempFile))
				{
					foreach (ZipEntry e in zipFile)
					{
						Stream instream = zipFile.GetInputStream(e);
						CheckKnownEntry(instream, 1);
					}
					zipFile.Close();
				}
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		/// <summary>
		/// Check that ZipFile doesnt find entries when there is more than 64K of data at the end.
		/// </summary>
		/// <remarks>
		/// This may well be flawed but is the current behaviour.
		/// </remarks>
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void FindEntriesInArchiveExtraData()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");

			tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
			var longComment = new String('A', 65535);
			FileStream tempStream = File.Create(tempFile);
			MakeZipFile(tempStream, false, "", 1, 1, longComment);

			tempStream.WriteByte(85);
			tempStream.Close();

			bool fails = false;
			try
			{
				using (ZipFile zipFile = new ZipFile(tempFile))
				{
					foreach (ZipEntry e in zipFile)
					{
						Stream instream = zipFile.GetInputStream(e);
						CheckKnownEntry(instream, 1);
					}
					zipFile.Close();
				}
			}
			catch
			{
				fails = true;
			}

			File.Delete(tempFile);
			Assert.IsTrue(fails, "Currently zip file wont be found");
		}

		/// <summary>
		/// Test ZipFile Find method operation
		/// </summary>
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void FindEntry()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");

			tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
			MakeZipFile(tempFile, new string[] { "Farriera", "Champagne", "Urban myth" }, 10, "Aha");

			using (ZipFile zipFile = new ZipFile(tempFile))
			{
				Assert.AreEqual(3, zipFile.Count, "Expected 1 entry");

				int testIndex = zipFile.FindEntry("Farriera", false);
				Assert.AreEqual(0, testIndex, "Case sensitive find failure");
				Assert.IsTrue(string.Compare(zipFile[testIndex].Name, "Farriera", StringComparison.Ordinal) == 0);

				testIndex = zipFile.FindEntry("Farriera", true);
				Assert.AreEqual(0, testIndex, "Case insensitive find failure");
				Assert.IsTrue(string.Compare(zipFile[testIndex].Name, "Farriera", StringComparison.OrdinalIgnoreCase) == 0);

				testIndex = zipFile.FindEntry("urban mYTH", false);
				Assert.AreEqual(-1, testIndex, "Case sensitive find failure");

				testIndex = zipFile.FindEntry("urban mYTH", true);
				Assert.AreEqual(2, testIndex, "Case insensitive find failure");
				Assert.IsTrue(string.Compare(zipFile[testIndex].Name, "urban mYTH", StringComparison.OrdinalIgnoreCase) == 0);

				testIndex = zipFile.FindEntry("Champane.", false);
				Assert.AreEqual(-1, testIndex, "Case sensitive find failure");

				testIndex = zipFile.FindEntry("Champane.", true);
				Assert.AreEqual(-1, testIndex, "Case insensitive find failure");

				zipFile.Close();
			}
			File.Delete(tempFile);
		}

		/// <summary>
		/// Check that ZipFile class handles no entries in zip file
		/// </summary>
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void HandlesNoEntries()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");

			tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
			MakeZipFile(tempFile, "", 0, 1, "Aha");

			using (ZipFile zipFile = new ZipFile(tempFile))
			{
				Assert.AreEqual(0, zipFile.Count);
				zipFile.Close();
			}

			File.Delete(tempFile);
		}

		[Test]
		[Category("Zip")]
		public void ArchiveTesting()
		{
			byte[] originalData = null;
			byte[] compressedData = MakeInMemoryZip(ref originalData, CompressionMethod.Deflated,
				6, 1024, null, true);

			var ms = new MemoryStream(compressedData);
			ms.Seek(0, SeekOrigin.Begin);

			using (ZipFile testFile = new ZipFile(ms))
			{
				Assert.That(testFile, Does.PassTestArchive(), "Unexpected error in archive detected");

				byte[] corrupted = new byte[compressedData.Length];
				Array.Copy(compressedData, corrupted, compressedData.Length);

				corrupted[123] = (byte)(~corrupted[123] & 0xff);
				ms = new MemoryStream(corrupted);
			}

			using (ZipFile testFile = new ZipFile(ms))
			{
				Assert.That(testFile, Does.Not.PassTestArchive(), "Error in archive not detected");
			}
		}

		private void TestDirectoryEntry(MemoryStream s)
		{
			var outStream = new ZipOutputStream(s);
			outStream.IsStreamOwner = false;
			outStream.PutNextEntry(new ZipEntry("YeOldeDirectory/"));
			outStream.Close();

			var ms2 = new MemoryStream(s.ToArray());
			using (ZipFile zf = new ZipFile(ms2))
			{
				Assert.That(zf, Does.PassTestArchive());
			}
		}

		[Test]
		[Category("Zip")]
		public void TestDirectoryEntry()
		{
			TestDirectoryEntry(new MemoryStream());
			TestDirectoryEntry(new MemoryStreamWithoutSeek());
		}

		private void TestEncryptedDirectoryEntry(MemoryStream s, int aesKeySize)
		{
			var outStream = new ZipOutputStream(s);
			outStream.Password = "Tonto hand me a beer";

			outStream.IsStreamOwner = false;
			outStream.PutNextEntry(new ZipEntry("YeUnreadableDirectory/") { AESKeySize = aesKeySize } );
			outStream.Close();

			var ms2 = new MemoryStream(s.ToArray());
			using (ZipFile zf = new ZipFile(ms2))
			{
				Assert.That(zf, Does.PassTestArchive());
			}
		}

		[Test]
		[Category("Zip")]
		public void TestEncryptedDirectoryEntry([Values(0, 128, 256)]int aesKeySize)
		{
			TestEncryptedDirectoryEntry(new MemoryStream(), aesKeySize);
			TestEncryptedDirectoryEntry(new MemoryStreamWithoutSeek(), aesKeySize);
		}

		[Test]
		[Category("Zip")]
		public void Crypto_AddEncryptedEntryToExistingArchiveSafe()
		{
			var ms = new MemoryStream();

			byte[] rawData;

			using (ZipFile testFile = new ZipFile(ms))
			{
				testFile.IsStreamOwner = false;
				testFile.BeginUpdate();
				testFile.Add(new StringMemoryDataSource("Aha"), "No1", CompressionMethod.Stored);
				testFile.Add(new StringMemoryDataSource("And so it goes"), "No2", CompressionMethod.Stored);
				testFile.Add(new StringMemoryDataSource("No3"), "No3", CompressionMethod.Stored);
				testFile.CommitUpdate();

				Assert.That(testFile, Does.PassTestArchive());
				rawData = ms.ToArray();
			}

			ms = new MemoryStream(rawData);

			using (ZipFile testFile = new ZipFile(ms))
			{
				Assert.That(testFile, Does.PassTestArchive());

				testFile.BeginUpdate(new MemoryArchiveStorage(FileUpdateMode.Safe));
				testFile.Password = "pwd";
				testFile.Add(new StringMemoryDataSource("Zapata!"), "encrypttest.xml");
				testFile.CommitUpdate();

				Assert.That(testFile, Does.PassTestArchive());

				int entryIndex = testFile.FindEntry("encrypttest.xml", true);
				Assert.IsNotNull(entryIndex >= 0);
				Assert.IsTrue(testFile[entryIndex].IsCrypted);
			}
		}

		[Test]
		[Category("Zip")]
		public void Crypto_AddEncryptedEntryToExistingArchiveDirect()
		{
			var ms = new MemoryStream();

			using (ZipFile testFile = new ZipFile(ms))
			{
				testFile.IsStreamOwner = false;
				testFile.BeginUpdate();
				testFile.Add(new StringMemoryDataSource("Aha"), "No1", CompressionMethod.Stored);
				testFile.Add(new StringMemoryDataSource("And so it goes"), "No2", CompressionMethod.Stored);
				testFile.Add(new StringMemoryDataSource("No3"), "No3", CompressionMethod.Stored);
				testFile.CommitUpdate();

				Assert.That(testFile, Does.PassTestArchive());
			}

			using (ZipFile testFile = new ZipFile(ms))
			{
				Assert.That(testFile, Does.PassTestArchive());
				testFile.IsStreamOwner = true;

				testFile.BeginUpdate();
				testFile.Password = "pwd";
				testFile.Add(new StringMemoryDataSource("Zapata!"), "encrypttest.xml");
				testFile.CommitUpdate();

				Assert.That(testFile, Does.PassTestArchive());

				int entryIndex = testFile.FindEntry("encrypttest.xml", true);
				Assert.IsNotNull(entryIndex >= 0);
				Assert.IsTrue(testFile[entryIndex].IsCrypted);
			}
		}

		[Test]
		[Category("Zip")]
		[Category("Unicode")]
		public void UnicodeNames()
		{
			using var memStream = new MemoryStream();
			using (var f = new ZipFile(memStream))
			{
				f.IsStreamOwner = false;

				f.BeginUpdate(new MemoryArchiveStorage());
				foreach (var (language, name, _) in StringTesting.TestSamples)
				{
					f.Add(new StringMemoryDataSource(language), name,
						CompressionMethod.Deflated, useUnicodeText: true);
				}
				f.CommitUpdate();

				Assert.That(f, Does.PassTestArchive());
			}
			memStream.Seek(0, SeekOrigin.Begin);
			using (var zf = new ZipFile(memStream))
			{
				foreach (var name in StringTesting.Filenames)
				{
					string content;
					var index = zf.FindEntry(name, ignoreCase: true);
					var entry = zf[index];

					using (var entryStream = zf.GetInputStream(entry))
					using (var sr = new StreamReader(entryStream))
					{
						content = sr.ReadToEnd();
					}

					TestContext.WriteLine($"Entry #{index}: {name}, Content: {content}");

					Assert.IsTrue(index >= 0);
					Assert.AreEqual(name, entry.Name);
				}
			}
		}

		[Test]
		[Category("Zip")]
		public void UpdateCommentOnlyInMemory()
		{
			var ms = new MemoryStream();

			using (ZipFile testFile = new ZipFile(ms))
			{
				testFile.IsStreamOwner = false;
				testFile.BeginUpdate();
				testFile.Add(new StringMemoryDataSource("Aha"), "No1", CompressionMethod.Stored);
				testFile.Add(new StringMemoryDataSource("And so it goes"), "No2", CompressionMethod.Stored);
				testFile.Add(new StringMemoryDataSource("No3"), "No3", CompressionMethod.Stored);
				testFile.CommitUpdate();

				Assert.That(testFile, Does.PassTestArchive());
			}

			using (ZipFile testFile = new ZipFile(ms))
			{
				Assert.That(testFile, Does.PassTestArchive());
				Assert.AreEqual("", testFile.ZipFileComment);
				testFile.IsStreamOwner = false;

				testFile.BeginUpdate();
				testFile.SetComment("Here is my comment");
				testFile.CommitUpdate();

				Assert.That(testFile, Does.PassTestArchive());
			}

			using (ZipFile testFile = new ZipFile(ms))
			{
				Assert.That(testFile, Does.PassTestArchive());
				Assert.AreEqual("Here is my comment", testFile.ZipFileComment);
			}
		}

		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void UpdateCommentOnlyOnDisk()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");

			tempFile = Path.Combine(tempFile, "SharpZipTest.Zip");
			if (File.Exists(tempFile))
			{
				File.Delete(tempFile);
			}

			using (ZipFile testFile = ZipFile.Create(tempFile))
			{
				testFile.BeginUpdate();
				testFile.Add(new StringMemoryDataSource("Aha"), "No1", CompressionMethod.Stored);
				testFile.Add(new StringMemoryDataSource("And so it goes"), "No2", CompressionMethod.Stored);
				testFile.Add(new StringMemoryDataSource("No3"), "No3", CompressionMethod.Stored);
				testFile.CommitUpdate();

				Assert.That(testFile, Does.PassTestArchive());
			}

			using (ZipFile testFile = new ZipFile(tempFile))
			{
				Assert.That(testFile, Does.PassTestArchive());
				Assert.AreEqual("", testFile.ZipFileComment);

				testFile.BeginUpdate(new DiskArchiveStorage(testFile, FileUpdateMode.Direct));
				testFile.SetComment("Here is my comment");
				testFile.CommitUpdate();

				Assert.That(testFile, Does.PassTestArchive());
			}

			using (ZipFile testFile = new ZipFile(tempFile))
			{
				Assert.That(testFile, Does.PassTestArchive());
				Assert.AreEqual("Here is my comment", testFile.ZipFileComment);
			}
			File.Delete(tempFile);

			// Variant using indirect updating.
			using (ZipFile testFile = ZipFile.Create(tempFile))
			{
				testFile.BeginUpdate();
				testFile.Add(new StringMemoryDataSource("Aha"), "No1", CompressionMethod.Stored);
				testFile.Add(new StringMemoryDataSource("And so it goes"), "No2", CompressionMethod.Stored);
				testFile.Add(new StringMemoryDataSource("No3"), "No3", CompressionMethod.Stored);
				testFile.CommitUpdate();

				Assert.That(testFile, Does.PassTestArchive());
			}

			using (ZipFile testFile = new ZipFile(tempFile))
			{
				Assert.That(testFile, Does.PassTestArchive());
				Assert.AreEqual("", testFile.ZipFileComment);

				testFile.BeginUpdate();
				testFile.SetComment("Here is my comment");
				testFile.CommitUpdate();

				Assert.That(testFile, Does.PassTestArchive());
			}

			using (ZipFile testFile = new ZipFile(tempFile))
			{
				Assert.That(testFile, Does.PassTestArchive());
				Assert.AreEqual("Here is my comment", testFile.ZipFileComment);
			}
			File.Delete(tempFile);
		}

		[Test]
		[Category("Zip")]
		public void NameFactory()
		{
			var memStream = new MemoryStream();
			var fixedTime = new DateTime(1981, 4, 3);
			using (ZipFile f = new ZipFile(memStream))
			{
				f.IsStreamOwner = false;
				((ZipEntryFactory)f.EntryFactory).IsUnicodeText = true;
				((ZipEntryFactory)f.EntryFactory).Setting = ZipEntryFactory.TimeSetting.Fixed;
				((ZipEntryFactory)f.EntryFactory).FixedDateTime = fixedTime;
				((ZipEntryFactory)f.EntryFactory).SetAttributes = 1;
				f.BeginUpdate(new MemoryArchiveStorage());

				var names = new string[]
				{
					"\u030A\u03B0",     // Greek
					"\u0680\u0685"      // Arabic
				};

				foreach (string name in names)
				{
					f.Add(new StringMemoryDataSource("Hello world"), name,
						  CompressionMethod.Deflated, true);
				}
				f.CommitUpdate();
				Assert.That(f, Does.PassTestArchive());

				foreach (string name in names)
				{
					int index = f.FindEntry(name, true);

					Assert.IsTrue(index >= 0);
					ZipEntry found = f[index];
					Assert.AreEqual(name, found.Name);
					Assert.IsTrue(found.IsUnicodeText);
					Assert.AreEqual(fixedTime, found.DateTime);
					Assert.IsTrue(found.IsDOSEntry);
				}
			}
		}

		[Test]
		[Category("Zip")]
		public void NestedArchive()
		{
			var ms = new MemoryStream();
			using (ZipOutputStream zos = new ZipOutputStream(ms))
			{
				zos.IsStreamOwner = false;
				var ze = new ZipEntry("Nest1");

				zos.PutNextEntry(ze);
				byte[] toWrite = Encoding.ASCII.GetBytes("Hello");
				zos.Write(toWrite, 0, toWrite.Length);
			}

			byte[] data = ms.ToArray();

			ms = new MemoryStream();
			using (ZipOutputStream zos = new ZipOutputStream(ms))
			{
				zos.IsStreamOwner = false;
				var ze = new ZipEntry("Container");
				ze.CompressionMethod = CompressionMethod.Stored;
				zos.PutNextEntry(ze);
				zos.Write(data, 0, data.Length);
			}

			using (ZipFile zipFile = new ZipFile(ms))
			{
				ZipEntry e = zipFile[0];
				Assert.AreEqual("Container", e.Name);

				using (ZipFile nested = new ZipFile(zipFile.GetInputStream(0)))
				{
					Assert.That(nested, Does.PassTestArchive());
					Assert.AreEqual(1, nested.Count);

					Stream nestedStream = nested.GetInputStream(0);

					var reader = new StreamReader(nestedStream);

					string contents = reader.ReadToEnd();

					Assert.AreEqual("Hello", contents);
				}
			}
		}

		private Stream GetPartialStream()
		{
			var ms = new MemoryStream();
			using (ZipOutputStream zos = new ZipOutputStream(ms))
			{
				zos.IsStreamOwner = false;
				var ze = new ZipEntry("E1");

				zos.PutNextEntry(ze);
				byte[] toWrite = Encoding.ASCII.GetBytes("Hello");
				zos.Write(toWrite, 0, toWrite.Length);
			}

			var zf = new ZipFile(ms);

			return zf.GetInputStream(0);
		}

		[Test]
		public void UnreferencedZipFileClosingPartialStream()
		{
			Stream s = GetPartialStream();

			GC.Collect();

			s.ReadByte();
		}

		/// <summary>
		/// Check that input stream is closed when IsStreamOwner is true (default), or leaveOpen is false
		/// </summary>
		[Test]
		[Category("Zip")]
		public void StreamClosedWhenOwner()
		{
			var ms = new MemoryStream();
			MakeZipFile(ms, false, "StreamClosedWhenOwner", 1, 10, "test");
			ms.Seek(0, SeekOrigin.Begin);
			var zipData = ms.ToArray();

			// Stream should be closed when leaveOpen is unspecified
			{
				var inMemoryZip = new TrackedMemoryStream(zipData);
				Assert.IsFalse(inMemoryZip.IsClosed, "Input stream should NOT be closed");

				using (var zipFile = new ZipFile(inMemoryZip))
				{
					Assert.IsTrue(zipFile.IsStreamOwner, "Should be stream owner by default");
				}

				Assert.IsTrue(inMemoryZip.IsClosed, "Input stream should be closed by default");
			}

			// Stream should be closed when leaveOpen is false
			{
				var inMemoryZip = new TrackedMemoryStream(zipData);
				Assert.IsFalse(inMemoryZip.IsClosed, "Input stream should NOT be closed");

				using (var zipFile = new ZipFile(inMemoryZip, false))
				{
					Assert.IsTrue(zipFile.IsStreamOwner, "Should be stream owner when leaveOpen is false");
				}

				Assert.IsTrue(inMemoryZip.IsClosed, "Input stream should be closed when leaveOpen is false");
			}
		}

		/// <summary>
		/// Check that input stream is not closed when IsStreamOwner is false;
		/// </summary>
		[Test]
		[Category("Zip")]
		public void StreamNotClosedWhenNotOwner()
		{
			var ms = new TrackedMemoryStream();
			MakeZipFile(ms, false, "StreamNotClosedWhenNotOwner", 1, 10, "test");
			ms.Seek(0, SeekOrigin.Begin);

			Assert.IsFalse(ms.IsClosed, "Input stream should NOT be closed");

			// Stream should not be closed when leaveOpen is true
			{
				using (var zipFile = new ZipFile(ms, true))
				{
					Assert.IsFalse(zipFile.IsStreamOwner, "Should NOT be stream owner when leaveOpen is true");
				}

				Assert.IsFalse(ms.IsClosed, "Input stream should NOT be closed when leaveOpen is true");
			}

			ms.Seek(0, SeekOrigin.Begin);

			// Stream should not be closed when IsStreamOwner is set to false after opening
			{
				using (var zipFile = new ZipFile(ms, false))
				{
					Assert.IsTrue(zipFile.IsStreamOwner, "Should be stream owner when leaveOpen is false");
					zipFile.IsStreamOwner = false;
					Assert.IsFalse(zipFile.IsStreamOwner, "Should be able to set IsStreamOwner to false");
				}

				Assert.IsFalse(ms.IsClosed, "Input stream should NOT be closed when IsStreamOwner is false");
			}
		}

		/// <summary>
		/// Check that input file is closed when IsStreamOwner is true (default), or leaveOpen is false
		/// </summary>
		[Test]
		[Category("Zip")]
		public void FileStreamClosedWhenOwner()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");

			tempFile = Path.Combine(tempFile, "SharpZipFileStreamClosedWhenOwnerTest.Zip");
			if (File.Exists(tempFile))
			{
				File.Delete(tempFile);
			}

			MakeZipFile(tempFile, "FileStreamClosedWhenOwner", 2, 10, "test");

			// Stream should be closed when leaveOpen is unspecified
			{
				var fileStream = new TrackedFileStream(tempFile);
				Assert.IsFalse(fileStream.IsClosed, "Input file should NOT be closed");

				using (var zipFile = new ZipFile(fileStream))
				{
					Assert.IsTrue(zipFile.IsStreamOwner, "Should be stream owner by default");
				}

				Assert.IsTrue(fileStream.IsClosed, "Input stream should be closed by default");
			}

			// Stream should be closed when leaveOpen is false
			{
				var fileStream = new TrackedFileStream(tempFile);
				Assert.IsFalse(fileStream.IsClosed, "Input stream should NOT be closed");

				using (var zipFile = new ZipFile(fileStream, false))
				{
					Assert.IsTrue(zipFile.IsStreamOwner, "Should be stream owner when leaveOpen is false");
				}

				Assert.IsTrue(fileStream.IsClosed, "Input stream should be closed when leaveOpen is false");
			}

			File.Delete(tempFile);
		}

		/// <summary>
		/// Check that input file is not closed when IsStreamOwner is false;
		/// </summary>
		[Test]
		[Category("Zip")]
		public void FileStreamNotClosedWhenNotOwner()
		{
			string tempFile = GetTempFilePath();
			Assert.IsNotNull(tempFile, "No permission to execute this test?");

			tempFile = Path.Combine(tempFile, "SharpZipFileStreamNotClosedWhenNotOwner.Zip");
			if (File.Exists(tempFile))
			{
				File.Delete(tempFile);
			}

			MakeZipFile(tempFile, "FileStreamClosedWhenOwner", 2, 10, "test");

			// Stream should not be closed when leaveOpen is true
			{
				using (var fileStream = new TrackedFileStream(tempFile))
				{
					Assert.IsFalse(fileStream.IsClosed, "Input file should NOT be closed");

					using (var zipFile = new ZipFile(fileStream, true))
					{
						Assert.IsFalse(zipFile.IsStreamOwner, "Should NOT be stream owner when leaveOpen is true");
					}

					Assert.IsFalse(fileStream.IsClosed, "Input stream should NOT be closed when leaveOpen is true");
				}
			}

			// Stream should not be closed when IsStreamOwner is set to false after opening
			{
				using (var fileStream = new TrackedFileStream(tempFile))
				{
					Assert.IsFalse(fileStream.IsClosed, "Input file should NOT be closed");

					using (var zipFile = new ZipFile(fileStream, false))
					{
						Assert.IsTrue(zipFile.IsStreamOwner, "Should be stream owner when leaveOpen is false");
						zipFile.IsStreamOwner = false;
						Assert.IsFalse(zipFile.IsStreamOwner, "Should be able to set IsStreamOwner to false");
					}

					Assert.IsFalse(fileStream.IsClosed, "Input stream should NOT be closed when leaveOpen is true");
				}
			}

			File.Delete(tempFile);
		}

		/// <summary>
		/// Check that input stream is only closed when construction fails and leaveOpen is false
		/// </summary>
		[Test]
		[Category("Zip")]
		public void StreamClosedOnError([Values(true, false)] bool leaveOpen)
		{
			var ms = new TrackedMemoryStream(new byte[32]);

			Assert.IsFalse(ms.IsClosed, "Underlying stream should NOT be closed initially");
			Assert.Throws<ZipException>(() =>
			{
				using var zf = new ZipFile(ms, leaveOpen);
			}, "Should have failed to load the file");

			if (leaveOpen)
			{
				Assert.IsFalse(ms.IsClosed, "Underlying stream should NOT be closed");
			}
			else
			{
				Assert.IsTrue(ms.IsClosed, "Underlying stream should be closed");
			}
		}

		[Test]
		[Category("Zip")]
		public void HostSystemPersistedFromOutputStream()
		{
			using (var ms = new MemoryStream())
			{
				var fileName = "testfile";

				using (var zos = new ZipOutputStream(ms) { IsStreamOwner = false })
				{
					var source = new StringMemoryDataSource("foo");
					zos.PutNextEntry(new ZipEntry(fileName) { HostSystem = (int)HostSystemID.Unix });
					source.GetSource().CopyTo(zos);
					zos.CloseEntry();
					zos.Finish();
				}

				ms.Seek(0, SeekOrigin.Begin);

				using (var zis = new ZipFile(ms))
				{
					var ze = zis.GetEntry(fileName);
					Assert.NotNull(ze);

					Assert.AreEqual((int)HostSystemID.Unix, ze.HostSystem);
					Assert.AreEqual(ZipConstants.VersionMadeBy, ze.VersionMadeBy);
				}
			}
		}

		[Test]
		[Category("Zip")]
		public void HostSystemPersistedFromZipFile()
		{
			using (var ms = new MemoryStream())
			{
				var fileName = "testfile";

				using (var zof = new ZipFile(ms, true))
				{
					var ze = zof.EntryFactory.MakeFileEntry(fileName, false);
					ze.HostSystem = (int)HostSystemID.Unix;

					zof.BeginUpdate();
					zof.Add(new StringMemoryDataSource("foo"), ze);
					zof.CommitUpdate();
				}

				ms.Seek(0, SeekOrigin.Begin);

				using (var zis = new ZipFile(ms))
				{
					var ze = zis.GetEntry(fileName);
					Assert.NotNull(ze);

					Assert.AreEqual((int)HostSystemID.Unix, ze.HostSystem);
					Assert.AreEqual(ZipConstants.VersionMadeBy, ze.VersionMadeBy);
				}
			}
		}

		/// <summary>
		/// Refs https://github.com/icsharpcode/SharpZipLib/issues/385
		/// Trying to add an AES Encrypted entry to ZipFile should throw as it isn't supported
		/// </summary>
		[Test]
		[Category("Zip")]
		public void AddingAnAESEncryptedEntryShouldThrow()
		{
			var memStream = new MemoryStream();
			using var zof = new ZipFile(memStream);
			var entry = new ZipEntry("test")
			{
				AESKeySize = 256,
			};

			zof.BeginUpdate();
			var exception = Assert.Throws<NotSupportedException>(() => zof.Add(new StringMemoryDataSource("foo"), entry));
			Assert.That(exception?.Message, Is.EqualTo("Creation of AES encrypted entries is not supported"));
		}

		/// <summary>
		/// Test that we can add a file entry and set the name to sometihng other than the name of the file.
		/// </summary>
		[Test]
		[Category("Zip")]
		[Category("CreatesTempFile")]
		public void AddFileWithAlternateName()
		{
			// Create a unique name that will be different from the file name
			var fileName = Utils.GetDummyFileName();

			using var sourceFile = Utils.GetDummyFile(size: 16);
			using var outputFile = Utils.GetTempFile();
			var inputContent = File.ReadAllText(sourceFile);
			using (var zf = ZipFile.Create(outputFile))
			{
				zf.BeginUpdate();

				// Add a file with the unique display name
				zf.Add(sourceFile, fileName);
					
				zf.CommitUpdate();
				zf.Close();
			}

			using (var zipFile = new ZipFile(outputFile))
			{
				Assert.That(zipFile.Count, Is.EqualTo(1));

				var fileEntry = zipFile.GetEntry(fileName);
				Assert.That(fileEntry, Is.Not.Null);

				using (var sr = new StreamReader(zipFile.GetInputStream(fileEntry)))
				{
					var outputContent = sr.ReadToEnd();
					Assert.AreEqual(inputContent, outputContent, "extracted content does not match source content");
				}
			}
		}

		/// <summary>
		/// Test a zip file using BZip2 compression.
		/// </summary>
		[TestCase(true)]
		[TestCase(false)]
		[Category("Zip")]
		public void ZipWithBZip2Compression(bool encryptEntries)
		{
			string password = "pwd";

			using (var memStream = new MemoryStream())
			{
				using (ZipFile f = new ZipFile(memStream, leaveOpen: true))
				{
					if (encryptEntries)
						f.Password = password;

					f.BeginUpdate(new MemoryArchiveStorage());

					var m = new StringMemoryDataSource("BZip2Compressed");
					f.Add(m, "a.dat", CompressionMethod.BZip2);

					var m2 = new StringMemoryDataSource("DeflateCompressed");
					f.Add(m2, "b.dat", CompressionMethod.Deflated);
					f.CommitUpdate();
					Assert.That(f, Does.PassTestArchive());
				}

				memStream.Seek(0, SeekOrigin.Begin);

				using (ZipFile f = new ZipFile(memStream))
				{
					if (encryptEntries)
						f.Password = password;

					{
						var entry = f.GetEntry("a.dat");
						Assert.That(entry.CompressionMethod, Is.EqualTo(CompressionMethod.BZip2), "Compression method should be BZip2");
						Assert.That(entry.Version, Is.EqualTo(ZipConstants.VersionBZip2), "Entry version should be 46");
						Assert.That(entry.IsCrypted, Is.EqualTo(encryptEntries));

						using (var reader = new StreamReader(f.GetInputStream(entry)))
						{
							string contents = reader.ReadToEnd();
							Assert.That(contents, Is.EqualTo("BZip2Compressed"), "extract string must match original string");
						}
					}

					{
						var entry = f.GetEntry("b.dat");
						Assert.That(entry.CompressionMethod, Is.EqualTo(CompressionMethod.Deflated), "Compression method should be Deflated");
						Assert.That(entry.IsCrypted, Is.EqualTo(encryptEntries));

						using (var reader = new StreamReader(f.GetInputStream(entry)))
						{
							string contents = reader.ReadToEnd();
							Assert.That(contents, Is.EqualTo("DeflateCompressed"), "extract string must match original string");
						}
					}
				}

				// @@TODO@@ verify the archive with 7-zip?
			}
		}

		/// <summary>
		/// We should be able to read a bzip2 compressed zip file created by 7-zip.
		/// </summary>
		[Test]
		[Category("Zip")]
		public void ShouldReadBZip2ZipCreatedBy7Zip()
		{
			const string bZip2CompressedZipCreatedBy7Zip =
			"UEsDBC4AAAAMAIa50U4/rHf5qwAAAK8AAAAJAAAASGVsbG8udHh0QlpoOTFBWSZTWTL8pwYAA" +
			"BWfgEhlUAAiLUgQP+feMCAAiCKaeiaBobU9JiaAMGmoak9GmRNqPUDQ9T1PQsz/t9B6YvEdvF" +
			"5dhwXzGE1ooO41A6TtATBEFxFUq6trGtUcSJDyWWWj/S2VwY15fy3IqHi3hHUS+K76zdoDzQa" +
			"VGE/4YkYZe3JAtv1EsIqIsiTnnktIbBo1R4xY3JZEOm2BvwLuSKcKEgZflODAUEsBAj8ALgAA" +
			"AAwAhrnRTj+sd/mrAAAArwAAAAkAJAAAAAAAAAAgAAAAAAAAAEhlbGxvLnR4dAoAIAAAAAAAA" +
			"QAYAO97MLZZJdUB73swtlkl1QEK0UTFWCXVAVBLBQYAAAAAAQABAFsAAADSAAAAAAA=";

			const string originalText =
				"SharpZipLib (#ziplib, formerly NZipLib) is a compression library that supports Zip files using both stored and deflate compression methods, PKZIP 2.0 style and AES encryption.";

			var fileBytes = Convert.FromBase64String(bZip2CompressedZipCreatedBy7Zip);

			using var input = new MemoryStream(fileBytes, writable: false);
			using var zf = new ZipFile(input);
			var entry = zf.GetEntry("Hello.txt");
			Assert.That(entry.CompressionMethod, Is.EqualTo(CompressionMethod.BZip2), "Compression method should be BZip2");
			Assert.That(entry.Version, Is.EqualTo(ZipConstants.VersionBZip2), "Entry version should be 46");

			using var reader = new StreamReader(zf.GetInputStream(entry));
			var contents = reader.ReadToEnd();
			Assert.That(contents, Is.EqualTo(originalText), "extract string must match original string");
		}

		/// <summary>
		/// We should be able to read a bzip2 compressed / AES encrypted zip file created by 7-zip.
		/// </summary>
		[Test]
		[Category("Zip")]
		public void ShouldReadAESBZip2ZipCreatedBy7Zip()
		{
			const string bZip2CompressedZipCreatedBy7Zip =
			 "UEsDBDMAAQBjAIa50U4AAAAAxwAAAK8AAAAJAAsASGVsbG8udHh0AZkHAAIAQUUDDAAYg6jqf" +
			"kvZClVMOtgmqKT0/8I9fMPgo96myxw9hLQUhKj1Qczi3fT7QIhAnAKU+u03nA8rCKGWmDI5Qz" +
			"qPREy95boQVDPwmwEsWksv3GAWzMfzZUhmB/TgIJlA34a4yP0f2ucy3/QCQYo8QcHjBtjWX5b" +
			"dZn0+fwY9Ci7q8JSI8zNSbgQ0Ert/lIJ9MxQ4lzBxMl4LySkd104cDPh/FslTAcPtHoy8Mf1c" +
			"vnI1uICMgjWVeTqYrvSvt2uuHnqr4AiehArFiXTnUEsBAj8AMwABAGMAhrnRTgAAAADHAAAAr" +
			"wAAAAkALwAAAAAAAAAgAAAAAAAAAEhlbGxvLnR4dAoAIAAAAAAAAQAYAO97MLZZJdUBYdnjul" +
			"kl1QEK0UTFWCXVAQGZBwACAEFFAwwAUEsFBgAAAAABAAEAZgAAAPkAAAAAAA==";

			const string originalText =
				"SharpZipLib (#ziplib, formerly NZipLib) is a compression library that supports Zip files using both stored and deflate compression methods, PKZIP 2.0 style and AES encryption.";

			var fileBytes = Convert.FromBase64String(bZip2CompressedZipCreatedBy7Zip);

			using var input = new MemoryStream(fileBytes, writable: false);
			using var zf = new ZipFile(input);
			zf.Password = "password";

			var entry = zf.GetEntry("Hello.txt");
			Assert.That(entry.CompressionMethod, Is.EqualTo(CompressionMethod.BZip2), "Compression method should be BZip2");
			Assert.That(entry.Version, Is.EqualTo(ZipConstants.VERSION_AES), "Entry version should be 51");
			Assert.That(entry.IsCrypted, Is.True, "Entry should be encrypted");
			Assert.That(entry.AESKeySize, Is.EqualTo(256), "AES Keysize should be 256");

			using var reader = new StreamReader(zf.GetInputStream(entry));
			var contents = reader.ReadToEnd();
			Assert.That(contents, Is.EqualTo(originalText), "extract string must match original string");
		}

		/// <summary>
		/// Test for https://github.com/icsharpcode/SharpZipLib/issues/147, when deleting items in a zip
		/// </summary>
		/// <param name="useZip64">Whether Zip64 should be used in the test archive</param>
		[TestCase(UseZip64.On)]
		[TestCase(UseZip64.Off)]
		[Category("Zip")]
		public void TestDescriptorUpdateOnDelete(UseZip64 useZip64)
		{
			MemoryStream msw = new MemoryStreamWithoutSeek();
			using (ZipOutputStream outStream = new ZipOutputStream(msw))
			{
				outStream.UseZip64 = useZip64;
				outStream.IsStreamOwner = false;
				outStream.PutNextEntry(new ZipEntry("StripedMarlin"));
				outStream.WriteByte(89);

				outStream.PutNextEntry(new ZipEntry("StripedMarlin2"));
				outStream.WriteByte(91);
			}

			var zipData = msw.ToArray();
			Assert.That(zipData, Does.PassTestArchive());

			using (var memoryStream = new MemoryStream(zipData))
			{
				using (var zipFile = new ZipFile(memoryStream, leaveOpen: true))
				{
					zipFile.BeginUpdate();
					zipFile.Delete("StripedMarlin");
					zipFile.CommitUpdate();
				}

				memoryStream.Position = 0;

				using (var zipFile = new ZipFile(memoryStream, leaveOpen: true))
				{
					Assert.That(zipFile, Does.PassTestArchive());
				}
			}
		}

		/// <summary>
		/// Test for https://github.com/icsharpcode/SharpZipLib/issues/147, when adding items to a zip
		/// </summary>
		/// <param name="useZip64">Whether Zip64 should be used in the test archive</param>
		[TestCase(UseZip64.On)]
		[TestCase(UseZip64.Off)]
		[Category("Zip")]
		public void TestDescriptorUpdateOnAdd(UseZip64 useZip64)
		{
			MemoryStream msw = new MemoryStreamWithoutSeek();
			using (ZipOutputStream outStream = new ZipOutputStream(msw))
			{
				outStream.UseZip64 = useZip64;
				outStream.IsStreamOwner = false;
				outStream.PutNextEntry(new ZipEntry("StripedMarlin"));
				outStream.WriteByte(89);
			}

			var zipData = msw.ToArray();
			Assert.That(zipData, Does.PassTestArchive());

			using (var memoryStream = new MemoryStream())
			{
				memoryStream.Write(zipData, 0, zipData.Length);

				using (var zipFile = new ZipFile(memoryStream, leaveOpen: true))
				{
					zipFile.BeginUpdate();
					zipFile.Add(new StringMemoryDataSource("stripey"), "Zebra");
					zipFile.CommitUpdate();
				}

				memoryStream.Position = 0;

				using (var zipFile = new ZipFile(memoryStream, leaveOpen: true))
				{
					Assert.That(zipFile, Does.PassTestArchive());
				}
			}
		}

		/// <summary>
		/// Check that Zip files can be created with an empty file name
		/// </summary>
		[Test]
		[Category("Zip")]
		public void HandlesEmptyFileName()
		{
			using var ms = new MemoryStream();
			using (var zos = new ZipOutputStream(ms){IsStreamOwner = false})
			{
				zos.PutNextEntry(new ZipEntry(String.Empty));
				Utils.WriteDummyData(zos, 64);
			}
			ms.Seek(0, SeekOrigin.Begin);
			using (var zis = new ZipInputStream(ms){IsStreamOwner = false})
			{
				var entry = zis.GetNextEntry();
				Assert.That(entry.Name, Is.Empty);
				Assert.That(zis.ReadBytes(64).Length, Is.EqualTo(64));
			}
		}
	}
}
