/*
 * Copyright (c) 2011 Matthew Francis
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

package org.itadaki.bzip2;

import java.io.IOException;
import java.io.OutputStream;


/**
 * <p>An OutputStream wrapper that compresses BZip2 data</p>
 *
 * <p>Instances of this class are not threadsafe.</p>
 */
public class BZip2OutputStream extends OutputStream {

	/**
	 * The stream to which compressed BZip2 data is written
	 */
	private OutputStream outputStream;

	/**
	 * An OutputStream wrapper that provides bit-level writes
	 */
	private BZip2BitOutputStream bitOutputStream;

	/**
	 * (@code true} if the compressed stream has been finished, otherwise {@code false}
	 */
	private boolean streamFinished = false;

	/**
	 * The declared maximum block size of the stream (before final run-length decoding)
	 */
	private final int streamBlockSize;

	/**
	 * The merged CRC of all blocks compressed so far
	 */
	private int streamCRC = 0;

	/**
	 * The compressor for the current block
	 */
	private BZip2BlockCompressor blockCompressor;


	/* (non-Javadoc)
	 * @see java.io.OutputStream#write(int)
	 */
	@Override
	public void write (final int value) throws IOException {

		if (this.outputStream == null) {
			throw new BZip2Exception ("Stream closed");
		}

		if (this.streamFinished) {
			throw new BZip2Exception ("Write beyond end of stream");
		}

		if (!this.blockCompressor.write (value & 0xff)) {
			closeBlock();
			initialiseNextBlock();
			this.blockCompressor.write (value & 0xff);
		}

	}


	/* (non-Javadoc)
	 * @see java.io.OutputStream#write(byte[], int, int)
	 */
	@Override
	public void write (final byte[] data, int offset, int length) throws IOException {

		if (this.outputStream == null) {
			throw new BZip2Exception ("Stream closed");
		}

		if (this.streamFinished) {
			throw new BZip2Exception ("Write beyond end of stream");
		}

		int bytesWritten;
		while (length > 0) {
			if ((bytesWritten = this.blockCompressor.write (data, offset, length)) < length) {
				closeBlock();
				initialiseNextBlock();
			}
			offset += bytesWritten;
			length -= bytesWritten;
		}

	}


	/* (non-Javadoc)
	 * @see java.io.OutputStream#close()
	 */
	@Override
	public void close() throws IOException {

		if (this.outputStream != null) {
			finish();
			this.outputStream.close();
			this.outputStream = null;
		}

	}


	/**
	 * Initialises a new block for compression
	 */
	private void initialiseNextBlock() {

		this.blockCompressor = new BZip2BlockCompressor (this.bitOutputStream, this.streamBlockSize);

	}


	/**
	 * Compress and write out the block currently in progress. If no bytes have been written to the
	 * block, it is discarded
	 * @throws IOException on any I/O error writing to the output stream
	 */
	private void closeBlock() throws IOException {

		if (this.blockCompressor.isEmpty()) {
			return;
		}

		this.blockCompressor.close();
		int blockCRC = this.blockCompressor.getCRC();
		this.streamCRC = ((this.streamCRC << 1) | (this.streamCRC >>> 31)) ^ blockCRC;

	}


	/**
	 * Compresses and writes out any as yet unwritten data, then writes the end of the BZip2 stream.
	 * The underlying OutputStream is not closed
	 * @throws IOException on any I/O error writing to the output stream
	 */
	public void finish() throws IOException {

		if (!this.streamFinished) {
			this.streamFinished = true;
			try {
				closeBlock();
				this.bitOutputStream.writeBits (24, BZip2Constants.STREAM_END_MARKER_1);
				this.bitOutputStream.writeBits (24, BZip2Constants.STREAM_END_MARKER_2);
				this.bitOutputStream.writeInteger (this.streamCRC);
				this.bitOutputStream.flush();
				this.outputStream.flush();
			} finally {
				this.blockCompressor = null;
			}
		}

	}


	/**
	 * @param outputStream The output stream to write to
	 * @param blockSizeMultiplier The BZip2 block size as a multiple of 100,000 bytes (minimum 1,
	 * maximum 9). Larger block sizes require more memory for both compression and decompression,
	 * but give better compression ratios. <code>9</code> will usually be the best value to use
	 * @throws IOException on any I/O error writing to the output stream
	 */
	public BZip2OutputStream (final OutputStream outputStream, final int blockSizeMultiplier) throws IOException {

		if (outputStream == null) {
			throw new IllegalArgumentException ("Null output stream");
		}

		if ((blockSizeMultiplier < 1) || (blockSizeMultiplier > 9)) {
			throw new IllegalArgumentException ("Invalid BZip2 block size" + blockSizeMultiplier);
		}

		this.streamBlockSize = blockSizeMultiplier * 100000;
		this.outputStream = outputStream;
		this.bitOutputStream = new BZip2BitOutputStream (this.outputStream);

		this.bitOutputStream.writeBits (16, BZip2Constants.STREAM_START_MARKER_1);
		this.bitOutputStream.writeBits (8,  BZip2Constants.STREAM_START_MARKER_2);
		this.bitOutputStream.writeBits (8, '0' + blockSizeMultiplier);

		initialiseNextBlock();

	}


	/**
	 * Constructs a BZip2 stream compressor with the maximum (900,000 byte) block size
	 * @param outputStream The output stream to write to
	 * @throws IOException on any I/O error writing to the output stream
	 */
	public BZip2OutputStream (final OutputStream outputStream) throws IOException {

		this (outputStream, 9);

	}

}
