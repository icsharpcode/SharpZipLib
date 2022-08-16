using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Tests.Zip;
using System.Linq;
using System.Threading.Tasks;

namespace ICSharpCode.SharpZipLib.Tests.TestSupport
{
	/// <summary>
	/// Miscellaneous test utilities.
	/// </summary>
	public static class Utils
	{
		public static int DummyContentLength = 16;

		internal const int DefaultSeed = 5;
		private static Random random = new Random(DefaultSeed);
		
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

		/// <summary>
		/// Write pseudo-random data to <paramref name="fileName"/>,
		/// creating it if it does not exist or truncating it otherwise 
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="size"></param>
		/// <param name="seed"></param>
		public static void WriteDummyData(string fileName, int size, int seed = DefaultSeed)
		{
			using var fs = File.Create(fileName);
			WriteDummyData(fs, size, seed);
		}

		/// <summary>
		/// Write pseudo-random data to <paramref name="stream"/>
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="size"></param>
		/// <param name="seed"></param>
		public static void WriteDummyData(Stream stream, int size, int seed = DefaultSeed)
		{
			var bytes = GetDummyBytes(size, seed);
			stream.Write(bytes, offset: 0, bytes.Length);
		}
		
		/// <summary>
		/// Creates a buffer of <paramref name="size"/> pseudo-random bytes 
		/// </summary>
		/// <param name="size"></param>
		/// <param name="seed"></param>
		/// <returns></returns>
		public static byte[] GetDummyBytes(int size, int seed = DefaultSeed)
		{
			var random = new Random(seed);
			var bytes = new byte[size];
			random.NextBytes(bytes);
			return bytes;
		}
		
		public static async Task WriteDummyDataAsync(Stream stream, int size = -1)
		{
			var bytes = GetDummyBytes(size);
			await stream.WriteAsync(bytes, 0, bytes.Length);
		}

		/// <summary>
		/// Returns a file reference with <paramref name="size"/> bytes of dummy data written to it
		/// </summary>
		/// <param name="size"></param>
		/// <returns></returns>
		public static TempFile GetDummyFile(int size = 16)
		{
			var tempFile = new TempFile();
			using var fs = tempFile.Create();
			WriteDummyData(fs, size);
			return tempFile;
		}

		/// <summary>
		/// Returns a randomized file/directory name (without any path) using a generated GUID
		/// </summary>
		/// <returns></returns>
		public static string GetDummyFileName()
			=> string.Concat(Guid.NewGuid().ToByteArray().Select(b => $"{b:x2}"));

		/// <summary>
		/// Returns a reference to a temporary directory that deletes it's contents when disposed
		/// </summary>
		/// <returns></returns>
		public static TempDir GetTempDir() => new TempDir();

		/// <summary>
		/// Returns a reference to a temporary file that deletes it's referred file when disposed
		/// </summary>
		/// <returns></returns>
		public static TempFile GetTempFile() => new TempFile();

		public static void PatchFirstEntrySize(Stream stream, int newSize)
		{
			using(stream)
			{
				var sizeBytes = BitConverter.GetBytes(newSize);

				stream.Seek(18, SeekOrigin.Begin);
				stream.Write(sizeBytes, 0, 4);
				stream.Write(sizeBytes, 0, 4);
			}
		}
	}
	
	public class TestTraceListener : TraceListener
	{
		private readonly TextWriter _writer;
		public TestTraceListener(TextWriter writer)
		{
			_writer = writer;
		}

		public override void WriteLine(string message) => _writer.WriteLine(message);
		public override void Write(string message) => _writer.Write(message);
	}
	
	public class TempFile : FileSystemInfo, IDisposable
	{
		private FileInfo _fileInfo;

		public override string Name => _fileInfo.Name;
		public override bool Exists => _fileInfo.Exists;
		public string DirectoryName => _fileInfo.DirectoryName;

		public override string FullName => _fileInfo.FullName;

		public byte[] ReadAllBytes() => File.ReadAllBytes(_fileInfo.FullName);

		public static implicit operator string(TempFile tf) => tf._fileInfo.FullName;
		
		public override void Delete()
	    {
		    if(!Exists) return;
			_fileInfo.Delete();
	    }

		public FileStream Open(FileMode mode, FileAccess access) => _fileInfo.Open(mode, access);
		public FileStream Open(FileMode mode) => _fileInfo.Open(mode);
		public FileStream Create() => _fileInfo.Create();

	    public static TempFile WithDummyData(int size, string dirPath = null, string filename = null, int seed = Utils.DefaultSeed)
	    {
		    var tempFile = new TempFile(dirPath, filename);
		    Utils.WriteDummyData(tempFile.FullName, size, seed);
		    return tempFile;
	    }

	    internal TempFile(string dirPath = null, string filename = null)
	    {
		    dirPath ??= Path.GetTempPath();
		    filename ??= Utils.GetDummyFileName();
		    _fileInfo = new FileInfo(Path.Combine(dirPath, filename));
	    }

    	#region IDisposable Support

    	private bool _disposed; // To detect redundant calls

    	protected virtual void Dispose(bool disposing)
    	{
    		if (_disposed) return;
            if (disposing)
            {
	            try
	            {
		            Delete();
	            }
	            catch
	            {
		            // ignored
	            }
            }

    		_disposed = true;
    	}

    	public void Dispose()
    	{
    		Dispose(disposing: true);
    		GC.SuppressFinalize(this);
    	}

    	#endregion IDisposable Support
	}
  
  
  
    public class TempDir : FileSystemInfo, IDisposable
    {
	    public override string Name => Path.GetFileName(FullName);
        public override bool Exists => Directory.Exists(FullName);
        
        public static implicit operator string(TempDir td) => td.FullName;

        public override void Delete()
        {
	        if(!Exists) return;
	        Directory.Delete(FullPath, recursive: true);
        }

        public TempDir()
    	{
	        FullPath = Path.Combine(Path.GetTempPath(), Utils.GetDummyFileName());
    		Directory.CreateDirectory(FullPath);
    	}

        public TempFile CreateDummyFile(int size = 16, int seed = Utils.DefaultSeed)
	        => CreateDummyFile(null, size);

        public TempFile CreateDummyFile(string name, int size = 16, int seed = Utils.DefaultSeed)
	        => TempFile.WithDummyData(size, FullPath, name, seed);

        public TempFile GetFile(string fileName) => new TempFile(FullPath, fileName);
        
    	#region IDisposable Support

    	private bool _disposed; // To detect redundant calls

    	protected virtual void Dispose(bool disposing)
    	{
    		if (_disposed) return;
            if (disposing)
            {
	            try
	            {
		            Delete();
	            }
	            catch
	            {
		            // ignored
	            }
            }
            _disposed = true;
    	}

    	public void Dispose()
    	{
    		Dispose(true);
    		GC.SuppressFinalize(this);
    	}

        #endregion IDisposable Support
    }
}
