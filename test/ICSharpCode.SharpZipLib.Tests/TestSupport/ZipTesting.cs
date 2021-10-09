using System;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using NUnit.Framework;

namespace ICSharpCode.SharpZipLib.Tests.TestSupport
{
	/// <summary>
	/// Provides support for testing in memory zip archives.
	/// </summary>
	internal static class ZipTesting
	{
		public static void AssertValidZip(Stream stream, string password = null, bool usesAes = true)
		{
			Assert.That(TestArchive(stream, password), "Archive did not pass ZipFile.TestArchive");

			if (!string.IsNullOrEmpty(password) && usesAes)
			{
				Assert.Ignore("ZipInputStream does not support AES");
			}
			
			stream.Seek(0, SeekOrigin.Begin);

			Assert.DoesNotThrow(() =>
			{
				using var zis = new ZipInputStream(stream){Password = password};
				while (zis.GetNextEntry() != null)
				{
					new StreamReader(zis).ReadToEnd();
				}
			}, "Archive could not be read by ZipInputStream");
		}

		/// <summary>
		/// Tests the archive.
		/// </summary>
		/// <param name="data">The data.</param>
		/// <param name="password">The password.</param>
		/// <returns></returns>
		public static bool TestArchive(byte[] data, string password = null)
		{
			using var ms = new MemoryStream(data);
			return TestArchive(new MemoryStream(data), password);
		}

		/// <summary>
		/// Tests the archive.
		/// </summary>
		/// <param name="stream">The data.</param>
		/// <param name="password">The password.</param>
		/// <returns>true if archive tests ok; false otherwise.</returns>
		public static bool TestArchive(Stream stream, string password = null)
		{
			using var zipFile = new ZipFile(stream)
			{
				IsStreamOwner = false,
				Password = password,
			};
			
			return zipFile.TestArchive(true, TestStrategy.FindAllErrors, (status, message) => 
			{
				if (!string.IsNullOrWhiteSpace(message)) TestContext.Out.WriteLine(message);
			});
		}
	}
}
