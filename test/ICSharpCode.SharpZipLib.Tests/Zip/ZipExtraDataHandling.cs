using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using System;
using System.IO;

namespace ICSharpCode.SharpZipLib.Tests.Zip
{
	[TestFixture]
	public class ZipExtraDataHandling : ZipBase
	{
		/// <summary>
		/// Extra data for separate entries should be unique to that entry
		/// </summary>
		[Test]
		[Category("Zip")]
		public void IsDataUnique()
		{
			var a = new ZipEntry("Basil");
			byte[] extra = new byte[4];
			extra[0] = 27;
			a.ExtraData = extra;

			var b = (ZipEntry)a.Clone();
			b.ExtraData[0] = 89;
			Assert.IsTrue(b.ExtraData[0] != a.ExtraData[0], "Extra data not unique " + b.ExtraData[0] + " " + a.ExtraData[0]);

			var c = (ZipEntry)a.Clone();
			c.ExtraData[0] = 45;
			Assert.IsTrue(a.ExtraData[0] != c.ExtraData[0], "Extra data not unique " + a.ExtraData[0] + " " + c.ExtraData[0]);
		}

		[Test]
		[Category("Zip")]
		public void ExceedSize()
		{
			var zed = new ZipExtraData();
			byte[] buffer = new byte[65506];
			zed.AddEntry(1, buffer);
			Assert.AreEqual(65510, zed.Length);
			zed.AddEntry(2, new byte[21]);
			Assert.AreEqual(65535, zed.Length);

			bool caught = false;
			try
			{
				zed.AddEntry(3, null);
			}
			catch
			{
				caught = true;
			}

			Assert.IsTrue(caught, "Expected an exception when max size exceeded");
			Assert.AreEqual(65535, zed.Length);

			zed.Delete(2);
			Assert.AreEqual(65510, zed.Length);

			caught = false;
			try
			{
				zed.AddEntry(2, new byte[22]);
			}
			catch
			{
				caught = true;
			}
			Assert.IsTrue(caught, "Expected an exception when max size exceeded");
			Assert.AreEqual(65510, zed.Length);
		}

		[Test]
		[Category("Zip")]
		public void Deleting()
		{
			var zed = new ZipExtraData();
			Assert.AreEqual(0, zed.Length);

			// Tag 1 Totoal length 10
			zed.AddEntry(1, new byte[] { 10, 11, 12, 13, 14, 15 });
			Assert.AreEqual(10, zed.Length, "Length should be 10");
			Assert.AreEqual(10, zed.GetEntryData().Length, "Data length should be 10");

			// Tag 2 total length  9
			zed.AddEntry(2, new byte[] { 20, 21, 22, 23, 24 });
			Assert.AreEqual(19, zed.Length, "Length should be 19");
			Assert.AreEqual(19, zed.GetEntryData().Length, "Data length should be 19");

			// Tag 3 Total Length 6
			zed.AddEntry(3, new byte[] { 30, 31 });
			Assert.AreEqual(25, zed.Length, "Length should be 25");
			Assert.AreEqual(25, zed.GetEntryData().Length, "Data length should be 25");

			zed.Delete(2);
			Assert.AreEqual(16, zed.Length, "Length should be 16");
			Assert.AreEqual(16, zed.GetEntryData().Length, "Data length should be 16");

			// Tag 2 total length  9
			zed.AddEntry(2, new byte[] { 20, 21, 22, 23, 24 });
			Assert.AreEqual(25, zed.Length, "Length should be 25");
			Assert.AreEqual(25, zed.GetEntryData().Length, "Data length should be 25");

			zed.AddEntry(3, null);
			Assert.AreEqual(23, zed.Length, "Length should be 23");
			Assert.AreEqual(23, zed.GetEntryData().Length, "Data length should be 23");
		}

