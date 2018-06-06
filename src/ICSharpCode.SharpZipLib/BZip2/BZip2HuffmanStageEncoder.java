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
import java.util.Arrays;


/**
 * An encoder for the BZip2 Huffman encoding stage
 */
class BZip2HuffmanStageEncoder {

	/**
	 * Used in initial Huffman table generation
	 */
	private static final int HUFFMAN_HIGH_SYMBOL_COST = 15;

	/**
	 * The BZip2BitOutputStream to which the Huffman tables and data is written
	 */
	private final BZip2BitOutputStream bitOutputStream;

	/**
	 * The output of the Move To Front Transform and Run Length Encoding[2] stages
	 */
	private final char[] mtfBlock;

	/**
	 * The actual number of values contained in the {@link mtfBlock} array
	 */
	private int mtfLength;

	/**
	 * The number of unique values in the {@link mtfBlock} array
	 */
	private int mtfAlphabetSize;

	/**
	 * The global frequencies of values within the {@link mtfBlock} array
	 */
	private final int[] mtfSymbolFrequencies;

	/**
	 * The Canonical Huffman code lengths for each table
	 */
	private final int[][] huffmanCodeLengths;

	/**
	 * Merged code symbols for each table. The value at each position is ((code length << 24) | code)
	 */
	private final int[][] huffmanMergedCodeSymbols;

	/**
	 * The selectors for each segment
	 */
	private final byte[] selectors;


	/**
	 * Selects an appropriate table count for a given MTF length
	 * @param mtfLength The length to select a table count for
	 * @return The selected table count
	 */
	private static int selectTableCount (final int mtfLength) {

		if (mtfLength >= 2400) return 6;
		if (mtfLength >= 1200) return 5;
		if (mtfLength >= 600) return 4;
		if (mtfLength >= 200) return 3;
		return 2;

	}


	/**
	 * Generate a Huffman code length table for a given list of symbol frequencies
	 * @param alphabetSize The total number of symbols
	 * @param symbolFrequencies The frequencies of the symbols
	 * @param codeLengths The array to which the generated code lengths should be written
	 */
	private static void generateHuffmanCodeLengths (final int alphabetSize, final int[] symbolFrequencies, final int[] codeLengths) {

		final int[] mergedFrequenciesAndIndices = new int[alphabetSize];
		final int[] sortedFrequencies = new int[alphabetSize];

		// The Huffman allocator needs its input symbol frequencies to be sorted, but we need to return code lengths in the same order as the
		// corresponding frequencies are passed in

		// The symbol frequency and index are merged into a single array of integers - frequency in the high 23 bits, index in the low 9 bits.
		//     2^23 = 8,388,608 which is higher than the maximum possible frequency for one symbol in a block
		//     2^9 = 512 which is higher than the maximum possible alphabet size (== 258)
		// Sorting this array simultaneously sorts the frequencies and leaves a lookup that can be used to cheaply invert the sort
		for (int i = 0; i < alphabetSize; i++) {
			mergedFrequenciesAndIndices[i] = (symbolFrequencies[i] << 9) | i;
		}
		Arrays.sort (mergedFrequenciesAndIndices);
		for (int i = 0; i < alphabetSize; i++) {
			sortedFrequencies[i] = mergedFrequenciesAndIndices[i] >>> 9;
		}

		// Allocate code lengths - the allocation is in place, so the code lengths will be in the sortedFrequencies array afterwards
		HuffmanAllocator.allocateHuffmanCodeLengths (sortedFrequencies, BZip2Constants.HUFFMAN_ENCODE_MAXIMUM_CODE_LENGTH);

		// Reverse the sort to place the code lengths in the same order as the symbols whose frequencies were passed in
		for (int i = 0; i < alphabetSize; i++) {
			codeLengths[mergedFrequenciesAndIndices[i] & 0x1ff] = sortedFrequencies[i];
		}

	}


