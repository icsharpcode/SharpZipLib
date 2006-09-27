// ZipFile.cs
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

// Defines this to force entries to be in Zip64 format for testing
// #define FORCE_ZIP64

using System;
using System.Collections;
using System.IO;
using System.Text;

#if !COMPACT_FRAMEWORK
using System.Security.Cryptography;
#endif

using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Encryption;

namespace ICSharpCode.SharpZipLib.Zip 
{

	/// <summary>
	/// Arguments used with KeysRequiredEvent
	/// </summary>
	public class KeysRequiredEventArgs : EventArgs
	{

		#region Instance Fields
		string fileName;
		byte[] key;
		#endregion
		
		/// <summary>
		/// Get the name of the file for which keys are required.
		/// </summary>
		public string FileName
		{
			get { return fileName; }
		}
	
		/// <summary>
		/// Get/set the key value
		/// </summary>
		public byte[] Key
		{
			get { return key; }
			set { key = value; }
		}
	
		/// <summary>
		/// Initialise a new instance of <see cref="KeysRequiredEventArgs"></see>
		/// </summary>
		/// <param name="name">The name of the file for which keys are required.</param>
		public KeysRequiredEventArgs(string name)
		{
			fileName = name;
		}
	
		/// <summary>
		/// Initialise a new instance of <see cref="KeysRequiredEventArgs"></see>
		/// </summary>
		/// <param name="name">The name of the file for which keys are required.</param>
		/// <param name="keyValue">The current key value.</param>
		public KeysRequiredEventArgs(string name, byte[] keyValue)
		{
			fileName = name;
			key = keyValue;
		}
	}
	
	/// <summary>
	/// This class represents a Zip archive.  You can ask for the contained
	/// entries, or get an input stream for a file entry.  The entry is
	/// automatically decompressed.
	/// 
	/// This class is thread safe:  You can open input streams for arbitrary
	/// entries in different threads.
	/// <br/>
	/// <br/>Author of the original java version : Jochen Hoenicke
	/// </summary>
	/// <example>
	/// <code>
	/// using System;
	/// using System.Text;
	/// using System.Collections;
	/// using System.IO;
	/// 
	/// using ICSharpCode.SharpZipLib.Zip;
	/// 
	/// class MainClass
	/// {
	/// 	static public void Main(string[] args)
	/// 	{
	/// 		using (ZipFile zFile = new ZipFile(args[0])) {
	/// 			Console.WriteLine("Listing of : " + zFile.Name);
	/// 			Console.WriteLine("");
	/// 			Console.WriteLine("Raw Size    Size      Date     Time     Name");
	/// 			Console.WriteLine("--------  --------  --------  ------  ---------");
	/// 			foreach (ZipEntry e in zFile) {
	/// 				DateTime d = e.DateTime;
	/// 				Console.WriteLine(
	/// 					"{0, -10}{1, -10}{2}  {3}   {4}", e.Size, e.CompressedSize,
	/// 					d.ToString("dd-MM-yy"), d.ToString("t"),
	/// 					e.Name);
	/// 			}
	/// 		}
	/// 	}
	/// }
	/// </code>
	/// </example>
	public class ZipFile : IEnumerable, IDisposable
	{
		#region Instance Fields
		string     name_;
		string     comment_;
		Stream     baseStream_;
		bool       isStreamOwner = true;
		long       offsetOfFirstEntry = 0;
		ZipEntry[] entries_;
		bool isDisposed_;
		#endregion
		
		#region KeyHandling
		
		/// <summary>
		/// Delegate for handling keys/password setting during compresion/decompression.
		/// </summary>
		public delegate void KeysRequiredEventHandler(
			object sender,
			KeysRequiredEventArgs e
		);

		/// <summary>
		/// Event handler for handling encryption keys.
		/// </summary>
		public KeysRequiredEventHandler KeysRequired;

		/// <summary>
		/// Handles getting of encryption keys when required.
		/// </summary>
		/// <param name="fileName">The file for which encryption keys are required.</param>
		void OnKeysRequired(string fileName)
		{
			if (KeysRequired != null) {
				KeysRequiredEventArgs krea = new KeysRequiredEventArgs(fileName, key);
				KeysRequired(this, krea);
				key = krea.Key;
			}
		}
		
		byte[] key = null;
		
		/// <summary>
		/// Get/set the encryption key value.
		/// </summary>
		byte[] Key
		{
			get { return key; }
			set { key = value; }
		}
		
		/// <summary>
		/// Password to be used for encrypting/decrypting files.
		/// </summary>
		/// <remarks>Set to null if no password is required.</remarks>
		public string Password
		{
			set 
			{
				if ( (value == null) || (value.Length == 0) ) {
					key = null;
				}
				else {
					key = PkzipClassic.GenerateKeys(Encoding.ASCII.GetBytes(value));
				}
			}
		}
		
		byte[] iv = null;
		
		/// <summary>
		/// Get a value indicating wether encryption keys are currently available.
		/// </summary>
		bool HaveKeys
		{
		 get { return key != null; }
		}
		#endregion
	
		#region Constructors
		/// <summary>
		/// Opens a Zip file with the given name for reading.
		/// </summary>
		/// <exception cref="IOException">
		/// An i/o error occurs
		/// </exception>
		/// <exception cref="ZipException">
		/// The file doesn't contain a valid zip archive.
		/// </exception>
		public ZipFile(string name)
		{
			name_ = name;
			baseStream_ = File.OpenRead(name);
			
			try {
				ReadEntries();
			}
			catch {
				DisposeInternal(true);
				throw;
			}
		}
		
		/// <summary>
		/// Opens a Zip file reading the given FileStream
		/// </summary>
		/// <exception cref="IOException">
		/// An i/o error occurs.
		/// </exception>
		/// <exception cref="ZipException">
		/// The file doesn't contain a valid zip archive.
		/// </exception>
		public ZipFile(FileStream file)
		{
			baseStream_  = file;
			name_ = file.Name;
			
			try {
				ReadEntries();
			}
			catch {
				DisposeInternal(true);
				throw;
			}
		}
		
		/// <summary>
		/// Opens a Zip file reading the given Stream
		/// </summary>
		/// <exception cref="IOException">
		/// An i/o error occurs
		/// </exception>
		/// <exception cref="ZipException">
		/// The file doesn't contain a valid zip archive.<br/>
		/// The stream provided cannot seek
		/// </exception>
		public ZipFile(Stream baseStream)
		{
			baseStream_  = baseStream;
			name_ = null;
			
			try {
				ReadEntries();
			}
			catch {
				DisposeInternal(true);
				throw;
			}
		}

		/// <summary>
		/// Initialises a default Zip file with no entries and no file storage.
		/// </summary>
		internal ZipFile()
		{
			entries_ = new ZipEntry[0];
		}
		
		#endregion
		
		/// <summary>
		/// Create a new <see cref="ZipFile"/> whose data will be stored in a file.
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns>Returns a new created ZipFile.</returns>
		public static ZipFile Create(string fileName)
		{
			ZipFile result = new ZipFile();
			result.name_ = fileName;
			result.baseStream_ = File.Create(fileName);
			return result;
		}

		/// <summary>
		/// Finalize this instance.
		/// </summary>
		~ZipFile()
		{
			Dispose(false);
		}
		
		/// <summary>
		/// Closes the ZipFile.  If the stream is <see cref="IsStreamOwner">owned</see> then this also closes the underlying input stream.
		/// Once closed, no further instance methods should be called.
		/// </summary>
		/// <exception cref="System.IO.IOException">
		/// An i/o error occurs.
		/// </exception>
		public void Close()
		{
			DisposeInternal(true);
			GC.SuppressFinalize(this);
		}
		
		#region IDisposable Members
		void IDisposable.Dispose()
		{
			Close();
		}
		#endregion

