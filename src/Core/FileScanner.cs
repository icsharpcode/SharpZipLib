// ZipConstants.cs
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

namespace ICSharpCode.SharpZipLib.Core
{
	public class ScanEventArgs : EventArgs
	{
		public ScanEventArgs(string name)
		{
			this.name = name;
			Continue = true;
		}
		
		string name;
		public string Name
		{
			get { return name; }
		}
		
		public bool Continue;
	}

	public class DirectoryEventArgs : ScanEventArgs
	{
		/// <summary>
		/// Initialize an instance of <see cref="DirectoryEventsArgs"></see>.
		/// </summary>
		/// <param name="name">The name for this directory.</param>
		/// <param name="isEmpty">Flag value indicating if any matching files are contained in this directory.</param>
		public DirectoryEventArgs(string name, bool isEmpty)
			: base (name)
		{
			this.isEmpty = isEmpty;
		}
		
		/// <summary>
		/// Geta value indicating if the directory contains any matching files or not.
		/// </summary>
		public bool IsEmpty
		{
			get { return isEmpty; }
		}
		
		bool isEmpty;
	}
	
	public class ScanFailureEventArgs
	{
		public ScanFailureEventArgs(string name, Exception e)
		{
			Name = name;
			this.Exception = e;
			Continue = true;
		}
		public string Name;
		public Exception Exception;
		public bool Continue;
	}
	
	public delegate void ProcessDirectoryDelegate(object Sender, DirectoryEventArgs e);
	public delegate void ProcessFileDelegate(object sender, ScanEventArgs e);
	public delegate void DirectoryFailureDelegate(object sender, ScanFailureEventArgs e);
	public delegate void FileFailureDelegate(object sender, ScanFailureEventArgs e);

	/// <summary>
	/// FileScanner allows for scanning of files and directories.
	/// </summary>
	public class FileScanner
	{
		public FileScanner(string filter)
		{
			fileFilter = new FileFilter(filter);
		}
		
		public FileScanner(string dirFilter, string fileFilter)
		{
			this.fileFilter = new FileFilter(fileFilter);
			this.dirFilter = new NameFilter(dirFilter);
		}
		
		public ProcessDirectoryDelegate ProcessDirectory;
		public ProcessFileDelegate ProcessFile;

		public DirectoryFailureDelegate DirectoryFailure;
		public FileFailureDelegate FileFailure;
		
		public void OnDirectoryFailure(string directory, Exception e)
		{
			if ( DirectoryFailure != null ) {
				ScanFailureEventArgs args = new ScanFailureEventArgs(directory, e);
				DirectoryFailure(this, args);
				alive = args.Continue;
			}
		}
		
		public void OnFileFailure(string file, Exception e)
		{
			if ( FileFailure != null ) {
				ScanFailureEventArgs args = new ScanFailureEventArgs(file, e);
				FileFailure(this, args);
				alive = args.Continue;
			}
		}
		
		public void OnProcessFile(string file)
		{
			if ( ProcessFile != null ) {
				ScanEventArgs args = new ScanEventArgs(file);
				ProcessFile(this, args);
				alive = args.Continue;
			}
		}
		
		public void OnProcessDirectory(string directory, bool isEmpty)
		{
			if ( ProcessDirectory != null ) {
				DirectoryEventArgs args = new DirectoryEventArgs(directory, isEmpty);
				ProcessDirectory(this, args);
				alive = args.Continue;
			}
		}
		
		public void Scan(string directory, bool recurse)
		{
			alive = true;
			ScanDir(directory, recurse);
		}
		
		void ScanDir(string directory, bool recurse)
		{

			try {
				string[] names = System.IO.Directory.GetFiles(directory);
				OnProcessDirectory(directory, names.Length == 0);
				if ( !alive ) {
					return;
				}
				
				foreach (string fileName in names) {
					try {
						if ( fileFilter.IsMatch(fileName) ) {
							OnProcessFile(fileName);
							if ( !alive ) {
								return;
							}
						}
					}
					catch (Exception e)
					{
						OnFileFailure(fileName, e);
						if ( !alive ) {
							return;
						}
					}
				}
			}
			catch (Exception e) {
				OnDirectoryFailure(directory, e);
				if ( !alive ) {
					return;
				}
			}

			if (recurse) {
				try {
					string[] names = System.IO.Directory.GetDirectories(directory);
					foreach (string fulldir in names) {
						if ((dirFilter == null) || (dirFilter.IsMatch(fulldir))) {
							ScanDir(fulldir, true);
						}
					}
				}
				catch (Exception e) {
					OnDirectoryFailure(directory, e);
					if ( !alive ) {
						return;
					}
				}
			}
		}
		
		#region Instance Fields
		FileFilter fileFilter;
		NameFilter dirFilter;
		bool alive;
		#endregion
	}
}
