using System;
using System.IO;

using NUnit.Framework;

using ICSharpCode.SharpZipLib.Tar;

namespace ICSharpCode.SharpZipLib.Tests.Tar {
	
	/// <summary>
	/// This class contains test cases for Tar archive handling
	/// TODO  A whole lot more tests
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
	}
}
