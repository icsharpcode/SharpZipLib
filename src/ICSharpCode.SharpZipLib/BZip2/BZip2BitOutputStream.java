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
 * <p>An OutputStream wrapper that allows the writing of single bit booleans, unary numbers, bit
 * strings of arbitrary length (up to 24 bits), and bit aligned 32-bit integers. A single byte at a
 * time is written to the wrapped stream when sufficient bits have been accumulated
 */
public class BZip2BitOutputStream {

	/**
	 * The stream to which bits are written
	 */
	private final OutputStream outputStream;

	/**
	 * A buffer of bits waiting to be written to the output stream
	 */
	private int bitBuffer;

	/**
	 * The number of bits currently buffered in {@link #bitBuffer}
	 */
	private int bitCount;


	/**
	 * Writes a single bit to the wrapped output stream
	 * @param value The bit to write
	 * @throws IOException if an error occurs writing to the stream
	 */
	public void writeBoolean (final boolean value) throws IOException {

		int bitCount = this.bitCount + 1;
		int bitBuffer = this.bitBuffer | ((value ? 1 : 0) << (32 - bitCount));

		if (bitCount == 8) {
			this.outputStream.write (bitBuffer >>> 24);
			bitBuffer = 0;
			bitCount = 0;
		}

		this.bitBuffer = bitBuffer;
		this.bitCount = bitCount;

	}


	/**
	 * Writes a zero-terminated unary number to the wrapped output stream
	 * @param value The number to write (must be non-negative)
	 * @throws IOException if an error occurs writing to the stream
	 */
	public void writeUnary (int value) throws IOException {

		while (value-- > 0) {
			writeBoolean (true); 
		}
		writeBoolean (false);

	}


	/**
	 * Writes up to 24 bits to the wrapped output stream
	 * @param count The number of bits to write (maximum 24)
	 * @param value The bits to write
	 * @throws IOException if an error occurs writing to the stream
	 */
	public void writeBits (final int count, final int value) throws IOException {

		int bitCount = this.bitCount;
		int bitBuffer = this.bitBuffer | ((value << (32 - count)) >>> bitCount);
		bitCount += count;

		while (bitCount >= 8) {
			this.outputStream.write (bitBuffer >>> 24);
			bitBuffer <<= 8;
			bitCount -= 8;
		}

		this.bitBuffer = bitBuffer;
		this.bitCount = bitCount;

	}


	/**
	 * Writes an integer as 32 bits of output
	 * @param value The integer to write
	 * @throws IOException if an error occurs writing to the stream
	 */
	public void writeInteger (final int value) throws IOException {

		writeBits (16, (value >>> 16) & 0xffff);
		writeBits (16, value & 0xffff);

	}


	/**
	 * Writes any remaining bits to the output stream, zero padding to a whole byte as required
	 * @throws IOException if an error occurs writing to the stream
	 */
	public void flush() throws IOException {

		if (this.bitCount > 0) {
			writeBits (8 - this.bitCount, 0);
		}

	}


	/**
	 * @param outputStream The OutputStream to wrap
	 */
	public BZip2BitOutputStream (final OutputStream outputStream) {

		this.outputStream = outputStream;

	}

}
