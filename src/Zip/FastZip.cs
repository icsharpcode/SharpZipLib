// FastZip.cs
//
// Copyright 2005 John Reilly
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// Linking this library statically or dynamically with other modules is
// making a combined work based on this library.  Thus, the terms and
// conditions of the GNU General Public License cover the whole
// combination.
// 
// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module.  An independent module is a module which is not derived from
// or based on this library.  If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so.  If you do not wish to do so, delete this
// exception statement from your version.


using System;
using System.IO;
using ICSharpCode.SharpZipLib.Core;

namespace ICSharpCode.SharpZipLib.Zip
{
	/// <summary>
	/// FastZipEvents supports all events applicable to <see cref="FastZip">FastZip</see> operations.
	/// </summary>
	public class FastZipEvents
	{
		/// <summary>
		/// Delegate to invoke when processing directories.
		/// </summary>
		public ProcessDirectoryDelegate ProcessDirectory;
		
		/// <summary>
		/// Delegate to invoke when processing files.
		/// </summary>
		public ProcessFileDelegate ProcessFile;

		/// <summary>
		/// Delegate to invoke when processing directory failures.
		/// </summary>
		public DirectoryFailureDelegate DirectoryFailure;
		
		/// <summary>
		/// Delegate to invoke when processing file failures.
		/// </summary>
		public FileFailureDelegate FileFailure;
		
		/// <summary>
		/// Raise the directory failure event.
		/// </summary>
		/// <param name="directory">The directory.</param>
		/// <param name="e">The exception for this event.</param>
		public void OnDirectoryFailure(string directory, Exception e)
		{
			if ( DirectoryFailure != null ) {
				ScanFailureEventArgs args = new ScanFailureEventArgs(directory, e);
				DirectoryFailure(this, args);
			}
		}
		
		/// <summary>
		/// Raises the file failure event.
		/// </summary>
		/// <param name="file">The file for this event.</param>
		/// <param name="e">The exception for this event.</param>
		public void OnFileFailure(string file, Exception e)
		{
			if ( FileFailure != null ) {
				ScanFailureEventArgs args = new ScanFailureEventArgs(file, e);
				FileFailure(this, args);
			}
		}
		
		/// <summary>
		/// Raises the ProcessFileEvent.
		/// </summary>
		/// <param name="file">The file for this event.</param>
		/// <returns>A boolean indicating if execution should continue or not.</returns>
		public bool OnProcessFile(string file)
		{
			bool result = true;
			if ( ProcessFile != null ) {
				ScanEventArgs args = new ScanEventArgs(file);
				ProcessFile(this, args);
				result = args.ContinueRunning;
			}
			return result;
		}
		
		/// <summary>
		/// Raises the ProcessDirectoryEvent.
		/// </summary>
		/// <param name="directory">The directory for this event.</param>
		/// <param name="hasMatchingFiles">Flag indicating if directory has matching files as determined by the current filter.</param>
		public void OnProcessDirectory(string directory, bool hasMatchingFiles)
		{
			if ( ProcessDirectory != null ) {
				DirectoryEventArgs args = new DirectoryEventArgs(directory, hasMatchingFiles);
				ProcessDirectory(this, args);
			}
		}
	}
	
	/// <summary>
	/// FastZip provides facilities for creating and extracting zip files.
	/// Only relative paths are supported.
	/// </summary>
	public class FastZip
	{
		#region Constructors
		/// <summary>
		/// Initialise a default instance of <see cref="FastZip"/>.
		/// </summary>
		public FastZip()
		{
		}

		/// <summary>
		/// Initialise a new instance of <see cref="FastZip"/>
		/// </summary>
		/// <param name="events">The <see cref="FastZipEvents">events</see> to use during operations.</param>
		public FastZip(FastZipEvents events)
		{
			events_ = events;
		}
		#endregion
		
		/// <summary>
		/// Defines the desired handling when overwriting files.
		/// </summary>
		public enum Overwrite {
			/// <summary>
			/// Prompt the user to confirm overwriting
			/// </summary>
			Prompt,
			/// <summary>
			/// Never overwrite files.
			/// </summary>
			Never,
			/// <summary>
			/// Always overwrite files.
			/// </summary>
			Always
		}

