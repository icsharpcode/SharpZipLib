// FileFilter.cs
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
	/// <summary>
	/// FileFilter filters files.
	/// </summary>
	public class FileFilter
	{
		public FileFilter(string filter)
		{
			nameFilter = new NameFilter(filter);
		}

		public virtual bool IsMatch(string fileName)
		{
			FileInfo fileInfo = new FileInfo(fileName);
			return nameFilter.IsMatch(fileInfo.FullName);
		}
		
		#region Instance Fields
		NameFilter nameFilter;
		#endregion
	}
	
	public class NameAndSizeFilter : FileFilter
	{
		
		public NameAndSizeFilter(string filter, long minSize, long maxSize) : base(filter)
		{
			this.minSize = minSize;
			this.maxSize = maxSize;
		}
		
		public override bool IsMatch(string fileName)
		{
			FileInfo fileInfo = new FileInfo(fileName);
			long length = fileInfo.Length;
			return base.IsMatch(fileName) &&
				(MinSize <= length) && (MaxSize >= length);
		}
		
		long minSize = 0;
		
		public long MinSize
		{
			get { return minSize; }
			set { minSize = value; }
		}
		
		long maxSize = long.MaxValue;
		
		public long MaxSize
		{
			get { return maxSize; }
			set { maxSize = value; }
		}
		
		DateTime minDateTime;
		
		public DateTime MinDateTime
		{
			get { return minDateTime; }
			set { minDateTime = value; }
		}

		DateTime maxDateTime;
		
		public DateTime MaxDateTime
		{
			get { return maxDateTime; }
			set { maxDateTime = value; }
		}
		
	}
}
