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
		/// Raise the <see cref="DirectoryFailure">directory failure</see> event.
		/// </summary>
		/// <param name="directory">The directory causing the failure.</param>
		/// <param name="e">The exception for this event.</param>
		/// <returns>A boolean indicating if execution should continue or not.</returns>
		public bool OnDirectoryFailure(string directory, Exception e)
		{
			bool result = false;
			if ( DirectoryFailure != null ) {
				ScanFailureEventArgs args = new ScanFailureEventArgs(directory, e);
				DirectoryFailure(this, args);
				result = args.ContinueRunning;
			}
			return result;
		}
		
		/// <summary>
		/// Raises the <see cref="FileFailure">file failure delegate</see>.
		/// </summary>
		/// <param name="file">The file causing the failure.</param>
		/// <param name="e">The exception for this failure.</param>
		/// <returns>A boolean indicating if execution should continue or not.</returns>
		public bool OnFileFailure(string file, Exception e)
		{
			bool result = false;
			if ( FileFailure != null ) {
				ScanFailureEventArgs args = new ScanFailureEventArgs(file, e);
				FileFailure(this, args);
				result = args.ContinueRunning;
			}
			return result;
		}
		
		/// <summary>
		/// Raises the <see cref="ProcessFile">Process File delegate</see>.
		/// </summary>
		/// <param name="file">The file being processed.</param>
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
		/// Fires the <see cref="ProcessDirectory">process directory</see> delegate.
		/// </summary>
		/// <param name="directory">The directory being processed.</param>
		/// <param name="hasMatchingFiles">Flag indicating if directory has matching files as determined by the current filter.</param>
		public bool OnProcessDirectory(string directory, bool hasMatchingFiles)
		{
			bool result = true;
			if ( ProcessDirectory != null ) {
				DirectoryEventArgs args = new DirectoryEventArgs(directory, hasMatchingFiles);
				ProcessDirectory(this, args);
				result = args.ContinueRunning;
			}
			return result;
		}
	}
	
	/// <summary>
	/// FastZip provides facilities for creating and extracting zip files.
	/// </summary>
	public class FastZip
	{
		#region Enumerations
		/// <summary>
		/// Defines the desired handling when overwriting files during extraction.
		/// </summary>
		public enum Overwrite 
		{
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
		#endregion
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
		#region Properties
		/// <summary>
		/// Get/set a value indicating wether empty directories should be created.
		/// </summary>
		public bool CreateEmptyDirectories
		{
			get { return createEmptyDirectories_; }
			set { createEmptyDirectories_ = value; }
		}

#if !NETCF_1_0
		/// <summary>
		/// Get / set the password value.
		/// </summary>
		public string Password
		{
			get { return password_; }
			set { password_ = value; }
		}
#endif

		/// <summary>
		/// Get or set the <see cref="INameTransform"></see> active when creating Zip files.
		/// </summary>
		/// <seealso cref="EntryFactory"></seealso>
		public INameTransform NameTransform
		{
			get { return entryFactory_.NameTransform; }
			set {
				entryFactory_.NameTransform = value;
			}
		}

		/// <summary>
		/// Get or set the <see cref="IEntryFactory"></see> active when creating Zip files.
		/// </summary>
		public IEntryFactory EntryFactory
		{
			get { return entryFactory_; }
			set {
				if ( value == null ) {
					entryFactory_ = new ZipEntryFactory();
				}
				else {
					entryFactory_ = value;
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
		
		/// <summary>
		/// Get/set a value indicating wether file attributes should
		/// be restored during extract operations
		/// </summary>
		public bool RestoreAttributesOnExtract
		{
			get { return restoreAttributesOnExtract_; }
			set { restoreAttributesOnExtract_ = value; }
		}
		#endregion
		#region Delegates
		/// <summary>
		/// Delegate called when confirming overwriting of files.
		/// </summary>
		public delegate bool ConfirmOverwriteDelegate(string fileName);
		#endregion
		#region CreateZip
		/// <summary>
		/// Create a zip file.
		/// </summary>
		/// <param name="zipFileName">The name of the zip file to create.</param>
		/// <param name="sourceDirectory">The directory to source files from.</param>
		/// <param name="recurse">True to recurse directories, false for no recursion.</param>
		/// <param name="fileFilter">The <see cref="PathFilter">file filter</see> to apply.</param>
		/// <param name="directoryFilter">The <see cref="PathFilter">directory filter</see> to apply.</param>
		public void CreateZip(string zipFileName, string sourceDirectory, 
			bool recurse, string fileFilter, string directoryFilter)
		{
			CreateZip(File.Create(zipFileName), sourceDirectory, recurse, fileFilter, directoryFilter);
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
			CreateZip(File.Create(zipFileName), sourceDirectory, recurse, fileFilter, null);
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
			NameTransform = new ZipNameTransform(sourceDirectory);
			sourceDirectory_ = sourceDirectory;

			using ( outputStream_ = new ZipOutputStream(outputStream) ) {

#if !NETCF_1_0
				if ( password_ != null ) {
					outputStream_.Password = password_;
				}
#endif

				FileSystemScanner scanner = new FileSystemScanner(fileFilter, directoryFilter);
				scanner.ProcessFile += new ProcessFileDelegate(ProcessFile);
				if ( this.CreateEmptyDirectories ) {
					scanner.ProcessDirectory += new ProcessDirectoryDelegate(ProcessDirectory);
				}
				
				if (events_ != null) {
					if ( events_.FileFailure != null ) {
						scanner.FileFailure += events_.FileFailure;
					}

					if ( events_.DirectoryFailure != null ) {
						scanner.DirectoryFailure += events_.DirectoryFailure;
					}
				}

				scanner.Scan(sourceDirectory, recurse);
			}
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
			
			using ( zipFile_ = new ZipFile(zipFileName) ) {

#if !NETCF_1_0
				if (password_ != null) {
					zipFile_.Password = password_;
				}
#endif

				System.Collections.IEnumerator enumerator = zipFile_.GetEnumerator();
				while ( continueRunning_ && enumerator.MoveNext()) {
					ZipEntry entry = (ZipEntry) enumerator.Current;
					if ( entry.IsFile )
					{
						if ( directoryFilter_.IsMatch(Path.GetDirectoryName(entry.Name)) && fileFilter_.IsMatch(entry.Name) ) {
							ExtractEntry(entry);
						}
					}
					else if ( entry.IsDirectory ) {
						if ( directoryFilter_.IsMatch(entry.Name) && CreateEmptyDirectories ) {
							ExtractEntry(entry);
						}
					}
					else {
						// Do nothing for volume labels etc...
					}
				}
			}
		}
		#endregion
		#region Internal Processing
		void ProcessDirectory(object sender, DirectoryEventArgs e)
		{
			if ( !e.HasMatchingFiles && CreateEmptyDirectories ) {
				if ( events_ != null ) {
					events_.OnProcessDirectory(e.Name, e.HasMatchingFiles);
				}
				
				if ( e.ContinueRunning ) {
					if (e.Name != sourceDirectory_) {
						ZipEntry entry = entryFactory_.MakeDirectoryEntry(e.Name);
						outputStream_.PutNextEntry(entry);
					}
				}
			}
		}
		
		void ProcessFile(object sender, ScanEventArgs e)
		{
			if ( (events_ != null) && (events_.ProcessFile != null) ) {
				events_.ProcessFile(sender, e);
			}
			
			if ( e.ContinueRunning ) {
				ZipEntry entry = entryFactory_.MakeFileEntry(e.Name);
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
				StreamUtils.Copy(stream, outputStream_, buffer_);
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
					try {
						using ( FileStream outputStream = File.Create(targetName) ) {
							if ( buffer_ == null ) {
								buffer_ = new byte[4096];
							}
							StreamUtils.Copy(zipFile_.GetInputStream(entry), outputStream, buffer_);
						}

#if !NETCF_1_0 && !NETCF_2_0
						if ( restoreDateTimeOnExtract_ ) {
							File.SetLastWriteTime(targetName, entry.DateTime);
						}
						
						if ( RestoreAttributesOnExtract && entry.IsDOSEntry && (entry.ExternalFileAttributes != -1)) {
							FileAttributes fileAttributes = (FileAttributes) entry.ExternalFileAttributes;
							// TODO: FastZip - Setting of other file attributes on extraction is a little trickier.
							fileAttributes &= (FileAttributes.Archive | FileAttributes.Normal | FileAttributes.ReadOnly | FileAttributes.Hidden);
							File.SetAttributes(targetName, fileAttributes);
						}
#endif						
					}
					catch(Exception ex) {
						if ( events_ != null ) {
							continueRunning_ = events_.OnFileFailure(targetName, ex);
						}
						else {
							continueRunning_ = false;
						}
					}
				}
			}
		}

		void ExtractEntry(ZipEntry entry)
		{
			bool doExtraction = false;
			
			string nameText = entry.Name;
			
			if ( entry.IsFile ) {
				doExtraction = NameIsValid(nameText) && entry.IsCompressionMethodSupported();
			}
			else if ( entry.IsDirectory ) {
				doExtraction = NameIsValid(nameText);
			}
			
			// TODO: Fire delegate were compression method not supported, or name is invalid.

			string dirName = null;
			string targetName = null;
			
			if ( doExtraction ) {
				// Handle invalid entry names by chopping of path root.
				if (Path.IsPathRooted(nameText)) {
					string workName = Path.GetPathRoot(nameText);
					nameText = nameText.Substring(workName.Length);
				}
				
				if ( nameText.Length > 0 ) {
					targetName = Path.Combine(targetDirectory_, nameText);
					if ( entry.IsDirectory ) {
						dirName = targetName;
					}
					else {
						dirName = Path.GetDirectoryName(Path.GetFullPath(targetName));
					}
				}
				else {
					doExtraction = false;
				}
			}
			
			if ( doExtraction && !Directory.Exists(dirName) ) {
				if ( !entry.IsDirectory || CreateEmptyDirectories ) {
					try {
						Directory.CreateDirectory(dirName);
					}
					catch (Exception ex) {
						doExtraction = false;
						if ( events_ != null ) {
							if ( entry.IsDirectory ) {
								continueRunning_ = events_.OnDirectoryFailure(targetName, ex);
							}
							else {
								continueRunning_ = events_.OnFileFailure(targetName, ex);
							}
						}
						else {
							continueRunning_ = false;
						}
					}
				}
			}
			
			if ( doExtraction && entry.IsFile ) {
				ExtractFileEntry(entry, targetName);
			}
		}

		static int MakeExternalAttributes(FileInfo info)
		{
			return (int)info.Attributes;
		}
		
#if NET_1_0 || NET_1_1 || NETCF_1_0
		static bool NameIsValid(string name)
		{
			return (name != null) &&
				(name.Length > 0) &&
				(name.IndexOfAny(Path.InvalidPathChars) < 0);
		}
#else
		static bool NameIsValid(string name)
		{
			return (name != null) &&
				(name.Length > 0) &&
				(name.IndexOfAny(Path.GetInvalidPathChars()) < 0);
		}
#endif
		#endregion
		#region Instance Fields
		bool continueRunning_;
		byte[] buffer_;
		ZipOutputStream outputStream_;
		ZipFile zipFile_;
		string targetDirectory_;
		string sourceDirectory_;
		NameFilter fileFilter_;
		NameFilter directoryFilter_;
		Overwrite overwrite_;
		ConfirmOverwriteDelegate confirmDelegate_;
		
		bool restoreDateTimeOnExtract_;
		bool restoreAttributesOnExtract_;
		bool createEmptyDirectories_;
		FastZipEvents events_;
		IEntryFactory entryFactory_ = new ZipEntryFactory();
		
#if !NETCF_1_0		
		string password_;
#endif	

		#endregion
	}
}
