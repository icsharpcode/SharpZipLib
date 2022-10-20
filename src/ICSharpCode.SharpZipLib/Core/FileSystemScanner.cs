using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ICSharpCode.SharpZipLib.Core
{
	#region EventArgs

	/// <summary>
	/// Event arguments for scanning.
	/// </summary>
	public class ScanEventArgs : EventArgs
	{
		/// <summary>
		/// Initialise a new instance of <see cref="ScanEventArgs"/>
		/// </summary>
		/// <param name="name">The file or directory name.</param>
		public ScanEventArgs(string name)
		{
			Name = name;
		}

		/// <summary>
		/// The file or directory name for this event.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Get set a value indicating if scanning should continue or not.
		/// </summary>
		public bool ContinueRunning { get; set; } = true;
	}

	/// <summary>
	/// Event arguments during processing of a single file or directory.
	/// </summary>
	public class ProgressEventArgs : EventArgs
	{
		/// <summary>
		/// Initialise a new instance of <see cref="ScanEventArgs"/>
		/// </summary>
		/// <param name="name">The file or directory name if known.</param>
		/// <param name="processed">The number of bytes processed so far</param>
		/// <param name="target">The total number of bytes to process, 0 if not known</param>
		public ProgressEventArgs(string name, long processed, long target)
		{
			Name = name;
			Processed = processed;
			Target = target;
		}

		/// <summary>
		/// The name for this event if known.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Get set a value indicating whether scanning should continue or not.
		/// </summary>
		public bool ContinueRunning { get; set; } = true;

		/// <summary>
		/// Get a percentage representing how much of the <see cref="Target"></see> has been processed
		/// </summary>
		/// <value>0.0 to 100.0 percent; 0 if target is not known.</value>
		public float PercentComplete => Target <= 0 ? 0 : ((float)Processed / (float)Target) * 100.0f;

		/// <summary>
		/// The number of bytes processed so far
		/// </summary>
		public long Processed { get; }

		/// <summary>
		/// The number of bytes to process.
		/// </summary>
		/// <remarks>Target may be 0 or negative if the value isnt known.</remarks>
		public long Target { get; }
	}

	/// <summary>
	/// Event arguments for directories.
	/// </summary>
	public class DirectoryEventArgs : ScanEventArgs
	{
		/// <summary>
		/// Initialize an instance of <see cref="DirectoryEventArgs"></see>.
		/// </summary>
		/// <param name="name">The name for this directory.</param>
		/// <param name="hasMatchingFiles">Flag value indicating if any matching files are contained in this directory.</param>
		public DirectoryEventArgs(string name, bool hasMatchingFiles)
			: base(name)
		{
			HasMatchingFiles = hasMatchingFiles;
		}

		/// <summary>
		/// Get a value indicating if the directory contains any matching files or not.
		/// </summary>
		public bool HasMatchingFiles { get; }
	}

	/// <summary>
	/// Arguments passed when scan failures are detected.
	/// </summary>
	public class ScanFailureEventArgs : EventArgs
	{
		#region Constructors

		/// <summary>
		/// Initialise a new instance of <see cref="ScanFailureEventArgs"></see>
		/// </summary>
		/// <param name="name">The name to apply.</param>
		/// <param name="e">The exception to use.</param>
		public ScanFailureEventArgs(string name, Exception e)
		{
			Name = name;
			Exception = e;
		}

		#endregion Constructors

		/// <summary>
		/// The applicable name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// The applicable exception.
		/// </summary>
		public Exception Exception { get; }

		/// <summary>
		/// Get / set a value indicating whether scanning should continue.
		/// </summary>
		public bool ContinueRunning { get; set; } = true;
	}

	#endregion EventArgs

	#region Delegates

	/// <summary>
	/// Delegate invoked before starting to process a file.
	/// </summary>
	/// <param name="sender">The source of the event</param>
	/// <param name="e">The event arguments.</param>
	public delegate void ProcessFileHandler(object sender, ScanEventArgs e);

	/// <summary>
	/// Delegate invoked during processing of a file or directory
	/// </summary>
	/// <param name="sender">The source of the event</param>
	/// <param name="e">The event arguments.</param>
	public delegate void ProgressHandler(object sender, ProgressEventArgs e);

	/// <summary>
	/// Delegate invoked when a file has been completely processed.
	/// </summary>
	/// <param name="sender">The source of the event</param>
	/// <param name="e">The event arguments.</param>
	public delegate void CompletedFileHandler(object sender, ScanEventArgs e);

	/// <summary>
	/// Delegate invoked when a directory failure is detected.
	/// </summary>
	/// <param name="sender">The source of the event</param>
	/// <param name="e">The event arguments.</param>
	public delegate void DirectoryFailureHandler(object sender, ScanFailureEventArgs e);

	/// <summary>
	/// Delegate invoked when a file failure is detected.
	/// </summary>
	/// <param name="sender">The source of the event</param>
	/// <param name="e">The event arguments.</param>
	public delegate void FileFailureHandler(object sender, ScanFailureEventArgs e);

	#endregion Delegates

	/// <summary>
	/// FileSystemScanner provides facilities scanning of files and directories.
	/// </summary>
	public class FileSystemScanner
	{
		#region Constructors

		/// <summary>
		/// Initialise a new instance of <see cref="FileSystemScanner"></see>
		/// </summary>
		/// <param name="filter">The <see cref="PathFilter">file filter</see> to apply when scanning.</param>
		public FileSystemScanner(string filter)
		{
			fileFilter_ = new PathFilter(filter);
		}

		/// <summary>
		/// Initialise a new instance of <see cref="FileSystemScanner"></see>
		/// </summary>
		/// <param name="fileFilter">The <see cref="PathFilter">file filter</see> to apply.</param>
		/// <param name="directoryFilter">The <see cref="PathFilter"> directory filter</see> to apply.</param>
		public FileSystemScanner(string fileFilter, string directoryFilter)
		{
			fileFilter_ = new PathFilter(fileFilter);
			directoryFilter_ = new PathFilter(directoryFilter);
		}

		/// <summary>
		/// Initialise a new instance of <see cref="FileSystemScanner"></see>
		/// </summary>
		/// <param name="fileFilter">The file <see cref="IScanFilter">filter</see> to apply.</param>
		public FileSystemScanner(IScanFilter fileFilter)
		{
			fileFilter_ = fileFilter;
		}

		/// <summary>
		/// Initialise a new instance of <see cref="FileSystemScanner"></see>
		/// </summary>
		/// <param name="fileFilter">The file <see cref="IScanFilter">filter</see>  to apply.</param>
		/// <param name="directoryFilter">The directory <see cref="IScanFilter">filter</see>  to apply.</param>
		public FileSystemScanner(IScanFilter fileFilter, IScanFilter directoryFilter)
		{
			fileFilter_ = fileFilter;
			directoryFilter_ = directoryFilter;
		}

		#endregion Constructors

		#region Delegates

		/// <summary>
		/// Delegate to invoke when a directory is processed.
		/// </summary>
		public event EventHandler<DirectoryEventArgs> ProcessDirectory;

		/// <summary>
		/// Delegate to invoke when a file is processed.
		/// </summary>
		public ProcessFileHandler ProcessFile;

		/// <summary>
		/// Delegate to invoke when processing for a file has finished.
		/// </summary>
		public CompletedFileHandler CompletedFile;

		/// <summary>
		/// Delegate to invoke when a directory failure is detected.
		/// </summary>
		public DirectoryFailureHandler DirectoryFailure;

		/// <summary>
		/// Delegate to invoke when a file failure is detected.
		/// </summary>
		public FileFailureHandler FileFailure;

		#endregion Delegates

		/// <summary>
		/// Raise the DirectoryFailure event.
		/// </summary>
		/// <param name="directory">The directory name.</param>
		/// <param name="e">The exception detected.</param>
		private bool OnDirectoryFailure(string directory, Exception e)
		{
			if (DirectoryFailure == null) return false;
			
			var args = new ScanFailureEventArgs(directory, e);
			DirectoryFailure(this, args);
			alive_ = args.ContinueRunning;
			return true;
		}

		/// <summary>
		/// Raise the FileFailure event.
		/// </summary>
		/// <param name="file">The file name.</param>
		/// <param name="e">The exception detected.</param>
		private bool OnFileFailure(string file, Exception e)
		{
			if (FileFailure == null) return false;
			
			var args = new ScanFailureEventArgs(file, e);
			FileFailure(this, args);
			alive_ = args.ContinueRunning;
			return true;
		}

		/// <summary>
		/// Raise the ProcessFile event.
		/// </summary>
		/// <param name="file">The file name.</param>
		private void OnProcessFile(string file)
		{
			if (ProcessFile == null) return;
			
			var args = new ScanEventArgs(file);
			ProcessFile(this, args);
			alive_ = args.ContinueRunning;
		}

		/// <summary>
		/// Raise the complete file event
		/// </summary>
		/// <param name="file">The file name</param>
		private void OnCompleteFile(string file)
		{
			if (CompletedFile == null) return;
			var args = new ScanEventArgs(file);
			CompletedFile(this, args);
			alive_ = args.ContinueRunning;
		}

		/// <summary>
		/// Raise the ProcessDirectory event.
		/// </summary>
		/// <param name="directory">The directory name.</param>
		/// <param name="hasMatchingFiles">Flag indicating if the directory has matching files.</param>
		private void OnProcessDirectory(string directory, bool hasMatchingFiles)
		{
			EventHandler<DirectoryEventArgs> handler = ProcessDirectory;

			if (handler != null)
			{
				var args = new DirectoryEventArgs(directory, hasMatchingFiles);
				handler(this, args);
				alive_ = args.ContinueRunning;
			}
		}

		/// <summary>
		/// Scan a directory.
		/// </summary>
		/// <param name="directory">The base directory to scan.</param>
		/// <param name="recurse">True to recurse subdirectories, false to scan a single directory.</param>
		public void Scan(string directory, bool recurse)
		{
			alive_ = true;
			ScanDir(directory, recurse);
		}

		/// <summary>
		/// The method used to enumerate files in the supplied <paramref name="directory"/>
		/// </summary>
		/// <param name="directory"></param>
		protected virtual IEnumerable<string> GetFiles(string directory) => Directory.EnumerateFiles(directory);
		
		/// <summary>
		/// The method used to enumerate directories in the supplied <paramref name="directory"/>
		/// </summary>
		/// <param name="directory"></param>
		protected virtual IEnumerable<string> GetDirectories(string directory) => Directory.EnumerateDirectories(directory);

		private void ScanDir(string directory, bool recurse)
		{
			try
			{
				var names = GetFiles(directory).Where(fileFilter_.IsMatch).ToArray();
				var hasMatch = names.Any();
				
				OnProcessDirectory(directory, hasMatch);

				if (alive_ && hasMatch)
				{
					foreach (var fileName in names)
					{
						try
						{
							OnProcessFile(fileName);
							if (!alive_)
							{
								break;
							}
						}
						catch (Exception e)
						{
							if (!OnFileFailure(fileName, e))
							{
								throw;
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				if (!OnDirectoryFailure(directory, e))
				{
					throw;
				}
			}

			if (!alive_ || !recurse) return;
			
			try
			{
				var subDirectories = GetDirectories(directory)
					.Where(d => d != null && directoryFilter_.IsMatch(d));
				foreach (var fulldir in subDirectories)
				{
					ScanDir(fulldir, recurse: true);
					if (!alive_)
					{
						break;
					}
				}
			}
			catch (Exception e)
			{
				if (!OnDirectoryFailure(directory, e))
				{
					throw;
				}
			}
			
		}

		#region Instance Fields

		/// <summary>
		/// The file filter currently in use.
		/// </summary>
		private IScanFilter fileFilter_;

		/// <summary>
		/// The directory filter currently in use.
		/// </summary>
		private IScanFilter directoryFilter_;

		/// <summary>
		/// Flag indicating if scanning should continue running.
		/// </summary>
		private bool alive_;

		#endregion Instance Fields
	}
}