	/**
	 * Generate initial Huffman code length tables, giving each table a different low cost section
	 * of the alphabet that is roughly equal in overall cumulative frequency. Note that the initial
	 * tables are invalid for actual Huffman code generation, and only serve as the seed for later
	 * iterative optimisation in {@link #optimiseSelectorsAndHuffmanTables(int)}.
	 */
	private void generateHuffmanOptimisationSeeds () {

		final int[][] huffmanCodeLengths = this.huffmanCodeLengths;
		final int[] mtfSymbolFrequencies = this.mtfSymbolFrequencies;
		final int mtfAlphabetSize = this.mtfAlphabetSize;

		final int totalTables = huffmanCodeLengths.length;

		int remainingLength = this.mtfLength;
		int lowCostEnd = -1;

		for (int i = 0; i < totalTables; i++) {

			final int targetCumulativeFrequency = remainingLength / (totalTables - i);
			final int lowCostStart = lowCostEnd + 1;
			int actualCumulativeFrequency = 0;

			while ((actualCumulativeFrequency < targetCumulativeFrequency) && (lowCostEnd < (mtfAlphabetSize - 1))) {
				actualCumulativeFrequency += mtfSymbolFrequencies[++lowCostEnd];
			}

			if ((lowCostEnd > lowCostStart) && (i != 0) && (i != (totalTables - 1)) && (((totalTables - i) & 1) == 0)) {
				actualCumulativeFrequency -= mtfSymbolFrequencies[lowCostEnd--];
			}

			final int[] tableCodeLengths = huffmanCodeLengths[i];
			for (int j = 0; j < mtfAlphabetSize; j++) {
				if ((j < lowCostStart) || (j > lowCostEnd)) {
					tableCodeLengths[j] = HUFFMAN_HIGH_SYMBOL_COST;
				}
			}

			remainingLength -= actualCumulativeFrequency;

		}

	}


	/**
	 * Co-optimise the selector list and the alternative Huffman table code lengths. This method is
	 * called repeatedly in the hope that the total encoded size of the selectors, the Huffman code
	 * lengths and the block data encoded with them will converge towards a minimum.<br>
	 * If the data is highly incompressible, it is possible that the total encoded size will
	 * instead diverge (increase) slightly.<br>
	 * @param storeSelectors If {@code true}, write out the (final) chosen selectors
	 */
	private void optimiseSelectorsAndHuffmanTables (final boolean storeSelectors) {

		final char[] mtfBlock = this.mtfBlock;
		final byte[] selectors = this.selectors;
		final int[][] huffmanCodeLengths = this.huffmanCodeLengths;
		final int mtfLength = this.mtfLength;
		final int mtfAlphabetSize = this.mtfAlphabetSize;

		final int totalTables = huffmanCodeLengths.length;
		final int[][] tableFrequencies = new int[totalTables][mtfAlphabetSize];

		int selectorIndex = 0;

		// Find the best table for each group of 50 block bytes based on the current Huffman code lengths
		for (int groupStart = 0; groupStart < mtfLength;) {

			final int groupEnd = Math.min (groupStart + BZip2Constants.HUFFMAN_GROUP_RUN_LENGTH, mtfLength) - 1;

			// Calculate the cost of this group when encoded by each table
			short[] cost = new short[totalTables];
			for (int i = groupStart; i <= groupEnd; i++) {
				final int value = mtfBlock[i];
				for (int j = 0; j < totalTables; j++) {
					cost[j] += huffmanCodeLengths[j][value];
				}
			}

			// Find the table with the least cost for this group
			byte bestTable = 0;
			int bestCost = cost[0];
			for (byte i = 1 ; i < totalTables; i++) {
				final int tableCost = cost[i];
				if (tableCost < bestCost) {
					bestCost = tableCost;
					bestTable = i;
				}
			}

			// Accumulate symbol frequencies for the table chosen for this block
			final int[] bestGroupFrequencies = tableFrequencies[bestTable];
			for (int i = groupStart; i <= groupEnd; i++) {
				bestGroupFrequencies[mtfBlock[i]]++;
			}

			// Store a selector indicating the table chosen for this block
			if (storeSelectors) {
				selectors[selectorIndex++] = bestTable;
			}

			groupStart = groupEnd + 1;

		}

		// Generate new Huffman code lengths based on the frequencies for each table accumulated in this iteration
		for (int i = 0; i < totalTables; i++) {
			generateHuffmanCodeLengths (mtfAlphabetSize, tableFrequencies[i], huffmanCodeLengths[i]);
		}

	}


	/**
	 * Assigns Canonical Huffman codes based on the calculated lengths
	 */
	private void assignHuffmanCodeSymbols() {

		final int[][] huffmanMergedCodeSymbols = this.huffmanMergedCodeSymbols;
		final int[][] huffmanCodeLengths = this.huffmanCodeLengths;
		final int mtfAlphabetSize = this.mtfAlphabetSize;

		final int totalTables = huffmanCodeLengths.length;

		for (int i = 0; i < totalTables; i++) {

			final int[] tableLengths = huffmanCodeLengths[i];

			int minimumLength = 32;
			int maximumLength = 0;
			for (int j = 0; j < mtfAlphabetSize; j++) {
				final int length = tableLengths[j];
				if (length > maximumLength) {
					maximumLength = length;
				}
				if (length < minimumLength) {
					minimumLength = length;
				}
			}

			int code = 0;
			for (int j = minimumLength; j <= maximumLength; j++) {
				for (int k = 0; k < mtfAlphabetSize; k++) {
					if ((huffmanCodeLengths[i][k] & 0xff) == j) {
						huffmanMergedCodeSymbols[i][k] = (j << 24) | code;
						code++;
					}
				}
				code <<= 1;
			}

		}

	}