		void DisposeInternal(bool disposing)
		{
			if ( !isDisposed_ )
			{
				isDisposed_ = true;
				entries_ = null;
				if ( IsStreamOwner ) 
				{
					lock(baseStream_) 
					{
						baseStream_.Close();
					}
				}
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			DisposeInternal(disposing);
		}
		
		/// <summary>
		/// Get/set a flag indicating if the underlying stream is owned by the ZipFile instance.
		/// If the flag is true then the stream will be closed when <see cref="Close">Close</see> is called.
		/// </summary>
		/// <remarks>
		/// The default value is true in all cases.
		/// </remarks>
		bool IsStreamOwner
		{
			get { return isStreamOwner; }
			set { isStreamOwner = value; }
		}
		
		/// <summary>
		/// Read an unsigned short in little endian byte order.
		/// </summary>
		/// <returns>Returns the value read.</returns>
		/// <exception cref="IOException">
		/// An i/o error occurs.
		/// </exception>
		/// <exception cref="EndOfStreamException">
		/// The file ends prematurely
		/// </exception>
		ushort ReadLeUshort()
		{
			return unchecked((ushort)((ushort)baseStream_.ReadByte() | (ushort)(baseStream_.ReadByte() << 8)));
		}

		/// <summary>
		/// Read a uint in little endian byte order.
		/// </summary>
		/// <returns>Returns the value read.</returns>
		/// <exception cref="IOException">
		/// An i/o error occurs.
		/// </exception>
		/// <exception cref="System.IO.EndOfStreamException">
		/// The file ends prematurely
		/// </exception>
		uint ReadLeUint()
		{
			return (uint)(ReadLeUshort() | (ReadLeUshort() << 16));
		}

		ulong ReadLeUlong()
		{
			return ReadLeUint() | (ReadLeUint() << 32);
		}

		
		// NOTE this returns the offset of the first byte after the signature.
		long LocateBlockWithSignature(int signature, long endLocation, int minimumBlockSize, int maximumVariableData)
		{
			using ( ZipHelperStream les = new ZipHelperStream(baseStream_) )
			{
				return les.LocateBlockWithSignature(signature, endLocation, minimumBlockSize, maximumVariableData);
			}
		}
		
		/// <summary>
		/// Search for and read the central directory of a zip file filling the entries
		/// array.  This is called exactly once by the constructors.
		/// </summary>
		/// <exception cref="System.IO.IOException">
		/// An i/o error occurs.
		/// </exception>
		/// <exception cref="ICSharpCode.SharpZipLib.Zip.ZipException">
		/// The central directory is malformed or cannot be found
		/// </exception>
		void ReadEntries()
		{
			// Search for the End Of Central Directory.  When a zip comment is
			// present the directory may start earlier.
			// 
			// TODO: The search is limited to 64K which is the maximum size of a trailing comment field to aid speed.
			// This should be compatible with both SFX and ZIP files but has only been tested for Zip files
			// Need to confirm this is valid in all cases.
			// Could also speed this up by reading memory in larger blocks.			

			if (baseStream_.CanSeek == false) {
				throw new ZipException("ZipFile stream must be seekable");
			}
			
			long locatedEndOfCentralDir   = LocateBlockWithSignature(ZipConstants.EndOfCentralDirectorySignature,
			                                                         baseStream_.Length, ZipConstants.EndOfCentralRecordBaseSize, 0xffff);
			if (locatedEndOfCentralDir < 0) {
				throw new ZipException("Cannot find central directory");
			}

			// Read end of central directory record
			ushort thisDiskNumber           = ReadLeUshort();
			ushort startCentralDirDisk      = ReadLeUshort();
			ulong entriesForThisDisk        = ReadLeUshort();
			ulong entriesForWholeCentralDir = ReadLeUshort();
			ulong centralDirSize            = ReadLeUint();
			long offsetOfCentralDir         = ReadLeUint();
			uint commentSize                = ReadLeUshort();
			
			byte[] zipComment = new byte[commentSize]; 

			StreamUtils.ReadFully(baseStream_, zipComment);
			comment_ = ZipConstants.ConvertToString(zipComment); 
			
			bool isZip64 = false;

			// Check if zip64 header information is required.
			if ( (thisDiskNumber == 0xffff) ||
				(startCentralDirDisk == 0xffff) ||
				(entriesForThisDisk == 0xffff) ||
				(entriesForWholeCentralDir == 0xffff) ||
				(centralDirSize == 0xffffffff) ||
				(offsetOfCentralDir == 0xffffffff) )
			{
				isZip64 = true;

				long offset = LocateBlockWithSignature(ZipConstants.Zip64CentralDirLocatorSignature, locatedEndOfCentralDir, 0, 0x1000);
				if ( offset < 0 )
				{
					throw new ZipException("Cannot find Zip64 locator");
				}

				// number of the disk with the start of the zip64 end of central directory 4 bytes 
				// relative offset of the zip64 end of central directory record 8 bytes 
				// total number of disks 4 bytes 
				uint startDisk64 = ReadLeUint();
				ulong offset64 = ReadLeUlong();
				uint totalDisks = ReadLeUint();

				baseStream_.Position = (long)offset64;
				long sig64 = ReadLeUint();

				if ( sig64 != ZipConstants.Zip64CentralFileHeaderSignature )
				{
					throw new ZipException(string.Format("Invalid Zip64 Central directory signature at {0:X}", offset64));
				}

				// NOTE: Record size = SizeOfFixedFields + SizeOfVariableData - 12.
				ulong recordSize = ( ulong )ReadLeUlong();
				int versionMadeBy = ReadLeUshort();
				int versionToExtract = ReadLeUshort();
				uint thisDisk = ReadLeUint();
				uint centralDirDisk = ReadLeUint();
				entriesForThisDisk = ReadLeUlong();
				entriesForWholeCentralDir = ReadLeUlong();
				centralDirSize = ReadLeUlong();
				offsetOfCentralDir = (long)ReadLeUlong();

				// NOTE: zip64 extensible data sector (variable size) is ignored.
			}
			
			entries_ = new ZipEntry[entriesForThisDisk];
			
			// SFX support, find the offset of the first entry vis the start of the stream
			// This applies to Zip files that are appended to the end of an SFX stub.
			// Zip files created by some archivers have the offsets altered to reflect the true offsets
			// and so dont require any adjustment here...
			if ( !isZip64 && (offsetOfCentralDir < locatedEndOfCentralDir - (4 + (long)centralDirSize)) ) {
				offsetOfFirstEntry = locatedEndOfCentralDir - (4 + (long)centralDirSize + offsetOfCentralDir);
				if (offsetOfFirstEntry <= 0) {
					throw new ZipException("Invalid SFX file");
				}
			}

			baseStream_.Seek(offsetOfFirstEntry + offsetOfCentralDir, SeekOrigin.Begin);
			
			for (ulong i = 0; i < entriesForThisDisk; i++) {
				if (ReadLeUint() != ZipConstants.CentralHeaderSignature) {
					throw new ZipException("Wrong Central Directory signature");
				}
				
				int versionMadeBy      = ReadLeUshort();
				int versionToExtract   = ReadLeUshort();
				int bitFlags           = ReadLeUshort();
				int method             = ReadLeUshort();
				uint dostime           = ReadLeUint();
				uint crc               = ReadLeUint();
				uint csize             = ReadLeUint();
				uint size              = ReadLeUint();
				int nameLen            = ReadLeUshort();
				int extraLen           = ReadLeUshort();
				int commentLen         = ReadLeUshort();
				
				int diskStartNo        = ReadLeUshort();  // Not currently used
				int internalAttributes = ReadLeUshort();  // Not currently used

				uint externalAttributes = ReadLeUint();
				long offset            = ReadLeUint();
				
				byte[] buffer = new byte[Math.Max(nameLen, commentLen)];
				
				StreamUtils.ReadFully(baseStream_, buffer, 0, nameLen);
				string name = ZipConstants.ConvertToString(buffer, nameLen);
				
				ZipEntry entry = new ZipEntry(name, versionToExtract, versionMadeBy, (CompressionMethod)method);
				entry.Crc = crc & 0xffffffffL;
				entry.Size = size & 0xffffffffL;
				entry.CompressedSize = csize & 0xffffffffL;
				entry.Flags = bitFlags;
				entry.DosTime = (uint)dostime;
				
				if (extraLen > 0) {
					byte[] extra = new byte[extraLen];
					StreamUtils.ReadFully(baseStream_, extra);
					entry.ExtraData = extra;
				}
				
				if (commentLen > 0) {
					StreamUtils.ReadFully(baseStream_, buffer, 0, commentLen);
					entry.Comment = ZipConstants.ConvertToString(buffer, commentLen);
				}
				
				entry.ZipFileIndex           = (long)i;
				entry.Offset                 = offset;
				entry.ExternalFileAttributes = (int)externalAttributes;
				
				entries_[i] = entry;
			}
		}
		
		/// <summary>
		/// Returns an enumerator for the Zip entries in this Zip file.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// The Zip file has been closed.
		/// </exception>
		public IEnumerator GetEnumerator()
		{
			if (entries_ == null) {
				throw new InvalidOperationException("ZipFile has closed");
			}
			
			return new ZipEntryEnumeration(entries_);
		}
		
		/// <summary>
		/// Return the index of the entry with a matching name
		/// </summary>
		/// <param name="name">Entry name to find</param>
		/// <param name="ignoreCase">If true the comparison is case insensitive</param>
		/// <returns>The index position of the matching entry or -1 if not found</returns>
		/// <exception cref="InvalidOperationException">
		/// The Zip file has been closed.
		/// </exception>
		public int FindEntry(string name, bool ignoreCase)
		{
			if (entries_ == null) {
				throw new InvalidOperationException("ZipFile has been closed");
			}
			
			for (int i = 0; i < entries_.Length; i++) {
				if (string.Compare(name, entries_[i].Name, ignoreCase) == 0) {
					return i;
				}
			}
			return -1;
		}
		
		/// <summary>
		/// Indexer property for ZipEntries
		/// </summary>
		[System.Runtime.CompilerServices.IndexerNameAttribute("EntryByIndex")]
		public ZipEntry this[int index] {
			get {
				return (ZipEntry) entries_[index].Clone();	
			}
		}
		
		/// <summary>
		/// Searches for a zip entry in this archive with the given name.
		/// String comparisons are case insensitive
		/// </summary>
		/// <param name="name">
		/// The name to find. May contain directory components separated by slashes ('/').
		/// </param>
		/// <returns>
		/// A clone of the zip entry, or null if no entry with that name exists.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// The Zip file has been closed.
		/// </exception>
		public ZipEntry GetEntry(string name)
		{
			if (entries_ == null) {
				throw new InvalidOperationException("ZipFile has been closed");
			}
			
			int index = FindEntry(name, true);
			return index >= 0 ? (ZipEntry) entries_[index].Clone() : null;
		}
		
		/// <summary>
		/// Test an archive for integrity/validity
		/// </summary>
		/// <param name="testData">Perform low level data Crc check</param>
		/// <returns>true iff the test passes, false otherwise</returns>
		public bool TestArchive(bool testData)
		{
			long errorCount = 0;
			HeaderTest test = testData ? (HeaderTest.Header | HeaderTest.Extract) : HeaderTest.Header;

			try {
				for (int i = 0; i < Count; ++i) {
					long offset = TestLocalHeader(this[i], test);
					if ( testData && this[i].IsFile ) {
						Stream entryStream = this.GetInputStream(this[i]);
						
						// TODO: ZipFile.TestArchive events for updating info, recording errors etc
						Crc32 crc = new Crc32();
						byte[] buffer = new byte[4096];
						int bytesRead;
						while ((bytesRead = entryStream.Read(buffer, 0, buffer.Length)) > 0) {
							crc.Update(buffer, 0, bytesRead);
						}
	
						if (this[i].Crc != crc.Value) {
							errorCount += 1;
							
							// TODO: ZipFile.TestArchive failure event here....
							break; // TODO: ZipFile.TestArchive Allow processing of all entries despite errors when events are in place to inform of individual problems.
						}
					}
				}
			}
			catch {
				errorCount += 1;
			}
			return (errorCount == 0);
		}

		[Flags]
		enum HeaderTest
		{
			Extract = 0x01,     // Check that this header represents an entry that can be extracted
			Header  = 0x02,     // Check that this header is valid
		}

	
		/// <summary>
		/// Test the local header against that provided from the central directory
		/// </summary>
		/// <param name="entry">
		/// The entry to test against
		/// </param>
		/// <param name="tests">The type of test to carry out.</param>
		/// <returns>The offset of the entries data in the file</returns>
		long TestLocalHeader(ZipEntry entry, HeaderTest tests)
		{
			lock(baseStream_) 
			{
				bool testHeader = (tests & HeaderTest.Header) != 0;
				bool testData = (tests & HeaderTest.Extract) != 0;

				// TODO: Check for chunks of the file that arent referenced by a header in central directory
				// The 'Corrina Johns' test.
				
				baseStream_.Seek(offsetOfFirstEntry + entry.Offset, SeekOrigin.Begin);
				if ((int)ReadLeUint() != ZipConstants.LocalHeaderSignature) {
					throw new ZipException(string.Format("Wrong local header signature @{0:X}", offsetOfFirstEntry + entry.Offset));
				}

				short extractVersion = ( short )ReadLeUshort();
				short localFlags = ( short )ReadLeUshort();
				short compressionMethod = ( short )ReadLeUshort();
				short fileTime = ( short )ReadLeUshort();
				short fileDate = ( short )ReadLeUshort();
				uint crcValue = ReadLeUint();
				long size = ReadLeUint();
				long compressedSize = ReadLeUint();
				int storedNameLength = ReadLeUshort();
				int extraDataLength = ReadLeUshort();

				if ( testData )
				{
					if ( entry.IsFile )
					{
						if ( !entry.IsCompressionMethodSupported() )
						{
							throw new ZipException("Compression method not supported");
						}

						if ( (extractVersion > ZipConstants.VersionMadeBy)
							|| ((extractVersion > 20) && (extractVersion < ZipConstants.VersionZip64)) )
						{
							throw new ZipException(string.Format("Version required to extract this entry not supported ({0})", extractVersion));
						}

						if ( (localFlags & ( int )(GeneralBitFlags.Patched | GeneralBitFlags.StrongEncryption | GeneralBitFlags.EnhancedCompress | GeneralBitFlags.HeaderMasked)) != 0 )
						{
							throw new ZipException("The library doesnt support the zip version required to extract this entry");
						}
					}
				}


				if ( testHeader )
				{
					// Version to extract is a known value
					if ( (extractVersion != 10) &&
						(extractVersion != 20) &&
						(extractVersion != 21) &&
						(extractVersion != 25) &&
						(extractVersion != 27) &&
						(extractVersion != 45) &&
						(extractVersion != 46) &&
						(extractVersion != 50) &&
						(extractVersion != 51) &&
						(extractVersion != 61) &&
						(extractVersion != 62)
						)
					{
						throw new ZipException(string.Format("Version required to extract this entry is invalid ({0})", extractVersion));
					}

					// Local entry flags dont have reserved bit set on.
					if ( (localFlags & ( int )GeneralBitFlags.Reserved) != 0 )
					{
						throw new ZipException("Reserved bit flag cannot bet set.");
					}

					// Encryption requires extract version >= 20
					if ( ((localFlags & ( int )GeneralBitFlags.Encrypted) != 0) && (extractVersion < 20) )
					{
						throw new ZipException(string.Format("Version required to extract this entry is too low for encryption ({0})", extractVersion));
					}

					// Strong encryption requires encryption flag to be set and extract version >= 50.
					if ( (localFlags & (int)GeneralBitFlags.StrongEncryption) != 0 )
					{
						if ( (localFlags & (int)GeneralBitFlags.Encrypted) == 0 )
						{
							throw new ZipException("Strong encryption flag set but encryption flag is not set");
						}

						if ( extractVersion < 50 )
						{
							throw new ZipException(string.Format("Version required to extract this entry is too low for encryption ({0})", extractVersion));
						}
					}

					// Patched entries require extract version >= 27
					if ( ((localFlags & ( int )GeneralBitFlags.Patched) != 0) && (extractVersion < 27) )
					{
						throw new ZipException(string.Format("Patched data requires higher version than ({0})", extractVersion));
					}

					// Central header flags match local entry flags.
					if ( localFlags != entry.Flags )
					{
						throw new ZipException("Central header/local header flags mismatch");
					}

					// Central header compression method matches local entry
					if ( entry.CompressionMethod != ( CompressionMethod )compressionMethod )
					{
						throw new ZipException("Central header/local header compression method mismatch");
					}

					// Strong encryption and extract version match
					if ( (localFlags & ( int )GeneralBitFlags.StrongEncryption) != 0 )
					{
						if ( extractVersion < 62 )
						{
							throw new ZipException("Strong encryption flag set but version not high enough");
						}
					}

					if ( (localFlags & ( int )GeneralBitFlags.HeaderMasked) != 0 )
					{
						if ( (fileTime != 0) || (fileDate != 0) )
						{
							throw new ZipException("Header masked set but date/time values non-zero");
						}
					}

					if ( (localFlags & ( int )GeneralBitFlags.Descriptor) == 0 )
					{
						if ( crcValue != (uint)entry.Crc )
						{
							throw new ZipException("Central header/local header crc mismatch");
						}
					}

					// Crc valid for empty entry.
					if ( (size == 0) && (compressedSize == 0) )
					{
						if ( crcValue != 0 )
						{
							throw new ZipException("Invalid CRC for empty entry");
						}
					}

					// TODO: make test more correct...  can't compare lengths as was done originally as this can fail for MBCS strings
					// Assuming a code page at this point is not valid?  Best is to store the name length in the ZipEntry probably
					if ( entry.Name.Length > storedNameLength )
					{
						throw new ZipException("File name length mismatch");
					}

					byte[] nameData = new byte[storedNameLength];
					StreamUtils.ReadFully(baseStream_, nameData);

					string localName = ZipConstants.ConvertToString(nameData);

					// Central directory and local entry name match
					if ( localName != entry.Name )
					{
						throw new ZipException("Central header and local header file name mismatch");
					}

					// Directories have zero size.
					if ( entry.IsDirectory )
					{
						if ( (compressedSize != 0) || (size != 0) )
						{
							// Probably incorrect for some compressors
							throw new ZipException("Directory cannot have size");
						}
					}

					// TODO: Check for illegal paths, non zip format names.
					// Non-zip formats would be a warning?

					byte[] data = new byte[extraDataLength];
					StreamUtils.ReadFully(baseStream_, data);
					ZipExtraData ed = new ZipExtraData(data);

					// Extra data / zip64 checks
					if ( ed.Find(1) )
					{
						// Zip64 extra data but extract version too low
						if ( extractVersion < ZipConstants.VersionZip64 )
						{
							throw new ZipException(
								string.Format("Extra data contains Zip64 information but version {0}.{1} is not high enough",
								extractVersion / 10, extractVersion % 10));
						}

						// Zip64 extra data but size fields dont indicate its required.
						if ( (( uint )size != uint.MaxValue) && (( uint )compressedSize != uint.MaxValue) )
						{
							throw new ZipException("Entry sizes not correct for Zip64");
						}

						size = ed.ReadLong();
						compressedSize = ed.ReadLong();
					}
					else
					{
						// No zip64 extra data but entry requires it.
						if ( (extractVersion >= ZipConstants.VersionZip64) &&
							((( uint )size == uint.MaxValue) || (( uint )compressedSize == uint.MaxValue)) )
						{
							throw new ZipException("Required Zip64 extended information missing");
						}
					}
				}
					
				int extraLen = storedNameLength + ReadLeUshort();
				return offsetOfFirstEntry + entry.Offset + ZipConstants.LocalHeaderBaseSize + extraLen;
			}
		}
		
		/// <summary>
		/// Locate the data for a given entry.
		/// </summary>
		/// <returns>
		/// The start offset of the data.
		/// </returns>
		/// <exception cref="System.IO.EndOfStreamException">
		/// The stream ends prematurely
		/// </exception>
		/// <exception cref="ICSharpCode.SharpZipLib.Zip.ZipException">
		/// The local header signature is invalid, the entry and central header file name lengths are different
		/// or the local and entry compression methods dont match
		/// </exception>
		long LocateEntry(ZipEntry entry)
		{
			return TestLocalHeader(entry, HeaderTest.Extract);
		}
		
		void CheckClassicPassword(CryptoStream classicCryptoStream, ZipEntry entry)
		{
			byte[] cryptbuffer = new byte[ZipConstants.CryptoHeaderSize];
			StreamUtils.ReadFully(classicCryptoStream, cryptbuffer);

			if ((entry.Flags & (int)GeneralBitFlags.Descriptor) == 0) {
				if (cryptbuffer[ZipConstants.CryptoHeaderSize - 1] != (byte)(entry.Crc >> 24)) {
					throw new ZipException("Invalid password");
				}
			}
			else {
				if (cryptbuffer[ZipConstants.CryptoHeaderSize - 1] != (byte)((entry.DosTime >> 8) & 0xff)) {
					throw new ZipException("Invalid password");
				}
			}
		}

		Stream CreateAndInitDecryptionStream(Stream baseStream, ZipEntry entry)
		{
			CryptoStream result = null;

			if (entry.Version < ZipConstants.VersionStrongEncryption 
				|| (entry.Flags & (int)GeneralBitFlags.StrongEncryption) == 0) {
				PkzipClassicManaged classicManaged = new PkzipClassicManaged();

				OnKeysRequired(entry.Name);
				if (HaveKeys == false) {
					throw new ZipException("No password available for encrypted stream");
				}

				result = new CryptoStream(baseStream, classicManaged.CreateDecryptor(key, iv), CryptoStreamMode.Read);
				CheckClassicPassword(result, entry);
			}
			else {
				throw new ZipException("Decryption method not supported");
			}

			return result;
		}

		void WriteEncryptionHeader(Stream stream, long crcValue)
		{
			byte[] cryptBuffer = new byte[ZipConstants.CryptoHeaderSize];
			Random rnd = new Random();
			rnd.NextBytes(cryptBuffer);
			cryptBuffer[11] = (byte)(crcValue >> 24);
			stream.Write(cryptBuffer, 0, cryptBuffer.Length);
		}

		Stream CreateAndInitEncryptionStream(Stream baseStream, ZipEntry entry)
		{
			CryptoStream result = null;
			if (entry.Version < ZipConstants.VersionStrongEncryption
			    || (entry.Flags & (int)GeneralBitFlags.StrongEncryption) == 0) {
				PkzipClassicManaged classicManaged = new PkzipClassicManaged();

				OnKeysRequired(entry.Name);
				if (HaveKeys == false) {
					throw new ZipException("No password available for encrypted stream");
				}

				result = new CryptoStream(baseStream, classicManaged.CreateEncryptor(key, iv), CryptoStreamMode.Write);
				if (entry.Crc < 0 || (entry.Flags & 8) != 0) {
					WriteEncryptionHeader(result, entry.DosTime << 16);
				}
				else {
					WriteEncryptionHeader(result, entry.Crc);
				}
			}
			return result;
		}

		/// <summary>
		/// Gets an output stream for the specified <see cref="ZipEntry"/>
		/// </summary>
		/// <param name="entry">The entry to get an outputstream for.</param>
		/// <param name="fileName"></param>
		/// <returns>The output stream obtained for the entry.</returns>
		Stream GetOutputStream(ZipEntry entry, string fileName)
		{
			baseStream_.Seek(0, SeekOrigin.End);
			Stream result = File.OpenWrite(fileName);
		
			if (entry.IsCrypted == true)
			{
				result = CreateAndInitEncryptionStream(result, entry);
			}
		
			switch (entry.CompressionMethod) 
			{
				case CompressionMethod.Stored:
					break;
		
				case CompressionMethod.Deflated:
					result = new DeflaterOutputStream(result);
					break;
					
				default:
					throw new ZipException("Unknown compression method " + entry.CompressionMethod);
			}
			return result;
		}

		/// <summary>
		/// Creates an input stream reading the given zip entry as
		/// uncompressed data.  Normally zip entry should be an entry
		/// returned by GetEntry().
		/// </summary>
		/// <returns>
		/// the input stream.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// The ZipFile has already been closed
		/// </exception>
		/// <exception cref="ICSharpCode.SharpZipLib.Zip.ZipException">
		/// The compression method for the entry is unknown
		/// </exception>
		/// <exception cref="IndexOutOfRangeException">
		/// The entry is not found in the ZipFile
		/// </exception>
		public Stream GetInputStream(ZipEntry entry)
		{
			if (entries_ == null) {
				throw new InvalidOperationException("ZipFile has closed");
			}
			
			long index = entry.ZipFileIndex;
			if ( (index < 0) || (index >= entries_.Length) || (entries_[index].Name != entry.Name) ) {
				index = FindEntry(entry.Name, true);
				if (index < 0) {
					throw new IndexOutOfRangeException();
				}
			}
			return GetInputStream(index);			
		}
		
		/// <summary>
		/// Creates an input stream reading a zip entry
		/// </summary>
		/// <param name="entryIndex">The index of the entry to obtain an input stream for.</param>
		/// <returns>
		/// An input stream.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// The ZipFile has already been closed
		/// </exception>
		/// <exception cref="ICSharpCode.SharpZipLib.Zip.ZipException">
		/// The compression method for the entry is unknown
		/// </exception>
		/// <exception cref="IndexOutOfRangeException">
		/// The entry is not found in the ZipFile
		/// </exception>
		public Stream GetInputStream(long entryIndex)
		{
			if ( entries_ == null ) {
				throw new InvalidOperationException("ZipFile has closed");
			}
			
			long start = LocateEntry(entries_[entryIndex]);
			CompressionMethod method = entries_[entryIndex].CompressionMethod;
			Stream istr = new PartialInputStream(baseStream_, start, entries_[entryIndex].CompressedSize);

			if (entries_[entryIndex].IsCrypted == true) {
				istr = CreateAndInitDecryptionStream(istr, entries_[entryIndex]);
				if (istr == null) {
					throw new ZipException("Unable to decrypt this entry");
				}
			}

			switch (method) {
				case CompressionMethod.Stored:
					return istr;
				case CompressionMethod.Deflated:
					return new InflaterInputStream(istr, new Inflater(true));
				default:
					throw new ZipException("Unsupported compression method " + method);
			}
		}

		
		/// <summary>
		/// Gets the comment for the zip file.
		/// </summary>
		public string ZipFileComment {
			get {
				return comment_;
			}
		}
		
		/// <summary>
		/// Gets the name of this zip file.
		/// </summary>
		public string Name {
			get {
				return name_;
			}
		}
		
		/// <summary>
		/// Gets the number of entries in this zip file.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// The Zip file has been closed.
		/// </exception>
		[Obsolete("Use Count instead")]
		public int Size {
			get {
				if (entries_ != null) {
					return entries_.Length;
				} else {
					throw new InvalidOperationException("ZipFile is closed");
				}
			}
		}
		
		/// <summary>
		/// Get the count of entries in this zip file.
		/// </summary>
		public long Count {
			get {
				if (entries_ != null) {
					return entries_.Length;
				}
				else {
					throw new InvalidOperationException("ZipFile is closed");
				}
			}
		}
		
		#region Updates

		// Issues:
		// In what order are add, update etc applied?
		//   Order specified, could end up being asked to delete what you have just added.
		//   Deletes, then adds then modifies?
		// Multiple adds for same files become one add?
		// Delete of non-existant entry ignored?
		// Modify with no actual modifications ignored?
		//
		// Wildcard handling of filenames?  (would be nice)
		//
		// Modifies etc how to specific changes?
		// Safety mode and handling of files during updates

		enum UpdateCommand
		{
			Copy,       // Copy original file contents.
			Add,        // Add a new file to the archive.
			Modify      // Change encryption, compression, attributes, name, time etc, of an existing file.
		}

		/// <summary>
		/// Represents a pending update to a Zip file.
		/// </summary>
		class ZipUpdate
		{
			#region Constructors
			public ZipUpdate(UpdateCommand command, string filename, string entryName, CompressionMethod compressionMethod)
			{
				command_ = command;
				entry_ = new ZipEntry(entryName);
#if FORCE_ZIP64
				entry_.versionToExtract = ZipConstants.VersionZip64;
#endif
				entry_.CompressionMethod = compressionMethod;
				filename_ = filename;
			}

			public ZipUpdate(UpdateCommand command, string filename, string entryName)
			{
				command_ = command;
				entry_ = new ZipEntry(entryName);
				filename_ = filename;
			}

			public ZipUpdate(ZipEntry target, ZipEntry source)
			{
				command_ = UpdateCommand.Modify;
				entry_ = ( ZipEntry )target.Clone();
				sourceEntry_ = ( ZipEntry )source.Clone();
			}

			public ZipUpdate(UpdateCommand command, ZipEntry entry)
			{
				command_ = command;
				entry_ = ( ZipEntry )entry.Clone();
			}

			public ZipUpdate(ZipEntry entry)
				: this(UpdateCommand.Copy, entry)
			{
				// Do nothing.
			}
			#endregion

			public ZipEntry Entry
			{
				get { return entry_; }
			}

			public ZipEntry SourceEntry
			{
				get { return sourceEntry_; }
			}

			public UpdateCommand Command
			{
				get { return command_; }
			}

			public string Filename
			{
				get { return filename_; }
			}

			public long SizePatchOffset
			{
				get { return sizePatchOffset_; }
				set { sizePatchOffset_ = value; }
			}

			public long CrcPatchOffset
			{
				get { return crcPatchOffset_; }
				set { crcPatchOffset_ = value; }
			}

			#region Instance Fields
			ZipEntry entry_;
			ZipEntry sourceEntry_;
			UpdateCommand command_;
			string filename_;
			long sizePatchOffset_ = -1;
			long crcPatchOffset_ = -1;
			#endregion
	   }

		ArrayList updates_ = null;

		bool contentsEdited_ = false;

		public void BeginUpdate()
		{
			if ( name_ == null )
			{
				throw new ZipException("Cannot update when filename is not known");
			}

			updates_ = new ArrayList();

			if ( entries_ != null )
			{
				foreach ( ZipEntry entry in entries_ )
				{
					updates_.Add(new ZipUpdate(entry));
				}
			}

			contentsEdited_ = false;
			commentEdited_ = false;
			newComment_ = null;
		}

		public void AbortUpdate()
		{
			updates_ = null;
		}

		/// <summary>
		/// Write an unsigned short in little endian byte order.
		/// </summary>
		private void WriteLeShort(int value)
		{
			baseStream_.WriteByte(( byte )(value & 0xff));
			baseStream_.WriteByte(( byte )((value >> 8) & 0xff));
		}

		private void WriteLeUshort(ushort value)
		{
			baseStream_.WriteByte(( byte )(value & 0xff));
			baseStream_.WriteByte(( byte )(value >> 8));
		}

		/// <summary>
		/// Write an int in little endian byte order.
		/// </summary>
		private void WriteLeInt(int value)
		{
			WriteLeShort(value);
			WriteLeShort(value >> 16);
		}

		private void WriteLeUint(uint value)
		{
			WriteLeUshort((ushort)(value & 0xffff));
			WriteLeUshort((ushort)(value >> 16));
		}

		/// <summary>
		/// Write a long in little endian byte order.
		/// </summary>
		private void WriteLeLong(long value)
		{
			WriteLeInt(( int )(value & 0xffffffff));
			WriteLeInt(( int )(value >> 32));
		}

		private void WriteLeULong(ulong value)
		{
			WriteLeUint(( uint )(value & 0xffffffff));
			WriteLeUint(( uint )(value >> 32));
		}

#warning TODO: Move ZipFile.WriteLocalEntryHeader to ZipHelperStream.
		void WriteLocalEntryHeader(ZipUpdate update)
		{
			ZipEntry entry = update.Entry;

			// TODO: Local offset will require adjusting for multi-disk zip files?
			entry.Offset = baseStream_.Position;

			// TODO: Does this make sense?
			if ( !entry.IsCrypted && ((entry.Flags & ( int )GeneralBitFlags.Descriptor) != 0) )
			{
				entry.Flags &= ~(( int )GeneralBitFlags.Descriptor);
			}

			// Write the local file header
			WriteLeInt(ZipConstants.LocalHeaderSignature);

			WriteLeShort(entry.Version);
			WriteLeShort(entry.Flags);
			WriteLeShort(( byte )entry.CompressionMethod);

			WriteLeInt(( int )entry.DosTime);

			if ( !entry.HasCrc )
			{
				// Note patch address for updating later.
				update.CrcPatchOffset = baseStream_.Position;
				WriteLeInt(( int )0);
			}
			else
			{
				WriteLeInt(( int )entry.Crc);
			}

			// TODO: How to handle situation where size/compressed size is not known?
			// Could make all entries Zip64
			// Or user could choose.  Currently user can choose 
			if ( entry.LocalHeaderRequiresZip64 == true )
			{
				WriteLeInt(-1);
				WriteLeInt(-1);
			}
			else
			{
				if ( (entry.CompressedSize < 0) || (entry.Size < 0) )
				{
					update.SizePatchOffset = baseStream_.Position;
				}
				WriteLeInt(entry.IsCrypted ? ( int )entry.CompressedSize + ZipConstants.CryptoHeaderSize : ( int )entry.CompressedSize);
				WriteLeInt(( int )entry.Size);
			}

			byte[] name = ZipConstants.ConvertToArray(entry.Name);

			if ( name.Length > 0xFFFF )
			{
				throw new ZipException("Entry name too long.");
			}

			ZipExtraData ed = new ZipExtraData(entry.ExtraData);

			if ( entry.LocalHeaderRequiresZip64 )
			{
				ed.StartNewEntry();

				// Local entry header always includes size and compressed size.
				ed.AddLeLong(entry.Size);
				ed.AddLeLong(entry.CompressedSize);
				ed.AddNewEntry(1);
			}
			else
			{
				ed.Delete(1);
			}

			entry.ExtraData = ed.GetEntryData();

			WriteLeShort(name.Length);
			WriteLeShort(entry.ExtraData.Length);

			baseStream_.Write(name, 0, name.Length);

			if ( entry.LocalHeaderRequiresZip64 )
			{
				if ( !ed.Find(1) )
				{
					throw new ZipException("Internal error cannot find extra data");
				}

				update.SizePatchOffset = baseStream_.Position + ed.CurrentReadIndex;
			}

			baseStream_.Write(entry.ExtraData, 0, entry.ExtraData.Length);
		}

		int WriteCentralDirectoryHeader(ZipEntry entry)
		{
			if ( !entry.IsCrypted && ((entry.Flags & ( int )GeneralBitFlags.Descriptor) != 0) )
			{
				entry.Flags &= ~(( int )GeneralBitFlags.Descriptor);
			}

			// Write the local file header
			WriteLeInt(ZipConstants.CentralHeaderSignature);

			// Version made by
			WriteLeShort(entry.Version);

			// Version required to extract
			WriteLeShort(entry.Version);

			WriteLeShort(entry.Flags);
			WriteLeShort((byte)entry.CompressionMethod);
			WriteLeInt((int)entry.DosTime);
			WriteLeInt((int)entry.Crc);

			if ( entry.LocalHeaderRequiresZip64 == true )
			{
				WriteLeInt(-1);
				WriteLeInt(-1);
			}
			else
			{
				WriteLeInt(entry.IsCrypted ? (int)entry.CompressedSize + ZipConstants.CryptoHeaderSize : ( int )entry.CompressedSize);
				WriteLeInt((int)entry.Size);
			}

			byte[] name = ZipConstants.ConvertToArray(entry.Name);

			if ( name.Length > 0xFFFF )
			{
				throw new ZipException("Entry name is too long.");
			}

			WriteLeShort(name.Length);

			// Central header extra data is different to local header version so regenerate.
			ZipExtraData ed = new ZipExtraData(entry.ExtraData);

			if ( entry.CentralHeaderRequiresZip64 )
			{
				ed.StartNewEntry();
#if !FORCE_ZIP64
				if ( entry.Size >= 0xffffffff )
#endif
				{
					ed.AddLeLong(entry.Size);
				}

#if !FORCE_ZIP64
				if ( entry.CompressedSize >= 0xffffffff )
#endif
				{
					ed.AddLeLong(entry.CompressedSize);
				}

				if ( entry.Offset >= 0xffffffff )
				{
					ed.AddLeLong(entry.Offset);
				}

				// Number of disk on which this file starts isnt supported and is never written here.
				ed.AddNewEntry(1);
			}
			else
			{
				// Should already be done when local header was added.
				ed.Delete(1);
			}

			byte[] centralExtraData = ed.GetEntryData();

			WriteLeShort(centralExtraData.Length);
			WriteLeShort(entry.Comment != null ? entry.Comment.Length : 0);

			WriteLeShort(0);	// disk number
			WriteLeShort(0);	// internal file attr

			// External file attributes...
			if ( entry.ExternalFileAttributes != -1 )
			{
				WriteLeInt(entry.ExternalFileAttributes);
			}
			else
			{
				if ( entry.IsDirectory )
				{
					WriteLeInt(16);
				}
				else
				{
					WriteLeInt(0);
				}
			}

			if ( entry.Offset >= 0xffffffff )
			{
				WriteLeUint(0xffffffff);
			}
			else
			{
				WriteLeInt(( int )entry.Offset);
			}

			baseStream_.Write(name, 0, name.Length);

			if ( centralExtraData.Length > 0 )
			{
				baseStream_.Write(centralExtraData, 0, centralExtraData.Length);
			}

			byte[] rawComment = entry.Comment != null ? Encoding.ASCII.GetBytes(entry.Comment) : new byte[0];
			baseStream_.Write(rawComment, 0, rawComment.Length);

			return ZipConstants.CentralHeaderBaseSize + name.Length + centralExtraData.Length + rawComment.Length;
		}

		byte[] copyBuffer = null;

		/// <summary>
		/// Get a raw memory buffer.
		/// </summary>
		/// <returns>Returns a raw memory buffer.</returns>
		byte[] GetBuffer()
		{
			if ( copyBuffer == null )
			{
				copyBuffer = new byte[4096];
			}
			return copyBuffer;
		}

		void CopyBytes(ZipUpdate update, Stream dest, Stream source, long bytesToCopy)
		{
			Crc32 crc = new Crc32();

			byte[] buffer = GetBuffer();

			long targetBytes = bytesToCopy;
			long totalBytesRead = 0;

			update.Entry.CompressedSize = bytesToCopy;

			int bytesRead;
			do
			{
				int readSize = buffer.Length;

				if ( bytesToCopy < readSize )
				{
					readSize = (int)bytesToCopy;
				}

				bytesRead = source.Read(buffer, 0, readSize);
				crc.Update(buffer, 0, bytesRead);
				dest.Write(buffer, 0, bytesRead);
				bytesToCopy -= bytesRead;
				totalBytesRead += bytesRead;
			}
			while ( (bytesRead > 0) && (bytesToCopy > 0) );

			if ( totalBytesRead != targetBytes )
			{
				throw new ZipException(string.Format("Failed to copy bytes expected {0} read {1}", targetBytes, totalBytesRead));
			}

			// TODO: Will require a patch if the crc calculated is different to whats there already!!!
			update.Entry.Crc = crc.Value;
		}

		ZipNameTransform updateNameTransform = new ZipNameTransform(true);

		int FindExistingUpdate(ZipEntry entry)
		{
			// TODO: Handling of relative\absolute paths etc.
			int result = -1;
			string convertedName = updateNameTransform.TransformFile(entry.Name);
			for ( int index = 0; index < updates_.Count; ++index )
			{
				ZipUpdate zu = ( ZipUpdate )updates_[index];
				if ( (zu.Entry.ZipFileIndex == entry.ZipFileIndex) && (string.Compare(convertedName, zu.Entry.Name, true) == 0) )
				{
					result = index;
					break;
				}
			}
			return result;
		}

		int FindExistingUpdate(string fileName)
		{
			// TODO: Handling of relative\absolute paths etc.
			int result = -1;
			string convertedName = updateNameTransform.TransformFile(fileName);
			for ( int index = 0; index < updates_.Count; ++index )
			{
				if ( string.Compare(convertedName, (( ZipUpdate )updates_[index]).Entry.Name, true) == 0 )
				{
					result = index;
					break;
				}
			}
			return result;
		}

		string MakeTempFilename(string original)
		{
			// TODO: This should create a zero byte temp file just to be a little more certain
			int counter = 1;
			string newName = original + ".zz_";
			while ( File.Exists(newName) )
			{
				newName = string.Format("{0}.zz{1}_", original, counter);
			}
			return newName;
		}

		/// <summary>
		/// An <see cref="UncompressedStream"/> is a stream that you can write uncompressed data
		/// to and flush, but cannot read, seek or do anything else to.
		/// </summary>
		public class UncompressedStream : Stream
		{
			public UncompressedStream(Stream baseStream)
			{
				baseStream_ = baseStream;
			}

			/// <summary>
			/// Close this stream instance.
			/// </summary>
			public override void Close()
			{
				// Do nothing
			}

			/// <summary>
			/// Gets a value indicating whether the current stream supports reading.
			/// </summary>
			public override bool CanRead
			{
				get
				{
					return false;
				}
			}

			/// <summary>
			/// Write any buffered data to underlying storage.
			/// </summary>
			public override void Flush()
			{
				baseStream_.Flush();
			}

			/// <summary>
			/// Gets a value indicating whether the current stream supports writing.
			/// </summary>
			public override bool CanWrite
			{
				get
				{
					return baseStream_.CanWrite;
				}
			}

			/// <summary>
			/// Gets a value indicating whether the current stream supports seeking.
			/// </summary>
			public override bool CanSeek
			{
				get
				{
					return false;
				}
			}

			/// <summary>
			/// Get the length in bytes of the stream.
			/// </summary>
			public override long Length
			{
				get
				{
					return 0;
				}
			}

			/// <summary>
			/// Gets or sets the position within the current stream.
			/// </summary>
			public override long Position
			{
				get
				{
					return baseStream_.Position;
				}
				
				set
				{
				}
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				return 0;
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				return 0;
			}

			public override void SetLength(long value)
			{
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				baseStream_.Write(buffer, offset, count);
			}

			#region Instance Fields
			Stream baseStream_;
			#endregion
		}

		/// <summary>
		/// Gets an output stream for the specified <see cref="ZipEntry"/>
		/// </summary>
		/// <param name="entry">The entry to get an outputstream for.</param>
		/// <returns>The output stream obtained for the entry.</returns>
		Stream GetOutputStream(ZipEntry entry)
		{
			Stream result = baseStream_;

			if ( entry.IsCrypted == true )
			{
				result = CreateAndInitEncryptionStream(result, entry);
			}

			switch ( entry.CompressionMethod )
			{
				case CompressionMethod.Stored:
					result = new UncompressedStream(result);
					break;

				case CompressionMethod.Deflated:
					DeflaterOutputStream dos = new DeflaterOutputStream(result, new Deflater(9, true));
					dos.IsStreamOwner = false;
					result = dos;
					break;

				default:
					throw new ZipException("Unknown compression method " + entry.CompressionMethod);
			}
			return result;
		}

		void AddEntry(ZipFile workFile, ZipUpdate update)
		{
			workFile.WriteLocalEntryHeader(update);
			long dataStart = workFile.baseStream_.Position;

			if ( update.Entry.IsFile && (update.Filename != null) )
			{
				using ( Stream output = workFile.GetOutputStream(update.Entry) )
				{
					using ( Stream source = File.OpenRead(update.Filename) )
					{
						CopyBytes(update, output, source, source.Length);
					}
				}
			}

			long dataEnd = workFile.baseStream_.Position;
			update.Entry.CompressedSize = dataEnd - dataStart;
		}

		void ModifyEntry(ZipFile workFile, ZipUpdate update)
		{
			workFile.WriteLocalEntryHeader(update);
			long dataStart = workFile.baseStream_.Position;

			// TODO: This is slow if the changes don't effect the data!!
			if ( update.Entry.IsFile && (update.Filename != null) )
			{
				using ( Stream output = workFile.GetOutputStream(update.Entry) )
				{
					using ( Stream source = this.GetInputStream(update.SourceEntry) )
					{
						CopyBytes(update, output, source, source.Length);
					}
				}
			}

			long dataEnd = workFile.baseStream_.Position;
			update.Entry.CompressedSize = dataEnd - dataStart;
		}

		void Reopen()
		{
			this.baseStream_ = File.OpenRead(Name);
			this.ReadEntries();
		}

		void UpdateCommentOnly()
		{
			string tempName = null;

			long baseLength = baseStream_.Length;

			baseStream_.Close();
			baseStream_ = null;

			ZipHelperStream updateFile = null;
			if ( UpdateMode == FileUpdateMode.Safe )
			{
				tempName = Path.GetTempFileName();
				File.Copy(Name, tempName, true);
				updateFile = new ZipHelperStream(tempName);
			}
			else
			{
				updateFile = new ZipHelperStream(Name);
			}

			using ( updateFile )
			{
				long locatedCentralDirOffset = 
					updateFile.LocateBlockWithSignature(ZipConstants.EndOfCentralDirectorySignature, 
														baseLength, ZipConstants.EndOfCentralRecordBaseSize, 0xffff);
				if ( locatedCentralDirOffset < 0 )
				{
					throw new ZipException("Cannot find central directory");
				}

				const int CentralHeaderCommentSizeOffset = 16;
				updateFile.Position += CentralHeaderCommentSizeOffset;

				byte[] rawComment = newComment_.RawComment;

				updateFile.WriteLeShort(rawComment.Length);
				updateFile.Write(rawComment, 0, rawComment.Length);
				updateFile.SetLength(updateFile.Position);
			}

			if ( UpdateMode == FileUpdateMode.Safe )
			{
				File.Delete(Name);
				File.Move(tempName, Name);
			}

			Reopen();
		}

		void RunUpdates()
		{
			// TODO: create temp file on same volume to speed up move later??
			long sizeEntries = 0;
			bool allOk = true;

			// A zip writer class would be better than this slightly confusing use of the same class
			// workFile isnt a true ZipFile instance at any point in its life cycle currently.
			ZipFile workFile = ZipFile.Create(Path.GetTempFileName());

			try
			{
				foreach ( ZipUpdate update in updates_ )
				{
					switch ( update.Command )
					{
						case UpdateCommand.Copy:
							workFile.WriteLocalEntryHeader(update);
							baseStream_.Seek(update.Entry.Offset, SeekOrigin.Begin);
							// TODO: What about data descriptors?  They will require extra logic here...
							CopyBytes(update, workFile.baseStream_, baseStream_, update.Entry.CompressedSize);
							break;

						case UpdateCommand.Add:
							AddEntry(workFile, update);
							break;

						case UpdateCommand.Modify:
							ModifyEntry(workFile, update);
							break;
					}
				}

				long centralDirOffset = workFile.baseStream_.Position;

				foreach ( ZipUpdate update in updates_ )
				{
					sizeEntries += workFile.WriteCentralDirectoryHeader(update.Entry);
				}

				byte[] theComment = (newComment_ != null) ? newComment_.RawComment : ZipConstants.ConvertToArray(comment_);
				using ( ZipHelperStream zhs = new ZipHelperStream(workFile.baseStream_) )
				{
					zhs.WriteEndOfCentralDirectory(updates_.Count, sizeEntries, centralDirOffset, theComment);
				}

				// And now patch entries...
				foreach ( ZipUpdate update in updates_ )
				{
					// If the size of the entry is zero leave the crc as 0 as well.
					// The calculated crc will be all bits on...
					if ( (update.CrcPatchOffset > 0) && (update.Entry.CompressedSize > 0) )
					{
						workFile.baseStream_.Position = update.CrcPatchOffset;
						workFile.WriteLeInt(( int )update.Entry.Crc);
					}

					if ( update.SizePatchOffset > 0 )
					{
						workFile.baseStream_.Position = update.SizePatchOffset;
						if ( update.Entry.LocalHeaderRequiresZip64 )
						{
							workFile.WriteLeLong(update.Entry.Size);
							workFile.WriteLeLong(update.Entry.CompressedSize);
						}
						else
						{
							workFile.WriteLeInt(( int )update.Entry.CompressedSize);
							workFile.WriteLeInt(( int )update.Entry.Size);
						}
					}
				}
			}
			catch
			{
				allOk = false;
			}
			finally
			{
				workFile.Close();
			}

			if ( allOk )
			{
				string moveTempName = MakeTempFilename(Name);
				bool newFileCreated = false;

				try
				{
					this.baseStream_.Close();
					File.Move(Name, moveTempName);
					File.Move(workFile.Name, Name);
					newFileCreated = true;
					File.Delete(moveTempName);
					Reopen();
				}
				catch ( Exception )
				{
					allOk = false;

					// Try to roll back changes...
					if ( !newFileCreated )
					{
						File.Move(moveTempName, Name);
						File.Delete(workFile.Name);
					}
				}
			}
			else
			{
				File.Delete(workFile.Name);
			}

			// Issues:
			// Attempting to add a file that already exists. What if anything happens?
			//   - Old one is deleted
			//
			// Add a file multiple times?
			//   - Last one is added.
			//
			// delete a file multiple times.
			//   - file is deleted.
			//
			// Modify a deleted file...
			//   - Exception is thrown
			//
			// Wildcards, do we allow them?  If so what implications are there for this?
			//    Can complicate testing for conditions already mentioned in issues is the main thing.
			//    Expanding wildcards prior to processing would solve this but what about floppies/removeable media
			// Requests to modify non-existant entries?
		}

		public void CommitUpdate()
		{
			if ( contentsEdited_ )
			{
				RunUpdates();
			}
			else if ( commentEdited_ )
			{
				updateMode_ = FileUpdateMode.Direct; // TODO: This is for testing only..
				UpdateCommentOnly();
			}
			else
			{
				if ( (entries_ != null) && (entries_.Length == 0) )
				{
					byte[] theComment = (newComment_ != null) ? newComment_.RawComment : ZipConstants.ConvertToArray(comment_);
					using ( ZipHelperStream zhs = new ZipHelperStream(baseStream_) )
					{
						zhs.WriteEndOfCentralDirectory(0, 0, 0, theComment);
					}
				}
			}
		}

		/// <summary>
		/// Get a value indicating wether updating is in progress.
		/// </summary>
		public bool IsUpdating
		{
			get { return updates_ != null; }
		}

		void CheckUpdating()
		{
			if ( updates_ == null )
			{
				throw new ZipException("Cannot update until BeginUpdate has been called");
			}
		}

		// Adding of options like compression, etc make it appealling to allow adding of ZipEntry instances.
		// Its that or constructor with a million parameters or allow last add to be altered in some fashion.
		//
		// Speculation:
		// BeginAdd(fileName);
		// Add.Compression = Deflate;
		// Add.Level = 8;
		// EndAdd();
		//
		// TODO: Consider allowing updates from non-disk sources, streams would be the most general way of doing that.
		public void UpdateAddFile(string fileName, CompressionMethod compressionMethod)
		{
			CheckUpdating();
			contentsEdited_ = true;
			int index = FindExistingUpdate(fileName);

			if ( index >= 0 )
			{
				updates_.RemoveAt(index);
			}

			updates_.Add(new ZipUpdate(UpdateCommand.Add, fileName,
				updateNameTransform.TransformFile(fileName),
				compressionMethod));
		}

		public void UpdateAddFile(string fileName)
		{
			CheckUpdating();
			UpdateAddFile(fileName, CompressionMethod.Deflated);
		}

		public void UpdateAddEntry(ZipEntry entry)
		{
			CheckUpdating();
			if ( (entry.Size != 0) || (entry.CompressedSize != 0) )
			{
				throw new ZipException("Entry cannot have any data");
			}
			contentsEdited_ = true;
			updates_.Add(new ZipUpdate(UpdateCommand.Add, entry));
		}

		// TODO: Allow renaming of zip entries.
		public void UpdateModifyEntry(ZipEntry entry, ZipEntry updated)
		{
			CheckUpdating();
			contentsEdited_ = true;
			updates_.Add(new ZipUpdate(entry, updated));
		}

		class ZipComment
		{
			public ZipComment(string comment)
			{
				comment_ = comment;
				rawComment_ = null;
			}

			public ZipComment(byte[] rawString)
			{
				comment_ = null;
				rawComment_ = rawString;
			}

			public int RawLength
			{
				get
				{
					GetRawComment();
					return rawComment_.Length;
				}
			}

			public byte[] RawComment
			{
				get
				{
					GetRawComment();
					return rawComment_;
				}
			}

			static public implicit operator string(ZipComment comment)
			{
				comment.GetRawComment();
				return comment.comment_;
			}

			public void LocaleChanged()
			{
				rawComment_ = null;
			}

			void GetComment()
			{
				if ( comment_ == null )
				{
					comment_ = ZipConstants.ConvertToString(rawComment_);
				}
			}

			void GetRawComment()
			{
				if ( rawComment_ == null )
				{
					rawComment_ = ZipConstants.ConvertToArray(comment_);
				}
			}

			#region Instance Fields
			string comment_;
			byte[] rawComment_;
			#endregion
		}

		ZipComment newComment_;
		bool commentEdited_;
		FileUpdateMode updateMode_;

		enum FileUpdateMode
		{
			Safe,
			Direct
		}

		FileUpdateMode UpdateMode
		{
			get { return updateMode_; }
		}

		public void UpdateSetComment(string comment)
		{
			newComment_ = new ZipComment(comment);

			if ( newComment_.RawLength  > 0xffff )
			{
				newComment_ = null;
				throw new ZipException("Comment length exceeds maximum - 65535");
			}

			// We dont take account of the original and current comment appearing to be the same
			// as encoding may be different.
			commentEdited_ = true;
		}

		public bool UpdateDeleteFile(string fileName)
		{
			bool result = false;
			int index = FindExistingUpdate(fileName);
			if ( index >= 0 )
			{
				result = true;
				contentsEdited_ = true;
				updates_.RemoveAt(index);
			}
			return result;
		}

		public bool UpdateDeleteEntry(ZipEntry entry)
		{
			bool result = false;

			int index = FindExistingUpdate(entry);
			if ( index >= 0 )
			{
				result = true;
				contentsEdited_ = true;
				updates_.RemoveAt(index);
			}
			return result;
		}

		#endregion
		
		class ZipEntryEnumeration : IEnumerator
		{
			ZipEntry[] array;
			int ptr = -1;
			
			public ZipEntryEnumeration(ZipEntry[] arr)
			{
				array = arr;
			}
			
			public object Current {
				get {
					return array[ptr];
				}
			}
			
			public void Reset()
			{
				ptr = -1;
			}
			
			public bool MoveNext() 
			{
				return (++ptr < array.Length);
			}
		}
		
		class PartialInputStream : InflaterInputStream
		{
			Stream baseStream;
			long filepos, end;
			
			public PartialInputStream(Stream baseStream, long start, long len) : base(baseStream)
			{
				this.baseStream = baseStream;
				filepos = start;
				end = start + len;
			}
			
			public override int Available 
			{
				get {
					long amount = end - filepos;
					if (amount > Int32.MaxValue) {
						return Int32.MaxValue;
					}
					
					return (int) amount;
				}
			}

			/// <summary>
			/// Read a byte from this stream.
			/// </summary>
			/// <returns>Returns the byte read or -1 on end of stream.</returns>
			public override int ReadByte()
			{
				if (filepos == end) {
					return -1; //ok
				}
				
				lock(baseStream) {
					baseStream.Seek(filepos++, SeekOrigin.Begin);
					return baseStream.ReadByte();
				}
			}
			

			/// <summary>
			/// Close this partial input stream.
			/// </summary>
			/// <remarks>
			/// The underlying stream is not closed.  Close the parent ZipFile class to do that.
			/// </remarks>
			public override void Close()
			{
				// Do nothing at all!
			}
			
			public override int Read(byte[] b, int off, int len)
			{
				if (len > end - filepos) {
					len = (int) (end - filepos);
					if (len == 0) {
						return 0;
					}
				}
				
				lock(baseStream) {
					baseStream.Seek(filepos, SeekOrigin.Begin);
					int count = baseStream.Read(b, off, len);
					if (count > 0) {
						filepos += len;
					}
					return count;
				}
			}
			
			public long SkipBytes(long amount)
			{
				if (amount < 0) {
					throw new ArgumentOutOfRangeException();
				}
				
				if (amount > end - filepos) {
					amount = end - filepos;
				}
				
				filepos += amount;
				return amount;
			}
		}
	}
}
