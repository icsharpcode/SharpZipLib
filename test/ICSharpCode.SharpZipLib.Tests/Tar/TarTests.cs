using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tests.TestSupport;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;

namespace ICSharpCode.SharpZipLib.Tests.Tar
{
	/// <summary>
	/// This class contains test cases for Tar archive handling.
	/// </summary>
	[TestFixture]
	public class TarTestSuite
	{
		private int entryCount;

		private void EntryCounter(TarArchive archive, TarEntry entry, string message)
		{
			entryCount++;
		}

		/// <summary>
		/// Test that an empty archive can be created and when read has 0 entries in it
		/// </summary>
		[Test]
		[Category("Tar")]
		public void EmptyTar()
		{
			var ms = new MemoryStream();
			int recordSize = 0;
			using (TarArchive tarOut = TarArchive.CreateOutputTarArchive(ms))
			{
				recordSize = tarOut.RecordSize;
			}

			Assert.IsTrue(ms.GetBuffer().Length > 0, "Archive size must be > zero");
			Assert.AreEqual(ms.GetBuffer().Length % recordSize, 0, "Archive size must be a multiple of record size");

			var ms2 = new MemoryStream();
			ms2.Write(ms.GetBuffer(), 0, ms.GetBuffer().Length);
			ms2.Seek(0, SeekOrigin.Begin);

			using (TarArchive tarIn = TarArchive.CreateInputTarArchive(ms2))
			{
				entryCount = 0;
				tarIn.ProgressMessageEvent += EntryCounter;
				tarIn.ListContents();
				Assert.AreEqual(0, entryCount, "Expected 0 tar entries");
			}
		}

		/// <summary>
		/// Check that the tar block factor can be varied successfully.
		/// </summary>
		[Test]
		[Category("Tar")]
		public void BlockFactorHandling()
		{
			const int MinimumBlockFactor = 1;
			const int MaximumBlockFactor = 64;
			const int FillFactor = 2;

			for (int factor = MinimumBlockFactor; factor < MaximumBlockFactor; ++factor)
			{
				var ms = new MemoryStream();

				using (TarOutputStream tarOut = new TarOutputStream(ms, factor))
				{
					TarEntry entry = TarEntry.CreateTarEntry("TestEntry");
					entry.Size = (TarBuffer.BlockSize * factor * FillFactor);
					tarOut.PutNextEntry(entry);

					byte[] buffer = new byte[TarBuffer.BlockSize];

					var r = new Random();
					r.NextBytes(buffer);

					// Last block is a partial one
					for (int i = 0; i < factor * FillFactor; ++i)
					{
						tarOut.Write(buffer, 0, buffer.Length);
					}
				}

				byte[] tarData = ms.ToArray();
				Assert.IsNotNull(tarData, "Data written is null");

				// Blocks = Header + Data Blocks + Zero block + Record trailer
				int usedBlocks = 1 + (factor * FillFactor) + 2;
				int totalBlocks = usedBlocks + (factor - 1);
				totalBlocks /= factor;
				totalBlocks *= factor;

				Assert.AreEqual(TarBuffer.BlockSize * totalBlocks, tarData.Length, "Tar file should contain {0} blocks in length",
					totalBlocks);

				if (usedBlocks < totalBlocks)
				{
					// Start at first byte after header.
					int byteIndex = TarBuffer.BlockSize * ((factor * FillFactor) + 1);
					while (byteIndex < tarData.Length)
					{
						int blockNumber = byteIndex / TarBuffer.BlockSize;
						int offset = blockNumber % TarBuffer.BlockSize;
						Assert.AreEqual(0, tarData[byteIndex],
							string.Format("Trailing block data should be null iteration {0} block {1} offset {2}  index {3}",
							factor,
							blockNumber, offset, byteIndex));
						byteIndex += 1;
					}
				}
			}
		}

