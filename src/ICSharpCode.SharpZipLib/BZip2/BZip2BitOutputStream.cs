using System;
using System.IO;

namespace ICSharpCode.SharpZipLib.BZip2 {


	/**
	 * <p>An OutputStream wrapper that allows the writing of single bit booleans, unary numbers, bit
	 * strings of arbitrary length (up to 24 bits), and bit aligned 32-bit integers. A single byte at a
	 * time is written to the wrapped stream when sufficient bits have been accumulated
	 */
	public class BZip2BitOutputStream {

		/**
		 * The stream to which bits are written
		 */
		private Stream outputStream;

		/**
		 * A buffer of bits waiting to be written to the output stream
		 */
		private uint bitBuffer;

		/**
		 * The number of bits currently buffered in {@link #bitBuffer}
		 */
		private int bitCount;


		/**
		 * Writes a single bit to the wrapped output stream
		 * @param value The bit to write
		 * @throws IOException if an error occurs writing to the stream
		 */
		public void writeBoolean(bool value) {

			bitCount++;
			bitBuffer |= ((uint)(value ? 1 : 0) << (32 - bitCount));

			if (bitCount == 8) {
				outputStream.WriteByte((byte) (bitBuffer >> 24));
				bitBuffer = 0;
				bitCount = 0;
			}

		}


		/**
		 * Writes a zero-terminated unary number to the wrapped output stream
		 * @param value The number to write (must be non-negative)
		 * @throws IOException if an error occurs writing to the stream
		 */
		public void writeUnary(int value) {

			while (value-- > 0) {
				writeBoolean(true);
			}
			writeBoolean(false);

		}


		/**
		 * Writes up to 24 bits to the wrapped output stream
		 * @param count The number of bits to write (maximum 24)
		 * @param value The bits to write
		 * @throws IOException if an error occurs writing to the stream
		 */
		public void writeBits(int count, uint value) {

			bitBuffer |= ((value << (32 - count)) >> bitCount);
			bitCount += count;

			while (bitCount >= 8) {
				outputStream.WriteByte((byte)(bitBuffer >> 24));
				bitBuffer <<= 8;
				bitCount -= 8;
			}

		}


		/**
		 * Writes an integer as 32 bits of output
		 * @param value The integer to write
		 * @throws IOException if an error occurs writing to the stream
		 */
		public void writeInteger(uint value) {

			writeBits(16, (value >> 16) & 0xffff);
			writeBits(16, value & 0xffff);

		}


		/**
		 * Writes any remaining bits to the output stream, zero padding to a whole byte as required
		 * @throws IOException if an error occurs writing to the stream
		 */
		public void flush() {

			if (bitCount > 0) {
				writeBits(8 - bitCount, 0);
			}

		}


		/**
		 * @param outputStream The OutputStream to wrap
		 */
		public BZip2BitOutputStream(Stream outputStream) {

			this.outputStream = outputStream;

		}

	}

}
