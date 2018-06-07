using System;
using System.IO;

namespace ICSharpCode.SharpZipLib.BZip2
{

	/**
	 * <p>An InputStream wrapper that decompresses BZip2 data</p>
	 *
	 * <p>A BZip2 stream consists of one or more blocks of compressed data. This decompressor reads a
	 * whole block at a time, then progressively returns decompressed output.</p>
	 *
	 * <p>On encountering any error decoding the compressed stream, an IOException is thrown, and
	 * further reads will return {@code -1}</p>
	 *
	 * <p><b>Note:</b> Each BZip2 compressed block contains a CRC code which is verified after the block
	 * has been read completely. If verification fails, an exception is thrown on the read from
	 * the block, <b>potentially after corrupt data has already been returned</b>. The compressed stream
	 * also contains a CRC code which is verified once the end of the stream has been reached.
	 * <b>This check may fail even if every individual block in the stream passes CRC verification</b>.
	 * If this possibility is of concern, you should read and store the entire decompressed stream
	 * before further processing.</p>
	 *
	 * <p>Instances of this class are not threadsafe.</p>
	 */
	public class BZip2InputStream : Stream {

		/**
		 * The stream from which compressed BZip2 data is read and decoded
		 */
		private Stream inputStream;

		/**
		 * An InputStream wrapper that provides bit-level reads
		 */
		private BZip2BitInputStream bitInputStream;

		/**
		 * If {@code true}, the caller is assumed to have read away the stream's leading "BZ" identifier
		 * bytes
		 */
		private bool headerless;

		/**
		 * (@code true} if the end of the compressed stream has been reached, otherwise {@code false}
		 */
		private bool streamComplete = false;

		/**
		 * The declared block size of the stream (before run-length decoding). The block
		 * will usually be smaller, but no block in the stream has to be exactly this large, and an
		 * encoder could in theory choose to mix blocks of any size up to this value. Its function is
		 * therefore as a hint to the decompressor as to how much working space is sufficient to
		 * decompress blocks in a given stream
		 */
		private uint streamBlockSize;

		/**
		 * The merged CRC of all blocks decompressed so far
		 */
		private uint streamCRC = 0;

		/**
		 * The decompressor for the current block
		 */
		private BZip2BlockDecompressor blockDecompressor = null;

		private bool isOwner;

		/// <summary>
		/// True if the underlying stream will be closed with the current Stream
		/// </summary>
		public bool IsStreamOwner => isOwner;

		/// <summary>
		/// Returns true as long as the underlying stream is readable
		/// </summary>
		public override bool CanRead => inputStream?.CanRead ?? false;

		public override bool CanSeek => false;

		public override bool CanWrite => false;

		/// <summary>
		/// Returns underlying stream length
		/// </summary>
		public override long Length => inputStream.Length;

		/// <summary>
		/// Returns underlying stream position. Readonly.
		/// </summary>
		public override long Position { get => inputStream.Position; set => throw new NotImplementedException(); }


		/// <summary>Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.</summary>
		/// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
		/// <exception cref="NotSupportedException">The stream does not support reading. </exception>
		/// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override int ReadByte() {

			int nextByte = -1;
			if (blockDecompressor == null) {
				initialiseStream();
			} else {
				nextByte = blockDecompressor.Read();
			}

			if (nextByte == -1) {
				if (initialiseNextBlock()) {
					nextByte = blockDecompressor.Read();
				}
			}

			return nextByte;

		}



	/// <summary>When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
	/// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
	/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset" /> and (<paramref name="offset" /> + <paramref name="count" /> - 1) replaced by the bytes read from the current source. </param>
	/// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin storing the data read from the current stream. </param>
	/// <param name="count">The maximum number of bytes to be read from the current stream. </param>
	/// <exception cref="ArgumentException">The sum of <paramref name="offset" /> and <paramref name="count" /> is larger than the buffer length. </exception>
	/// <exception cref="ArgumentNullException">
	///   <paramref name="buffer" /> is null. </exception>
	/// <exception cref="ArgumentOutOfRangeException">
	///   <paramref name="offset" /> or <paramref name="count" /> is negative. </exception>
	/// <exception cref="IOException">An I/O error occurs. </exception>
	/// <exception cref="NotSupportedException">The stream does not support reading. </exception>
	/// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
	public override int Read(byte[] destination, int offset, int length) {

			int bytesRead = -1;
			if (this.blockDecompressor == null) {
				initialiseStream();
			} else {
				bytesRead = blockDecompressor.Read(destination, offset, length);
			}

			if (bytesRead == -1) {
				if (initialiseNextBlock()) {
					bytesRead = blockDecompressor.Read(destination, offset, length);
				}
			}

			return bytesRead;

		}