		/// <summary>
		/// Check that the tar trailer only contains nulls.
		/// </summary>
		[Test]
		[Category("Tar")]
		public void TrailerContainsNulls()
		{
			const int TestBlockFactor = 3;

			for (int iteration = 0; iteration < TestBlockFactor * 2; ++iteration)
			{
				var ms = new MemoryStream();

				using (TarOutputStream tarOut = new TarOutputStream(ms, TestBlockFactor))
				{
					TarEntry entry = TarEntry.CreateTarEntry("TestEntry");
					if (iteration > 0)
					{
						entry.Size = (TarBuffer.BlockSize * (iteration - 1)) + 9;
					}
					tarOut.PutNextEntry(entry);

					byte[] buffer = new byte[TarBuffer.BlockSize];

					var r = new Random();
					r.NextBytes(buffer);

					if (iteration > 0)
					{
						for (int i = 0; i < iteration - 1; ++i)
						{
							tarOut.Write(buffer, 0, buffer.Length);
						}

						// Last block is a partial one
						for (int i = 1; i < 10; ++i)
						{
							tarOut.WriteByte((byte)i);
						}
					}
				}

				byte[] tarData = ms.ToArray();
				Assert.IsNotNull(tarData, "Data written is null");

				// Blocks = Header + Data Blocks + Zero block + Record trailer
				int usedBlocks = 1 + iteration + 2;
				int totalBlocks = usedBlocks + (TestBlockFactor - 1);
				totalBlocks /= TestBlockFactor;
				totalBlocks *= TestBlockFactor;

				Assert.AreEqual(TarBuffer.BlockSize * totalBlocks, tarData.Length,
					string.Format("Tar file should be {0} blocks in length", totalBlocks));

				if (usedBlocks < totalBlocks)
				{
					// Start at first byte after header.
					int byteIndex = TarBuffer.BlockSize * (iteration + 1);
					while (byteIndex < tarData.Length)
					{
						int blockNumber = byteIndex / TarBuffer.BlockSize;
						int offset = blockNumber % TarBuffer.BlockSize;
						Assert.AreEqual(0, tarData[byteIndex],
							string.Format("Trailing block data should be null iteration {0} block {1} offset {2}  index {3}",
							iteration,
							blockNumber, offset, byteIndex));
						byteIndex += 1;
					}
				}
			}
		}

		private void TryLongName(string name)
		{
			var ms = new MemoryStream();
			using (TarOutputStream tarOut = new TarOutputStream(ms))
			{
				DateTime modTime = DateTime.Now;

				TarEntry entry = TarEntry.CreateTarEntry(name);
				tarOut.PutNextEntry(entry);
			}

			var ms2 = new MemoryStream();
			ms2.Write(ms.GetBuffer(), 0, ms.GetBuffer().Length);
			ms2.Seek(0, SeekOrigin.Begin);

			using (TarInputStream tarIn = new TarInputStream(ms2))
			{
				TarEntry nextEntry = tarIn.GetNextEntry();

				Assert.AreEqual(nextEntry.Name, name, "Name match failure");
			}
		}

		/// <summary>
		/// Check that long names are handled correctly for reading and writing.
		/// </summary>
		[Test]
		[Category("Tar")]
		public void LongNames()
		{
			TryLongName("11111111112222222222333333333344444444445555555555" +
						"6666666666777777777788888888889999999999000000000");

			TryLongName("11111111112222222222333333333344444444445555555555" +
						"66666666667777777777888888888899999999990000000000");

			TryLongName("11111111112222222222333333333344444444445555555555" +
						"66666666667777777777888888888899999999990000000000" +
						"1");

			TryLongName("11111111112222222222333333333344444444445555555555" +
						"66666666667777777777888888888899999999990000000000" +
						"11111111112222222222333333333344444444445555555555" +
						"66666666667777777777888888888899999999990000000000");

			TryLongName("11111111112222222222333333333344444444445555555555" +
						"66666666667777777777888888888899999999990000000000" +
						"11111111112222222222333333333344444444445555555555" +
						"66666666667777777777888888888899999999990000000000" +
						"11111111112222222222333333333344444444445555555555" +
						"66666666667777777777888888888899999999990000000000" +
						"11111111112222222222333333333344444444445555555555" +
						"66666666667777777777888888888899999999990000000000" +
						"11111111112222222222333333333344444444445555555555" +
						"66666666667777777777888888888899999999990000000000");

			for (int n = 1; n < 1024; ++n)
			{
				string format = "{0," + n + "}";
				string formatted = string.Format(format, "A");
				TryLongName(formatted);
			}
		}

