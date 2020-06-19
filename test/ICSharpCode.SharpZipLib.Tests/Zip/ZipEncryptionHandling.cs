using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Tests.TestSupport;

namespace ICSharpCode.SharpZipLib.Tests.Zip
{
	[TestFixture]
	public class ZipEncryptionHandling
	{
		[Test]
		[Category("Encryption")]
		[Category("Zip")]
		[TestCase(CompressionMethod.Stored)]
		[TestCase(CompressionMethod.Deflated)]
		public void Aes128Encryption(CompressionMethod compressionMethod)
		{
			CreateZipWithEncryptedEntries("foo", 128, compressionMethod);
		}

		[Test]
		[Category("Encryption")]
		[Category("Zip")]
		[TestCase(CompressionMethod.Stored)]
		[TestCase(CompressionMethod.Deflated)]
		public void Aes256Encryption(CompressionMethod compressionMethod)
		{
			CreateZipWithEncryptedEntries("foo", 256, compressionMethod);
		}

		[Test]
		[Category("Encryption")]
		[Category("Zip")]
		[TestCase(CompressionMethod.Stored)]
		[TestCase(CompressionMethod.Deflated)]
		public void ZipCryptoEncryption(CompressionMethod compressionMethod)
		{
			CreateZipWithEncryptedEntries("foo", 0, compressionMethod);
		}

		[Test]
		[Category("Encryption")]
		[Category("Zip")]
		public void ZipFileAesDecryption()
		{
			var password = "password";

			using (var ms = new MemoryStream())
			{
				WriteEncryptedZipToStream(ms, password, 256);

				var zipFile = new ZipFile(ms)
				{
					Password = password
				};

				foreach (ZipEntry entry in zipFile)
				{
					if (!entry.IsFile) continue;

					using (var zis = zipFile.GetInputStream(entry))
					using (var sr = new StreamReader(zis, Encoding.UTF8))
					{
						var content = sr.ReadToEnd();
						Assert.AreEqual(DummyDataString, content, "Decompressed content does not match input data");
					}
				}
			}
		}

		[Test]
		[Category("Encryption")]
		[Category("Zip")]
		public void ZipFileAesRead()
		{
			var password = "password";

			using (var ms = new SingleByteReadingStream())
			{
				WriteEncryptedZipToStream(ms, password, 256);
				ms.Seek(0, SeekOrigin.Begin);

				var zipFile = new ZipFile(ms)
				{
					Password = password
				};

				foreach (ZipEntry entry in zipFile)
				{
					if (!entry.IsFile) continue;

					using (var zis = zipFile.GetInputStream(entry))
					using (var sr = new StreamReader(zis, Encoding.UTF8))
					{
						var content = sr.ReadToEnd();
						Assert.AreEqual(DummyDataString, content, "Decompressed content does not match input data");
					}
				}
			}
		}

		/// <summary>
		/// Test using AES encryption on a file whose contents are Stored rather than deflated
		/// </summary>
		[Test]
		[Category("Encryption")]
		[Category("Zip")]
		public void ZipFileStoreAes()
		{
			string password = "password";

			using (var memoryStream = new MemoryStream())
			{
				// Try to create a zip stream
				WriteEncryptedZipToStream(memoryStream, password, 256, CompressionMethod.Stored);

				// reset
				memoryStream.Seek(0, SeekOrigin.Begin);

				// try to read it
				var zipFile = new ZipFile(memoryStream, leaveOpen: true)
				{
					Password = password
				};

				foreach (ZipEntry entry in zipFile)
				{
					if (!entry.IsFile) continue;

					// Should be stored rather than deflated
					Assert.That(entry.CompressionMethod, Is.EqualTo(CompressionMethod.Stored), "Entry should be stored");

					using (var zis = zipFile.GetInputStream(entry))
					using (var sr = new StreamReader(zis, Encoding.UTF8))
					{
						var content = sr.ReadToEnd();
						Assert.That(content, Is.EqualTo(DummyDataString), "Decompressed content does not match input data");
					}
				}
			}
		}

