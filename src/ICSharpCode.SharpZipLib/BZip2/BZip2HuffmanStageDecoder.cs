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


/**
 * A decoder for the BZip2 Huffman coding stage
 */
public class BZip2HuffmanStageDecoder {

	/**
	 * The BZip2BitInputStream from which Huffman codes are read
	 */
	private final BZip2BitInputStream bitInputStream;

	/**
	 * The Huffman table number to use for each group of 50 symbols
	 */
	private final byte[] selectors;

	/**
	 * The minimum code length for each Huffman table
	 */
	private final int[] minimumLengths = new int[BZip2Constants.HUFFMAN_MAXIMUM_TABLES];

	/**
	 * An array of values for each Huffman table that must be subtracted from the numerical value of
	 * a Huffman code of a given bit length to give its canonical code index
	 */
	private final int[][] codeBases = new int[BZip2Constants.HUFFMAN_MAXIMUM_TABLES][BZip2Constants.HUFFMAN_DECODE_MAXIMUM_CODE_LENGTH + 2];

	/**
	 * An array of values for each Huffman table that gives the highest numerical value of a Huffman
	 * code of a given bit length
	 */
	private final int[][] codeLimits = new int[BZip2Constants.HUFFMAN_MAXIMUM_TABLES][BZip2Constants.HUFFMAN_DECODE_MAXIMUM_CODE_LENGTH + 1];

	/**
	 * A mapping for each Huffman table from canonical code index to output symbol
	 */
	private final int[][] codeSymbols = new int[BZip2Constants.HUFFMAN_MAXIMUM_TABLES][BZip2Constants.HUFFMAN_MAXIMUM_ALPHABET_SIZE];

	/**
	 * The Huffman table for the current group
	 */
	private int currentTable;

	/**
	 * The index of the current group within the selectors array
	 */
	private int groupIndex = -1;

	/**
	 * The byte position within the current group. A new group is selected every 50 decoded bytes
	 */
	private int groupPosition = -1;


	/**
	 * Constructs Huffman decoding tables from lists of Canonical Huffman code lengths
	 * @param alphabetSize The total number of codes (uniform for each table)
	 * @param tableCodeLengths The Canonical Huffman code lengths for each table
	 */
	private void createHuffmanDecodingTables (final int alphabetSize, final byte[][] tableCodeLengths) {

		for (int table = 0; table < tableCodeLengths.length; table++) {

			final int[] tableBases = this.codeBases[table];
			final int[] tableLimits = this.codeLimits[table];
			final int[] tableSymbols = this.codeSymbols[table];

			final byte[] codeLengths = tableCodeLengths[table];
			int minimumLength = BZip2Constants.HUFFMAN_DECODE_MAXIMUM_CODE_LENGTH;
			int maximumLength = 0;

			// Find the minimum and maximum code length for the table
			for (int i = 0; i < alphabetSize; i++) {
				maximumLength = Math.max (codeLengths[i], maximumLength);
				minimumLength = Math.min (codeLengths[i], minimumLength);
			}
			this.minimumLengths[table] = minimumLength;

			// Calculate the first output symbol for each code length
			for (int i = 0; i < alphabetSize; i++) {
				tableBases[codeLengths[i] + 1]++;
			}
			for (int i = 1; i < BZip2Constants.HUFFMAN_DECODE_MAXIMUM_CODE_LENGTH + 2; i++) {
				tableBases[i] += tableBases[i - 1];
			}

			// Calculate the first and last Huffman code for each code length (codes at a given
			// length are sequential in value)
			int code = 0;
			for (int i = minimumLength; i <= maximumLength; i++) {
				int base = code;
				code += tableBases[i + 1] - tableBases[i];
				tableBases[i] = base - tableBases[i];
				tableLimits[i] = code - 1;
				code <<= 1;
			}

			// Populate the mapping from canonical code index to output symbol
			int codeIndex = 0;
			for (int bitLength = minimumLength; bitLength <= maximumLength; bitLength++) {
				for (int symbol = 0; symbol < alphabetSize; symbol++) {
					if (codeLengths[symbol] == bitLength) {
						tableSymbols[codeIndex++] = symbol;
					}
				}
			}

		}

	}


	/**
	 * Decodes and returns the next symbol
	 * @return The decoded symbol
	 * @throws IOException if the end of the input stream is reached while decoding
	 */
	public int nextSymbol() throws IOException {

		final BZip2BitInputStream bitInputStream = this.bitInputStream;

		// Move to next group selector if required
		if (((++this.groupPosition % BZip2Constants.HUFFMAN_GROUP_RUN_LENGTH) == 0)) {
			this.groupIndex++;
			if (this.groupIndex == this.selectors.length) {
				throw new BZip2Exception ("Error decoding BZip2 block");
			}
			this.currentTable = this.selectors[this.groupIndex] & 0xff;
		}

		final int currentTable = this.currentTable;
		final int[] tableLimits = this.codeLimits[currentTable];
		int codeLength = this.minimumLengths[currentTable];

		// Starting with the minimum bit length for the table, read additional bits one at a time
		// until a complete code is recognised
		int codeBits = bitInputStream.readBits (codeLength);
		for (; codeLength <= BZip2Constants.HUFFMAN_DECODE_MAXIMUM_CODE_LENGTH; codeLength++) {
			if (codeBits <= tableLimits[codeLength]) {
				// Convert the code to a symbol index and return
				return this.codeSymbols[currentTable][codeBits - this.codeBases[currentTable][codeLength]];
			}
			codeBits = (codeBits << 1) | bitInputStream.readBits (1);
		}

		// A valid code was not recognised
		throw new BZip2Exception ("Error decoding BZip2 block");

	}


	/**
	 * @param bitInputStream The BZip2BitInputStream from which Huffman codes are read
	 * @param alphabetSize The total number of codes (uniform for each table)
	 * @param tableCodeLengths The Canonical Huffman code lengths for each table
	 * @param selectors The Huffman table number to use for each group of 50 symbols
	 */
	public BZip2HuffmanStageDecoder (final BZip2BitInputStream bitInputStream, final int alphabetSize, final byte[][] tableCodeLengths, final byte[] selectors) {

		this.bitInputStream = bitInputStream;
		this.selectors = selectors;
		this.currentTable = this.selectors[0];

		createHuffmanDecodingTables (alphabetSize, tableCodeLengths);

	}

}
