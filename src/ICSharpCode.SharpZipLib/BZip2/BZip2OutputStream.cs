using System;
using System.IO;

namespace ICSharpCode.SharpZipLib.BZip2
{

	/**
	 * <p>An OutputStream wrapper that compresses BZip2 data</p>
	 *
	 * <p>Instances of this class are not threadsafe.</p>
	 */
	public class BZip2OutputStream : Stream {

		/**
		 * The stream to which compressed BZip2 data is written
		 */
		private Stream outputStream;

		/**
		 * An OutputStream wrapper that provides bit-level writes
		 */
		private BZip2BitOutputStream bitOutputStream;

		/**
		 * (@code true} if the compressed stream has been finished, otherwise {@code false}
		 */
		private bool streamFinished = false;

		/**
		 * The declared maximum block size of the stream (before run-length decoding)
		 */
		private int streamBlockSize;

		/**
		 * The merged CRC of all blocks compressed so far
		 */
		private uint streamCRC = 0;

		private bool isOwner;

		/// <summary>
		/// True if the underlying stream will be closed with the current Stream
		/// </summary>
		public bool IsStreamOwner => isOwner;

		/**
		 * The compressor for the current block
		 */
		private BZip2BlockCompressor blockCompressor;

		public override bool CanRead => false;

		public override bool CanSeek => false;

		public override bool CanWrite => true;

		public override long Length => outputStream.Length;

		public override long Position
		{
			get => outputStream.Position;
			set => throw new NotImplementedException();
		}

		/* (non-Javadoc)
		 * @see java.io.OutputStream#write(int)
		 */

	public override void WriteByte(byte value) {

			if (this.outputStream == null) {
				throw new BZip2Exception("Stream closed");
			}

			if (this.streamFinished) {
				throw new BZip2Exception("Write beyond end of stream");
			}

			if (!this.blockCompressor.Write(value & 0xff)) {
				closeBlock();
				initialiseNextBlock();
				this.blockCompressor.Write(value & 0xff);
			}

		}



		/// <summary>When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.</summary>
		/// <param name="buffer">An array of bytes. This method copies <paramref name="count" /> bytes from <paramref name="buffer" /> to the current stream. </param>
		/// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin copying bytes to the current stream. </param>
		/// <param name="count">The number of bytes to be written to the current stream. </param>
		/// <exception cref="ArgumentException">The sum of <paramref name="offset" /> and <paramref name="count" /> is greater than the buffer length.</exception>
		/// <exception cref="ArgumentNullException">
		///   <paramref name="buffer" />  is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///   <paramref name="offset" /> or <paramref name="count" /> is negative.</exception>
		/// <exception cref="IOException">An I/O error occured, such as the specified file cannot be found.</exception>
		/// <exception cref="NotSupportedException">The stream does not support writing.</exception>
		/// <exception cref="ObjectDisposedException">
		///   <see cref="M:System.IO.Stream.Write(System.Byte[],System.Int32,System.Int32)" /> was called after the stream was closed.</exception>
		public override void Write(byte[] buffer, int offset, int count)
		{

			if (outputStream == null) {
				throw new BZip2Exception("Output stream is closed");
			}

			if (streamFinished) {
				throw new BZip2Exception("Cannot write beyond end of stream");
			}

			int bytesWritten;
			while (count > 0) {
				if ((bytesWritten = blockCompressor.Write(buffer, offset, count)) < count) {
					closeBlock();
					initialiseNextBlock();
				}
				offset += bytesWritten;
				count -= bytesWritten;
			}
		}


		protected override void Dispose(bool disposing)
		{
			if (outputStream != null)
			{
				finish();
				if (disposing && isOwner)
				{
					outputStream.Dispose();
				}
			}
		}


		/**
		 * Initialises a new block for compression
		 */
		private void initialiseNextBlock() {

			this.blockCompressor = new BZip2BlockCompressor(this.bitOutputStream, this.streamBlockSize);

		}