		[Test]
		[Category("Tar")]
		public void ExtendedHeaderLongName()
		{
			string expectedName = "lftest-0000000000111111111122222222223333333333444444444455555555556666666666777777777788888888889999999999";

			var input64 = @"Li9QYXhIZWFkZXJzLjExOTY5L2xmdGVzdC0wMDAwMDAwMDAwMTExMTExMTExMTIyMjIyMjIyMjIz
							MzMzMzMzMzMzNDQ0NDQ0NDQ0NDU1NTU1NTU1NTU2NjY2NjY2NjY2Nzc3NzAwMDA2NDQAMDAwMDAw
							MAAwMDAwMDAwADAwMDAwMDAwMzE3ADEzMzE2MTYyMzMzADAyMTYwNgAgeAAAAAAAAAAAAAAAAAAA
							AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
							AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAB1c3RhcgAwMAAAAAAAAAAAAAAAAAAAAAAAAAAA
							AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
							AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
							AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
							AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAx
							MTcgcGF0aD1sZnRlc3QtMDAwMDAwMDAwMDExMTExMTExMTEyMjIyMjIyMjIyMzMzMzMzMzMzMzQ0
							NDQ0NDQ0NDQ1NTU1NTU1NTU1NjY2NjY2NjY2Njc3Nzc3Nzc3Nzc4ODg4ODg4ODg4OTk5OTk5OTk5
							OQozMCBtdGltZT0xNTMwNDU1MjU5LjcwNjU0ODg4OAozMCBhdGltZT0xNTMwNDU1MjU5LjcwNjU0
							ODg4OAozMCBjdGltZT0xNTMwNDU1MjU5LjcwNjU0ODg4OAoAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
							AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
							AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
							AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
							AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
							AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAGxm
							dGVzdC0wMDAwMDAwMDAwMTExMTExMTExMTIyMjIyMjIyMjIzMzMzMzMzMzMzNDQ0NDQ0NDQ0NDU1
							NTU1NTU1NTU2NjY2NjY2NjY2Nzc3Nzc3Nzc3Nzg4ODg4ODg4ODg5OTkwMDAwNjY0ADAwMDE3NTAA
							MDAwMTc1MAAwMDAwMDAwMDAwMAAxMzMxNjE2MjMzMwAwMjM3MjcAIDAAAAAAAAAAAAAAAAAAAAAA
							AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
							AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAdXN0YXIAMDBuaWxzAAAAAAAAAAAAAAAAAAAAAAAA
							AAAAAAAAAAAAAG5pbHMAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAMDAwMDAwMAAwMDAwMDAw";

			var buffer = new byte[2560];
			var truncated = Convert.FromBase64String(input64);
			Array.Copy(truncated, buffer, truncated.Length);
			truncated = null;

			using (var ms = new MemoryStream(buffer))
			using (var tis = new TarInputStream(ms))
			{
				var entry = tis.GetNextEntry();
				Assert.IsNotNull(entry, "Entry is null");

				Assert.IsNotNull(entry.Name, "Entry name is null");

				Assert.AreEqual(expectedName.Length, entry.Name.Length, $"Entry name is truncated to {entry.Name.Length} bytes.");

				Assert.AreEqual(expectedName, entry.Name, "Entry name does not match expected value");
			}
		}

