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
	/// <summary>
	/// SimpleZip provides for creating and extracting zip files.
	/// </summary>
	public class SimpleZip
	{
		public enum Overwrite {
			Prompt,
			Never,
			Always
		}

		public delegate bool ConfirmOverwriteDelegate(string fileName);
		
		public void CreateZip(string zipFileName, string sourceDirectory, bool recurse, string fileFilter)
		{
			outputStream = new ZipOutputStream(File.Create(zipFileName));
			try {
				FileScanner scanner = new FileScanner(fileFilter);
				scanner.ProcessFile += new ProcessFileDelegate(ProcessFile);
				scanner.Scan(sourceDirectory, recurse);
			}
			finally {
				outputStream.Close();
			}
		}
		
		public void ExtractZip(string zipFileName, string targetDirectory, string fileFilter) 
		{
			ExtractZip(zipFileName, targetDirectory, fileFilter, Overwrite.Always, null);
		}
		
		public void ExtractZip(string zipFileName, string targetDirectory, string fileFilter, 
		                       Overwrite overwrite, ConfirmOverwriteDelegate confirmDelegate)
		{
			if ((overwrite == Overwrite.Prompt) && (confirmDelegate == null)) {
				throw new ArgumentNullException("confirmDelegate");
			}
			this.overwrite = overwrite;
			this.confirmDelegate = confirmDelegate;
			this.targetDir = targetDirectory;
			nameFilter = new NameFilter(fileFilter);
			
			inputStream = new ZipInputStream(File.OpenRead(zipFileName));
			if (password != null)
				inputStream.Password = password;
		
			try {
				ZipEntry entry;
				while ( (entry = inputStream.GetNextEntry()) != null ) {
					if ( nameFilter.IsMatch(entry.Name) ) {
						ExtractEntry(entry);
					}
				}
			}
			finally {
				inputStream.Close();
			}
		}
		
		void ProcessFile(object sender, ScanEventArgs e)
		{
			ZipEntry entry = new ZipEntry(e.Name);
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
				do
				{
					length = stream.Read(buffer, 0, buffer.Length);
					outputStream.Write(buffer, 0, length);
				} while ( length > 0 );
			}
			finally {
				stream.Close();
			}
		}
		
		void ExtractEntry(ZipEntry entry)
		{
			// try and sort out the correct place to save this entry
			string entryFileName;
			
			if (Path.IsPathRooted(entry.Name)) {
				string workName = Path.GetPathRoot(entry.Name);
				workName = entry.Name.Substring(workName.Length);
				entryFileName = Path.Combine(Path.GetDirectoryName(workName), Path.GetFileName(entry.Name));
			} else {
				entryFileName = entry.Name;
			}
			string targetName = Path.Combine(targetDir, entryFileName);
			string fullPath = Path.GetDirectoryName(Path.GetFullPath(targetName));
			
			// Could be an option or parameter to allow failure or try creation
			if (Directory.Exists(fullPath) == false)
			{
				try {
					Directory.CreateDirectory(fullPath);
				}
				catch {
					return;
				}
			}
			else if ((overwrite == Overwrite.Prompt) && (confirmDelegate != null)) {
				if (File.Exists(targetName) == true) {
					if ( !confirmDelegate(targetName) ) {
						return;
					}
				}
			}
		
			if (entryFileName.Length > 0) {
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
		
		#region Instance Fields
		byte[] buffer;
		ZipOutputStream outputStream;
		ZipInputStream inputStream;
		string password = null;
		string targetDir;
		NameFilter nameFilter;
		Overwrite overwrite;
		ConfirmOverwriteDelegate confirmDelegate;
		bool restoreDateTime = false;
		#endregion
	}
}