		/**
		 * Compress and write out the block currently in progress. If no bytes have been written to the
		 * block, it is discarded
		 * @ on any I/O error writing to the output stream
		 */
		private void closeBlock() {

			if (blockCompressor.isEmpty()) {
				return;
			}

			blockCompressor.Close();
			var blockCRC = blockCompressor.CRC;
			streamCRC = ((streamCRC << 1) | (streamCRC >> 31)) ^ blockCRC;

		}


		/**
		 * Compresses and writes out any as yet unwritten data, then writes the end of the BZip2 stream.
		 * The underlying OutputStream is not closed
		 * @ on any I/O error writing to the output stream
		 */
		public void finish() {

			if (!streamFinished) {
				streamFinished = true;
				try {
					closeBlock();
					bitOutputStream.writeBits(24, BZip2Constants.STREAM_END_MARKER_1);
					bitOutputStream.writeBits(24, BZip2Constants.STREAM_END_MARKER_2);
					bitOutputStream.writeInteger(streamCRC);
					bitOutputStream.flush();
					outputStream.Flush();
				}
				finally
				{
					blockCompressor = null;
				}
			}

		}

		/// <summary>When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.</summary>
		/// <exception cref="IOException">An I/O error occurs. </exception>
		public override void Flush() => outputStream.Flush();

		/// <summary>When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
		/// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
		/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset" /> and (<paramref name="offset" /> + <paramref name="count" /> - 1) replaced by the bytes read from the current source. </param>
		/// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin storing the data read from the current stream. </param>
		/// <param name="count">The maximum number of bytes to be read from the current stream. </param>
		/// <exception cref="NotSupportedException">The stream does not support reading. </exception>
		/// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

		/// <summary>When overridden in a derived class, sets the position within the current stream.</summary>
		/// <returns>The new position within the current stream.</returns>
		/// <param name="offset">A byte offset relative to the <paramref name="origin" /> parameter. </param>
		/// <param name="origin">A value of type <see cref="SeekOrigin" /> indicating the reference point used to obtain the new position. </param>
		/// <exception cref="NotSupportedException">The stream does not support seeking, such as if the stream is constructed from a pipe or console output. </exception>
		/// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

		/// <summary>When overridden in a derived class, sets the length of the current stream.</summary>
		/// <param name="value">The desired length of the current stream in bytes. </param>
		/// <exception cref="NotSupportedException">The stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output. </exception>
		/// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override void SetLength(long value) => throw new NotImplementedException();


		/**
		 * @param outputStream The output stream to write to
		 * @param blockSizeMultiplier The BZip2 block size as a multiple of 100,000 bytes (minimum 1,
		 * maximum 9). Larger block sizes require more memory for both compression and decompression,
		 * but give better compression ratios. <code>9</code> will usually be the best value to use
		 * @ on any I/O error writing to the output stream
		 */
		public BZip2OutputStream(Stream outputStream, byte blockSizeMultiplier = 9, bool isOwner = true) {

			if ((blockSizeMultiplier < 1) || (blockSizeMultiplier > 9)) {
				throw new ArgumentOutOfRangeException($"Invalid BZip2 block size {blockSizeMultiplier}, valid range: 1-9");
			}

			this.streamBlockSize = blockSizeMultiplier * 100000;
			this.outputStream = outputStream ?? throw new ArgumentNullException("Output stream cannot be null");
			this.bitOutputStream = new BZip2BitOutputStream(this.outputStream);

			bitOutputStream.writeBits(16, BZip2Constants.STREAM_START_MARKER_1);
			bitOutputStream.writeBits(8, BZip2Constants.STREAM_START_MARKER_2);
			bitOutputStream.writeBits(8, (uint)('0' + blockSizeMultiplier));

			this.isOwner = isOwner;

			initialiseNextBlock();

		}

	}
}
