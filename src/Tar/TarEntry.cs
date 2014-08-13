// TarEntry.cs
//
// Copyright (C) 2001 Mike Krueger
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

namespace ICSharpCode.SharpZipLib.Tar 
{
	/// <summary>
	/// This class represents an entry in a Tar archive. It consists
	/// of the entry's header, as well as the entry's File. Entries
	/// can be instantiated in one of three ways, depending on how
	/// they are to be used.
	/// <p>
	/// TarEntries that are created from the header bytes read from
	/// an archive are instantiated with the TarEntry( byte[] )
	/// constructor. These entries will be used when extracting from
	/// or listing the contents of an archive. These entries have their
	/// header filled in using the header bytes. They also set the File
	/// to null, since they reference an archive entry not a file.</p>
	/// <p>
	/// TarEntries that are created from files that are to be written
	/// into an archive are instantiated with the CreateEntryFromFile(string)
	/// pseudo constructor. These entries have their header filled in using
	/// the File's information. They also keep a reference to the File
	/// for convenience when writing entries.</p>
	/// <p>
	/// Finally, TarEntries can be constructed from nothing but a name.
	/// This allows the programmer to construct the entry by hand, for
	/// instance when only an InputStream is available for writing to
	/// the archive, and the header information is constructed from
	/// other information. In this case the header fields are set to
	/// defaults and the File is set to null.</p>
	/// <see cref="TarHeader"/>
	/// </summary>
	public class TarEntry : ICloneable
	{
		#region Constructors
		/// <summary>
		/// Initialise a default instance of <see cref="TarEntry"/>.
		/// </summary>
		private TarEntry()
		{
			header = new TarHeader();
		}
		
		/// <summary>
		/// Construct an entry from an archive's header bytes. File is set
		/// to null.
		/// </summary>
		/// <param name = "headerBuffer">
		/// The header bytes from a tar archive entry.
		/// </param>
		public TarEntry(byte[] headerBuffer)
		{
			header = new TarHeader();
			header.ParseBuffer(headerBuffer);
		}
		
		/// <summary>
		/// Construct a TarEntry using the <paramref name="header">header</paramref> provided
		/// </summary>
		/// <param name="header">Header details for entry</param>
		public TarEntry(TarHeader header)
		{
			if ( header == null )
			{
				throw new ArgumentNullException("header");
			}

			this.header = (TarHeader)header.Clone();
		}
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Clone this tar entry.
		/// </summary>
		/// <returns>Returns a clone of this entry.</returns>
		public object Clone()
		{
			TarEntry entry = new TarEntry();
#if !PCL
			entry.file = file;
#endif
			entry.header = (TarHeader)header.Clone();
			entry.Name = Name;
			return entry;
		}
		#endregion

		/// <summary>
		/// Construct an entry with only a <paramref name="name">name</paramref>.
		/// This allows the programmer to construct the entry's header "by hand". 
		/// </summary>
		/// <param name="name">The name to use for the entry</param>
		/// <returns>Returns the newly created <see cref="TarEntry"/></returns>
		public static TarEntry CreateTarEntry(string name)
		{
			TarEntry entry = new TarEntry();
			TarEntry.NameTarHeader(entry.header, name);
			return entry;
		}
#if !PCL
		/// <summary>
		/// Construct an entry for a file. File is set to file, and the
		/// header is constructed from information from the file.
		/// </summary>
		/// <param name = "fileName">The file name that the entry represents.</param>
		/// <returns>Returns the newly created <see cref="TarEntry"/></returns>
		public static TarEntry CreateEntryFromFile(string fileName)
		{
			TarEntry entry = new TarEntry();
			entry.GetFileTarHeader(entry.header, fileName);
			return entry;
		}
#endif
		/// <summary>
		/// Determine if the two entries are equal. Equality is determined
		/// by the header names being equal.
		/// </summary>
		/// <param name="obj">The <see cref="Object"/> to compare with the current Object.</param>
		/// <returns>
		/// True if the entries are equal; false if not.
		/// </returns>
		public override bool Equals(object obj)
		{
			TarEntry localEntry = obj as TarEntry;

			if ( localEntry != null )
			{
				return Name.Equals(localEntry.Name);
			}
			return false;
		}
		
		/// <summary>
		/// Derive a Hash value for the current <see cref="Object"/>
		/// </summary>
		/// <returns>A Hash code for the current <see cref="Object"/></returns>
		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}
		
		/// <summary>
		/// Determine if the given entry is a descendant of this entry.
		/// Descendancy is determined by the name of the descendant
		/// starting with this entry's name.
		/// </summary>
		/// <param name = "toTest">
		/// Entry to be checked as a descendent of this.
		/// </param>
		/// <returns>
		/// True if entry is a descendant of this.
		/// </returns>
		public bool IsDescendent(TarEntry toTest)
		{
			if ( toTest == null ) {
				throw new ArgumentNullException("toTest");
			}

			return toTest.Name.StartsWith(Name);
		}
		