		/// <summary>Releases the unmanaged resources used by the <see cref="Stream" /> and optionally releases the managed resources.</summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected override void Dispose(bool disposing)
		{
			this.streamComplete = true;

			if (disposing && IsStreamOwner)
			{
				this.inputStream.Dispose();
			}
		}

		///<summary>Reads the stream header and checks that the data appears to be a valid BZip2 stream</summary>
		///<exception cref="IOException">Stream header is not valid</exception>
		private void initialiseStream() {

			/* If the stream has been explicitly closed, throw an exception */
			if (bitInputStream == null) {
				throw new BZip2Exception("Stream closed");
			}

			/* If we're already at the end of the stream, do nothing */
			if (streamComplete) {
				return;
			}

			/* Read the stream header */
			try {
				uint marker1 = headerless ? 0 : this.bitInputStream.readBits(16);
				uint marker2 = bitInputStream.readBits(8);
				uint blockSize = (bitInputStream.readBits(8) - '0');

				if (
						   (!this.headerless && (marker1 != BZip2Constants.STREAM_START_MARKER_1))
						|| (marker2 != BZip2Constants.STREAM_START_MARKER_2)
						|| (blockSize < 1) || (blockSize > 9))
				{
					throw new BZip2Exception("Invalid BZip2 header");
				}

				streamBlockSize = blockSize * 100000;
			} catch (IOException e) {
				// If the stream header was not valid, stop trying to read more data
				streamComplete = true;
				throw e;
			}


		}


		/**
		 * Prepares a new block for decompression if any remain in the stream. If a previous block has
		 * completed, its CRC is checked and merged into the stream CRC. If the previous block was the
		 * block in the stream, the stream CRC is validated
		 * @return {@code true} if a block was successfully initialised, or {@code false} if the end of
		 *                      file marker was encountered
		 * @throws IOException if either the block or stream CRC check failed, if the following data is
		 *                      not a valid block-header or end-of-file marker, or if the following
		 *                      block could not be decoded
		 */
		private bool initialiseNextBlock() {

			/* If we're already at the end of the stream, do nothing */
			if (this.streamComplete) {
				return false;
			}

			/* If a block is complete, check the block CRC and integrate it into the stream CRC */
			if (this.blockDecompressor != null) {
				uint blockCRC = this.blockDecompressor.CheckCRC();
				this.streamCRC = ((this.streamCRC << 1) | (this.streamCRC >> 31)) ^ blockCRC;
			}

			/* Read block-header or end-of-stream marker */
			uint marker1 = bitInputStream.readBits(24);
			uint marker2 = bitInputStream.readBits(24);

			if (marker1 == BZip2Constants.BLOCK_HEADER_MARKER_1 && marker2 == BZip2Constants.BLOCK_HEADER_MARKER_2) {
				// Initialise a new block
				try {
					blockDecompressor = new BZip2BlockDecompressor(bitInputStream, streamBlockSize);
				} catch (IOException e) {
					// If the block could not be decoded, stop trying to read more data
					this.streamComplete = true;
					throw e;
				}
				return true;
			} else if (marker1 == BZip2Constants.STREAM_END_MARKER_1 && marker2 == BZip2Constants.STREAM_END_MARKER_2) {
				// Read and verify the end-of-stream CRC
				streamComplete = true;
				uint storedCombinedCRC = bitInputStream.readInteger();
				if (storedCombinedCRC != streamCRC) {
					throw new BZip2Exception("BZip2 stream CRC error");
				}
				return false;
			}

			/* If what was read is not a valid block-header or end-of-stream marker, the stream is broken */
			streamComplete = true;
			throw new BZip2Exception("BZip2 stream format error");

		}

		public override void Flush()
		{
			throw new NotImplementedException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}


		/**
		 * @param inputStream The InputStream to wrap
		 * @param headerless If {@code true}, the caller is assumed to have read away the stream's
		 *                   leading "BZ" identifier bytes
		 */
		public BZip2InputStream(Stream inputStream, bool isOwner = true, bool headerless = false)
		{
			this.inputStream = inputStream ?? throw new ArgumentNullException("Input stream cannot be null");
			this.bitInputStream = new BZip2BitInputStream(inputStream);
			this.headerless = headerless;
			this.isOwner = isOwner;

		}

	}
}
