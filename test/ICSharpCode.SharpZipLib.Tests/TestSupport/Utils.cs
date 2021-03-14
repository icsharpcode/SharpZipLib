using NUnit.Framework;
using System;
using System.IO;
using System.Text;

namespace ICSharpCode.SharpZipLib.Tests.TestSupport
{
	/// <summary>
	/// Miscellaneous test utilities.
	/// </summary>
	public static class Utils
	{
		public static int DummyContentLength = 16;

		private static Random random = new Random();
		
		/// <summary>
		/// Returns the system root for the current platform (usually c:\ for windows and / for others)
		/// </summary>
		public static string SystemRoot { get; } = 
			Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));

		private static void Compare(byte[] a, byte[] b)
		{
			
			if (a == null)
			{
				throw new ArgumentNullException(nameof(a));
			}

			if (b == null)
			{
				throw new ArgumentNullException(nameof(b));
			}

			Assert.AreEqual(a.Length, b.Length);
			for (int i = 0; i < a.Length; ++i)
			{
				Assert.AreEqual(a[i], b[i]);
			}
		}

		public static void WriteDummyData(string fileName, int size = -1)
		{
			using(var fs = File.OpenWrite(fileName))
			{
				WriteDummyData(fs, size);
			}
		}

		public static void WriteDummyData(Stream stream, int size = -1)
		{
			var bytes = (size < 0)
				? Encoding.ASCII.GetBytes(DateTime.UtcNow.Ticks.ToString("x16"))
				: new byte[size];

			if(size > 0)
			{
				random.NextBytes(bytes);
			}

			stream.Write(bytes, 0, bytes.Length);
		}

		public static TempFile GetDummyFile(int size = -1)
		{
			var tempFile = new TempFile();
			WriteDummyData(tempFile.Filename, size);
			return tempFile;
		}

		public static string GetDummyFileName()
			=> $"{random.Next():x8}{random.Next():x8}{random.Next():x8}";

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
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			#endregion IDisposable Support
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
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			internal string CreateDummyFile(int size = -1)
				=> CreateDummyFile(GetDummyFileName(), size);

			internal string CreateDummyFile(string name, int size = -1)
			{
				var fileName = Path.Combine(Fullpath, name);
				WriteDummyData(fileName, size);
				return fileName;
			}

			#endregion IDisposable Support
		}
	}
}