		/// <summary>
		/// Test equals function for tar headers.
		/// </summary>
		[Test]
		[Category("Tar")]
		public void HeaderEquality()
		{
			var h1 = new TarHeader();
			var h2 = new TarHeader();

			Assert.IsTrue(h1.Equals(h2));

			h1.Name = "ABCDEFG";
			Assert.IsFalse(h1.Equals(h2));
			h2.Name = h1.Name;
			Assert.IsTrue(h1.Equals(h2));

			h1.Mode = 33188;
			Assert.IsFalse(h1.Equals(h2));
			h2.Mode = h1.Mode;
			Assert.IsTrue(h1.Equals(h2));

			h1.UserId = 654;
			Assert.IsFalse(h1.Equals(h2));
			h2.UserId = h1.UserId;
			Assert.IsTrue(h1.Equals(h2));

			h1.GroupId = 654;
			Assert.IsFalse(h1.Equals(h2));
			h2.GroupId = h1.GroupId;
			Assert.IsTrue(h1.Equals(h2));

			h1.Size = 654;
			Assert.IsFalse(h1.Equals(h2));
			h2.Size = h1.Size;
			Assert.IsTrue(h1.Equals(h2));

			h1.ModTime = DateTime.Now;
			Assert.IsFalse(h1.Equals(h2));
			h2.ModTime = h1.ModTime;
			Assert.IsTrue(h1.Equals(h2));

			h1.TypeFlag = 165;
			Assert.IsFalse(h1.Equals(h2));
			h2.TypeFlag = h1.TypeFlag;
			Assert.IsTrue(h1.Equals(h2));

			h1.LinkName = "link";
			Assert.IsFalse(h1.Equals(h2));
			h2.LinkName = h1.LinkName;
			Assert.IsTrue(h1.Equals(h2));

			h1.Magic = "other";
			Assert.IsFalse(h1.Equals(h2));
			h2.Magic = h1.Magic;
			Assert.IsTrue(h1.Equals(h2));

			h1.Version = "1";
			Assert.IsFalse(h1.Equals(h2));
			h2.Version = h1.Version;
			Assert.IsTrue(h1.Equals(h2));

			h1.UserName = "nuser";
			Assert.IsFalse(h1.Equals(h2));
			h2.UserName = h1.UserName;
			Assert.IsTrue(h1.Equals(h2));

			h1.GroupName = "group";
			Assert.IsFalse(h1.Equals(h2));
			h2.GroupName = h1.GroupName;
			Assert.IsTrue(h1.Equals(h2));

			h1.DevMajor = 165;
			Assert.IsFalse(h1.Equals(h2));
			h2.DevMajor = h1.DevMajor;
			Assert.IsTrue(h1.Equals(h2));

			h1.DevMinor = 164;
			Assert.IsFalse(h1.Equals(h2));
			h2.DevMinor = h1.DevMinor;
			Assert.IsTrue(h1.Equals(h2));
		}

		[Test]
		[Category("Tar")]
		public void Checksum()
		{
			var ms = new MemoryStream();
			using (TarOutputStream tarOut = new TarOutputStream(ms))
			{
				DateTime modTime = DateTime.Now;

				TarEntry entry = TarEntry.CreateTarEntry("TestEntry");
				entry.TarHeader.Mode = 12345;

				tarOut.PutNextEntry(entry);
			}

			var ms2 = new MemoryStream();
			ms2.Write(ms.GetBuffer(), 0, ms.GetBuffer().Length);
			ms2.Seek(0, SeekOrigin.Begin);
			TarEntry nextEntry;

			using (TarInputStream tarIn = new TarInputStream(ms2))
			{
				nextEntry = tarIn.GetNextEntry();
				Assert.IsTrue(nextEntry.TarHeader.IsChecksumValid, "Checksum should be valid");
			}

			var ms3 = new MemoryStream();
			ms3.Write(ms.GetBuffer(), 0, ms.GetBuffer().Length);
			ms3.Seek(0, SeekOrigin.Begin);
			ms3.Write(new byte[] { 34 }, 0, 1);
			ms3.Seek(0, SeekOrigin.Begin);

			using (TarInputStream tarIn = new TarInputStream(ms3))
			{
				bool trapped = false;

				try
				{
					nextEntry = tarIn.GetNextEntry();
				}
				catch (TarException)
				{
					trapped = true;
				}

				Assert.IsTrue(trapped, "Checksum should be invalid");
			}
		}

		/// <summary>
		/// Check that values set are preserved when writing and reading archives.
		/// </summary>
		[Test]
		[Category("Tar")]
		public void ValuesPreserved()
		{
			var ms = new MemoryStream();
			TarEntry entry;
			DateTime modTime = DateTime.Now;

			using (TarOutputStream tarOut = new TarOutputStream(ms))
			{
				entry = TarEntry.CreateTarEntry("TestEntry");
				entry.GroupId = 12;
				entry.UserId = 14;
				entry.ModTime = modTime;
				entry.UserName = "UserName";
				entry.GroupName = "GroupName";
				entry.TarHeader.Mode = 12345;

				tarOut.PutNextEntry(entry);
			}

			var ms2 = new MemoryStream();
			ms2.Write(ms.GetBuffer(), 0, ms.GetBuffer().Length);
			ms2.Seek(0, SeekOrigin.Begin);

			using (TarInputStream tarIn = new TarInputStream(ms2))
			{
				TarEntry nextEntry = tarIn.GetNextEntry();
				Assert.AreEqual(entry.TarHeader.Checksum, nextEntry.TarHeader.Checksum, "Checksum");

				Assert.IsTrue(nextEntry.Equals(entry), "Entries should be equal");
				Assert.IsTrue(nextEntry.TarHeader.Equals(entry.TarHeader), "Headers should match");

				// Tar only stores seconds
				var truncatedTime = new DateTime(modTime.Year, modTime.Month, modTime.Day,
					modTime.Hour, modTime.Minute, modTime.Second);
				Assert.AreEqual(truncatedTime, nextEntry.ModTime, "Modtimes should match");

				entryCount = 0;
				while (nextEntry != null)
				{
					++entryCount;
					nextEntry = tarIn.GetNextEntry();
				}

				Assert.AreEqual(1, entryCount, "Expected 1 entry");
			}
		}