		/// <summary>
		/// Get this entry's header.
		/// </summary>
		/// <returns>
		/// This entry's TarHeader.
		/// </returns>
		public TarHeader TarHeader 
		{
			get {
				return header;
			}
		}
		
		/// <summary>
		/// Get/Set this entry's name.
		/// </summary>
		public string Name 
		{
			get {
				return header.Name;
			}
			set {
				header.Name = value;
			}
		}
		
		/// <summary>
		/// Get/set this entry's user id.
		/// </summary>
		public int UserId 
		{
			get {
				return header.UserId;
			}
			set {
				header.UserId = value;
			}
		}
		
		/// <summary>
		/// Get/set this entry's group id.
		/// </summary>
		public int GroupId 
		{
			get {
				return header.GroupId;
			}
			set {
				header.GroupId = value;
			}
		}
		
		/// <summary>
		/// Get/set this entry's user name.
		/// </summary>
		public string UserName 
		{
			get {
				return header.UserName;
			}
			set {
				header.UserName = value;
			}
		}
		
		/// <summary>
		/// Get/set this entry's group name.
		/// </summary>
		public string GroupName 
		{
			get {
				return header.GroupName;
			}
			set {
				header.GroupName = value;
			}
		}
		
		/// <summary>
		/// Convenience method to set this entry's group and user ids.
		/// </summary>
		/// <param name="userId">
		/// This entry's new user id.
		/// </param>
		/// <param name="groupId">
		/// This entry's new group id.
		/// </param>
		public void SetIds(int userId, int groupId)
		{
			UserId  = userId; 
			GroupId = groupId;
		}
		
		/// <summary>
		/// Convenience method to set this entry's group and user names.
		/// </summary>
		/// <param name="userName">
		/// This entry's new user name.
		/// </param>
		/// <param name="groupName">
		/// This entry's new group name.
		/// </param>
		public void SetNames(string userName, string groupName)
		{
			UserName  = userName;
			GroupName = groupName;
		}

		/// <summary>
		/// Get/Set the modification time for this entry
		/// </summary>
		public DateTime ModTime {
			get {
				return header.ModTime;
			}
			set {
				header.ModTime = value;
			}
		}
		
#if !PCL
		/// <summary>
		/// Get this entry's file.
		/// </summary>
		/// <returns>
		/// This entry's file.
		/// </returns>
		public string File {
			get {
				return file;
			}
		}
#endif

		/// <summary>
		/// Get/set this entry's recorded file size.
		/// </summary>
		public long Size {
			get {
				return header.Size;
			}
			set {
				header.Size = value;
			}
		}
		
		/// <summary>
		/// Return true if this entry represents a directory, false otherwise
		/// </summary>
		/// <returns>
		/// True if this entry is a directory.
		/// </returns>
		public bool IsDirectory {
			get {
#if !PCL
				if (file != null) {
					return Directory.Exists(file);
				}
#endif		
				if (header != null) {
					if ((header.TypeFlag == TarHeader.LF_DIR) || Name.EndsWith( "/" )) {
						return true;
					}
				}
				return false;
			}
		}
#if !PCL
		/// <summary>
		/// Fill in a TarHeader with information from a File.
		/// </summary>
		/// <param name="header">
		/// The TarHeader to fill in.
		/// </param>
		/// <param name="file">
		/// The file from which to get the header information.
		/// </param>
		public void GetFileTarHeader(TarHeader header, string file)
		{
			if ( header == null ) {
				throw new ArgumentNullException("header");
			}

			if ( file == null ) {
				throw new ArgumentNullException("file");
			}

			this.file = file;

			// bugfix from torhovl from #D forum:
			string name = file;

#if !NETCF_1_0 && !NETCF_2_0 && !PCL
			// 23-Jan-2004 GnuTar allows device names in path where the name is not local to the current directory
			if (name.IndexOf(Environment.CurrentDirectory) == 0) {
				name = name.Substring(Environment.CurrentDirectory.Length);
			}
#endif
			
/*
			if (Path.DirectorySeparatorChar == '\\') 
			{
				// check if the OS is Windows
				// Strip off drive letters!
				if (name.Length > 2) 
				{
					char ch1 = name[0];
					char ch2 = name[1];
					
					if (ch2 == ':' && Char.IsLetter(ch1)) 
					{
						name = name.Substring(2);
					}
				}
			}
*/

			name = name.Replace(Path.DirectorySeparatorChar, '/');

			// No absolute pathnames
			// Windows (and Posix?) paths can start with UNC style "\\NetworkDrive\",
			// so we loop on starting /'s.
			while (name.StartsWith("/")) {
				name = name.Substring(1);
			}

			header.LinkName = String.Empty;
			header.Name     = name;
			
			if (Directory.Exists(file)) {
				header.Mode     = 1003; // Magic number for security access for a UNIX filesystem
				header.TypeFlag = TarHeader.LF_DIR;
				if ( (header.Name.Length == 0) || header.Name[header.Name.Length - 1] != '/') {
					header.Name = header.Name + "/";
				}
				
				header.Size     = 0;
			} else {
				header.Mode     = 33216; // Magic number for security access for a UNIX filesystem
				header.TypeFlag = TarHeader.LF_NORMAL;
				header.Size     = new FileInfo(file.Replace('/', Path.DirectorySeparatorChar)).Length;
			}

			header.ModTime = System.IO.File.GetLastWriteTime(file.Replace('/', Path.DirectorySeparatorChar)).ToUniversalTime();
			header.DevMajor = 0;
			header.DevMinor = 0;
		}
		
