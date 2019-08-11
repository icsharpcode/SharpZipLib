using ICSharpCode.SharpZipLib.Lzw;
using ICSharpCode.SharpZipLib.Tests.TestSupport;
using NUnit.Framework;
using System.IO;

namespace ICSharpCode.SharpZipLib.Tests.Lzw
{
	[TestFixture]
	public class LzwTestSuite
	{
		//[Test]
		//[Category("LZW")]
		//public void TestLzw() {
		//    LzwInputStream str = new LzwInputStream(File.OpenRead("D:\\hour2890.09n.Z"));
		//    Stream raw = File.OpenRead("D:\\hour2890.09n");
		//    byte[] data = new byte[1028 * 1028];
		//    byte[] dataRaw = new byte[1028 * 1028];
		//    raw.Read(dataRaw, 0, 1028);
		//    str.Read(data, 0, 1028);
		//    for (int i = 0; i < 1028; i++) {
		//        Assert.AreEqual(data[i], dataRaw[i]);
		//    }

		//    Stream output = File.Open("D:\\erase.txt", FileMode.CreateNew);
		//    output.Write(data, 0, 1028);
		//    output.Close();
		//    raw.Close();
		//}

		//[Test]
		//[Category("LZW")]
		//public void TestStream() {
		//    using (Stream inStream = new LzwInputStream(File.OpenRead("D:\\hour2890.09n.Z")))
		//    using (FileStream outStream = File.Create("D:\\hour2890.09n")) {
		//        byte[] buffer = new byte[4096];
		//        StreamUtils.Copy(inStream, outStream, buffer);
		//    }
		//}

		[Test]
		[Category("LZW")]
		public void ZeroLengthInputStream()
		{
			var lis = new LzwInputStream(new MemoryStream());
			bool exception = false;
			try
			{
				lis.ReadByte();
			}
			catch
			{
				exception = true;
			}

			Assert.IsTrue(exception, "reading from an empty stream should cause an exception");
		}

		[Test]
		[Category("LZW")]
		public void InputStreamOwnership()
		{
			var memStream = new TrackedMemoryStream();
			var s = new LzwInputStream(memStream);

			Assert.IsFalse(memStream.IsClosed, "Shouldnt be closed initially");
			Assert.IsFalse(memStream.IsDisposed, "Shouldnt be disposed initially");

			s.Close();

			Assert.IsTrue(memStream.IsClosed, "Should be closed after parent owner close");
			Assert.IsTrue(memStream.IsDisposed, "Should be disposed after parent owner close");

			memStream = new TrackedMemoryStream();
			s = new LzwInputStream(memStream);

			Assert.IsFalse(memStream.IsClosed, "Shouldnt be closed initially");
			Assert.IsFalse(memStream.IsDisposed, "Shouldnt be disposed initially");

			s.IsStreamOwner = false;
			s.Close();

			Assert.IsFalse(memStream.IsClosed, "Should not be closed after parent owner close");
			Assert.IsFalse(memStream.IsDisposed, "Should not be disposed after parent owner close");
		}
	}
}