		/// <summary>
		/// Get/set a value indicating wether empty directories should be created.
		/// </summary>
		public bool CreateEmptyDirectories
		{
			get { return createEmptyDirectories_; }
			set { createEmptyDirectories_ = value; }
		}

		/// <summary>
		/// Get / set the password value.
		/// </summary>
		public string Password
		{
			get { return password_; }
			set { password_ = value; }
		}

		/// <summary>
		/// Delegate called when confirming overwriting of files.
		/// </summary>
		public delegate bool ConfirmOverwriteDelegate(string fileName);
		
		#region CreateZip
		/// <summary>
		/// Create a zip file.
		/// </summary>
		/// <param name="zipFileName">The name of the zip file to create.</param>
		/// <param name="sourceDirectory">The directory to source files from.</param>
		/// <param name="recurse">True to recurse directories, false for no recursion.</param>
		/// <param name="fileFilter">The <see cref="PathFilter">file filter</see> to apply.</param>
		/// <param name="directoryFilter">The <see cref="PathFilter">directory filter</see> to apply.</param>
		public void CreateZip(string zipFileName, string sourceDirectory, bool recurse, string fileFilter, string directoryFilter)
		{
			NameTransform = new ZipNameTransform(true, sourceDirectory);
			sourceDirectory_ = sourceDirectory;
			
			using (outputStream_ = new ZipOutputStream(File.Create(zipFileName))) {
				FileSystemScanner scanner = new FileSystemScanner(fileFilter, directoryFilter);
				scanner.ProcessFile += new ProcessFileDelegate(ProcessFile);
				if ( CreateEmptyDirectories ) {
					scanner.ProcessDirectory += new ProcessDirectoryDelegate(ProcessDirectory);
				}
				scanner.Scan(sourceDirectory, recurse);
			}
		}
		
		/// <summary>
		/// Create a zip archive sending output to the <paramref name="outputStream"/> passed.
		/// </summary>
		/// <param name="outputStream">The stream to write archive data to.</param>
		/// <param name="sourceDirectory">The directory to source files from.</param>
		/// <param name="recurse">True to recurse directories, false for no recursion.</param>
		/// <param name="fileFilter">The <see cref="PathFilter">file filter</see> to apply.</param>
		/// <param name="directoryFilter">The <see cref="PathFilter">directory filter</see> to apply.</param>
		public void CreateZip(Stream outputStream, string sourceDirectory, bool recurse, string fileFilter, string directoryFilter)
		{
			NameTransform = new ZipNameTransform(true, sourceDirectory);
			sourceDirectory_ = sourceDirectory;

			using ( outputStream_ = new ZipOutputStream(outputStream) )
			{
				FileSystemScanner scanner = new FileSystemScanner(fileFilter, directoryFilter);
				scanner.ProcessFile += new ProcessFileDelegate(ProcessFile);
				if ( this.CreateEmptyDirectories )
				{
					scanner.ProcessDirectory += new ProcessDirectoryDelegate(ProcessDirectory);
				}
				scanner.Scan(sourceDirectory, recurse);
			}
		}


		/// <summary>
		/// Create a zip file/archive.
		/// </summary>
		/// <param name="zipFileName">The name of the zip file to create.</param>
		/// <param name="sourceDirectory">The directory to obtain files and directories from.</param>
		/// <param name="recurse">True to recurse directories, false for no recursion.</param>
		/// <param name="fileFilter">The file filter to apply.</param>
		public void CreateZip(string zipFileName, string sourceDirectory, bool recurse, string fileFilter)
		{
			CreateZip(zipFileName, sourceDirectory, recurse, fileFilter, null);
		}
		#endregion
		
		#region ExtractZip
		/// <summary>
		/// Extract the contents of a zip file.
		/// </summary>
		/// <param name="zipFileName">The zip file to extract from.</param>
		/// <param name="targetDirectory">The directory to save extracted information in.</param>
		/// <param name="fileFilter">A filter to apply to files.</param>
		public void ExtractZip(string zipFileName, string targetDirectory, string fileFilter) 
		{
			ExtractZip(zipFileName, targetDirectory, Overwrite.Always, null, fileFilter, null, restoreDateTimeOnExtract_);
		}
		
