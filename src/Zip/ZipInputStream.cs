// ZipInputStream.cs
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
using System.Text;
using System.IO;

using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace ICSharpCode.SharpZipLib.Zip 
{
	/// <summary>
	/// This is an InflaterInputStream that reads the files baseInputStream an zip archive
	/// one after another.  It has a special method to get the zip entry of
	/// the next file.  The zip entry contains information about the file name
	/// size, compressed size, Crc, etc.
	/// It includes support for Stored and Deflated entries.
	/// <br/>
	/// <br/>Author of the original java version : Jochen Hoenicke
	/// </summary>
	/// 
	/// <example> This sample shows how to read a zip file
	/// <code lang="C#">
	/// using System;
	/// using System.Text;
	/// using System.IO;
	/// 
	/// using ICSharpCode.SharpZipLib.Zip;
	/// 
	/// class MainClass
	/// {
	/// 	public static void Main(string[] args)
	/// 	{
	/// 		ZipInputStream s = new ZipInputStream(File.OpenRead(args[0]));
	/// 		
	/// 		ZipEntry theEntry;
	/// 		while ((theEntry = s.GetNextEntry()) != null) {
	/// 			int size = 2048;
	/// 			byte[] data = new byte[2048];
	/// 			
	/// 			Console.Write("Show contents (y/n) ?");
	/// 			if (Console.ReadLine() == "y") {
	/// 				while (true) {
	/// 					size = s.Read(data, 0, data.Length);
	/// 					if (size > 0) {
	/// 						Console.Write(new ASCIIEncoding().GetString(data, 0, size));
	/// 					} else {
	/// 						break;
	/// 					}
	/// 				}
	/// 			}
	/// 		}
	/// 		s.Close();
	/// 	}
	/// }	
	/// </code>
	/// </example>
	public class ZipInputStream : InflaterInputStream
	{
		Crc32 crc = new Crc32();
		ZipEntry entry = null;
		
		long size;
		int method;
		int flags;
		long avail;
		string password = null;
		
		/// <summary>
		/// Optional password used for encryption when non-null
		/// </summary>
		public string Password {
			get {
				return password;
			}
			set {
				password = value;
			}
		}
		

		/// <summary>
		/// Gets a value indicating if the entry can be decompressed
		/// </summary>
		/// <remarks>
		/// The entry can only be decompressed if the library supports the zip features required to extract it.
		/// See the <see cref="ZipEntry.Version">ZipEntry Version</see> property for more details.
		/// </remarks>
		public bool CanDecompressEntry {
			get {
				return entry != null && entry.Version <= ZipConstants.VERSION_MADE_BY;
			}
		}
		
		/// <summary>
		/// Creates a new Zip input stream, for reading a zip archive.
		/// </summary>
		public ZipInputStream(Stream baseInputStream) : base(baseInputStream, new Inflater(true))
		{
		}
		
		void FillBuf(int size)
		{
			avail = len = baseInputStream.Read(buf, 0, Math.Min(buf.Length, size));
		}
		
		int ReadBuf(byte[] outBuf, int offset, int length)
		{
			if (avail <= 0) {
				FillBuf(length);
				if (avail <= 0) {
					return 0;
				}
			}

			if (length > avail) {
				length = (int)avail;
			}

			System.Array.Copy(buf, len - (int)avail, outBuf, offset, length);
			avail -= length;
			return length;
		}
		
		void ReadFully(byte[] outBuf)
		{
			int off = 0;
			int len = outBuf.Length;
			while (len > 0) {
				int count = ReadBuf(outBuf, off, len);
				if (count <= 0) {
					throw new ZipException("Unexpected EOF"); 
				}
				off += count;
				len -= count;
			}
		}
		
		int ReadLeByte()
		{
			if (avail <= 0) {
				FillBuf(1);
				if (avail <= 0) {
					throw new ZipException("EOF in header");
				}
			}
			return buf[len - avail--] & 0xff;
		}
		
		/// <summary>
		/// Read an unsigned short baseInputStream little endian byte order.
		/// </summary>
		int ReadLeShort()
		{
			return ReadLeByte() | (ReadLeByte() << 8);
		}
		
		/// <summary>
		/// Read an int baseInputStream little endian byte order.
		/// </summary>
		int ReadLeInt()
		{
			return ReadLeShort() | (ReadLeShort() << 16);
		}
		
		/// <summary>
		/// Read an int baseInputStream little endian byte order.
		/// </summary>
		long ReadLeLong()
		{
			return ReadLeInt() | (ReadLeInt() << 32);
		}
		
		/// <summary>
		/// Advances to the next entry in the archive
		/// </summary>
		/// <returns>
		/// The next <see cref="ZipEntry">entry</see> in the archive or null if there are no more entries.
		/// </returns>
		/// <remarks>
		/// If the previous entry is still open <see cref="CloseEntry">CloseEntry</see> is called.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// Input stream is closed
		/// </exception>
		/// <exception cref="ZipException">
		/// Password is not set, password is invalid, compression method is invalid, 
		/// version required to extract is not supported
		/// </exception>
		public ZipEntry GetNextEntry()
		{
			if (crc == null) {
				throw new InvalidOperationException("Closed.");
			}
			
			if (entry != null) {
				CloseEntry();
			}
			
			if (this.cryptbuffer != null) {

				if (avail == 0 && inf.RemainingInput != 0) {
					avail = inf.RemainingInput - 16;
					inf.Reset();
				}
				baseInputStream.Position -= this.len;
				baseInputStream.Read(this.buf, 0, this.len);
				
			}
			
			if (avail <= 0) {
				FillBuf(ZipConstants.LOCHDR);
			}
			
			int header = ReadLeInt();

			if (header == ZipConstants.CENSIG || 
			    header == ZipConstants.ENDSIG || 
			    header == ZipConstants.CENDIGITALSIG || 
			    header == ZipConstants.CENSIG64) {
			    // No more individual entries exist
				Close();
				return null;
			}

			// -jr- 07-Dec-2003 Ignore spanning temporary signatures if found
			// SPANNINGSIG is same as descriptor signature and is untested as yet.
			if (header == ZipConstants.SPANTEMPSIG || header == ZipConstants.SPANNINGSIG) {
				header = ReadLeInt();
			}
			
			if (header != ZipConstants.LOCSIG) {
				throw new ZipException("Wrong Local header signature: 0x" + String.Format("{0:X}", header));
			}
			
			short versionRequiredToExtract = (short)ReadLeShort();
			
			flags          = ReadLeShort();
			method         = ReadLeShort();
			uint dostime   = (uint)ReadLeInt();
			int crc2       = ReadLeInt();
			csize          = ReadLeInt();
			size           = ReadLeInt();
			int nameLen    = ReadLeShort();
			int extraLen   = ReadLeShort();
			
			bool isCrypted = (flags & 1) == 1;
			
			byte[] buffer = new byte[nameLen];
			ReadFully(buffer);
			
			string name = ZipConstants.ConvertToString(buffer);
			
			entry = new ZipEntry(name, versionRequiredToExtract);
			entry.Flags = flags;
			
			if (method == (int)CompressionMethod.Stored && (!isCrypted && csize != size || (isCrypted && csize - ZipConstants.CRYPTO_HEADER_SIZE != size))) {
				throw new ZipException("Stored, but compressed != uncompressed");
			}
			
			if (method != (int)CompressionMethod.Stored && method != (int)CompressionMethod.Deflated) {
				throw new ZipException("unknown compression method " + method);
			}
			
			entry.CompressionMethod = (CompressionMethod)method;
			
			if ((flags & 8) == 0) {
				entry.Crc  = crc2 & 0xFFFFFFFFL;
				entry.Size = size & 0xFFFFFFFFL;
				entry.CompressedSize = csize & 0xFFFFFFFFL;
				BufferReadSize = 0;
			} else {
				
				if (isCrypted) {
					BufferReadSize = 1;
				} else {
					BufferReadSize = 0;
				}

				// This allows for GNU, WinZip and possibly other archives, the PKZIP spec says these are zero
				// under these circumstances.
				if (crc2 != 0) {
					entry.Crc = crc2 & 0xFFFFFFFFL;
				}
				
				if (size != 0) {
					entry.Size = size & 0xFFFFFFFFL;
				}
				if (csize != 0) {
					entry.CompressedSize = csize & 0xFFFFFFFFL;
				}
			}
			
			
			entry.DosTime = dostime;
			
			if (extraLen > 0) {
				byte[] extra = new byte[extraLen];
				ReadFully(extra);
				entry.ExtraData = extra;
			}
			
			// TODO How to handle this?
			// This library cannot handle versions greater than 20
			// Throwing an exception precludes getting at later possibly useable entries.
			// Could also skip this entry entirely
			// Letting it slip past here isnt so great as it wont work
			if (versionRequiredToExtract > 20) {
				throw new ZipException("Libray cannot extract this entry version required (" + versionRequiredToExtract.ToString() + ")");
			}
			
			// test for encryption
			if (isCrypted) {
				if (password == null) {
					throw new ZipException("No password set.");
				}
				InitializePassword(password);
				cryptbuffer = new byte[ZipConstants.CRYPTO_HEADER_SIZE];
				ReadFully(cryptbuffer);
				DecryptBlock(cryptbuffer, 0, cryptbuffer.Length);
				
				if ((flags & 8) == 0) {
					if (cryptbuffer[ZipConstants.CRYPTO_HEADER_SIZE - 1] != (byte)(crc2 >> 24)) {
						throw new ZipException("Invalid password");
					}
				}
				else {
					if (cryptbuffer[ZipConstants.CRYPTO_HEADER_SIZE - 1] != (byte)((dostime >> 8) & 0xff)) {
						throw new ZipException("Invalid password");
					}
				}
				
				if (csize >= ZipConstants.CRYPTO_HEADER_SIZE) {
					csize -= ZipConstants.CRYPTO_HEADER_SIZE;
				}
			} else {
				cryptbuffer = null;
			}
			
			if (method == (int)CompressionMethod.Deflated && avail > 0) {
				System.Array.Copy(buf, len - (int)avail, buf, 0, (int)avail);
				len = (int)avail;
				avail = 0;
				if (isCrypted) {
					DecryptBlock(buf, 0, Math.Min((int)csize, len));
				}
				inf.SetInput(buf, 0, len);
			}
			
			return entry;
		}
		
		void ReadDataDescriptor()
		{
			if (ReadLeInt() != ZipConstants.EXTSIG) {
				throw new ZipException("Data descriptor signature not found");
			}
			
			entry.Crc = ReadLeInt() & 0xFFFFFFFFL;
			csize = ReadLeInt();
			size = ReadLeInt();
			
			entry.Size = size & 0xFFFFFFFFL;
			entry.CompressedSize = csize & 0xFFFFFFFFL;
		}
		
		/// <summary>
		/// Closes the current zip entry and moves to the next one.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// The stream is closed
		/// </exception>
		/// <exception cref="ZipException">
		/// The Zip stream ends early
		/// </exception>
		public void CloseEntry()
		{
			if (crc == null) {
				throw new InvalidOperationException("Closed.");
			}
			
			if (entry == null) {
				return;
			}
			
			if (method == (int)CompressionMethod.Deflated) {
				if ((flags & 8) != 0) {
					// We don't know how much we must skip, read until end.
					byte[] tmp = new byte[2048];
					while (Read(tmp, 0, tmp.Length) > 0)
						;
					// read will close this entry
					return;
				}
				csize -= inf.TotalIn;
				avail = inf.RemainingInput;
			}
			
			if (avail > csize && csize >= 0) {
				avail -= csize;
			} else {
				csize -= avail;
				avail = 0;
				while (csize != 0) {
					int skipped = (int)base.Skip(csize & 0xFFFFFFFFL);
					
					if (skipped <= 0) {
						throw new ZipException("Zip archive ends early.");
					}
					
					csize -= skipped;
				}
			}
			
			size = 0;
			crc.Reset();
			if (method == (int)CompressionMethod.Deflated) {
				inf.Reset();
			}
			entry = null;
		}
		
		/// <summary>
		/// Returns 1 if there is an entry available
		/// Otherwise returns 0.
		/// </summary>
		public override int Available {
			get {
				return entry != null ? 1 : 0;
			}
		}
		
		/// <summary>
		/// Reads a byte from the current zip entry.
		/// </summary>
		/// <returns>
		/// The byte or -1 if end of stream is reached.
		/// </returns>
		/// <exception name="System.IO.IOException">
		/// An i/o error occured.
		/// </exception>
		/// <exception name="ICSharpCode.SharpZipLib.ZipException">
		/// The deflated stream is corrupted.
		/// </exception>
		public override int ReadByte()
		{
			byte[] b = new byte[1];
			if (Read(b, 0, 1) <= 0) {
				return -1; // ok
			}
			return b[0] & 0xff;
		}
		
		/// <summary>
		/// Reads a block of bytes from the current zip entry.
		/// </summary>
		/// <returns>
		/// The number of bytes read (this may be less than the length requested, even before the end of stream), or 0 on end of stream.
		/// </returns>
		/// <exception name="IOException">
		/// An i/o error occured.
		/// </exception>
		/// <exception cref="ZipException">
		/// The deflated stream is corrupted.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// The stream is not open.
		/// </exception>
		public override int Read(byte[] b, int off, int len)
		{
			if (crc == null) {
				throw new InvalidOperationException("Closed.");
			}
			
			if (entry == null) {
				return 0;
			}
			
			bool finished = false;
			
			switch (method) {
				case (int)CompressionMethod.Deflated:
					len = base.Read(b, off, len);
					if (len <= 0) {
						if (!inf.IsFinished) {
							throw new ZipException("Inflater not finished!?");
						}
						avail = inf.RemainingInput;
						
						if ((flags & 8) == 0 && (inf.TotalIn != csize || inf.TotalOut != size)) {
							throw new ZipException("size mismatch: " + csize + ";" + size + " <-> " + inf.TotalIn + ";" + inf.TotalOut);
						}
						inf.Reset();
						finished = true;
					}
					break;
				
				case (int)CompressionMethod.Stored:
					if (len > csize && csize >= 0) {
						len = (int)csize;
					}
					len = ReadBuf(b, off, len);
					if (len > 0) {
						csize -= len;
						size -= len;
					}
					
					if (csize == 0) {
						finished = true;
					} else {
						if (len < 0) {
							throw new ZipException("EOF in stored block");
						}
					}
					
					// cipher text needs decrypting
					if (cryptbuffer != null) {
						DecryptBlock(b, off, len);
					}
					
					break;
			}
				
			if (len > 0) {
				crc.Update(b, off, len);
			}
			
			if (finished) {
				StopDecrypting();
				if ((flags & 8) != 0) {
					ReadDataDescriptor();
				}
				
				if ((crc.Value & 0xFFFFFFFFL) != entry.Crc && entry.Crc != -1) {
					throw new ZipException("CRC mismatch");
				}
				crc.Reset();
				entry = null;
			}
			return len;
		}

		/// <summary>
		/// Closes the zip input stream
		/// </summary>
		public override void Close()
		{
			base.Close();
			crc = null;
			entry = null;
		}
	}
}
