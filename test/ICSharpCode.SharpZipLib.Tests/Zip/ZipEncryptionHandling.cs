﻿using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

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
		public void Aes256Encryption()
		{
			CreateZipWithEncryptedEntries("foo", 256);
		}

		private static readonly string[] possible7zPaths = new[] {
			// Check in PATH
			"7z", "7za",

			// Check in default install location
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7z.exe"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "7z.exe"),
		};

		public static bool TryGet7zBinPath(out string path7z)
		{
			foreach (var testPath in possible7zPaths)
			{
				try
				{
					var p = Process.Start(new ProcessStartInfo(testPath, "i")
					{
						RedirectStandardOutput = true
					});
					if (!p.WaitForExit(3000))
					{
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

		public void CreateZipWithEncryptedEntries(string password, int keySize)
		{
			using (var ms = new MemoryStream())
			{
				using (var zs = new ZipOutputStream(ms))
				{
					zs.IsStreamOwner = false;
					zs.SetLevel(9); // 0-9, 9 being the highest level of compression
					zs.Password = password;  // optional. Null is the same as not setting. Required if using AES.

					ZipEntry zipEntry = new ZipEntry("test");
					zipEntry.AESKeySize = keySize;
					zipEntry.DateTime = DateTime.Now;

					zs.PutNextEntry(zipEntry);

					byte[] dummyData = Encoding.UTF8.GetBytes(@"Lorem ipsum dolor sit amet, consectetur adipiscing elit. 
Fusce bibendum diam ac nunc rutrum ornare. Maecenas blandit elit ligula, eget suscipit lectus rutrum eu. 
Maecenas aliquam, purus mattis pulvinar pharetra, nunc orci maximus justo, sed facilisis massa dui sed lorem. 
Vestibulum id iaculis leo. Duis porta ante lorem. Duis condimentum enim nec lorem tristique interdum. Fusce in faucibus libero.");

					using (var dummyStream = new MemoryStream(dummyData))
					{
						dummyStream.CopyTo(zs);
					}

					zs.CloseEntry();
				}

				if (TryGet7zBinPath(out string path7z))
				{

					ms.Seek(0, SeekOrigin.Begin);

					var fileName = Path.GetTempFileName();

					try
					{

						using (var fs = File.OpenWrite(fileName))
						{
							ms.CopyTo(fs);
						}

						var p = Process.Start(path7z, $"t -p{password} {fileName}");
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
	}
}