		/// <summary>
		/// Test using AES encryption on a file whose contents are Stored rather than deflated
		/// </summary>
		[Test]
		[Category("Encryption")]
		[Category("Zip")]
		public void ZipFileStoreAesPartialRead([Values(1, 7, 17)] int readSize)
		{
			string password = "password";

			using (var memoryStream = new MemoryStream())
			{
				// Try to create a zip stream
				WriteEncryptedZipToStream(memoryStream, password, 256, CompressionMethod.Stored);

				// reset
				memoryStream.Seek(0, SeekOrigin.Begin);

				// try to read it
				var zipFile = new ZipFile(memoryStream, leaveOpen: true)
				{
					Password = password
				};

				foreach (ZipEntry entry in zipFile)
				{
					if (!entry.IsFile) continue;

					// Should be stored rather than deflated
					Assert.That(entry.CompressionMethod, Is.EqualTo(CompressionMethod.Stored), "Entry should be stored");

					using (var ms = new MemoryStream())
					{
						using (var zis = zipFile.GetInputStream(entry))
						{
							byte[] buffer = new byte[readSize];

							while (true)
							{
								int read = zis.Read(buffer, 0, readSize);

								if (read == 0)
									break;

								ms.Write(buffer, 0, read);
							}
						}

						ms.Seek(0, SeekOrigin.Begin);

						using (var sr = new StreamReader(ms, Encoding.UTF8))
						{
							var content = sr.ReadToEnd();
							Assert.That(content, Is.EqualTo(DummyDataString), "Decompressed content does not match input data");
						}
					}
				}
			}
		}

		/// <summary>
		/// Test adding files to an encrypted zip
		/// </summary>
		[Test]
		[Category("Encryption")]
		[Category("Zip")]
		public void ZipFileAesAdd()
		{
			string password = "password";
			string testData = "AdditionalData";
			int keySize = 256;

			using (var memoryStream = new MemoryStream())
			{
				// Try to create a zip stream
				WriteEncryptedZipToStream(memoryStream, password, keySize, CompressionMethod.Deflated);

				// reset
				memoryStream.Seek(0, SeekOrigin.Begin);

				// Update the archive with ZipFile
				{
					using (var zipFile = new ZipFile(memoryStream, leaveOpen: true) { Password = password })
					{
						zipFile.BeginUpdate();
						zipFile.Add(new StringMemoryDataSource(testData), "AdditionalEntry", CompressionMethod.Deflated);
						zipFile.CommitUpdate();
					}
				}

				// Test the updated archive
				{
					memoryStream.Seek(0, SeekOrigin.Begin);

					using (var zipFile = new ZipFile(memoryStream, leaveOpen: true) { Password = password })
					{
						Assert.That(zipFile.Count, Is.EqualTo(2), "Incorrect entry count in updated archive");

						// Disabled because of bug #317
						// Assert.That(zipFile.TestArchive(true), Is.True);

						// Check the original entry
						{
							var originalEntry = zipFile.GetEntry("test");
							Assert.That(originalEntry.IsCrypted, Is.True);
							Assert.That(originalEntry.AESKeySize, Is.EqualTo(keySize));


							using (var zis = zipFile.GetInputStream(originalEntry))
							using (var sr = new StreamReader(zis, Encoding.UTF8))
							{
								var content = sr.ReadToEnd();
								Assert.That(content, Is.EqualTo(DummyDataString), "Decompressed content does not match input data");
							}
						}

						// Check the additional entry
						// This should be encrypted, though currently only with ZipCrypto
						{
							var additionalEntry = zipFile.GetEntry("AdditionalEntry");
							Assert.That(additionalEntry.IsCrypted, Is.True);

							using (var zis = zipFile.GetInputStream(additionalEntry))
							using (var sr = new StreamReader(zis, Encoding.UTF8))
							{
								var content = sr.ReadToEnd();
								Assert.That(content, Is.EqualTo(testData), "Decompressed content does not match input data");
							}
						}
					}
				}

				// As an extra test, verify the file with 7-zip
				VerifyZipWith7Zip(memoryStream, password);
			}
		}

