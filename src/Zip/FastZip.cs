// SimpleZip.cs
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
	public class FastZipEvents
	{
		public ProcessDirectoryDelegate ProcessDirectory;
		public ProcessFileDelegate ProcessFile;

		public DirectoryFailureDelegate DirectoryFailure;
		public FileFailureDelegate FileFailure;
		
		public void OnDirectoryFailure(string directory, Exception e)
		{
			if ( DirectoryFailure != null ) {
				ScanFailureEventArgs args = new ScanFailureEventArgs(directory, e);
				DirectoryFailure(this, args);
			}
		}
		
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
		public void OnProcessFile(string file)
		{
			if ( ProcessFile != null ) {
				ScanEventArgs args = new ScanEventArgs(file);
				ProcessFile(this, args);
			}
		}
		
		/// <summary>
		/// Raises the ProcessDirectoryEvent.
		/// </summary>
		/// <param name="directory">The directory for this event.</param>
		/// <param name="isEmpty">Flag indicating if directory is empty as determined by the current filter.</param>
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
		public FastZip()
		{
			this.events = null;
		}
		
		public FastZip(FastZipEvents events)
		{
			this.events = events;
		}
		
		public enum Overwrite {
			Prompt,
			Never,
			Always
		}

		public bool CreateEmptyDirectories
		{
			get { return createEmptyDirectories; }
			set { createEmptyDirectories = value; }
		}
		
		public delegate bool ConfirmOverwriteDelegate(string fileName);
		
		public void CreateZip(string zipFileName, string sourceDirectory, bool recurse, string fileFilter, string directoryFilter)
		{
			NameTransform = new ZipNameTransform(true, sourceDirectory);
			this.sourceDirectory = sourceDirectory;
			
			outputStream = new ZipOutputStream(File.Create(zipFileName));
			try {
				FileSystemScanner scanner = new FileSystemScanner(fileFilter, directoryFilter);
				scanner.ProcessFile += new ProcessFileDelegate(ProcessFile);
				if ( this.CreateEmptyDirectories ) {
					scanner.ProcessDirectory += new ProcessDirectoryDelegate(ProcessDirectory);
				}
				scanner.Scan(sourceDirectory, recurse);
			}
			finally {
				outputStream.Close();
			}
		}
		
		public void CreateZip(string zipFileName, string sourceDirectory, bool recurse, string fileFilter)
		{
			CreateZip(zipFileName, sourceDirectory, recurse, fileFilter, null);
		}
		
		public void ExtractZip(string zipFileName, string targetDirectory, string fileFilter) 
		{
			ExtractZip(zipFileName, targetDirectory, Overwrite.Always, null, fileFilter, null);
		}
		
		public void ExtractZip(string zipFileName, string targetDirectory, 
		                       Overwrite overwrite, ConfirmOverwriteDelegate confirmDelegate, 
		                       string fileFilter, string directoryFilter)
		{
			if ((overwrite == Overwrite.Prompt) && (confirmDelegate == null)) {
				throw new ArgumentNullException("confirmDelegate");
			}
			this.overwrite = overwrite;
			this.confirmDelegate = confirmDelegate;
			this.targetDirectory = targetDirectory;
			this.fileFilter = new NameFilter(fileFilter);
			this.directoryFilter = new NameFilter(directoryFilter);
			
			inputStream = new ZipInputStream(File.OpenRead(zipFileName));
			
			try {
				
				if (password != null) {
					inputStream.Password = password;
				}

				ZipEntry entry;
				while ( (entry = inputStream.GetNextEntry()) != null ) {
					if ( this.directoryFilter.IsMatch(Path.GetDirectoryName(entry.Name)) && this.fileFilter.IsMatch(entry.Name) ) {
						ExtractEntry(entry);
					}
				}
			}
			finally {
				inputStream.Close();
			}
		}
		
		void ProcessDirectory(object sender, DirectoryEventArgs e)
		{
			if ( !e.HasMatchingFiles && createEmptyDirectories ) {
				if ( events != null ) {
					events.OnProcessDirectory(e.Name, e.HasMatchingFiles);
				}
				
				if (e.Name != sourceDirectory) {
					string cleanedName = nameTransform.TransformDirectory(e.Name);
					ZipEntry entry = new ZipEntry(cleanedName);
					outputStream.PutNextEntry(entry);
				}
			}
		}
		
		void ProcessFile(object sender, ScanEventArgs e)
		{
			if ( events != null ) {
				events.OnProcessFile(e.Name);
			}
			string cleanedName = nameTransform.TransformFile(e.Name);
			ZipEntry entry = new ZipEntry(cleanedName);
			outputStream.PutNextEntry(entry);
			AddFileContents(e.Name);
		}

		void AddFileContents(string name)
		{
			if ( buffer == null ) {
				buffer = new byte[4096];
			}

			FileStream stream = File.OpenRead(name);
			try {
				int length;
				do {
					length = stream.Read(buffer, 0, buffer.Length);
					outputStream.Write(buffer, 0, length);
				} while ( length > 0 );
			}
			finally {
				stream.Close();
			}
		}
		
		void ExtractFileEntry(ZipEntry entry, string targetName)
		{
			bool proceed = true;
			if ((overwrite == Overwrite.Prompt) && (confirmDelegate != null)) {
				if (File.Exists(targetName) == true) {
					proceed = confirmDelegate(targetName);
				}
			}

			if ( proceed ) {
				
				if ( events != null ) {
					events.OnProcessFile(entry.Name);
				}
			
				FileStream streamWriter = File.Create(targetName);
			
				try {
					if ( buffer == null ) {
						buffer = new byte[4096];
					}
					
					int size;
		
					do {
						size = inputStream.Read(buffer, 0, buffer.Length);
						streamWriter.Write(buffer, 0, size);
					} while (size > 0);
				}
				finally {
					streamWriter.Close();
				}
	
				if (restoreDateTime) {
					File.SetLastWriteTime(targetName, entry.DateTime);
				}
			}
		}

		bool NameIsValid(string name)
		{
			return name != null && name.Length > 0 && name.IndexOfAny(Path.InvalidPathChars) < 0;
		}
		
		void ExtractEntry(ZipEntry entry)
		{
			bool doExtraction = NameIsValid(entry.Name);
			
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
				
				targetName = Path.Combine(targetDirectory, entryFileName);
				dirName = Path.GetDirectoryName(Path.GetFullPath(targetName));
	
				doExtraction = doExtraction && (entryFileName.Length > 0);
			}
			
			if ( doExtraction && !Directory.Exists(dirName) )
			{
				if ( !entry.IsDirectory || this.CreateEmptyDirectories ) {
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
		
		/// <summary>
		/// Get or set the <see cref="ZipNameTransform"> active when creating Zip files.</see>
		/// </summary>
		public ZipNameTransform NameTransform
		{
			get { return nameTransform; }
			set {
				if ( value == null ) {
					nameTransform = new ZipNameTransform();
				}
				else {
					nameTransform = value;
				}
			}
		}
		
		#region Instance Fields
		byte[] buffer;
		ZipOutputStream outputStream;
		ZipInputStream inputStream;
		string password = null;
		string targetDirectory;
		string sourceDirectory;
		NameFilter fileFilter;
		NameFilter directoryFilter;
		Overwrite overwrite;
		ConfirmOverwriteDelegate confirmDelegate;
		bool restoreDateTime = false;
		bool createEmptyDirectories = false;
		FastZipEvents events;
		ZipNameTransform nameTransform;
		#endregion
	}
}
