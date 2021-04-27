using ICSharpCode.SharpZipLib.Tests.TestSupport;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace ICSharpCode.SharpZipLib.Tests.Zip
{
	[TestFixture]
	public class ZipEntryHandling : ZipBase
	{
		private byte[] MakeLocalHeader(string asciiName, short versionToExtract, short flags, short method,
							  int dostime, int crc, int compressedSize, int size)
		{
			using (TrackedMemoryStream ms = new TrackedMemoryStream())
			{
				ms.WriteByte((byte)'P');
				ms.WriteByte((byte)'K');
				ms.WriteByte(3);
				ms.WriteByte(4);

				ms.WriteLEShort(versionToExtract);
				ms.WriteLEShort(flags);
				ms.WriteLEShort(method);
				ms.WriteLEInt(dostime);
				ms.WriteLEInt(crc);
				ms.WriteLEInt(compressedSize);
				ms.WriteLEInt(size);

				byte[] rawName = Encoding.ASCII.GetBytes(asciiName);
				ms.WriteLEShort((short)rawName.Length);
				ms.WriteLEShort(0);
				ms.Write(rawName, 0, rawName.Length);
				return ms.ToArray();
			}
		}

		private ZipEntry MakeEntry(string asciiName, short versionToExtract, short flags, short method,
							  int dostime, int crc, int compressedSize, int size)
		{
			byte[] data = MakeLocalHeader(asciiName, versionToExtract, flags, method,
										  dostime, crc, compressedSize, size);

			var zis = new ZipInputStream(new MemoryStream(data));

			ZipEntry ze = zis.GetNextEntry();
			return ze;
		}

		private void PiecewiseCompare(ZipEntry lhs, ZipEntry rhs)
		{
			Type entryType = typeof(ZipEntry);
			BindingFlags binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

			FieldInfo[] fields = entryType.GetFields(binding);

			Assert.Greater(fields.Length, 8, "Failed to find fields");

			foreach (FieldInfo info in fields)
			{
				object lValue = info.GetValue(lhs);
				object rValue = info.GetValue(rhs);

				Assert.AreEqual(lValue, rValue);
			}
		}

		/// <summary>
		/// Test that obsolete copy constructor works correctly.
		/// </summary>
		[Test]
		[Category("Zip")]
		public void Copying()
		{
			long testCrc = 3456;
			long testSize = 99874276;
			long testCompressedSize = 72347;
			var testExtraData = new byte[] { 0x00, 0x01, 0x00, 0x02, 0x0EF, 0xFE };
			string testName = "Namu";
			int testFlags = 4567;
			long testDosTime = 23434536;
			CompressionMethod testMethod = CompressionMethod.Deflated;

			string testComment = "A comment";

			var source = new ZipEntry(testName);
			source.Crc = testCrc;
			source.Comment = testComment;
			source.Size = testSize;
			source.CompressedSize = testCompressedSize;
			source.ExtraData = testExtraData;
			source.Flags = testFlags;
			source.DosTime = testDosTime;
			source.CompressionMethod = testMethod;

#pragma warning disable 0618
			var clone = new ZipEntry(source);
#pragma warning restore

			PiecewiseCompare(source, clone);
		}

		/// <summary>
		/// Check that cloned entries are correct.
		/// </summary>
		[Test]
		[Category("Zip")]
		public void Cloning()
		{
			long testCrc = 3456;
			long testSize = 99874276;
			long testCompressedSize = 72347;
			var testExtraData = new byte[] { 0x00, 0x01, 0x00, 0x02, 0x0EF, 0xFE };
			string testName = "Namu";
			int testFlags = 4567;
			long testDosTime = 23434536;
			CompressionMethod testMethod = CompressionMethod.Deflated;

			string testComment = "A comment";

			var source = new ZipEntry(testName);
			source.Crc = testCrc;
			source.Comment = testComment;
			source.Size = testSize;
			source.CompressedSize = testCompressedSize;
			source.ExtraData = testExtraData;
			source.Flags = testFlags;
			source.DosTime = testDosTime;
			source.CompressionMethod = testMethod;

			var clone = (ZipEntry)source.Clone();

			// Check values against originals
			Assert.AreEqual(testName, clone.Name, "Cloned name mismatch");
			Assert.AreEqual(testCrc, clone.Crc, "Cloned crc mismatch");
			Assert.AreEqual(testComment, clone.Comment, "Cloned comment mismatch");
			Assert.AreEqual(testExtraData, clone.ExtraData, "Cloned Extra data mismatch");
			Assert.AreEqual(testSize, clone.Size, "Cloned size mismatch");
			Assert.AreEqual(testCompressedSize, clone.CompressedSize, "Cloned compressed size mismatch");
			Assert.AreEqual(testFlags, clone.Flags, "Cloned flags mismatch");
			Assert.AreEqual(testDosTime, clone.DosTime, "Cloned DOSTime mismatch");
			Assert.AreEqual(testMethod, clone.CompressionMethod, "Cloned Compression method mismatch");

			// Check against source
			PiecewiseCompare(source, clone);
		}

		/// <summary>
		/// Setting entry comments to null should be allowed
		/// </summary>
		[Test]
		[Category("Zip")]
		public void NullEntryComment()
		{
			var test = new ZipEntry("null");
			test.Comment = null;
		}

		/// <summary>
		/// Entries with null names arent allowed
		/// </summary>
		[Test]
		[Category("Zip")]
		//[ExpectedException(typeof(ArgumentNullException))]
		public void NullNameInConstructor()
		{
			string name = null;
			ZipEntry test; // = new ZipEntry(name);

			Assert.That(() => test = new ZipEntry(name),
				Throws.TypeOf<ArgumentNullException>());
		}

		[Test]
		[Category("Zip")]
		public void DateAndTime()
		{
			var ze = new ZipEntry("Pok");

			// -1 is not strictly a valid MS-DOS DateTime value.
			// ZipEntry is lenient about handling invalid values.
			ze.DosTime = -1;

			Assert.AreEqual(new DateTime(2107, 12, 31, 23, 59, 59), ze.DateTime);

			// 0 is a special value meaning Now.
			ze.DosTime = 0;
			TimeSpan diff = DateTime.Now - ze.DateTime;

			// Value == 2 seconds!
			ze.DosTime = 1;
			Assert.AreEqual(new DateTime(1980, 1, 1, 0, 0, 2), ze.DateTime);

			// Over the limit are set to max.
			ze.DateTime = new DateTime(2108, 1, 1);
			ze.DosTime = ze.DosTime;
			Assert.AreEqual(new DateTime(2107, 12, 31, 23, 59, 58), ze.DateTime);

			// Under the limit are set to min.
			ze.DateTime = new DateTime(1906, 12, 4);
			ze.DosTime = ze.DosTime;
			Assert.AreEqual(new DateTime(1980, 1, 1, 0, 0, 0), ze.DateTime);
		}

		[Test]
		[Category("Zip")]
		public void DateTimeSetsDosTime()
		{
			var ze = new ZipEntry("Pok");

			long original = ze.DosTime;

			ze.DateTime = new DateTime(1987, 9, 12);
			Assert.AreNotEqual(original, ze.DosTime);
			Assert.AreEqual(0, TestHelper.CompareDosDateTimes(new DateTime(1987, 9, 12), ze.DateTime));
		}

		[Test]
		public void CanDecompress()
		{
			int dosTime = 12;
			int crc = 0xfeda;

			ZipEntry ze = MakeEntry("a", 10, 0, (short)CompressionMethod.Deflated,
									dosTime, crc, 1, 1);

			Assert.IsTrue(ze.CanDecompress);

			ze = MakeEntry("a", 45, 0, (short)CompressionMethod.Stored,
									dosTime, crc, 1, 1);
			Assert.IsTrue(ze.CanDecompress);

			ze = MakeEntry("a", 99, 0, (short)CompressionMethod.Deflated,
									dosTime, crc, 1, 1);
			Assert.IsFalse(ze.CanDecompress);
		}
	}
}