		/// <summary>
		/// Test deleting files from an encrypted zip
		/// </summary>
		[Test]
		[Category("Encryption")]
		[Category("Zip")]
		public void ZipFileAesDelete()
		{
			string password = "password";
			int keySize = 256;

			using (var memoryStream = new MemoryStream())
			{
				// Try to create a zip stream
				WriteEncryptedZipToStream(memoryStream, 3, password, keySize, CompressionMethod.Deflated);

				// reset
				memoryStream.Seek(0, SeekOrigin.Begin);

				// delete one of the entries from the file
				{
					using (var zipFile = new ZipFile(memoryStream, leaveOpen: true) { Password = password })
					{
						// Must have 3 entries to start with
						Assert.That(zipFile.Count, Is.EqualTo(3), "Must have 3 entries to start with");

						var entryToDelete = zipFile.GetEntry("test-1");
						Assert.That(entryToDelete, Is.Not.Null, "the entry that we want to delete must exist");

						zipFile.BeginUpdate();
						zipFile.Delete(entryToDelete);
						zipFile.CommitUpdate();
					}
				}

				// Test the updated archive
				{
					memoryStream.Seek(0, SeekOrigin.Begin);

					using (var zipFile = new ZipFile(memoryStream, leaveOpen: true) { Password = password })
					{
						// We should now only have 2 files
						Assert.That(zipFile.Count, Is.EqualTo(2), "Incorrect entry count in updated archive");

						// Disabled because of bug #317
						// Assert.That(zipFile.TestArchive(true), Is.True);

						// Check the first entry
						{
							var originalEntry = zipFile.GetEntry("test-0");
							Assert.That(originalEntry.IsCrypted, Is.True);
							Assert.That(originalEntry.AESKeySize, Is.EqualTo(keySize));


							using (var zis = zipFile.GetInputStream(originalEntry))
							using (var sr = new StreamReader(zis, Encoding.UTF8))
							{
								var content = sr.ReadToEnd();
								Assert.That(content, Is.EqualTo(DummyDataString), "Decompressed content does not match input data");
							}
						}

						// Check the second entry
						{
							var originalEntry = zipFile.GetEntry("test-2");
							Assert.That(originalEntry.IsCrypted, Is.True);
							Assert.That(originalEntry.AESKeySize, Is.EqualTo(keySize));


							using (var zis = zipFile.GetInputStream(originalEntry))
							using (var sr = new StreamReader(zis, Encoding.UTF8))
							{
								var content = sr.ReadToEnd();
								Assert.That(content, Is.EqualTo(DummyDataString), "Decompressed content does not match input data");
							}
						}
					}
				}

				// As an extra test, verify the file with 7-zip
				VerifyZipWith7Zip(memoryStream, password);
			}
		}

		// This is a zip file with one AES encrypted entry, whose password in an empty string.
		const string TestFileWithEmptyPassword = @"UEsDBDMACQBjACaj0FAyKbop//////////8EAB8AdGVzdAEAEAA4AAAA
			AAAAAFIAAAAAAAAAAZkHAAIAQUUDCABADvo3YqmCtIE+lhw26kjbqkGsLEOk6bVA+FnSpVD4yGP4Mr66Hs14aTtsPUaANX2
            Z6qZczEmwoaNQpNBnKl7p9YOG8GSHDfTCUU/AZvT4yGFhUEsHCDIpuilSAAAAAAAAADgAAAAAAAAAUEsBAjMAMwAJAGMAJq
            PQUDIpuin//////////wQAHwAAAAAAAAAAAAAAAAAAAHRlc3QBABAAOAAAAAAAAABSAAAAAAAAAAGZBwACAEFFAwgAUEsFBgAAAAABAAEAUQAAAKsAAAAAAA==";

		/// <summary>
		/// Test reading an AES encrypted entry whose password is an empty string.
		/// </summary>
		/// <remarks>
		/// Test added for https://github.com/icsharpcode/SharpZipLib/issues/471.
		/// </remarks>
		[Test]
		[Category("Zip")]
		public void ZipFileAESReadWithEmptyPassword()
		{
			var fileBytes = Convert.FromBase64String(TestFileWithEmptyPassword);

			using (var ms = new MemoryStream(fileBytes))
			using (var zipFile = new ZipFile(ms, leaveOpen: true))
			{
				zipFile.Password = string.Empty;

				var entry = zipFile.FindEntry("test", true);

				using (var inputStream = zipFile.GetInputStream(entry))
				using (var sr = new StreamReader(inputStream, Encoding.UTF8))
				{
					var content = sr.ReadToEnd();
					Assert.That(content, Is.EqualTo("Lorem ipsum dolor sit amet, consectetur adipiscing elit."), "Decompressed content does not match expected data");
				}
			}
		}

		private static readonly string[] possible7zPaths = new[] {
			// Check in PATH
			"7z", "7za",

			// Check in default install location
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "7-Zip", "7z.exe"),
		};

