// ZipNameTransform.cs
//
// Copyright 2007 John Reilly
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
using System.Text;

using ICSharpCode.SharpZipLib.Core;

namespace ICSharpCode.SharpZipLib.Zip
{
	/// <summary>
	/// WindowsNameTransform transforms ZipFile names to windows compatible ones.
	/// </summary>
	public class WindowsNameTransform : INameTransform
	{
		/// <summary>
		/// Initialises a new instance of <see cref="WindowsNameTransform"/>
		/// </summary>
		/// <param name="baseDirectory"></param>
		public WindowsNameTransform(string baseDirectory)
		{
			if ( (baseDirectory != null) && (!IsValidName(baseDirectory)) ) {
#if NETCF_1_0
				throw new ArgumentException("Directory name is invalid");
#else
				throw new ArgumentException("Directory name is invalid", "baseDirectory");
#endif
			}
			baseDirectory_=Path.GetFullPath(baseDirectory);
		}
		
		/// <summary>
		/// Initialise a default instance of <see cref="WindowsNameTransform"/>
		/// </summary>
		public WindowsNameTransform()
		{
			// Do nothing.
		}
		
		/// <summary>
		/// Gets or sets a value containing the target directory to prefix values with.
		/// </summary>
		public string BaseDirectory
		{
			get { return baseDirectory_; }
			set {
				if ( value == null ) {
					throw new ArgumentNullException("value");
				}
				if ( !IsValidName(value) ) {
					throw new ArgumentException("Name is invalid");
				}
				baseDirectory_ = Path.GetFullPath(value);
			}
		}
		
		/// <summary>
		/// Gets or sets a value indicating wether paths on incoming values should be removed.
		/// </summary>
		public bool TrimIncomingPaths
		{
			get { return trimIncomingPaths_; }
			set { trimIncomingPaths_ = value; }
		}
		
		/// <summary>
		/// Initialise static class information.
		/// </summary>
		static WindowsNameTransform()
		{
			char[] invalidPathChars;
			
#if NET_1_0 || NET_1_1 || NETCF_1_0
			invalidPathChars = Path.InvalidPathChars;
#else
			invalidPathChars = Path.GetInvalidPathChars();
#endif
			int howMany = invalidPathChars.Length + 2;

			InvalidEntryChars = new char[howMany];
			Array.Copy(invalidPathChars, 0, InvalidEntryChars, 0, invalidPathChars.Length);
			InvalidEntryChars[howMany - 1] = '*';
			InvalidEntryChars[howMany - 2] = '?';
		}
		
		/// <summary>
		/// Transform a windows directory name according to the Zip file naming conventions.
		/// </summary>
		/// <param name="name">The directory name to transform.</param>
		/// <returns>The transformed name.</returns>
		public string TransformDirectory(string name)
		{
			name = TransformFile(name);
			if (name.Length > 0) {
				if ( !name.EndsWith(@"\") ) {
					name += @"\";
				}
			}
			else {
				throw new ZipException("Cannot have an empty directory name");
			}
			return name;
		}
		
		/// <summary>
		/// Transform a ZipArchive name to a windows style one.
		/// </summary>
		/// <param name="name">The file name to transform.</param>
		/// <returns>The transformed name.</returns>
		public string TransformFile(string name)
		{
			if (name != null) {
				name = MakeValidName(name, '_');
				
				if ( trimIncomingPaths_ ) {
					name = Path.GetFileName(name);
				}
				
				if ( baseDirectory_ != null ) {
					name = Path.Combine(baseDirectory_, name);
				}	
			}
			else {
				name = string.Empty;
			}
			return name;
		}
		
		/// <summary>
		/// Test a name to see if it is a valid name for a windwos filename.
		/// </summary>
		/// <param name="name">The name to test.</param>
		/// <returns>Returns true if the name is a valid zip name; false otherwise.</returns>
		public static bool IsValidName(string name)
		{
			bool result = 
				(name != null) &&
				(name.IndexOfAny(InvalidEntryChars) < 0)
				;
			return result;
		}
		
		/// <summary>
		/// Force a name to be valid by replacing invalid characters with a fixed value
		/// </summary>
		/// <param name="name">The name to force valid</param>
		/// <param name="replacement">The replacement character to use.</param>
		/// <returns>Returns a valid name</returns>
		static string MakeValidName(string name, char replacement)
		{
			if ( name == null ) {
				throw new ArgumentNullException("name");
			}
			
			name = name.Replace("/", @"\");
			
			// Handle invalid entry names by chopping of path root.
			if (Path.IsPathRooted(name)) {
				string workName = Path.GetPathRoot(name);
				name = name.Substring(workName.Length);
			}

			while ( (name.Length > 0) && (name[0] == '\\')) {
				name = name.Remove(0, 1);
			}

			int index = name.IndexOfAny(InvalidEntryChars);
			if (index > 0) {
				StringBuilder builder = new StringBuilder(name);

				while (index >= 0 ) {
					builder[index] = replacement;

					if (index >= name.Length) {
						index = -1;
					}
					else {
						index = name.IndexOfAny(InvalidEntryChars, index + 1);
					}
				}
				name = builder.ToString();
			}
					
			return name;
		}
		
		#region Instance Fields
		string baseDirectory_;
		bool trimIncomingPaths_;
		#endregion
		
		#region Class Fields
		static readonly char[] InvalidEntryChars;
		#endregion
	}
}