		/// <summary>
		/// Extract the contents of a zip file.
		/// </summary>
		/// <param name="zipFileName">The zip file to extract from.</param>
		/// <param name="targetDirectory">The directory to save extracted information in.</param>
		/// <param name="overwrite">The style of <see cref="Overwrite">overwriting</see> to apply.</param>
		/// <param name="confirmDelegate">A delegate to invoke when confirming overwriting.</param>
		/// <param name="fileFilter">A filter to apply to files.</param>
		/// <param name="directoryFilter">A filter to apply to directories.</param>
		/// <param name="restoreDateTime">Flag indicating wether to restore the date and time for extracted files.</param>
		public void ExtractZip(string zipFileName, string targetDirectory, 
		                       Overwrite overwrite, ConfirmOverwriteDelegate confirmDelegate, 
		                       string fileFilter, string directoryFilter, bool restoreDateTime)
		{
			if ( (overwrite == Overwrite.Prompt) && (confirmDelegate == null) ) {
				throw new ArgumentNullException("confirmDelegate");
			}

			continueRunning_ = true;
			overwrite_ = overwrite;
			confirmDelegate_ = confirmDelegate;
			targetDirectory_ = targetDirectory;
			fileFilter_ = new NameFilter(fileFilter);
			directoryFilter_ = new NameFilter(directoryFilter);
			restoreDateTimeOnExtract_ = restoreDateTime;
			
			using ( inputStream_ = new ZipInputStream(File.OpenRead(zipFileName)) ) {
				if (password_ != null) {
					inputStream_.Password = password_;
				}

				ZipEntry entry;
				while ( continueRunning_ && (entry = inputStream_.GetNextEntry()) != null ) {
					if ( directoryFilter_.IsMatch(Path.GetDirectoryName(entry.Name)) && fileFilter_.IsMatch(entry.Name) ) {
						ExtractEntry(entry);
					}
				}
			}
		}
		#endregion
		
		#region Processing
		void ProcessDirectory(object sender, DirectoryEventArgs e)
		{
			if ( !e.HasMatchingFiles && CreateEmptyDirectories ) {
				if ( events_ != null ) {
					events_.OnProcessDirectory(e.Name, e.HasMatchingFiles);
				}
				
				if (e.Name != sourceDirectory_) {
					string cleanedName = nameTransform_.TransformDirectory(e.Name);
					ZipEntry entry = new ZipEntry(cleanedName);
					outputStream_.PutNextEntry(entry);
				}
			}
		}
		
		static int MakeExternalAttributes(FileInfo info)
		{
			return (int)info.Attributes;
		}

		void UpdateEntry(ZipEntry entry, FileInfo info)
		{
//TODO: Setting attributes and HostSystem like this may be incorrect and its not tested.
			entry.DateTime = info.LastWriteTime;
			entry.ExternalFileAttributes = MakeExternalAttributes(info);

			if ( (Environment.OSVersion.Platform == System.PlatformID.Win32S) ||
				(Environment.OSVersion.Platform == System.PlatformID.Win32Windows)  ||
				(Environment.OSVersion.Platform == System.PlatformID.WinCE)
				)
			{
				entry.HostSystem = (int)HostSystemID.Msdos;
			}
			else if (
				Environment.OSVersion.Platform == System.PlatformID.Win32NT
				)
			{
				entry.HostSystem = (int)HostSystemID.WindowsNT;
				// TODO: Add extra data to include NTFS information.
			}
			else {
				// TODO: Mono support for HostSystem/External file attributes
				// entry.HostSystem = (int)ZipEntry.HostSystemID.Unix;
			}
		}

		void ProcessFile(object sender, ScanEventArgs e)
		{
			if ( (events_ != null) && (events_.ProcessFile != null) ) {
				events_.ProcessFile(sender, e);
			}
			
			if ( e.ContinueRunning ) {
				string cleanedName = nameTransform_.TransformFile(e.Name);
				ZipEntry entry = new ZipEntry(cleanedName);

				FileInfo info = new FileInfo(e.Name);
				UpdateEntry(entry, info);

				outputStream_.PutNextEntry(entry);
				AddFileContents(e.Name);
			}
		}