		[Test]
		[Category("Zip")]
		public void BasicOperations()
		{
			var zed = new ZipExtraData(null);
			Assert.AreEqual(0, zed.Length);

			zed = new ZipExtraData(new byte[] { 1, 0, 0, 0 });
			Assert.AreEqual(4, zed.Length, "A length should be 4");

			var zed2 = new ZipExtraData();
			Assert.AreEqual(0, zed2.Length);

			zed2.AddEntry(1, new byte[] { });

			byte[] data = zed.GetEntryData();
			for (int i = 0; i < data.Length; ++i)
			{
				Assert.AreEqual(zed2.GetEntryData()[i], data[i]);
			}

			Assert.AreEqual(4, zed2.Length, "A1 length should be 4");

			bool findResult = zed.Find(2);
			Assert.IsFalse(findResult, "A - Shouldnt find tag 2");

			findResult = zed.Find(1);
			Assert.IsTrue(findResult, "A - Should find tag 1");
			Assert.AreEqual(0, zed.ValueLength, "A- Length of entry should be 0");
			Assert.AreEqual(-1, zed.ReadByte());
			Assert.AreEqual(0, zed.GetStreamForTag(1).Length, "A - Length of stream should be 0");

			zed = new ZipExtraData(new byte[] { 1, 0, 3, 0, 1, 2, 3 });
			Assert.AreEqual(7, zed.Length, "Expected a length of 7");

			findResult = zed.Find(1);
			Assert.IsTrue(findResult, "B - Should find tag 1");
			Assert.AreEqual(3, zed.ValueLength, "B - Length of entry should be 3");
			for (int i = 1; i <= 3; ++i)
			{
				Assert.AreEqual(i, zed.ReadByte());
			}
			Assert.AreEqual(-1, zed.ReadByte());

			Stream s = zed.GetStreamForTag(1);
			Assert.AreEqual(3, s.Length, "B.1 Stream length should be 3");
			for (int i = 1; i <= 3; ++i)
			{
				Assert.AreEqual(i, s.ReadByte());
			}
			Assert.AreEqual(-1, s.ReadByte());

			zed = new ZipExtraData(new byte[] { 1, 0, 3, 0, 1, 2, 3, 2, 0, 1, 0, 56 });
			Assert.AreEqual(12, zed.Length, "Expected a length of 12");

			findResult = zed.Find(1);
			Assert.IsTrue(findResult, "C.1 - Should find tag 1");
			Assert.AreEqual(3, zed.ValueLength, "C.1 - Length of entry should be 3");
			for (int i = 1; i <= 3; ++i)
			{
				Assert.AreEqual(i, zed.ReadByte());
			}
			Assert.AreEqual(-1, zed.ReadByte());

			findResult = zed.Find(2);
			Assert.IsTrue(findResult, "C.2 - Should find tag 2");
			Assert.AreEqual(1, zed.ValueLength, "C.2 - Length of entry should be 1");
			Assert.AreEqual(56, zed.ReadByte());
			Assert.AreEqual(-1, zed.ReadByte());

			s = zed.GetStreamForTag(2);
			Assert.AreEqual(1, s.Length);
			Assert.AreEqual(56, s.ReadByte());
			Assert.AreEqual(-1, s.ReadByte());

			zed = new ZipExtraData();
			zed.AddEntry(7, new byte[] { 33, 44, 55 });
			findResult = zed.Find(7);
			Assert.IsTrue(findResult, "Add.1 should find new tag");
			Assert.AreEqual(3, zed.ValueLength, "Add.1 length should be 3");
			Assert.AreEqual(33, zed.ReadByte());
			Assert.AreEqual(44, zed.ReadByte());
			Assert.AreEqual(55, zed.ReadByte());
			Assert.AreEqual(-1, zed.ReadByte());

			zed.AddEntry(7, null);
			findResult = zed.Find(7);
			Assert.IsTrue(findResult, "Add.2 should find new tag");
			Assert.AreEqual(0, zed.ValueLength, "Add.2 length should be 0");

			zed.StartNewEntry();
			zed.AddData(0xae);
			zed.AddNewEntry(55);

			findResult = zed.Find(55);
			Assert.IsTrue(findResult, "Add.3 should find new tag");
			Assert.AreEqual(1, zed.ValueLength, "Add.3 length should be 1");
			Assert.AreEqual(0xae, zed.ReadByte());
			Assert.AreEqual(-1, zed.ReadByte());

			zed = new ZipExtraData();
			zed.StartNewEntry();
			zed.AddLeLong(0);
			zed.AddLeLong(-4);
			zed.AddLeLong(-1);
			zed.AddLeLong(long.MaxValue);
			zed.AddLeLong(long.MinValue);
			zed.AddLeLong(0x123456789ABCDEF0);
			zed.AddLeLong(unchecked((long)0xFEDCBA9876543210));
			zed.AddNewEntry(567);

			s = zed.GetStreamForTag(567);
			long longValue = ReadLong(s);
			Assert.AreEqual(longValue, zed.ReadLong(), "Read/stream mismatch");
			Assert.AreEqual(0, longValue, "Expected long value of zero");

			longValue = ReadLong(s);
			Assert.AreEqual(longValue, zed.ReadLong(), "Read/stream mismatch");
			Assert.AreEqual(-4, longValue, "Expected long value of -4");

			longValue = ReadLong(s);
			Assert.AreEqual(longValue, zed.ReadLong(), "Read/stream mismatch");
			Assert.AreEqual(-1, longValue, "Expected long value of -1");

			longValue = ReadLong(s);
			Assert.AreEqual(longValue, zed.ReadLong(), "Read/stream mismatch");
			Assert.AreEqual(long.MaxValue, longValue, "Expected long value of MaxValue");

			longValue = ReadLong(s);
			Assert.AreEqual(longValue, zed.ReadLong(), "Read/stream mismatch");
			Assert.AreEqual(long.MinValue, longValue, "Expected long value of MinValue");

			longValue = ReadLong(s);
			Assert.AreEqual(longValue, zed.ReadLong(), "Read/stream mismatch");
			Assert.AreEqual(0x123456789abcdef0, longValue, "Expected long value of MinValue");

			longValue = ReadLong(s);
			Assert.AreEqual(longValue, zed.ReadLong(), "Read/stream mismatch");
			Assert.AreEqual(unchecked((long)0xFEDCBA9876543210), longValue, "Expected long value of MinValue");
		}