		public static bool TryGet7zBinPath(out string path7z)
		{
			var runTimeLimit = TimeSpan.FromSeconds(3);

			foreach (var testPath in possible7zPaths)
			{
				try
				{
					var p = Process.Start(new ProcessStartInfo(testPath, "i")
					{
						RedirectStandardOutput = true,
						UseShellExecute = false
					});
					while (!p.StandardOutput.EndOfStream && (DateTime.Now - p.StartTime) < runTimeLimit)
					{
						p.StandardOutput.DiscardBufferedData();
					}
					if (!p.HasExited)
					{
						p.Close();
						Assert.Warn($"Timed out checking for 7z binary in \"{testPath}\"!");
						continue;
					}

					if (p.ExitCode == 0)
					{
						path7z = testPath;
						return true;
					}
				}
				catch (Exception)
				{
					continue;
				}
			}
			path7z = null;
			return false;
		}

		public void WriteEncryptedZipToStream(Stream stream, string password, int keySize, CompressionMethod compressionMethod = CompressionMethod.Deflated)
		{
			using (var zs = new ZipOutputStream(stream))
			{
				zs.IsStreamOwner = false;
				zs.SetLevel(9); // 0-9, 9 being the highest level of compression
				zs.Password = password;  // optional. Null is the same as not setting. Required if using AES.

				AddEncrypedEntryToStream(zs, $"test", keySize, compressionMethod);
			}
		}

		public void WriteEncryptedZipToStream(Stream stream, int entryCount, string password, int keySize, CompressionMethod compressionMethod)
		{
			using (var zs = new ZipOutputStream(stream))
			{
				zs.IsStreamOwner = false;
				zs.SetLevel(9); // 0-9, 9 being the highest level of compression
				zs.Password = password;  // optional. Null is the same as not setting. Required if using AES.

				for (int i = 0;  i < entryCount; i++)
				{
					AddEncrypedEntryToStream(zs, $"test-{i}", keySize, compressionMethod);
				}
			}
		}

		private void AddEncrypedEntryToStream(ZipOutputStream zipOutputStream, string entryName, int keySize, CompressionMethod compressionMethod)
		{
			ZipEntry zipEntry = new ZipEntry(entryName)
			{
				AESKeySize = keySize,
				DateTime = DateTime.Now,
				CompressionMethod = compressionMethod
			};

			zipOutputStream.PutNextEntry(zipEntry);

			byte[] dummyData = Encoding.UTF8.GetBytes(DummyDataString);

			using (var dummyStream = new MemoryStream(dummyData))
			{
				dummyStream.CopyTo(zipOutputStream);
			}

			zipOutputStream.CloseEntry();
		}

		public void CreateZipWithEncryptedEntries(string password, int keySize, CompressionMethod compressionMethod = CompressionMethod.Deflated)
		{
			using (var ms = new MemoryStream())
			{
				WriteEncryptedZipToStream(ms, password, keySize, compressionMethod);
				VerifyZipWith7Zip(ms, password);
			}
		}

		/// <summary>
		/// Helper function to verify the provided zip stream with 7Zip.
		/// </summary>
		/// <param name="zipStream">A stream containing the zip archive to test.</param>
		/// <param name="password">The password for the archive.</param>
		private void VerifyZipWith7Zip(Stream zipStream, string password)
		{
			if (TryGet7zBinPath(out string path7z))
			{
				Console.WriteLine($"Using 7z path: \"{path7z}\"");

				var fileName = Path.GetTempFileName();

				try
				{
					using (var fs = File.OpenWrite(fileName))
					{
						zipStream.Seek(0, SeekOrigin.Begin);
						zipStream.CopyTo(fs);
					}

					var p = Process.Start(path7z, $"t -p{password} \"{fileName}\"");
					if (!p.WaitForExit(2000))
					{
						Assert.Warn("Timed out verifying zip file!");
					}

					Assert.AreEqual(0, p.ExitCode, "Archive verification failed");
				}
				finally
				{
					File.Delete(fileName);
				}
			}
			else
			{
				Assert.Warn("Skipping file verification since 7za is not in path");
			}
		}


		private const string DummyDataString = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit.
Fusce bibendum diam ac nunc rutrum ornare. Maecenas blandit elit ligula, eget suscipit lectus rutrum eu.
Maecenas aliquam, purus mattis pulvinar pharetra, nunc orci maximus justo, sed facilisis massa dui sed lorem.
Vestibulum id iaculis leo. Duis porta ante lorem. Duis condimentum enim nec lorem tristique interdum. Fusce in faucibus libero.";
	}
}
