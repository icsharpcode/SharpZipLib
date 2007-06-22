// ZipOutputStream.cs
//
// Copyright (C) 2001 Mike Krueger
// Copyright (C) 2004 John Reilly
//
// This file was translated from java, it was part of the GNU Classpath
// Copyright (C) 2001 Free Software Foundation, Inc.
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
using System.Collections;
using System.Text;

using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace ICSharpCode.SharpZipLib.Zip
{
	/// <summary>
	/// This is a DeflaterOutputStream that writes the files into a zip
	/// archive one after another.  It has a special method to start a new
	/// zip entry.  The zip entries contains information about the file name
	/// size, compressed size, CRC, etc.
	/// 
	/// It includes support for Stored and Deflated entries.
	/// This class is not thread safe.
	/// <br/>
	/// <br/>Author of the original java version : Jochen Hoenicke
	/// </summary>
	/// <example> This sample shows how to create a zip file
	/// <code>
	/// using System;
	/// using System.IO;
	/// 
	/// using ICSharpCode.SharpZipLib.Core;
	/// using ICSharpCode.SharpZipLib.Zip;
	/// 
	/// class MainClass
	/// {
	/// 	public static void Main(string[] args)
	/// 	{
	/// 		string[] filenames = Directory.GetFiles(args[0]);
	/// 		byte[] buffer = new byte[4096];
	/// 		
	/// 		using ( ZipOutputStream s = new ZipOutputStream(File.Create(args[1])) ) {
	/// 		
	/// 			s.SetLevel(9); // 0 - store only to 9 - means best compression
	/// 		
	/// 			foreach (string file in filenames) {
	/// 				ZipEntry entry = new ZipEntry(file);
	/// 				s.PutNextEntry(entry);
	///
	/// 				using (FileStream fs = File.OpenRead(file)) {
	///						StreamUtils.Copy(fs, s, buffer);
	/// 				}
	/// 			}
	/// 		}
	/// 	}
	/// }	
	/// </code>
	/// </example>
	public class ZipOutputStream : DeflaterOutputStream
	{
		#region Instance Fields
		private ArrayList entries  = new ArrayList();
		private Crc32     crc      = new Crc32();
		private ZipEntry  curEntry;
		
		int defaultCompressionLevel = Deflater.DEFAULT_COMPRESSION;
		CompressionMethod curMethod = CompressionMethod.Deflated;

		/// <summary>
		/// Used to track the size of data for an entry during writing.
		/// </summary>
		private long size;
		/// <summary>
		/// Offset to be recorded for each entry in the central header.
		/// </summary>
		private long offset;
		
		private byte[] zipComment = new byte[0];
		bool patchEntryHeader;
		long crcPatchPos = -1;
		long sizePatchPos = -1;

		// Default is dynamic which is not backwards compatible and can cause problems
		// with XP's built in compression which cant read Zip64 archives.
		// However it does avoid the situation were a large file is added and cannot be completed correctly.
		UseZip64 useZip64_ = UseZip64.Dynamic;
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new Zip output stream, writing a zip archive.
		/// </summary>
		/// <param name="baseOutputStream">
		/// The output stream to which the archive contents are written.
		/// </param>
		public ZipOutputStream(Stream baseOutputStream)
			: base(baseOutputStream, new Deflater(Deflater.DEFAULT_COMPRESSION, true))
		{
		}
		#endregion
		
		/// <summary>
		/// Gets boolean indicating central header has been added for this archive...
		/// No further entries can be added once this has been done.
		/// </summary>
		public bool IsFinished 
		{
			get {
				return entries == null;
			}
		}

		/// <summary>
		/// Set the zip file comment.
		/// </summary>
		/// <param name="comment">
		/// The comment string
		/// </param>
		/// <exception name ="ArgumentOutOfRangeException">
		/// Encoding of comment is longer than 0xffff bytes.
		/// </exception>
		public void SetComment(string comment)
		{
			byte[] commentBytes = ZipConstants.ConvertToArray(comment);
			if (commentBytes.Length > 0xffff) {
				throw new ArgumentOutOfRangeException("comment");
			}
			zipComment = commentBytes;
		}
		
		/// <summary>
		/// Sets default compression level.  The new level will be activated
		/// immediately.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Level specified is not supported.
		/// </exception>
		/// <see cref="Deflater"/>
		public void SetLevel(int level)
		{
			defaultCompressionLevel = level;
			def.SetLevel(level);
		}
		
		/// <summary>
		/// Get the current deflate compression level
		/// </summary>
		/// <returns>The current compression level</returns>
		public int GetLevel()
		{
			return def.GetLevel();
		}

		/// <summary>
		/// Get / set a value indicating how Zip64 Extension usage is determined when adding entries.
		/// </summary>
		public UseZip64 UseZip64
		{
			get { return useZip64_; }
			set { useZip64_ = value; }
		}
		
		/// <summary>
		/// Write an unsigned short in little endian byte order.
		/// </summary>
		private void WriteLeShort(int value)
		{
			unchecked {
				baseOutputStream.WriteByte((byte)(value & 0xff));
				baseOutputStream.WriteByte((byte)((value >> 8) & 0xff));
			}
		}
		
		/// <summary>
		/// Write an int in little endian byte order.
		/// </summary>
		private void WriteLeInt(int value)
		{
			unchecked {
				WriteLeShort(value);
				WriteLeShort(value >> 16);
			}
		}
		
		/// <summary>
		/// Write an int in little endian byte order.
		/// </summary>
		private void WriteLeLong(long value)
		{
			unchecked {
				WriteLeInt((int)value);
				WriteLeInt((int)(value >> 32));
			}
		}
		
		/// <summary>
		/// Starts a new Zip entry. It automatically closes the previous
		/// entry if present.
		/// All entry elements bar name are optional, but must be correct if present.
		/// If the compression method is stored and the output is not patchable
		/// the compression for that entry is automatically changed to deflate level 0
		/// </summary>
		/// <param name="entry">
		/// the entry.
		/// </param>
		/// <exception cref="System.ArgumentNullException">
		/// if entry passed is null.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// if an I/O error occured.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// if stream was finished
		/// </exception>
		/// <exception cref="ZipException">
		/// Too many entries in the Zip file<br/>
		/// Entry name is too long<br/>
		/// Finish has already been called<br/>
		/// </exception>
		public void PutNextEntry(ZipEntry entry)
		{
			if ( entry == null ) {
				throw new ArgumentNullException("entry");
			}

			if (entries == null) {
				throw new InvalidOperationException("ZipOutputStream was finished");
			}
			
			if (curEntry != null) {
				CloseEntry();
			}

			if (entries.Count == int.MaxValue) {
				throw new ZipException("Too many entries for Zip file");
			}
			
			CompressionMethod method = entry.CompressionMethod;
			int compressionLevel = defaultCompressionLevel;
			
			// Clear flags that the library manages internally
			entry.Flags &= (int)GeneralBitFlags.UnicodeText;
			patchEntryHeader = false;
			bool headerInfoAvailable = true;
			
			if (method == CompressionMethod.Stored) {
				// Cant store values in a data descriptor as you cant extract stored files
				// if the length isnt known.
				entry.Flags &= ~8;
				if (entry.CompressedSize >= 0) {
					if (entry.Size < 0) {
						entry.Size = entry.CompressedSize;
					} else if (entry.Size != entry.CompressedSize) {
						throw new ZipException("Method STORED, but compressed size != size");
					}
				} else {
					if (entry.Size >= 0) {
						entry.CompressedSize = entry.Size;
					}
				}
					
				if (entry.Size < 0 || entry.Crc < 0) {
					if (CanPatchEntries == true) {
						headerInfoAvailable = false;
					}
					else {
						// Can't patch entries so storing is not possible.
						method = CompressionMethod.Deflated;
						compressionLevel = 0;
					}
				}
			}
				
			if (method == CompressionMethod.Deflated) {
				if (entry.Size == 0) {
					// No need to compress - no data.
					entry.CompressedSize = entry.Size;
					entry.Crc = 0;
					method = CompressionMethod.Stored;
				} else if ( (entry.CompressedSize < 0) || (entry.Size < 0) || (entry.Crc < 0) ) {
					headerInfoAvailable = false;
				}
			}
			
			if (headerInfoAvailable == false) {
				if (CanPatchEntries == false) {
					// Only way to record size and compressed size is to append a data descriptor
					// after compressed data.
					entry.Flags |= 8;
				} else {
					patchEntryHeader = true;
				}
			}
			
			if (Password != null) {
				entry.IsCrypted = true;
				if (entry.Crc < 0) {
					// Need to append a data descriptor as the crc isnt available for use
					// with encryption, the date is used instead.  Setting the flag
					// indicates this to the decompressor.
					entry.Flags |= 8;
				}
			}

			entry.Offset = offset;
			entry.CompressionMethod = (CompressionMethod)method;
			
			curMethod = method;
			sizePatchPos = -1;
			
			if ( (useZip64_ == UseZip64.On) || ((entry.Size < 0) && (useZip64_ == UseZip64.Dynamic)) ) {
				entry.ForceZip64();
			}

			// Write the local file header
			WriteLeInt(ZipConstants.LocalHeaderSignature);
			
			WriteLeShort(entry.Version);
			WriteLeShort(entry.Flags);
			WriteLeShort((byte)method);
			WriteLeInt((int)entry.DosTime);

			// TODO: Refactor header writing.  Its done in several places.
			if (headerInfoAvailable == true) {
				WriteLeInt((int)entry.Crc);
				if ( entry.LocalHeaderRequiresZip64 ) {
					WriteLeInt(-1);
					WriteLeInt(-1);
				}
				else {
					WriteLeInt(entry.IsCrypted ? (int)entry.CompressedSize + ZipConstants.CryptoHeaderSize : (int)entry.CompressedSize);
					WriteLeInt((int)entry.Size);
				}
			} else {
				if (patchEntryHeader == true) {
					crcPatchPos = baseOutputStream.Position;
				}
				WriteLeInt(0);	// Crc
				
				if ( patchEntryHeader ) {
					sizePatchPos = baseOutputStream.Position;
				}

				// For local header both sizes appear in Zip64 Extended Information
				if ( entry.LocalHeaderRequiresZip64 && patchEntryHeader ) {
					WriteLeInt(-1);
					WriteLeInt(-1);
				}
				else {
					WriteLeInt(0);	// Compressed size
					WriteLeInt(0);	// Uncompressed size
				}
			}

			byte[] name = ZipConstants.ConvertToArray(entry.Flags, entry.Name);
			
			if (name.Length > 0xFFFF) {
				throw new ZipException("Entry name too long.");
			}

			ZipExtraData ed = new ZipExtraData(entry.ExtraData);

			if (entry.LocalHeaderRequiresZip64 && (headerInfoAvailable || patchEntryHeader)) {
				ed.StartNewEntry();
				if (headerInfoAvailable) {
					ed.AddLeLong(entry.Size);
					ed.AddLeLong(entry.CompressedSize);
				}
				else {
					ed.AddLeLong(-1);
					ed.AddLeLong(-1);
				}
				ed.AddNewEntry(1);

				if ( !ed.Find(1) ) {
					throw new ZipException("Internal error cant find extra data");
				}
				
				if ( patchEntryHeader ) {
					sizePatchPos = ed.CurrentReadIndex;
				}
			}
			else {
				ed.Delete(1);
			}
			
			byte[] extra = ed.GetEntryData();

			WriteLeShort(name.Length);
			WriteLeShort(extra.Length);

			if ( name.Length > 0 ) {
				baseOutputStream.Write(name, 0, name.Length);
			}
			
			if ( entry.LocalHeaderRequiresZip64 && patchEntryHeader ) {
				sizePatchPos += baseOutputStream.Position;
			}

			if ( extra.Length > 0 ) {
				baseOutputStream.Write(extra, 0, extra.Length);
			}
			
			offset += ZipConstants.LocalHeaderBaseSize + name.Length + extra.Length;
			
			// Activate the entry.
			curEntry = entry;
			crc.Reset();
			if (method == CompressionMethod.Deflated) {
				def.Reset();
				def.SetLevel(compressionLevel);
			}
			size = 0;
			
			if (entry.IsCrypted == true) {
				if (entry.Crc < 0) {			// so testing Zip will says its ok
					WriteEncryptionHeader(entry.DosTime << 16);
				} else {
					WriteEncryptionHeader(entry.Crc);
				}
			}
		}
		
		/// <summary>
		/// Closes the current entry, updating header and footer information as required
		/// </summary>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurs.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// No entry is active.
		/// </exception>
		public void CloseEntry()
		{
			if (curEntry == null) {
				throw new InvalidOperationException("No open entry");
			}
			
			// First finish the deflater, if appropriate
			if (curMethod == CompressionMethod.Deflated) {
				base.Finish();
			}
			
			long csize = (curMethod == CompressionMethod.Deflated) ? def.TotalOut : size;
			
			if (curEntry.Size < 0) {
				curEntry.Size = size;
			} else if (curEntry.Size != size) {
				throw new ZipException("size was " + size + ", but I expected " + curEntry.Size);
			}
			
			if (curEntry.CompressedSize < 0) {
				curEntry.CompressedSize = csize;
			} else if (curEntry.CompressedSize != csize) {
				throw new ZipException("compressed size was " + csize + ", but I expected " + curEntry.CompressedSize);
			}
			
			if (curEntry.Crc < 0) {
				curEntry.Crc = crc.Value;
			} else if (curEntry.Crc != crc.Value) {
				throw new ZipException("crc was " + crc.Value +	", but I expected " + curEntry.Crc);
			}
			
			offset += csize;

			if (curEntry.IsCrypted == true) {
				curEntry.CompressedSize += ZipConstants.CryptoHeaderSize;
			}
				
			// Patch the header if possible
			if (patchEntryHeader == true) {
				patchEntryHeader = false;

				long curPos = baseOutputStream.Position;
				baseOutputStream.Seek(crcPatchPos, SeekOrigin.Begin);
				WriteLeInt((int)curEntry.Crc);
				
				if ( curEntry.LocalHeaderRequiresZip64 ) {
					
					if ( sizePatchPos == -1 ) {
						throw new ZipException("Entry requires zip64 but this has been turned off");
					}
					
					baseOutputStream.Seek(sizePatchPos, SeekOrigin.Begin);
					WriteLeLong(curEntry.Size);
					WriteLeLong(curEntry.CompressedSize);
				}
				else {
					WriteLeInt((int)curEntry.CompressedSize);
					WriteLeInt((int)curEntry.Size);
				}
				baseOutputStream.Seek(curPos, SeekOrigin.Begin);
			}

			// Add data descriptor if flagged as required
			if ((curEntry.Flags & 8) != 0) {
				WriteLeInt(ZipConstants.DataDescriptorSignature);
				WriteLeInt((int)curEntry.Crc);
				
				if ( curEntry.LocalHeaderRequiresZip64 ) {
					WriteLeLong(curEntry.CompressedSize);
					WriteLeLong(curEntry.Size);
					offset += ZipConstants.Zip64DataDescriptorSize;
				}
				else {
					WriteLeInt((int)curEntry.CompressedSize);
					WriteLeInt((int)curEntry.Size);
					offset += ZipConstants.DataDescriptorSize;
				}
			}
			
			entries.Add(curEntry);
			curEntry = null;
		}
		
		void WriteEncryptionHeader(long crcValue)
		{
			offset += ZipConstants.CryptoHeaderSize;
			
			InitializePassword(Password);
			
			byte[] cryptBuffer = new byte[ZipConstants.CryptoHeaderSize];
			Random rnd = new Random();
			rnd.NextBytes(cryptBuffer);
			cryptBuffer[11] = (byte)(crcValue >> 24);
			
			EncryptBlock(cryptBuffer, 0, cryptBuffer.Length);
			baseOutputStream.Write(cryptBuffer, 0, cryptBuffer.Length);
		}
		
		/// <summary>
		/// Writes the given buffer to the current entry.
		/// </summary>
		/// <exception cref="ZipException">
		/// Archive size is invalid
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// No entry is active.
		/// </exception>
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (curEntry == null) {
				throw new InvalidOperationException("No open entry.");
			}
			
			if ( buffer == null ) {
				throw new ArgumentNullException("buffer");
			}
			
			if ( offset < 0 ) {
#if NETCF_1_0
				throw new ArgumentOutOfRangeException("offset");
#else
				throw new ArgumentOutOfRangeException("offset", "Cannot be negative");
#endif
			}

			if ( count < 0 ) {
#if NETCF_1_0
				throw new ArgumentOutOfRangeException("count");
#else
				throw new ArgumentOutOfRangeException("count", "Cannot be negative");
#endif
			}

			if ( (buffer.Length - offset) < count ) {
				throw new ArgumentException("Invalid offset/count combination");
			}
			
			crc.Update(buffer, offset, count);
			size += count;
			
			switch (curMethod) {
				case CompressionMethod.Deflated:
					base.Write(buffer, offset, count);
					break;
				
				case CompressionMethod.Stored:
					if (Password != null) {
						// TODO: Stored & encrypted output can use a lot of memory/cpu with local copying..
						byte[] localBuffer = new byte[count];
						Array.Copy(buffer, offset, localBuffer, 0, count);
						EncryptBlock(localBuffer, 0, count);
						baseOutputStream.Write(localBuffer, offset, count);
					} else {
						baseOutputStream.Write(buffer, offset, count);
					}
					break;
			}
		}
		
		/// <summary>
		/// Finishes the stream.  This will write the central directory at the
		/// end of the zip file and flush the stream.
		/// </summary>
		/// <remarks>
		/// This is automatically called when the stream is closed.
		/// </remarks>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurs.
		/// </exception>
		/// <exception cref="ZipException">
		/// Comment exceeds the maximum length<br/>
		/// Entry name exceeds the maximum length
		/// </exception>
		public override void Finish()
		{
			if (entries == null)  {
				return;
			}
			
			if (curEntry != null) {
				CloseEntry();
			}
			
			long numEntries = entries.Count;
			long sizeEntries = 0;
			
			foreach (ZipEntry entry in entries) {
				WriteLeInt(ZipConstants.CentralHeaderSignature); 
				WriteLeShort(ZipConstants.VersionMadeBy);
				WriteLeShort(entry.Version);
				WriteLeShort(entry.Flags);
				WriteLeShort((short)entry.CompressionMethod);
				WriteLeInt((int)entry.DosTime);
				WriteLeInt((int)entry.Crc);

				if ( entry.IsZip64Forced() || 
					(entry.CompressedSize >= uint.MaxValue) )
				{
					WriteLeInt(-1);
				}
				else {
					WriteLeInt((int)entry.CompressedSize);
				}

				if ( entry.IsZip64Forced() ||
					(entry.Size >= uint.MaxValue) )
				{
					WriteLeInt(-1);
				}
				else {
					WriteLeInt((int)entry.Size);
				}

				byte[] name = ZipConstants.ConvertToArray(entry.Flags, entry.Name);
				
				if (name.Length > 0xffff) {
					throw new ZipException("Name too long.");
				}
				
				ZipExtraData ed = new ZipExtraData(entry.ExtraData);

				if ( entry.CentralHeaderRequiresZip64 ) {
					ed.StartNewEntry();
					if ( entry.IsZip64Forced() ||
						(entry.Size >= 0xffffffff) )
					{
						ed.AddLeLong(entry.Size);
					}

					if ( entry.IsZip64Forced() ||
						(entry.CompressedSize >= 0xffffffff) )
					{
						ed.AddLeLong(entry.CompressedSize);
					}

					if ( entry.Offset >= 0xffffffff )
					{
						ed.AddLeLong(entry.Offset);
					}

					ed.AddNewEntry(1);
				}
				else {
					ed.Delete(1);
				}

				byte[] extra = ed.GetEntryData();
				
				byte[] entryComment = 
					(entry.Comment != null) ? 
					ZipConstants.ConvertToArray(entry.Flags, entry.Comment) :
					new byte[0];

				if (entryComment.Length > 0xffff) {
					throw new ZipException("Comment too long.");
				}
				
				WriteLeShort(name.Length);
				WriteLeShort(extra.Length);
				WriteLeShort(entryComment.Length);
				WriteLeShort(0);	// disk number
				WriteLeShort(0);	// internal file attributes
									// external file attributes

				if (entry.ExternalFileAttributes != -1) {
					WriteLeInt(entry.ExternalFileAttributes);
				} else {
					if (entry.IsDirectory) {                         // mark entry as directory (from nikolam.AT.perfectinfo.com)
						WriteLeInt(16);
					} else {
						WriteLeInt(0);
					}
				}

				if ( entry.Offset >= uint.MaxValue ) {
					WriteLeInt(-1);
				}
				else {
					WriteLeInt((int)entry.Offset);
				}
				
				if ( name.Length > 0 ) {
					baseOutputStream.Write(name,    0, name.Length);
				}

				if ( extra.Length > 0 ) {
					baseOutputStream.Write(extra,   0, extra.Length);
				}

				if ( entryComment.Length > 0 ) {
					baseOutputStream.Write(entryComment, 0, entryComment.Length);
				}

				sizeEntries += ZipConstants.CentralHeaderBaseSize + name.Length + extra.Length + entryComment.Length;
			}
			
			using ( ZipHelperStream zhs = new ZipHelperStream(baseOutputStream) ) {
				zhs.WriteEndOfCentralDirectory(numEntries, sizeEntries, offset, zipComment);
			}

			entries = null;
		}
	}
}