		/// <summary>
		/// Check invalid mod times are detected
		/// </summary>
		[Test]
		[Category("Tar")]
		//[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void InvalidModTime()
		{
			TarEntry e = TarEntry.CreateTarEntry("test");
			//e.ModTime = DateTime.MinValue;

			Assert.That(() => e.ModTime = DateTime.MinValue,
				Throws.TypeOf<ArgumentOutOfRangeException>());
		}

		/// <summary>
		/// Check invalid sizes are detected
		/// </summary>
		[Test]
		[Category("Tar")]
		//[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void InvalidSize()
		{
			TarEntry e = TarEntry.CreateTarEntry("test");
			//e.Size = -6;

			Assert.That(() => e.Size = -6,
				Throws.TypeOf<ArgumentOutOfRangeException>());
		}

		/// <summary>
		/// Check invalid names are detected
		/// </summary>
		[Test]
		[Category("Tar")]
		//[ExpectedException(typeof(ArgumentNullException))]
		public void InvalidName()
		{
			TarEntry e = TarEntry.CreateTarEntry("test");
			//e.Name = null;

			Assert.That(() => e.Name = null,
				Throws.TypeOf<ArgumentNullException>());
		}

		/// <summary>
		/// Check setting user and group names.
		/// </summary>
		[Test]
		[Category("Tar")]
		public void UserAndGroupNames()
		{
			TarEntry e = TarEntry.CreateTarEntry("test");
			e.UserName = null;
			Assert.IsNotNull(e.UserName, "Name set to OS default");
			e.UserName = "";
			Assert.AreEqual(0, e.UserName.Length, "Empty name allowed");
			e.GroupName = null;
			Assert.AreEqual("None", e.GroupName, "default group name is None");
		}

		/// <summary>
		/// Check invalid magic values are detected
		/// </summary>
		[Test]
		[Category("Tar")]
		//[ExpectedException(typeof(ArgumentNullException))]
		public void InvalidMagic()
		{
			TarEntry e = TarEntry.CreateTarEntry("test");
			//e.TarHeader.Magic = null;

			Assert.That(() => e.TarHeader.Magic = null,
				Throws.TypeOf<ArgumentNullException>());
		}

		/// <summary>
		/// Check invalid link names are detected
		/// </summary>
		[Test]
		[Category("Tar")]
		//[ExpectedException(typeof(ArgumentNullException))]
		public void InvalidLinkName()
		{
			TarEntry e = TarEntry.CreateTarEntry("test");
			//e.TarHeader.LinkName = null;

			Assert.That(() => e.TarHeader.LinkName = null,
				Throws.TypeOf<ArgumentNullException>());
		}

		/// <summary>
		/// Check invalid version names are detected
		/// </summary>
		[Test]
		[Category("Tar")]
		//[ExpectedException(typeof(ArgumentNullException))]
		public void InvalidVersionName()
		{
			TarEntry e = TarEntry.CreateTarEntry("test");
			//e.TarHeader.Version = null;

			Assert.That(() => e.TarHeader.Version = null,
				Throws.TypeOf<ArgumentNullException>());
		}