	/**
	 * Write out the selector list and Huffman tables
	 * @throws IOException on any I/O error writing the data
	 */
	private void writeSelectorsAndHuffmanTables() throws IOException {

		final BZip2BitOutputStream bitOutputStream = this.bitOutputStream;
		final byte[] selectors = this.selectors;
		final int totalSelectors = selectors.length;
		final int[][] huffmanCodeLengths = this.huffmanCodeLengths;
		final int mtfAlphabetSize = this.mtfAlphabetSize;

		final int totalTables = huffmanCodeLengths.length;

		bitOutputStream.writeBits (3, totalTables);
		bitOutputStream.writeBits (15, totalSelectors);

		// Write the selectors
		MoveToFront selectorMTF = new MoveToFront();
		for (int i = 0; i < totalSelectors; i++) {
			bitOutputStream.writeUnary (selectorMTF.valueToFront (selectors[i]));
		}

		// Write the Huffman tables
		for (int i = 0; i < totalTables; i++) {
			final int[] tableLengths = huffmanCodeLengths[i];
			int currentLength = tableLengths[0];

			bitOutputStream.writeBits (5, currentLength);

			for (int j = 0; j < mtfAlphabetSize; j++) {
				final int codeLength = tableLengths[j];
				final int value = (currentLength < codeLength) ? 2 : 3;
				int delta = Math.abs (codeLength - currentLength);
				while (delta-- > 0) {
					bitOutputStream.writeBits (2, value);
				}
				bitOutputStream.writeBoolean (false);
				currentLength = codeLength;
			}
		}

	}


	/**
	 * Writes out the encoded block data
	 * @throws IOException on any I/O error writing the data
	 */
	private void writeBlockData() throws IOException {

		final BZip2BitOutputStream bitOutputStream = this.bitOutputStream;
		final int[][] huffmanMergedCodeSymbols = this.huffmanMergedCodeSymbols;
		final byte[] selectors = this.selectors;
		final char[] mtf = this.mtfBlock;
		final int mtfLength = this.mtfLength;

		int selectorIndex = 0;

		for (int mtfIndex = 0; mtfIndex < mtfLength;) {
			final int groupEnd = Math.min (mtfIndex + BZip2Constants.HUFFMAN_GROUP_RUN_LENGTH, mtfLength) - 1;
			final int[] tableMergedCodeSymbols = huffmanMergedCodeSymbols[selectors[selectorIndex++]];

			while (mtfIndex <= groupEnd) {
				final int mergedCodeSymbol = tableMergedCodeSymbols[mtf[mtfIndex++]];
				bitOutputStream.writeBits (mergedCodeSymbol >>> 24, mergedCodeSymbol);
			}
		}

	}


	/**
	 * Encodes and writes the block data
	 * @throws IOException on any I/O error writing the data
	 */
	public void encode() throws IOException {

		// Create optimised selector list and Huffman tables
		generateHuffmanOptimisationSeeds();
		for (int i = 3; i >= 0; i--) {
			optimiseSelectorsAndHuffmanTables (i == 0);
		}
		assignHuffmanCodeSymbols();

		// Write out the tables and the block data encoded with them
		writeSelectorsAndHuffmanTables();
		writeBlockData();

	}


	/**
	 * @param bitOutputStream The BZip2BitOutputStream to write to
	 * @param mtfBlock The MTF block data
	 * @param mtfLength The actual length of the MTF block
	 * @param mtfAlphabetSize The size of the MTF block's alphabet
	 * @param mtfSymbolFrequencies The frequencies the MTF block's symbols
	 */
	public BZip2HuffmanStageEncoder (final BZip2BitOutputStream bitOutputStream, final char[] mtfBlock, final int mtfLength, final int mtfAlphabetSize, final int[] mtfSymbolFrequencies) {

		this.bitOutputStream = bitOutputStream;
		this.mtfBlock = mtfBlock;
		this.mtfLength = mtfLength;
		this.mtfAlphabetSize = mtfAlphabetSize;
		this.mtfSymbolFrequencies = mtfSymbolFrequencies;

		final int totalTables = selectTableCount (mtfLength);

		this.huffmanCodeLengths = new int[totalTables][mtfAlphabetSize];
		this.huffmanMergedCodeSymbols = new int[totalTables][mtfAlphabetSize];
		this.selectors = new byte [(mtfLength + BZip2Constants.HUFFMAN_GROUP_RUN_LENGTH - 1) / BZip2Constants.HUFFMAN_GROUP_RUN_LENGTH];

	}

}
