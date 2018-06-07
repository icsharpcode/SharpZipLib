using System;
using System.IO;

namespace ICSharpCode.SharpZipLib.BZip2
{

	/**
	 * A decoder for the BZip2 Huffman coding stage
	 */
	public class BZip2HuffmanStageDecoder {

		/**
		 * The BZip2BitInputStream from which Huffman codes are read
		 */
		private BZip2BitInputStream bitInputStream;

		/**
		 * The Huffman table number to use for each group of 50 symbols
		 */
		private byte[] selectors;

		/**
		 * The minimum code length for each Huffman table
		 */
		private int[] minimumLengths = new int[BZip2Constants.HUFFMAN_MAXIMUM_TABLES];

		/**
		 * An array of values for each Huffman table that must be subtracted from the numerical value of
		 * a Huffman code of a given bit length to give its canonical code index
		 */
		private int[,] codeBases = new int[BZip2Constants.HUFFMAN_MAXIMUM_TABLES, BZip2Constants.HUFFMAN_DECODE_MAXIMUM_CODE_LENGTH + 2];

		/**
		 * An array of values for each Huffman table that gives the highest numerical value of a Huffman
		 * code of a given bit length
		 */
		private int[,] codeLimits = new int[BZip2Constants.HUFFMAN_MAXIMUM_TABLES, BZip2Constants.HUFFMAN_DECODE_MAXIMUM_CODE_LENGTH + 1];

		/**
		 * A mapping for each Huffman table from canonical code index to output symbol
		 */
		private int[,] codeSymbols = new int[BZip2Constants.HUFFMAN_MAXIMUM_TABLES, BZip2Constants.HUFFMAN_MAXIMUM_ALPHABET_SIZE];

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
		private void createHuffmanDecodingTables(int alphabetSize, byte[,] tableCodeLengths) {

			for (int table = 0; table < tableCodeLengths.GetLength(0); table++) {

				int minimumLength = BZip2Constants.HUFFMAN_DECODE_MAXIMUM_CODE_LENGTH;
				int maximumLength = 0;

				// Find the minimum and maximum code length for the table
				for (int i = 0; i < alphabetSize; i++) {
					maximumLength = Math.Max(tableCodeLengths[table, i], maximumLength);
					minimumLength = Math.Min(tableCodeLengths[table, i], minimumLength);
				}
				minimumLengths[table] = minimumLength;

				// Calculate the first output symbol for each code length
				for (int i = 0; i < alphabetSize; i++) {
					codeBases[table, tableCodeLengths[table, i] + 1]++;
				}
				for (int i = 1; i < BZip2Constants.HUFFMAN_DECODE_MAXIMUM_CODE_LENGTH + 2; i++) {
					codeBases[table,i] += codeBases[table, i - 1];
				}

				// Calculate the first and last Huffman code for each code length (codes at a given
				// length are sequential in value)
				int code = 0;
				for (int i = minimumLength; i <= maximumLength; i++) {
					int cb = code;
					code += codeBases[table, i + 1] - codeBases[table, i];
					codeBases[table, i] = cb - codeBases[table, i];
					codeLimits[table, i] = code - 1;
					code <<= 1;
				}

				// Populate the mapping from canonical code index to output symbol
				int codeIndex = 0;
				for (int bitLength = minimumLength; bitLength <= maximumLength; bitLength++) {
					for (int symbol = 0; symbol < alphabetSize; symbol++) {
						if (tableCodeLengths[table, symbol] == bitLength) {
							codeSymbols[table, codeIndex++] = symbol;
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
		public int nextSymbol() {

			BZip2BitInputStream bitInputStream = this.bitInputStream;

			// Move to next group selector if required
			if (((++groupPosition % BZip2Constants.HUFFMAN_GROUP_RUN_LENGTH) == 0)) {
				groupIndex++;
				if (groupIndex == selectors.Length) {
					throw new BZip2Exception("Error decoding BZip2 block");
				}
				this.currentTable = selectors[groupIndex] & 0xff;
			}

			int codeLength = minimumLengths[currentTable];

			// Starting with the minimum bit length for the table, read additional bits one at a time
			// until a complete code is recognised
			uint codeBits = bitInputStream.readBits(codeLength);
			for (; codeLength <= BZip2Constants.HUFFMAN_DECODE_MAXIMUM_CODE_LENGTH; codeLength++) {
				if (codeBits <= codeLimits[currentTable, codeLength]) {
					// Convert the code to a symbol index and return
					return codeSymbols[currentTable, codeBits - codeBases[currentTable, codeLength]];
				}
				codeBits = (codeBits << 1) | bitInputStream.readBits(1);
			}

			// A valid code was not recognised
			throw new BZip2Exception("Error decoding BZip2 block");

		}


		/**
		 * @param bitInputStream The BZip2BitInputStream from which Huffman codes are read
		 * @param alphabetSize The total number of codes (uniform for each table)
		 * @param tableCodeLengths The Canonical Huffman code lengths for each table
		 * @param selectors The Huffman table number to use for each group of 50 symbols
		 */
		public BZip2HuffmanStageDecoder(BZip2BitInputStream bitInputStream, int alphabetSize, byte[,] tableCodeLengths, byte[] selectors) {

			this.bitInputStream = bitInputStream;
			this.selectors = selectors;
			currentTable = this.selectors[0];

			createHuffmanDecodingTables(alphabetSize, tableCodeLengths);

		}

	}
}
