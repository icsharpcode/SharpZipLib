using System;
using System.IO;
using System.Text;

using NUnit.Framework;

using ICSharpCode.SharpZipLib.Tar;

namespace ICSharpCode.SharpZipLib.Tests.Tar {
	
	/// <summary>
	/// This class contains test cases for Tar archive handling
	/// </summary>
	[TestFixture]
	public class TarTestSuite
	{
		int entryCount;
		
		void EntryCounter(TarArchive archive, TarEntry entry, string message)
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
			MemoryStream ms = new MemoryStream();
			TarArchive tarOut = TarArchive.CreateOutputTarArchive(ms);
			tarOut.CloseArchive();
			
			Assert.IsTrue(ms.GetBuffer().Length > 0, "Archive size must be > zero");
			Assert.AreEqual(ms.GetBuffer().Length % tarOut.RecordSize, 0, "Archive size must be a multiple of record size");
			
			MemoryStream ms2 = new MemoryStream();
			ms2.Write(ms.GetBuffer(), 0, ms.GetBuffer().Length);
			ms2.Seek(0, SeekOrigin.Begin);
			
			TarArchive tarIn = TarArchive.CreateInputTarArchive(ms2);
			entryCount = 0;
			tarIn.ProgressMessageEvent += new ProgressMessageHandler(EntryCounter);
			tarIn.ListContents();
			Assert.AreEqual(0, entryCount, "Expected 0 tar entries");
		}
		
		void TryLongName(string name)
		{
			MemoryStream ms = new MemoryStream();
			TarOutputStream tarOut = new TarOutputStream(ms);

			DateTime modTime = DateTime.Now;

			TarEntry entry = TarEntry.CreateTarEntry(name);

			tarOut.PutNextEntry(entry);
			tarOut.Close();

			MemoryStream ms2 = new MemoryStream();
			ms2.Write(ms.GetBuffer(), 0, ms.GetBuffer().Length);
			ms2.Seek(0, SeekOrigin.Begin);

			TarInputStream tarIn = new TarInputStream(ms2);
			TarEntry nextEntry = tarIn.GetNextEntry();
			
			Assert.AreEqual(nextEntry.Name, name, "Name match failure");
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
		}

		/// <summary>
		/// Test equals function for tar headers.
		/// </summary>
		[Test]
		[Category("Tar")]
		public void HeaderEquality()
		{
			TarHeader h1 = new TarHeader();
			TarHeader h2 = new TarHeader();

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
		
			h1.Magic = "ustar";
			Assert.IsFalse(h1.Equals(h2));
			h2.Magic = h1.Magic;
			Assert.IsTrue(h1.Equals(h2));
		
			h1.Version = "1";
			Assert.IsFalse(h1.Equals(h2));
			h2.Version = h1.Version;
			Assert.IsTrue(h1.Equals(h2));
		
			h1.UserName = "user";
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
			MemoryStream ms = new MemoryStream();
			TarOutputStream tarOut = new TarOutputStream(ms);

			DateTime modTime = DateTime.Now;

			TarEntry entry = TarEntry.CreateTarEntry("TestEntry");
			entry.TarHeader.Mode = 12345;

			tarOut.PutNextEntry(entry);
			tarOut.Close();

			MemoryStream ms2 = new MemoryStream();
			ms2.Write(ms.GetBuffer(), 0, ms.GetBuffer().Length);
			ms2.Seek(0, SeekOrigin.Begin);

			TarInputStream tarIn = new TarInputStream(ms2);
			TarEntry nextEntry = tarIn.GetNextEntry();
			
			Assert.IsTrue(nextEntry.TarHeader.IsChecksumValid, "Checksum should be valid");
			
			MemoryStream ms3 = new MemoryStream();
			ms3.Write(ms.GetBuffer(), 0, ms.GetBuffer().Length);
			ms3.Seek(0, SeekOrigin.Begin);
			ms3.Write(new byte[1] { 34 }, 0, 1);
			ms3.Seek(0, SeekOrigin.Begin);

			tarIn = new TarInputStream(ms3);
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

		/// <summary>
		/// Check that values set are preserved when writing and reading archives.
		/// </summary>
		[Test]
		[Category("Tar")]
		public void ValuesPreserved()
		{
			MemoryStream ms = new MemoryStream();
			TarOutputStream tarOut = new TarOutputStream(ms);
			
			DateTime modTime = DateTime.Now;
			
			TarEntry entry = TarEntry.CreateTarEntry("TestEntry");
			entry.GroupId = 12;
			entry.UserId = 14;
			entry.ModTime = modTime;
			entry.UserName = "UserName";
			entry.GroupName = "GroupName";
			entry.TarHeader.Mode = 12345;
			
			tarOut.PutNextEntry(entry);
			tarOut.Close();

			MemoryStream ms2 = new MemoryStream();
			ms2.Write(ms.GetBuffer(), 0, ms.GetBuffer().Length);
			ms2.Seek(0, SeekOrigin.Begin);
			
			TarInputStream tarIn = new TarInputStream(ms2);
			TarEntry nextEntry = tarIn.GetNextEntry();
			Assert.AreEqual(entry.TarHeader.Checksum, nextEntry.TarHeader.Checksum, "Checksum");
			
			Assert.IsTrue(nextEntry.Equals(entry), "Entries should be equal");
			Assert.IsTrue(nextEntry.TarHeader.Equals(entry.TarHeader), "Headers should match");

			// Tar only stores seconds 
			DateTime truncatedTime = new DateTime(modTime.Year, modTime.Month, modTime.Day, 
			                                      modTime.Hour, modTime.Minute, modTime.Second);
			Assert.AreEqual(truncatedTime, nextEntry.ModTime, "Modtimes should match");
			
			int entryCount = 0;
			while ( nextEntry != null )
			{
				++entryCount;
				nextEntry = tarIn.GetNextEntry();
			}
			
			Assert.AreEqual(1, entryCount, "Expected 1 entry");
		}
		
		/// <summary>
		/// Check invalid mod times are detected
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void InvalidModTime()
		{
			TarEntry e = TarEntry.CreateTarEntry("test");
			e.ModTime = DateTime.MinValue;
		}
		
		
		/// <summary>
		/// Check invalid sizes are detected
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void InvalidSize()
		{
			TarEntry e = TarEntry.CreateTarEntry("test");
			e.Size = -6;
		}
		
		/// <summary>
		/// Check invalid names are detected
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void InvalidName()
		{
			TarEntry e = TarEntry.CreateTarEntry("test");
			e.Name = null;
		}
		
		/// <summary>
		/// Check setting user and group names.
		/// </summary>
		[Test]
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
		[ExpectedException(typeof(ArgumentNullException))]
		public void InvalidMagic()
		{
			TarEntry e = TarEntry.CreateTarEntry("test");
			e.TarHeader.Magic = null;
		}
		
		/// <summary>
		/// Check invalid link names are detected
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void InvalidLinkName()
		{
			TarEntry e = TarEntry.CreateTarEntry("test");
			e.TarHeader.LinkName = null;
		}
		
		/// <summary>
		/// Check invalid version names are detected
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void InvalidVersionName()
		{
			TarEntry e = TarEntry.CreateTarEntry("test");
			e.TarHeader.Version = null;
		}
	}
}
