using System;
using System.Collections.Generic;
using System.Text;

namespace ArchiveDiag.Tar
{
	public enum TypeFlag: byte
	{
		/// <summary>
		///  The "old way" of indicating a normal file.
		/// </summary>
		OldNorm = 0,

		/// <summary>
		/// Normal file type.
		/// </summary>
		Normal = (byte)'0',

		/// <summary>
		/// Link file type.
		/// </summary>
		Link = (byte)'1',

		/// <summary>
		/// Symbolic link file type.
		/// </summary>
		SymbolicLink = (byte)'2',

		/// <summary>
		/// Character device file type.
		/// </summary>
		CharacterDevice = (byte)'3',

		/// <summary>
		/// Block device file type.
		/// </summary>
		BlockDevice = (byte)'4',

		/// <summary>
		/// Directory file type.
		/// </summary>
		Directory = (byte)'5',

		/// <summary>
		/// FIFO (pipe) file type.
		/// </summary>
		FIFOPipe = (byte)'6',

		/// <summary>
		/// Contiguous file type.
		/// </summary>
		Contiguous = (byte)'7',

		/// <summary>
		/// Posix.1 2001 global extended header
		/// </summary>
		GlobalExtendedHeader = (byte)'g',

		/// <summary>
		/// Posix.1 2001 extended header
		/// </summary>
		ExtendedHeader = (byte)'x',

		// POSIX allows for upper case ascii type as extensions

		/// <summary>
		/// Solaris access control list file type
		/// </summary>
		AccessControlList = (byte)'A',

		/// <summary>
		/// GNU dir dump file type
		/// This is a dir entry that contains the names of files that were in the
		/// dir at the time the dump was made
		/// </summary>
		GNU_DumpDir = (byte)'D',

		/// <summary>
		/// Solaris Extended Attribute File
		/// </summary>
		ExtendedAttribute = (byte)'E',

		/// <summary>
		/// Inode (metadata only) no file content
		/// </summary>
		Inode = (byte)'I',

		/// <summary>
		/// Identifies the next file on the tape as having a long link name
		/// </summary>
		GNU_LongLink = (byte)'K',

		/// <summary>
		/// Identifies the next file on the tape as having a long name
		/// </summary>
		GNU_LongName = (byte)'L',

		/// <summary>
		/// Continuation of a file that began on another volume
		/// </summary>
		GNU_MultiVol = (byte)'M',

		/// <summary>
		/// For storing filenames that dont fit in the main header (old GNU)
		/// </summary>
		GNU_Names = (byte)'N',

		/// <summary>
		/// GNU Sparse file
		/// </summary>
		GNU_Sparse = (byte)'S',

		/// <summary>
		/// GNU Tape/volume header ignore on extraction
		/// </summary>
		GNU_VolumeHeader = (byte)'V',


	}
}