		void AddFileContents(string name)
		{
			if ( buffer_ == null ) {
				buffer_ = new byte[4096];
			}

			using (FileStream stream = File.OpenRead(name)) {
				int length;
				do {
					length = stream.Read(buffer_, 0, buffer_.Length);
					outputStream_.Write(buffer_, 0, length);
				} while ( length > 0 );
			}
		}
		
		void ExtractFileEntry(ZipEntry entry, string targetName)
		{
			bool proceed = true;
			if ( overwrite_ != Overwrite.Always ) {
				if ( File.Exists(targetName) ) {
					if ( (overwrite_ == Overwrite.Prompt) && (confirmDelegate_ != null) ) {
						proceed = confirmDelegate_(targetName);
					}
					else {
						proceed = false;
					}
				}
			}
			
			if ( proceed ) {
				if ( events_ != null ) {
					continueRunning_ = events_.OnProcessFile(entry.Name);
				}
			
				if ( continueRunning_ ) {
					using ( FileStream streamWriter = File.Create(targetName) ) {
						if ( buffer_ == null ) {
							buffer_ = new byte[4096];
						}
						
						int size;
			
						do {
							size = inputStream_.Read(buffer_, 0, buffer_.Length);
							streamWriter.Write(buffer_, 0, size);
						} while (size > 0);
					}
		
					if ( restoreDateTimeOnExtract_ ) {
						File.SetLastWriteTime(targetName, entry.DateTime);
					}
				}
			}
		}

		static bool NameIsValid(string name)
		{
			return (name != null) &&
				(name.Length > 0) &&
				(name.IndexOfAny(Path.InvalidPathChars) < 0);
		}
		
		void ExtractEntry(ZipEntry entry)
		{
			bool doExtraction = NameIsValid(entry.Name) && entry.IsCompressionMethodSupported();
			
			string dirName = null;
			string targetName = null;
			
			if ( doExtraction ) {
				string entryFileName;
				if (Path.IsPathRooted(entry.Name)) {
					string workName = Path.GetPathRoot(entry.Name);
					workName = entry.Name.Substring(workName.Length);
					entryFileName = Path.Combine(Path.GetDirectoryName(workName), Path.GetFileName(entry.Name));
				} else {
					entryFileName = entry.Name;
				}
				
				targetName = Path.Combine(targetDirectory_, entryFileName);
				dirName = Path.GetDirectoryName(Path.GetFullPath(targetName));
	
				doExtraction = (entryFileName.Length > 0);
			}
			
			if ( doExtraction && !Directory.Exists(dirName) )
			{
				if ( !entry.IsDirectory || CreateEmptyDirectories ) {
					try {
						Directory.CreateDirectory(dirName);
					}
					catch {
						doExtraction = false;
					}
				}
			}
			
			if ( doExtraction && entry.IsFile ) {
				ExtractFileEntry(entry, targetName);
			}
		}
		#endregion
		
		/// <summary>
		/// Get or set the <see cref="ZipNameTransform"> active when creating Zip files.</see>
		/// </summary>
		public ZipNameTransform NameTransform
		{
			get { return nameTransform_; }
			set {
				if ( value == null ) {
					nameTransform_ = new ZipNameTransform();
				}
				else {
					nameTransform_ = value;
				}
			}
		}

		/// <summary>
		/// Get/set a value indicating wether file dates and times should 
		/// be restored when extracting files from an archive.
		/// </summary>
		/// <remarks>The default value is false.</remarks>
		public bool RestoreDateTimeOnExtract
		{
			get {
				return restoreDateTimeOnExtract_;
			}
			set {
				restoreDateTimeOnExtract_ = value;
			}
		}
		
		#region Instance Fields
		bool continueRunning_;
		byte[] buffer_;
		ZipOutputStream outputStream_;
		ZipInputStream inputStream_;
		string password_;
		string targetDirectory_;
		string sourceDirectory_;
		NameFilter fileFilter_;
		NameFilter directoryFilter_;
		Overwrite overwrite_;
		ConfirmOverwriteDelegate confirmDelegate_;
		bool restoreDateTimeOnExtract_;
		bool createEmptyDirectories_;
		FastZipEvents events_;
		ZipNameTransform nameTransform_;
		#endregion
	}
}
