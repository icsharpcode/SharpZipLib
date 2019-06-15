﻿using ICSharpCode.SharpZipLib.Core;
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
		public void Aes128Encryption()
		{
			CreateZipWithEncryptedEntries("foo", 128);
		}

		[Test]
		[Category("Encryption")]
		[Category("Zip")]
		public void Aes128EncryptionStored()
		{
			CreateZipWithEncryptedEntries("foo", 128, CompressionMethod.Stored);
		}

		[Test]
		[Category("Encryption")]
		[Category("Zip")]
		public void Aes256Encryption()
		{
			CreateZipWithEncryptedEntries("foo", 256);
		}

		[Test]
		[Category("Encryption")]
		[Category("Zip")]
		public void Aes256EncryptionStored()
		{
			CreateZipWithEncryptedEntries("foo", 256, CompressionMethod.Stored);
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
		public void ZipFileStoreAesPartialRead()
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
							byte[] buffer = new byte[1];

							while (true)
							{
								int b = zis.ReadByte();

								if (b == -1)
									break;

								ms.WriteByte((byte)b);
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
						RedirectStandardOutput = true
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

				ZipEntry zipEntry = new ZipEntry("test");
				zipEntry.AESKeySize = keySize;
				zipEntry.DateTime = DateTime.Now;
				zipEntry.CompressionMethod = compressionMethod;

				zs.PutNextEntry(zipEntry);

				byte[] dummyData = Encoding.UTF8.GetBytes(DummyDataString);

				using (var dummyStream = new MemoryStream(dummyData))
				{
					dummyStream.CopyTo(zs);
				}

				zs.CloseEntry();
			}
		}

		public void CreateZipWithEncryptedEntries(string password, int keySize, CompressionMethod compressionMethod = CompressionMethod.Deflated)
		{
			using (var ms = new MemoryStream())
			{
				WriteEncryptedZipToStream(ms, password, keySize, compressionMethod);

				if (TryGet7zBinPath(out string path7z))
				{
					Console.WriteLine($"Using 7z path: \"{path7z}\"");

					ms.Seek(0, SeekOrigin.Begin);

					var fileName = Path.GetTempFileName();

					try
					{
						using (var fs = File.OpenWrite(fileName))
						{
							ms.CopyTo(fs);
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
		}

		private const string DummyDataString = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit.
Fusce bibendum diam ac nunc rutrum ornare. Maecenas blandit elit ligula, eget suscipit lectus rutrum eu.
Maecenas aliquam, purus mattis pulvinar pharetra, nunc orci maximus justo, sed facilisis massa dui sed lorem.
Vestibulum id iaculis leo. Duis porta ante lorem. Duis condimentum enim nec lorem tristique interdum. Fusce in faucibus libero.";
	}
}