		[Test]
		[Category("Tar")]
		public void CloningAndUniqueness()
		{
			// Partial test of cloning for TarHeader and TarEntry
			TarEntry e = TarEntry.CreateTarEntry("ohsogood");
			e.GroupId = 47;
			e.GroupName = "GroupName";
			e.ModTime = DateTime.Now;
			e.Size = 123234;

			TarHeader headerE = e.TarHeader;

			headerE.DevMajor = 99;
			headerE.DevMinor = 98;
			headerE.LinkName = "LanceLink";

			var d = (TarEntry)e.Clone();

			Assert.AreEqual(d.File, e.File);
			Assert.AreEqual(d.GroupId, e.GroupId);
			Assert.AreEqual(d.GroupName, e.GroupName);
			Assert.AreEqual(d.IsDirectory, e.IsDirectory);
			Assert.AreEqual(d.ModTime, e.ModTime);
			Assert.AreEqual(d.Size, e.Size);

			TarHeader headerD = d.TarHeader;

			Assert.AreEqual(headerE.Checksum, headerD.Checksum);
			Assert.AreEqual(headerE.LinkName, headerD.LinkName);

			Assert.AreEqual(99, headerD.DevMajor);
			Assert.AreEqual(98, headerD.DevMinor);

			Assert.AreEqual("LanceLink", headerD.LinkName);

			var entryf = new TarEntry(headerD);

			headerD.LinkName = "Something different";

			Assert.AreNotEqual(headerD.LinkName, entryf.TarHeader.LinkName, "Entry headers should be unique");
		}

		[Test]
		[Category("Tar")]
		public void OutputStreamOwnership()
		{
			var memStream = new TrackedMemoryStream();
			var s = new TarOutputStream(memStream);

			Assert.IsFalse(memStream.IsClosed, "Shouldnt be closed initially");
			Assert.IsFalse(memStream.IsDisposed, "Shouldnt be disposed initially");

			s.Close();

			Assert.IsTrue(memStream.IsClosed, "Should be closed after parent owner close");
			Assert.IsTrue(memStream.IsDisposed, "Should be disposed after parent owner close");

			memStream = new TrackedMemoryStream();
			s = new TarOutputStream(memStream);

			Assert.IsFalse(memStream.IsClosed, "Shouldnt be closed initially");
			Assert.IsFalse(memStream.IsDisposed, "Shouldnt be disposed initially");

			s.IsStreamOwner = false;
			s.Close();

			Assert.IsFalse(memStream.IsClosed, "Should not be closed after parent owner close");
			Assert.IsFalse(memStream.IsDisposed, "Should not be disposed after parent owner close");
		}

		[Test]
		[Category("Tar")]
		public void InputStreamOwnership()
		{
			var memStream = new TrackedMemoryStream();
			var s = new TarInputStream(memStream);

			Assert.IsFalse(memStream.IsClosed, "Shouldnt be closed initially");
			Assert.IsFalse(memStream.IsDisposed, "Shouldnt be disposed initially");

			s.Close();

			Assert.IsTrue(memStream.IsClosed, "Should be closed after parent owner close");
			Assert.IsTrue(memStream.IsDisposed, "Should be disposed after parent owner close");

			memStream = new TrackedMemoryStream();
			s = new TarInputStream(memStream);

			Assert.IsFalse(memStream.IsClosed, "Shouldnt be closed initially");
			Assert.IsFalse(memStream.IsDisposed, "Shouldnt be disposed initially");

			s.IsStreamOwner = false;
			s.Close();

			Assert.IsFalse(memStream.IsClosed, "Should not be closed after parent owner close");
			Assert.IsFalse(memStream.IsDisposed, "Should not be disposed after parent owner close");
		}

		[Test]
		[Category("Tar")]
		public void EndBlockHandling()
		{
			int dummySize = 70145;

			long outCount, inCount;

			using (var ms = new MemoryStream())
			{
				using (var tarOut = TarArchive.CreateOutputTarArchive(ms))
				using (var dummyFile = Utils.GetDummyFile(dummySize))
				{
					tarOut.IsStreamOwner = false;
					tarOut.WriteEntry(TarEntry.CreateEntryFromFile(dummyFile.Filename), false);
				}

				outCount = ms.Position;
				ms.Seek(0, SeekOrigin.Begin);

				using (var tarIn = TarArchive.CreateInputTarArchive(ms))
				using (var tempDir = new Utils.TempDir())
				{
					tarIn.IsStreamOwner = false;
					tarIn.ExtractContents(tempDir.Fullpath);

					foreach (var file in Directory.GetFiles(tempDir.Fullpath, "*", SearchOption.AllDirectories))
					{
						Console.WriteLine($"Extracted \"{file}\"");
					}
				}

				inCount = ms.Position;

				Console.WriteLine($"Output count: {outCount}");
				Console.WriteLine($"Input count: {inCount}");

				Assert.AreEqual(inCount, outCount, "Bytes read and bytes written should be equal");
			}
		}