		/// <summary>
		/// Get entries for all files present in this entries directory.
		/// If this entry doesnt represent a directory zero entries are returned.
		/// </summary>
		/// <returns>
		/// An array of TarEntry's for this entry's children.
		/// </returns>
		public TarEntry[] GetDirectoryEntries()
		{
			if ( (file == null) || !Directory.Exists(file)) {
				return new TarEntry[0];
			}
			
			string[]   list   = Directory.GetFileSystemEntries(file);
			TarEntry[] result = new TarEntry[list.Length];

			for (int i = 0; i < list.Length; ++i) {
				result[i] = TarEntry.CreateEntryFromFile(list[i]);
			}
			
			return result;
		}
#endif
		/// <summary>
		/// Write an entry's header information to a header buffer.
		/// </summary>
		/// <param name = "outBuffer">
		/// The tar entry header buffer to fill in.
		/// </param>
		public void WriteEntryHeader(byte[] outBuffer)
		{
			header.WriteHeader(outBuffer);
		}
		
		/// <summary>
		/// Convenience method that will modify an entry's name directly
		/// in place in an entry header buffer byte array.
		/// </summary>
		/// <param name="buffer">
		/// The buffer containing the entry header to modify.
		/// </param>
		/// <param name="newName">
		/// The new name to place into the header buffer.
		/// </param>
		static public void AdjustEntryName(byte[] buffer, string newName)
		{
			TarHeader.GetNameBytes(newName, buffer, 0, TarHeader.NAMELEN);
		}
		
		/// <summary>
		/// Fill in a TarHeader given only the entry's name.
		/// </summary>
		/// <param name="header">
		/// The TarHeader to fill in.
		/// </param>
		/// <param name="name">
		/// The tar entry name.
		/// </param>
		static public void NameTarHeader(TarHeader header, string name)
		{
			if ( header == null ) {
				throw new ArgumentNullException("header");
			}

			if ( name == null ) {
				throw new ArgumentNullException("name");
			}

			bool isDir = name.EndsWith("/");
			
			header.Name = name;
			header.Mode = isDir ? 1003 : 33216;
			header.UserId   = 0;
			header.GroupId  = 0;
			header.Size     = 0;
			
			header.ModTime  = DateTime.UtcNow;
			
			header.TypeFlag = isDir ? TarHeader.LF_DIR : TarHeader.LF_NORMAL;
			
			header.LinkName  = String.Empty;
			header.UserName  = String.Empty;
			header.GroupName = String.Empty;
			
			header.DevMajor = 0;
			header.DevMinor = 0;
		}

		#region Instance Fields
#if !PCL
		/// <summary>
		/// The name of the file this entry represents or null if the entry is not based on a file.
		/// </summary>
		string file;
#endif
		/// <summary>
		/// The entry's header information.
		/// </summary>
		TarHeader	header;
		#endregion
	}
}



/* The original Java file had this header:
	*
	** Authored by Timothy Gerard Endres
	** <mailto:time@gjt.org>  <http://www.trustice.com>
	**
	** This work has been placed into the public domain.
	** You may use this work in any way and for any purpose you wish.
	**
	** THIS SOFTWARE IS PROVIDED AS-IS WITHOUT WARRANTY OF ANY KIND,
	** NOT EVEN THE IMPLIED WARRANTY OF MERCHANTABILITY. THE AUTHOR
	** OF THIS SOFTWARE, ASSUMES _NO_ RESPONSIBILITY FOR ANY
	** CONSEQUENCE RESULTING FROM THE USE, MODIFICATION, OR
	** REDISTRIBUTION OF THIS SOFTWARE.
	**
	*/
