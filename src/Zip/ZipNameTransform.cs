/*
 * Created by SharpDevelop.
 * User: JohnR
 * Date: 13/02/2005
 * Time: 9:39 p.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

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