		[Test]
		[Category("Zip")]
		public void UnreadCountValid()
		{
			var zed = new ZipExtraData(new byte[] { 1, 0, 0, 0 });
			Assert.AreEqual(4, zed.Length, "Length should be 4");
			Assert.IsTrue(zed.Find(1), "Should find tag 1");
			Assert.AreEqual(0, zed.UnreadCount);

			// seven bytes
			zed = new ZipExtraData(new byte[] { 1, 0, 7, 0, 1, 2, 3, 4, 5, 6, 7 });
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			for (int i = 0; i < 7; ++i)
			{
				Assert.AreEqual(7 - i, zed.UnreadCount);
				zed.ReadByte();
			}

			zed.ReadByte();
			Assert.AreEqual(0, zed.UnreadCount);
		}

		[Test]
		[Category("Zip")]
		public void Skipping()
		{
			var zed = new ZipExtraData(new byte[] { 1, 0, 7, 0, 1, 2, 3, 4, 5, 6, 7 });
			Assert.AreEqual(11, zed.Length, "Length should be 11");
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			Assert.AreEqual(7, zed.UnreadCount);
			Assert.AreEqual(4, zed.CurrentReadIndex);

			zed.ReadByte();
			Assert.AreEqual(6, zed.UnreadCount);
			Assert.AreEqual(5, zed.CurrentReadIndex);

			zed.Skip(1);
			Assert.AreEqual(5, zed.UnreadCount);
			Assert.AreEqual(6, zed.CurrentReadIndex);

			zed.Skip(-1);
			Assert.AreEqual(6, zed.UnreadCount);
			Assert.AreEqual(5, zed.CurrentReadIndex);

			zed.Skip(6);
			Assert.AreEqual(0, zed.UnreadCount);
			Assert.AreEqual(11, zed.CurrentReadIndex);

			bool exceptionCaught = false;

			try
			{
				zed.Skip(1);
			}
			catch (ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Should fail to skip past end");

			Assert.AreEqual(0, zed.UnreadCount);
			Assert.AreEqual(11, zed.CurrentReadIndex);

			zed.Skip(-7);
			Assert.AreEqual(7, zed.UnreadCount);
			Assert.AreEqual(4, zed.CurrentReadIndex);

			exceptionCaught = false;
			try
			{
				zed.Skip(-1);
			}
			catch (ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Should fail to skip before beginning");
		}

		[Test]
		[Category("Zip")]
		public void ReadOverrunLong()
		{
			var zed = new ZipExtraData(new byte[] { 1, 0, 0, 0 });
			Assert.AreEqual(4, zed.Length, "Length should be 4");
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			// Empty Tag
			bool exceptionCaught = false;
			try
			{
				zed.ReadLong();
			}
			catch (ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Expected EOS exception");

			// seven bytes
			zed = new ZipExtraData(new byte[] { 1, 0, 7, 0, 1, 2, 3, 4, 5, 6, 7 });
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			exceptionCaught = false;
			try
			{
				zed.ReadLong();
			}
			catch (ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Expected EOS exception");

			zed = new ZipExtraData(new byte[] { 1, 0, 15, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 });
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			zed.ReadLong();

			exceptionCaught = false;
			try
			{
				zed.ReadLong();
			}
			catch (ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Expected EOS exception");
		}

		[Test]
		[Category("Zip")]
		public void ReadOverrunInt()
		{
			var zed = new ZipExtraData(new byte[] { 1, 0, 0, 0 });
			Assert.AreEqual(4, zed.Length, "Length should be 4");
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			// Empty Tag
			bool exceptionCaught = false;
			try
			{
				zed.ReadInt();
			}
			catch (ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Expected EOS exception");

			// three bytes
			zed = new ZipExtraData(new byte[] { 1, 0, 3, 0, 1, 2, 3 });
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			exceptionCaught = false;
			try
			{
				zed.ReadInt();
			}
			catch (ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Expected EOS exception");

			zed = new ZipExtraData(new byte[] { 1, 0, 7, 0, 1, 2, 3, 4, 5, 6, 7 });
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			zed.ReadInt();

			exceptionCaught = false;
			try
			{
				zed.ReadInt();
			}
			catch (ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Expected EOS exception");
		}

		[Test]
		[Category("Zip")]
		public void ReadOverrunShort()
		{
			var zed = new ZipExtraData(new byte[] { 1, 0, 0, 0 });
			Assert.AreEqual(4, zed.Length, "Length should be 4");
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			// Empty Tag
			bool exceptionCaught = false;
			try
			{
				zed.ReadShort();
			}
			catch (ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Expected EOS exception");

			// Single byte
			zed = new ZipExtraData(new byte[] { 1, 0, 1, 0, 1 });
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			exceptionCaught = false;
			try
			{
				zed.ReadShort();
			}
			catch (ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Expected EOS exception");

			zed = new ZipExtraData(new byte[] { 1, 0, 2, 0, 1, 2 });
			Assert.IsTrue(zed.Find(1), "Should find tag 1");

			zed.ReadShort();

			exceptionCaught = false;
			try
			{
				zed.ReadShort();
			}
			catch (ZipException)
			{
				exceptionCaught = true;
			}
			Assert.IsTrue(exceptionCaught, "Expected EOS exception");
		}

		[Test]
		[Category("Zip")]
		public void TaggedDataHandling()
		{
			var tagData = new NTTaggedData();
			DateTime modTime = tagData.LastModificationTime;
			byte[] rawData = tagData.GetData();
			tagData.LastModificationTime = tagData.LastModificationTime + TimeSpan.FromSeconds(40);
			Assert.AreNotEqual(tagData.LastModificationTime, modTime);
			tagData.SetData(rawData, 0, rawData.Length);
			Assert.AreEqual(10, tagData.TagID, "TagID mismatch");
			Assert.AreEqual(modTime, tagData.LastModificationTime, "NT Mod time incorrect");

			tagData.CreateTime = DateTime.FromFileTimeUtc(0);
			tagData.LastAccessTime = new DateTime(9999, 12, 31, 23, 59, 59);
			rawData = tagData.GetData();

			var unixData = new ExtendedUnixData();
			modTime = unixData.ModificationTime;
			unixData.ModificationTime = modTime; // Ensure flag is set.

			rawData = unixData.GetData();
			unixData.ModificationTime += TimeSpan.FromSeconds(100);
			Assert.AreNotEqual(unixData.ModificationTime, modTime);
			unixData.SetData(rawData, 0, rawData.Length);
			Assert.AreEqual(0x5455, unixData.TagID, "TagID mismatch");
			Assert.AreEqual(modTime, unixData.ModificationTime, "Unix mod time incorrect");
		}
	}
}
