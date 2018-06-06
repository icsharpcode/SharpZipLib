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
import java.io.InputStream;


/**
 * <p>An InputStream wrapper that allows the reading of single bit booleans, unary numbers, bit
 * strings of arbitrary length (up to 24 bits), and bit aligned 32-bit integers. A single byte at a
 * time is read from the wrapped stream when more bits are required</p>
 */
public class BZip2BitInputStream {

	/**
	 * The stream from which bits are read
	 */
	private final InputStream inputStream;

	/**
	 * A buffer of bits read from the input stream that have not yet been returned
	 */
	private int bitBuffer;

	/**
	 * The number of bits currently buffered in {@link #bitBuffer}
	 */
	private int bitCount;


	/**
	 * Reads a single bit from the wrapped input stream
	 * @return {@code true} if the bit read was {@code 1}, otherwise {@code false}
	 * @throws IOException if no more bits are available in the input stream
	 */
	public boolean readBoolean() throws IOException {

		int bitBuffer = this.bitBuffer;
		int bitCount = this.bitCount;

		if (bitCount > 0) {
			bitCount--;
		} else {
			int byteRead = this.inputStream.read();

			if (byteRead < 0) {
				throw new BZip2Exception ("Insufficient data");
			}

			bitBuffer = (bitBuffer << 8) | byteRead;
			bitCount += 7;
			this.bitBuffer = bitBuffer;
		}

		this.bitCount = bitCount;
		return ((bitBuffer & (1 << bitCount))) != 0;

	}


	/**
	 * Reads a zero-terminated unary number from the wrapped input stream
	 * @return The unary number
	 * @throws IOException if no more bits are available in the input stream
	 */
	public int readUnary() throws IOException {

		int bitBuffer = this.bitBuffer;
		int bitCount = this.bitCount;
		int unaryCount = 0;

		for (;;) {
			if (bitCount > 0) {
				bitCount--;
			} else  {
				int byteRead = this.inputStream.read();

				if (byteRead < 0) {
					throw new BZip2Exception ("Insufficient data");
				}

				bitBuffer = (bitBuffer << 8) | byteRead;
				bitCount += 7;
			}

			if (((bitBuffer & (1 << bitCount))) == 0) {
				this.bitBuffer = bitBuffer;
				this.bitCount = bitCount;
				return unaryCount;
			}
			unaryCount++;
		}

	}


	/**
	 * Reads up to 24 bits from the wrapped input stream
	 * @param count The number of bits to read (maximum 24)
	 * @return The bits requested, right-aligned within the integer
	 * @throws IOException if more bits are requested than are available in the input stream
	 */
	public int readBits (final int count) throws IOException {

		int bitBuffer = this.bitBuffer;
		int bitCount = this.bitCount;

		if (bitCount < count) {
			while (bitCount < count) {
				int byteRead = this.inputStream.read();

				if (byteRead < 0) {
					throw new BZip2Exception ("Insufficient data");
				}

				bitBuffer = (bitBuffer << 8) | byteRead;
				bitCount += 8;
			}

			this.bitBuffer = bitBuffer;
		}

		bitCount -= count;
		this.bitCount = bitCount;

		return (bitBuffer >>> bitCount) & ((1 << count) - 1);

	}


	/**
	 * Reads 32 bits of input as an integer
	 * @return The integer read
	 * @throws IOException if 32 bits are not available in the input stream
	 */
	public int readInteger() throws IOException {

		return (readBits (16) << 16) | (readBits (16));

	}


	/**
	 * @param inputStream The InputStream to wrap
	 */
	public BZip2BitInputStream (final InputStream inputStream) {

		this.inputStream = inputStream;

	}

}
