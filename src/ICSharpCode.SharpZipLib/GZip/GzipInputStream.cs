using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.IO;

namespace ICSharpCode.SharpZipLib.GZip
{
	/// <summary>
	/// This filter stream is used to decompress a "GZIP" format stream.
	/// The "GZIP" format is described baseInputStream RFC 1952.
	///
	/// author of the original java version : John Leuner
	/// </summary>
	/// <example> This sample shows how to unzip a gzipped file
	/// <code>
	/// using System;
	/// using System.IO;
	///
	/// using ICSharpCode.SharpZipLib.Core;
	/// using ICSharpCode.SharpZipLib.GZip;
	///
	/// class MainClass
	/// {
	/// 	public static void Main(string[] args)
	/// 	{
	///			using (Stream inStream = new GZipInputStream(File.OpenRead(args[0])))
	///			using (FileStream outStream = File.Create(Path.GetFileNameWithoutExtension(args[0]))) {
	///				byte[] buffer = new byte[4096];
	///				StreamUtils.Copy(inStream, outStream, buffer);
	/// 		}
	/// 	}
	/// }
	/// </code>
	/// </example>
	public class GZipInputStream : InflaterInputStream
	{
		#region Instance Fields

		/// <summary>
		/// CRC-32 value for uncompressed data
		/// </summary>
		protected Crc32 crc;

		/// <summary>
		/// Flag to indicate if we've read the GZIP header yet for the current member (block of compressed data).
		/// This is tracked per-block as the file is parsed.
		/// </summary>
		private bool readGZIPHeader;

		/// <summary>
		/// Flag to indicate if at least one block in a stream with concatenated blocks was read successfully.
		/// This allows us to exit gracefully if downstream data is not in gzip format.
		/// </summary>
		private bool completedLastBlock;

		#endregion Instance Fields

		#region Constructors

		/// <summary>
		/// Creates a GZipInputStream with the default buffer size
		/// </summary>
		/// <param name="baseInputStream">
		/// The stream to read compressed data from (baseInputStream GZIP format)
		/// </param>
		public GZipInputStream(Stream baseInputStream)
			: this(baseInputStream, 4096)
		{
		}

		/// <summary>
		/// Creates a GZIPInputStream with the specified buffer size
		/// </summary>
		/// <param name="baseInputStream">
		/// The stream to read compressed data from (baseInputStream GZIP format)
		/// </param>
		/// <param name="size">
		/// Size of the buffer to use
		/// </param>
		public GZipInputStream(Stream baseInputStream, int size)
			: base(baseInputStream, new Inflater(true), size)
		{
		}

		#endregion Constructors

		#region Stream overrides

		/// <summary>
		/// Reads uncompressed data into an array of bytes
		/// </summary>
		/// <param name="buffer">
		/// The buffer to read uncompressed data into
		/// </param>
		/// <param name="offset">
		/// The offset indicating where the data should be placed
		/// </param>
		/// <param name="count">
		/// The number of uncompressed bytes to be read
		/// </param>
		/// <returns>Returns the number of bytes actually read.</returns>
		public override int Read(byte[] buffer, int offset, int count)
		{
			// A GZIP file can contain multiple blocks of compressed data, although this is quite rare.
			// A compressed block could potentially be empty, so we need to loop until we reach EOF or
			// we find data.
			while (true)
			{
				// If we haven't read the header for this block, read it
				if (!readGZIPHeader)
				{
					// Try to read header. If there is no header (0 bytes available), this is EOF. If there is
					// an incomplete header, this will throw an exception.
					try
					{
						if (!ReadHeader())
						{
							return 0;
						}
					}
					catch (Exception ex) when (completedLastBlock && (ex is GZipException || ex is EndOfStreamException))
					{
						// if we completed the last block (i.e. we're in a stream that has multiple blocks concatenated
						// we want to return gracefully from any header parsing exceptions since sometimes there may
						// be trailing garbage on a stream
						return 0;
					}
				}

				// Try to read compressed data
				int bytesRead = base.Read(buffer, offset, count);
				if (bytesRead > 0)
				{
					crc.Update(new ArraySegment<byte>(buffer, offset, bytesRead));
				}

				// If this is the end of stream, read the footer
				if (inf.IsFinished)
				{
					ReadFooter();
				} else if (inf.RemainingInput == 0) {
					// If the stream is not finished but we have no more data to read, don't keep looping forever
					throw new GZipException("Unexpected EOF");
				}

				if (bytesRead > 0)
				{
					return bytesRead;
				}
			}
		}

		#endregion Stream overrides

		#region Support routines

