using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace ICSharpCode.SharpZipLib.Tests.TestSupport
{
	// Helper class for verifying zips with 7-zip
	internal static class SevenZipHelper
	{
		private static readonly string[] possible7zPaths = new[] {
			// Check in PATH
			"7z", "7za",

			// Check in default install location
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "7-Zip", "7z.exe"),
		};

		private static bool TryGet7zBinPath(out string path7z)
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

		/// <summary>
		/// Helper function to verify the provided zip stream with 7Zip.
		/// </summary>
		/// <param name="zipStream">A stream containing the zip archive to test.</param>
		/// <param name="password">The password for the archive.</param>
		internal static void VerifyZipWith7Zip(Stream zipStream, string password)
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

					var p = Process.Start(new ProcessStartInfo(path7z, $"t -p{password} \"{fileName}\"")
					{
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						UseShellExecute = false,
					});
					
					if (p == null)
					{
						Assert.Inconclusive("Failed to start 7z process. Skipping!");
					}
					if (!p.WaitForExit(2000))
					{
						Assert.Warn("Timed out verifying zip file!");
					}

					TestContext.Out.Write(p.StandardOutput.ReadToEnd());
					var errors = p.StandardError.ReadToEnd();
					Assert.IsEmpty(errors, "7z reported errors");
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
