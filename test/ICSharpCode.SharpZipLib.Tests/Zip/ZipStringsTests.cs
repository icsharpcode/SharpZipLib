using ICSharpCode.SharpZipLib.Tests.TestSupport;
using ICSharpCode.SharpZipLib.Tests.Zip;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Does = ICSharpCode.SharpZipLib.Tests.TestSupport.Does;

// As there is no way to order the test namespace execution order we use a name that should be alphabetically sorted before any other namespace
// This is because we have one test that only works when no encoding provider has been loaded which is not reversable once done.
namespace ICSharpCode.SharpZipLib.Tests._Zip
{
	[TestFixture]
	[Order(1)]
	public class ZipStringsTests
	{
		[Test]
		[Order(1)]
		// NOTE: This test needs to be run before any test registering CodePagesEncodingProvider.Instance
		public void TestSystemDefaultEncoding()
		{
			Console.WriteLine($"Default encoding before registering provider: {Encoding.GetEncoding(0).EncodingName}");
			Encoding.RegisterProvider(new TestEncodingProvider());
			Console.WriteLine($"Default encoding after registering provider: {Encoding.GetEncoding(0).EncodingName}");

			// Initialize a default StringCodec
			var sc = StringCodec.Default;

			var legacyEncoding = sc.ZipEncoding(false);
			Assert.That(legacyEncoding.EncodingName, Is.EqualTo(TestEncodingProvider.DefaultEncodingName));
			Assert.That(legacyEncoding.CodePage, Is.EqualTo(TestEncodingProvider.DefaultEncodingCodePage)); 
		}

		[Test]
		[Order(2)]
		public void TestFastZipRoundTripWithCodePage()
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			using var ms = new MemoryStream();
			using var zipFile = new TempFile();
			using var srcDir = new TempDir();
			using var dstDir = new TempDir();

			srcDir.CreateDummyFile("file1");
			srcDir.CreateDummyFile("слово");

			foreach(var f in Directory.EnumerateFiles(srcDir.FullName))
			{
				Console.WriteLine(f);
			}

			var fzCreate = new FastZip() { StringCodec = StringCodec.FromCodePage(866), UseUnicode = false };
			fzCreate.CreateZip(zipFile, srcDir.FullName, true, null);

			var fzExtract = new FastZip() { StringCodec = StringCodec.FromCodePage(866) };
			fzExtract.ExtractZip(zipFile, dstDir.FullName, null);

			foreach (var f in Directory.EnumerateFiles(dstDir.FullName))
			{
				Console.WriteLine(f);
			}

			Assert.That(dstDir.GetFile("file1").FullName, Does.Exist);
			Assert.That(dstDir.GetFile("слово").FullName, Does.Exist);
		}


		[Test]
		[Order(2)]
		public void TestZipFileRoundTripWithCodePage()
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			using var ms = new MemoryStream();
			using (var zf = ZipFile.Create(ms))
			{
				zf.StringCodec = StringCodec.FromCodePage(866);
				zf.BeginUpdate();
				zf.Add(MemoryDataSource.Empty, "file1", CompressionMethod.Stored, useUnicodeText: false);
				zf.Add(MemoryDataSource.Empty, "слово", CompressionMethod.Stored, useUnicodeText: false);
				zf.CommitUpdate();
			}

			ms.Seek(0, SeekOrigin.Begin);

