// ZipNameTransform.cs
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
	/// ZipNameTransform transforms name as per the Zip file convention.
	/// </summary>
	public class ZipNameTransform : INameTransform
	{
		public ZipNameTransform()
		{
			relativePath = true;
		}
		
		public ZipNameTransform(bool useRelativePaths)
		{
			relativePath = useRelativePaths;
		}

		public ZipNameTransform(string relativePathPrefix)
		{
			relativePrefix = relativePathPrefix;
			relativePath = true;
		}
		
		public string TransformDirectory(string name)
		{
			name = TransformFile(name);
			if (name.Length > 0) {
				if ( !name.EndsWith("/") ) {
					name += "/";
				}
			}
			else {
				name = "/";
			}
			return name;
		}
		
		public string TransformFile(string name)
		{
			if (name != null) {
				if ( relativePath && relativePrefix != null && name.IndexOf(relativePrefix) == 0 ) {
					name = name.Substring(relativePrefix.Length);
				}
				
				if (Path.IsPathRooted(name) == true) {
					// NOTE:
					// for UNC names...  \\machine\share\zoom\beet.txt gives \zoom\beet.txt
					name = name.Substring(Path.GetPathRoot(name).Length);
				}
				
				if (relativePath == true) {
					if (name.Length > 0 && (name[0] == Path.AltDirectorySeparatorChar || name[0] == Path.DirectorySeparatorChar)) {
						name = name.Remove(0, 1);
					}
				} else {
					if (name.Length > 0 && name[0] != Path.AltDirectorySeparatorChar && name[0] != Path.DirectorySeparatorChar) {
						name = name.Insert(0, "/");
					}
				}
				name = name.Replace(@"\", "/");
			}
			else {
				name = "";
			}
			return name;
		}

		public string RelativePrefix
		{
			get { return relativePrefix; }
			set { relativePrefix = value; }
		}
		
		bool relativePath;
		string relativePrefix;
	}
}