		private bool ReadHeader()
		{
			// Initialize CRC for this block
			crc = new Crc32();

			// Make sure there is data in file. We can't rely on ReadLeByte() to fill the buffer, as this could be EOF,
			// which is fine, but ReadLeByte() throws an exception if it doesn't find data, so we do this part ourselves.
			if (inputBuffer.Available <= 0)
			{
				inputBuffer.Fill();
				if (inputBuffer.Available <= 0)
				{
					// No header, EOF.
					return false;
				}
			}

			// 1. Check the two magic bytes
			var headCRC = new Crc32();
			int magic = inputBuffer.ReadLeByte();

			if (magic < 0)
			{
				throw new EndOfStreamException("EOS reading GZIP header");
			}

			headCRC.Update(magic);
			if (magic != (GZipConstants.GZIP_MAGIC >> 8))
			{
				throw new GZipException("Error GZIP header, first magic byte doesn't match");
			}

			//magic = baseInputStream.ReadByte();
			magic = inputBuffer.ReadLeByte();

			if (magic < 0)
			{
				throw new EndOfStreamException("EOS reading GZIP header");
			}

			if (magic != (GZipConstants.GZIP_MAGIC & 0xFF))
			{
				throw new GZipException("Error GZIP header,  second magic byte doesn't match");
			}

			headCRC.Update(magic);

			// 2. Check the compression type (must be 8)
			int compressionType = inputBuffer.ReadLeByte();

			if (compressionType < 0)
			{
				throw new EndOfStreamException("EOS reading GZIP header");
			}

			if (compressionType != 8)
			{
				throw new GZipException("Error GZIP header, data not in deflate format");
			}
			headCRC.Update(compressionType);

			// 3. Check the flags
			int flags = inputBuffer.ReadLeByte();
			if (flags < 0)
			{
				throw new EndOfStreamException("EOS reading GZIP header");
			}
			headCRC.Update(flags);

			/*    This flag byte is divided into individual bits as follows:

			bit 0   FTEXT
			bit 1   FHCRC
			bit 2   FEXTRA
			bit 3   FNAME
			bit 4   FCOMMENT
			bit 5   reserved
			bit 6   reserved
			bit 7   reserved
			*/

			// 3.1 Check the reserved bits are zero

			if ((flags & 0xE0) != 0)
			{
				throw new GZipException("Reserved flag bits in GZIP header != 0");
			}

			// 4.-6. Skip the modification time, extra flags, and OS type
			for (int i = 0; i < 6; i++)
			{
				int readByte = inputBuffer.ReadLeByte();
				if (readByte < 0)
				{
					throw new EndOfStreamException("EOS reading GZIP header");
				}
				headCRC.Update(readByte);
			}

			// 7. Read extra field
			if ((flags & GZipConstants.FEXTRA) != 0)
			{
				// XLEN is total length of extra subfields, we will skip them all
				int len1, len2;
				len1 = inputBuffer.ReadLeByte();
				len2 = inputBuffer.ReadLeByte();
				if ((len1 < 0) || (len2 < 0))
				{
					throw new EndOfStreamException("EOS reading GZIP header");
				}
				headCRC.Update(len1);
				headCRC.Update(len2);

				int extraLen = (len2 << 8) | len1;      // gzip is LSB first
				for (int i = 0; i < extraLen; i++)
				{
					int readByte = inputBuffer.ReadLeByte();
					if (readByte < 0)
					{
						throw new EndOfStreamException("EOS reading GZIP header");
					}
					headCRC.Update(readByte);
				}
			}

			// 8. Read file name
			if ((flags & GZipConstants.FNAME) != 0)
			{
				int readByte;
				while ((readByte = inputBuffer.ReadLeByte()) > 0)
				{
					headCRC.Update(readByte);
				}

				if (readByte < 0)
				{
					throw new EndOfStreamException("EOS reading GZIP header");
				}
				headCRC.Update(readByte);
			}

			// 9. Read comment
			if ((flags & GZipConstants.FCOMMENT) != 0)
			{
				int readByte;
				while ((readByte = inputBuffer.ReadLeByte()) > 0)
				{
					headCRC.Update(readByte);
				}

				if (readByte < 0)
				{
					throw new EndOfStreamException("EOS reading GZIP header");
				}

				headCRC.Update(readByte);
			}

			// 10. Read header CRC
			if ((flags & GZipConstants.FHCRC) != 0)
			{
				int tempByte;
				int crcval = inputBuffer.ReadLeByte();
				if (crcval < 0)
				{
					throw new EndOfStreamException("EOS reading GZIP header");
				}

				tempByte = inputBuffer.ReadLeByte();
				if (tempByte < 0)
				{
					throw new EndOfStreamException("EOS reading GZIP header");
				}

				crcval = (crcval << 8) | tempByte;
				if (crcval != ((int)headCRC.Value & 0xffff))
				{
					throw new GZipException("Header CRC value mismatch");
				}
			}

			readGZIPHeader = true;
			return true;
		}

		private void ReadFooter()
		{
			byte[] footer = new byte[8];

			// End of stream; reclaim all bytes from inf, read the final byte count, and reset the inflator
			long bytesRead = inf.TotalOut & 0xffffffff;
			inputBuffer.Available += inf.RemainingInput;
			inf.Reset();

			// Read footer from inputBuffer
			int needed = 8;
			while (needed > 0)
			{
				int count = inputBuffer.ReadClearTextBuffer(footer, 8 - needed, needed);
				if (count <= 0)
				{
					throw new EndOfStreamException("EOS reading GZIP footer");
				}
				needed -= count; // Jewel Jan 16
			}

			// Calculate CRC
			int crcval = (footer[0] & 0xff) | ((footer[1] & 0xff) << 8) | ((footer[2] & 0xff) << 16) | (footer[3] << 24);
			if (crcval != (int)crc.Value)
			{
				throw new GZipException("GZIP crc sum mismatch, theirs \"" + crcval + "\" and ours \"" + (int)crc.Value);
			}

			// NOTE The total here is the original total modulo 2 ^ 32.
			uint total =
				(uint)((uint)footer[4] & 0xff) |
				(uint)(((uint)footer[5] & 0xff) << 8) |
				(uint)(((uint)footer[6] & 0xff) << 16) |
				(uint)((uint)footer[7] << 24);

			if (bytesRead != total)
			{
				throw new GZipException("Number of bytes mismatch in footer");
			}

			// Mark header read as false so if another header exists, we'll continue reading through the file
			readGZIPHeader = false;

			// Indicate that we succeeded on at least one block so we can exit gracefully if there is trailing garbage downstream
			completedLastBlock = true;
		}

		#endregion Support routines
	}
}
