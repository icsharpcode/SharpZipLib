using System;
using System.IO;
using NUnit.Framework;

namespace ICSharpCode.SharpZipLib.Tests.TestSupport
{
	/// <summary>
	/// Miscellaneous test utilities.
	/// </summary>
	public static class Utils
	{
		static Random random = new Random();

		static void Compare(byte[] a, byte[] b)
		{
			if (a == null) {
				throw new ArgumentNullException(nameof(a));
			}

			if (b == null) {
				throw new ArgumentNullException(nameof(b));
			}

			Assert.AreEqual(a.Length, b.Length);
			for (int i = 0; i < a.Length; ++i) {
				Assert.AreEqual(a[i], b[i]);
			}
		}

		public static TempFile GetDummyFile(int size = -1)
		{
			var tempFile = new TempFile();
			if (size < 0)
			{
				File.WriteAllText(tempFile.Filename, DateTime.UtcNow.Ticks.ToString("x16"));
			}
			else if (size > 0)
			{
				var bytes = Array.CreateInstance(typeof(byte), size) as byte[];
				random.NextBytes(bytes);
				File.WriteAllBytes(tempFile.Filename, bytes);
			}
			return tempFile;
		}

		public class TempFile : IDisposable
		{
			public string Filename { get; internal set; }

			public TempFile()
			{
				Filename = Path.GetTempFileName();
			}

			#region IDisposable Support
			private bool disposed = false; // To detect redundant calls


			protected virtual void Dispose(bool disposing)
			{
				if (!disposed)
				{
					if (disposing && File.Exists(Filename))
					{
						try
						{
							File.Delete(Filename);
						}
						catch { }
					}

					disposed = true;
				}
			}

			public void Dispose()
				=> Dispose(true);

			#endregion

		}

		public class TempDir : IDisposable
		{
			public string Fullpath { get; internal set; }

			public TempDir()
			{
				Fullpath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
				Directory.CreateDirectory(Fullpath);
			}

			#region IDisposable Support
			private bool disposed = false; // To detect redundant calls


			protected virtual void Dispose(bool disposing)
			{
				if (!disposed)
				{
					if (disposing && Directory.Exists(Fullpath))
					{
						try
						{
							Directory.Delete(Fullpath, true);
						}
						catch { }
					}

					disposed = true;
				}
			}

			public void Dispose()
				=> Dispose(true);

			#endregion

		}
	}

	
}