			using (var zf = new ZipFile(ms, false, StringCodec.FromCodePage(866)) { IsStreamOwner = false })
			{
				Assert.That(zf.GetEntry("file1"), Is.Not.Null);
				Assert.That(zf.GetEntry("слово"), Is.Not.Null);
			}

		}

		[Test]
		[Order(2)]
		public void TestZipStreamRoundTripWithCodePage()
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			using var ms = new MemoryStream();
			using (var zos = new ZipOutputStream(ms, StringCodec.FromCodePage(866)) { IsStreamOwner = false })
			{
				zos.PutNextEntry(new ZipEntry("file1") { IsUnicodeText = false });
				zos.PutNextEntry(new ZipEntry("слово") { IsUnicodeText = false });
			}

			ms.Seek(0, SeekOrigin.Begin);

			using (var zis = new ZipInputStream(ms, StringCodec.FromCodePage(866)) { IsStreamOwner = false })
			{
				Assert.That(zis.GetNextEntry().Name, Is.EqualTo("file1"));
				Assert.That(zis.GetNextEntry().Name, Is.EqualTo("слово"));
			}

		}

		[Test]
		[Order(2)]
		public void TestZipCryptoPasswordEncodingRoundtrip()
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			var content = Utils.GetDummyBytes(32);

			using var ms = new MemoryStream();
			using (var zos = new ZipOutputStream(ms, StringCodec.FromCodePage(866)) { IsStreamOwner = false })
			{
				zos.Password = "слово";
				zos.PutNextEntry(new ZipEntry("file1"));
				zos.Write(content, 0, content.Length);
			}

			ms.Seek(0, SeekOrigin.Begin);

			using (var zis = new ZipInputStream(ms, StringCodec.FromCodePage(866)) { IsStreamOwner = false })
			{
				zis.Password = "слово";
				var entry = zis.GetNextEntry();
				var output = new byte[32];
				Assert.That(zis.Read(output, 0, 32), Is.EqualTo(32));
				Assert.That(output, Is.EqualTo(content));
			}

		}

		[Test]
		[Order(2)]
		public void TestZipStreamCommentEncodingRoundtrip()
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			var content = Utils.GetDummyBytes(32);

			using var ms = new MemoryStream();
			using (var zos = new ZipOutputStream(ms, StringCodec.FromCodePage(866)) { IsStreamOwner = false })
			{
				zos.SetComment("слово");
			}

			ms.Seek(0, SeekOrigin.Begin);

			using var zf = new ZipFile(ms, false, StringCodec.FromCodePage(866));
			Assert.That(zf.ZipFileComment, Is.EqualTo("слово"));
		}


		[Test]
		[Order(2)]
		public void TestZipFileCommentEncodingRoundtrip()
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			var content = Utils.GetDummyBytes(32);

			using var ms = new MemoryStream();
			using (var zf = ZipFile.Create(ms))
			{
				zf.StringCodec = StringCodec.FromCodePage(866);
				zf.BeginUpdate();
				zf.SetComment("слово");
				zf.CommitUpdate();
			}

			ms.Seek(0, SeekOrigin.Begin);

			using (var zf = new ZipFile(ms, false, StringCodec.FromCodePage(866)))
			{
				Assert.That(zf.ZipFileComment, Is.EqualTo("слово"));
			}
		}
	}


	internal class TestEncodingProvider : EncodingProvider
	{
		internal static string DefaultEncodingName = "TestDefaultEncoding";
		internal static int DefaultEncodingCodePage = -37;

		class TestDefaultEncoding : Encoding
		{
			public override string EncodingName => DefaultEncodingName;
			public override int CodePage => DefaultEncodingCodePage;

			public override int GetByteCount(char[] chars, int index, int count) 
				=> UTF8.GetByteCount(chars, index, count);

			public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) 
				=> UTF8.GetBytes(chars, charIndex, charCount, bytes, byteIndex);

			public override int GetCharCount(byte[] bytes, int index, int count)
				=> UTF8.GetCharCount(bytes, index, count);

			public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) 
				=> UTF8.GetChars(bytes, byteIndex, byteCount, chars, charIndex);

			public override int GetMaxByteCount(int charCount) => UTF8.GetMaxByteCount(charCount);

			public override int GetMaxCharCount(int byteCount) => UTF8.GetMaxCharCount(byteCount);
		}

		TestDefaultEncoding testDefaultEncoding = new TestDefaultEncoding();

		public override Encoding GetEncoding(int codepage)
			=> (codepage == 0 || codepage == DefaultEncodingCodePage) ? testDefaultEncoding : null;

		public override Encoding GetEncoding(string name) 
			=> DefaultEncodingName == name ? testDefaultEncoding : null;

#if NET6_0_OR_GREATER
		public override IEnumerable<EncodingInfo> GetEncodings()
		{
			yield return new EncodingInfo(this, DefaultEncodingCodePage, DefaultEncodingName, DefaultEncodingName);
		}
#endif
	}
}