		[Test]
		[Category("Tar")]
		[Category("Performance")]
		[Explicit("Long Running")]
		public void WriteThroughput()
		{
			const string EntryName = "LargeTarEntry";

			PerformanceTesting.TestWrite(TestDataSize.Large, bs =>
			{
				var tos = new TarOutputStream(bs);
				tos.PutNextEntry(new TarEntry(new TarHeader()
				{
					Name = EntryName,
					Size = (int)TestDataSize.Large,
				}));
				return tos;
			},
			stream =>
			{
				((TarOutputStream)stream).CloseEntry();
			});
		}

		[Test]
		[Category("Tar")]
		[Category("Performance")]
		[Explicit("Long Running")]
		public void SingleLargeEntry()
		{
			const string EntryName = "LargeTarEntry";
			const TestDataSize dataSize = TestDataSize.Large;

			PerformanceTesting.TestReadWrite(
				size: dataSize,
				input: bs =>
				{
					var tis = new TarInputStream(bs);
					var entry = tis.GetNextEntry();

					Assert.AreEqual(entry.Name, EntryName);
					return tis;
				},
				output: bs =>
				{
					var tos = new TarOutputStream(bs);
					tos.PutNextEntry(new TarEntry(new TarHeader()
					{
						Name = EntryName,
						Size = (int)dataSize,
					}));
					return tos;
				},
				outputClose: stream =>
				{
					((TarOutputStream)stream).CloseEntry();
				}
			);
		}

		// Test for corruption issue described @ https://github.com/icsharpcode/SharpZipLib/issues/321
		[Test]
		[Category("Tar")]
		public void ExtractingCorruptTarShouldntLeakFiles()
		{
			using (var memoryStream = new MemoryStream())
			{
				//Create a tar.gz in the output stream
				using (var gzipStream = new GZipOutputStream(memoryStream))
				{
					gzipStream.IsStreamOwner = false;

					using (var tarOut = TarArchive.CreateOutputTarArchive(gzipStream))
					using (var dummyFile = Utils.GetDummyFile(32000))
					{
						tarOut.IsStreamOwner = false;
						tarOut.WriteEntry(TarEntry.CreateEntryFromFile(dummyFile.Filename), false);
					}
				}

				// corrupt archive - make sure the file still has more than one block
				memoryStream.SetLength(16000);
				memoryStream.Seek(0, SeekOrigin.Begin);

				// try to extract
				using (var gzipStream = new GZipInputStream(memoryStream))
				{
					string tempDirName;
					gzipStream.IsStreamOwner = false;

					using (var tempDir = new Utils.TempDir())
					{
						tempDirName = tempDir.Fullpath;

						using (var tarIn = TarArchive.CreateInputTarArchive(gzipStream))
						{
							tarIn.IsStreamOwner = false;
							Assert.Throws<SharpZipBaseException>(() => tarIn.ExtractContents(tempDir.Fullpath));
						}
					}

					Assert.That(Directory.Exists(tempDirName), Is.False, "Temporary folder should have been removed");
				}
			}
		}	
		[Test]
		[TestCase(1)]
		[TestCase(100)]
		[TestCase(128)]
		[Category("Tar")]
		public void StreamWithJapaneseName(int length)
		{
			// U+3042 is Japanese Hiragana
			// https://unicode.org/charts/PDF/U3040.pdf
			var entryName = new string((char)0x3042, length);
			var data = new byte[32];
			using(var memoryStream = new MemoryStream())
			{
				using(var tarOutput = new TarOutputStream(memoryStream, System.Text.Encoding.UTF8))
				{
					var entry = TarEntry.CreateTarEntry(entryName);
					entry.Size = 32;
					tarOutput.PutNextEntry(entry);
					tarOutput.Write(data, 0, data.Length);
				}
				using(var memInput = new MemoryStream(memoryStream.ToArray()))
				using(var inputStream = new TarInputStream(memInput, Encoding.UTF8))
				{
					var buf = new byte[64];
					var entry = inputStream.GetNextEntry();
					Assert.AreEqual(entryName, entry.Name);
					var bytesread = inputStream.Read(buf, 0, buf.Length);
					Assert.AreEqual(data.Length, bytesread);
				}
			}
		}
	}
}
