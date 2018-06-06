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


/*
 * Block decoding consists of the following stages:
 * 1. Read block header - BZip2BlockDecompressor()
 * 2. Read Huffman tables - readHuffmanTables()
 * 3. Read and decode Huffman encoded data - decodeHuffmanData()
 * 4. Run-Length Decoding[2] - decodeHuffmanData()
 * 5. Inverse Move To Front Transform - decodeHuffmanData()
 * 6. Inverse Burrows Wheeler Transform - initialiseInverseBWT()
 * 7. Run-Length Decoding[1] - read()
 * 8. Optional Block De-Randomisation - read() (through decodeNextBWTByte())
 */
/**
 * Reads and decompresses a single BZip2 block
 */
public class BZip2BlockDecompressor {

	/**
	 * The BZip2 specification originally included the optional addition of a slight pseudo-random
	 * perturbation to the input data, in order to work around the block sorting algorithm's non-
	 * optimal performance on some types of input. The current mainline bzip2 does not require this
	 * and will not create randomised blocks, but compatibility is still required for old data (and
	 * third party compressors that haven't caught up). When decompressing a randomised block, for
	 * each value N in this array, a 1 will be XOR'd onto the output of the Burrows-Wheeler
	 * transform stage after N bytes, then the next N taken from the following entry.
	 */
	private static final int[] RNUMS = {
			619, 720, 127, 481, 931, 816, 813, 233, 566, 247, 985, 724, 205, 454, 863, 491,
			741, 242, 949, 214, 733, 859, 335, 708, 621, 574, 73, 654, 730, 472, 419, 436,
			278, 496, 867, 210, 399, 680, 480, 51, 878, 465, 811, 169, 869, 675, 611, 697,
			867, 561, 862, 687, 507, 283, 482, 129, 807, 591, 733, 623, 150, 238, 59, 379,
			684, 877, 625, 169, 643, 105, 170, 607, 520, 932, 727, 476, 693, 425, 174, 647,
			73, 122, 335, 530, 442, 853, 695, 249, 445, 515, 909, 545, 703, 919, 874, 474,
			882, 500, 594, 612, 641, 801, 220, 162, 819, 984, 589, 513, 495, 799, 161, 604,
			958, 533, 221, 400, 386, 867, 600, 782, 382, 596, 414, 171, 516, 375, 682, 485,
			911, 276, 98, 553, 163, 354, 666, 933, 424, 341, 533, 870, 227, 730, 475, 186,
			263, 647, 537, 686, 600, 224, 469, 68, 770, 919, 190, 373, 294, 822, 808, 206,
			184, 943, 795, 384, 383, 461, 404, 758, 839, 887, 715, 67, 618, 276, 204, 918,
			873, 777, 604, 560, 951, 160, 578, 722, 79, 804, 96, 409, 713, 940, 652, 934,
			970, 447, 318, 353, 859, 672, 112, 785, 645, 863, 803, 350, 139, 93, 354, 99,
			820, 908, 609, 772, 154, 274, 580, 184, 79, 626, 630, 742, 653, 282, 762, 623,
			680, 81, 927, 626, 789, 125, 411, 521, 938, 300, 821, 78, 343, 175, 128, 250,
			170, 774, 972, 275, 999, 639, 495, 78, 352, 126, 857, 956, 358, 619, 580, 124,
			737, 594, 701, 612, 669, 112, 134, 694, 363, 992, 809, 743, 168, 974, 944, 375,
			748, 52, 600, 747, 642, 182, 862, 81, 344, 805, 988, 739, 511, 655, 814, 334,
			249, 515, 897, 955, 664, 981, 649, 113, 974, 459, 893, 228, 433, 837, 553, 268,
			926, 240, 102, 654, 459, 51, 686, 754, 806, 760, 493, 403, 415, 394, 687, 700,
			946, 670, 656, 610, 738, 392, 760, 799, 887, 653, 978, 321, 576, 617, 626, 502,
			894, 679, 243, 440, 680, 879, 194, 572, 640, 724, 926, 56, 204, 700, 707, 151,
			457, 449, 797, 195, 791, 558, 945, 679, 297, 59, 87, 824, 713, 663, 412, 693,
			342, 606, 134, 108, 571, 364, 631, 212, 174, 643, 304, 329, 343, 97, 430, 751,
			497, 314, 983, 374, 822, 928, 140, 206, 73, 263, 980, 736, 876, 478, 430, 305,
			170, 514, 364, 692, 829, 82, 855, 953, 676, 246, 369, 970, 294, 750, 807, 827,
			150, 790, 288, 923, 804, 378, 215, 828, 592, 281, 565, 555, 710, 82, 896, 831,
			547, 261, 524, 462, 293, 465, 502, 56, 661, 821, 976, 991, 658, 869, 905, 758,
			745, 193, 768, 550, 608, 933, 378, 286, 215, 979, 792, 961, 61, 688, 793, 644,
			986, 403, 106, 366, 905, 644, 372, 567, 466, 434, 645, 210, 389, 550, 919, 135,
			780, 773, 635, 389, 707, 100, 626, 958, 165, 504, 920, 176, 193, 713, 857, 265,
			203, 50, 668, 108, 645, 990, 626, 197, 510, 357, 358, 850, 858, 364, 936, 638
	};

	/**
	 * Provides bits of input to decode
	 */
	private final BZip2BitInputStream bitInputStream;

	/**
	 * Calculates the block CRC from the fully decoded bytes of the block
	 */
	private final CRC32 crc = new CRC32();

	/**
	 * The CRC of the current block as read from the block header
	 */
	private final int blockCRC;

	/**
	 * {@code true} if the current block is randomised, otherwise {@code false}
	 */
	private final boolean blockRandomised;

	/* Huffman Decoding stage */

	/**
	 * The end-of-block Huffman symbol. Decoding of the block ends when this is encountered
	 */
	private int huffmanEndOfBlockSymbol;

	/**
	 * A map from Huffman symbol index to output character. Some types of data (e.g. ASCII text)
	 * may contain only a limited number of byte values; Huffman symbols are only allocated to
	 * those values that actually occur in the uncompressed data.
	 */
	private final byte[] huffmanSymbolMap = new byte[256];

	/* Move To Front stage */

	/**
	 * Counts of each byte value within the {@link bwtTransformedArray} data. Collected at the Move
	 * To Front stage, consumed by the Inverse Burrows Wheeler Transform stage
	 */
	private final int[] bwtByteCounts = new int[256];

	/**
	 * The Burrows-Wheeler Transform processed data. Read at the Move To Front stage, consumed by the
	 * Inverse Burrows Wheeler Transform stage 
	 */
	private byte[] bwtBlock;

	/* Inverse Burrows-Wheeler Transform stage */

	/**
	 * At each position contains the union of :-
	 *   An output character (8 bits)
	 *   A pointer from each position to its successor (24 bits, left shifted 8 bits)
	 * As the pointer cannot exceed the maximum block size of 900k, 24 bits is more than enough to
	 * hold it; Folding the character data into the spare bits while performing the inverse BWT,
	 * when both pieces of information are available, saves a large number of memory accesses in
	 * the final decoding stages.
	 */
	private int[] bwtMergedPointers;

	/**
	 * The current merged pointer into the Burrow-Wheeler Transform array
	 */
	private int bwtCurrentMergedPointer;

	/**
	 * The actual length in bytes of the current block at the Inverse Burrows Wheeler Transform
	 * stage (before final Run-Length Decoding)
	 */
	private int bwtBlockLength;

	/**
	 * The number of output bytes that have been decoded up to the Inverse Burrows Wheeler Transform
	 * stage
	 */
	private int bwtBytesDecoded;

	/* Run-Length Encoding and Random Perturbation stage */

	/**
	 * The most recently RLE decoded byte
	 */
	private int rleLastDecodedByte = -1;

	/**
	 * The number of previous identical output bytes decoded. After 4 identical bytes, the next byte
	 * decoded is an RLE repeat count
	 */
	private int rleAccumulator;

	/**
	 * The RLE repeat count of the current decoded byte. When this reaches zero, a new byte is
	 * decoded
	 */
	private int rleRepeat;

	/**
	 * If the current block is randomised, the position within the RNUMS randomisation array
	 */
	private int randomIndex = 0;

	/**
	 * If the current block is randomised, the remaining count at the current RNUMS position
	 */
	private int randomCount = RNUMS[0] - 1;


	/**
	 * Read and decode the block's Huffman tables
	 * @return A decoder for the Huffman stage that uses the decoded tables
	 * @throws IOException if the input stream reaches EOF before all table data has been read
	 */
	private BZip2HuffmanStageDecoder readHuffmanTables() throws IOException {

		final BZip2BitInputStream bitInputStream = this.bitInputStream;
		final byte[] huffmanSymbolMap = this.huffmanSymbolMap;
		final byte[][] tableCodeLengths = new byte[BZip2Constants.HUFFMAN_MAXIMUM_TABLES][BZip2Constants.HUFFMAN_MAXIMUM_ALPHABET_SIZE];

		/* Read Huffman symbol to output byte map */
		int huffmanUsedRanges = bitInputStream.readBits (16);
		int huffmanSymbolCount = 0;

		for (int i = 0; i < 16; i++) {
			if ((huffmanUsedRanges & ((1 << 15) >>> i)) != 0) {
				for (int j = 0, k = i << 4; j < 16; j++, k++) {
					if (bitInputStream.readBoolean()) {
						huffmanSymbolMap[huffmanSymbolCount++] = (byte)k;
					}
				}
			}
		}
		int endOfBlockSymbol = huffmanSymbolCount + 1;
		this.huffmanEndOfBlockSymbol = endOfBlockSymbol;

		/* Read total number of tables and selectors*/
		final int totalTables = bitInputStream.readBits (3);
		final int totalSelectors = bitInputStream.readBits (15);
		if (
				   (totalTables < BZip2Constants.HUFFMAN_MINIMUM_TABLES)
				|| (totalTables > BZip2Constants.HUFFMAN_MAXIMUM_TABLES)
				|| (totalSelectors < 1)
				|| (totalSelectors > BZip2Constants.HUFFMAN_MAXIMUM_SELECTORS)
		   )
		{
			throw new BZip2Exception ("BZip2 block Huffman tables invalid");
		}

		/* Read and decode MTFed Huffman selector list */
		final MoveToFront tableMTF = new MoveToFront();
		final byte[] selectors = new byte[totalSelectors];
		for (int selector = 0; selector < totalSelectors; selector++) {
			selectors[selector] = tableMTF.indexToFront (bitInputStream.readUnary());
		}

		/* Read the Canonical Huffman code lengths for each table */
		for (int table = 0; table < totalTables; table++) {
			int currentLength = bitInputStream.readBits (5);
			for (int i = 0; i <= endOfBlockSymbol; i++) {
				while (bitInputStream.readBoolean()) {
					currentLength += bitInputStream.readBoolean() ? -1 : 1;
				}
				tableCodeLengths[table][i] = (byte)currentLength;
			}
		}

		return new BZip2HuffmanStageDecoder (bitInputStream, endOfBlockSymbol + 1, tableCodeLengths, selectors);

	}


	/**
	 * Reads the Huffman encoded data from the input stream, performs Run-Length Decoding and
	 * applies the Move To Front transform to reconstruct the Burrows-Wheeler Transform array
	 * @param huffmanDecoder The Huffman decoder through which symbols are read
	 * @throws IOException if an end-of-block symbol was not decoded within the declared block size
	 */
	private void decodeHuffmanData (final BZip2HuffmanStageDecoder huffmanDecoder) throws IOException {

		final byte[] bwtBlock = this.bwtBlock;
		final byte[] huffmanSymbolMap = this.huffmanSymbolMap;
		final int streamBlockSize = this.bwtBlock.length;
		final int huffmanEndOfBlockSymbol = this.huffmanEndOfBlockSymbol;
		final int[] bwtByteCounts = this.bwtByteCounts;
		final MoveToFront symbolMTF = new MoveToFront();
		int bwtBlockLength = 0;
		int repeatCount = 0;
		int repeatIncrement = 1;
		int mtfValue = 0;

		for (;;) {
			final int nextSymbol = huffmanDecoder.nextSymbol();

			if (nextSymbol == BZip2Constants.HUFFMAN_SYMBOL_RUNA) {
				repeatCount += repeatIncrement;
				repeatIncrement <<= 1;
			} else if (nextSymbol == BZip2Constants.HUFFMAN_SYMBOL_RUNB) {
				repeatCount += repeatIncrement << 1;
				repeatIncrement <<= 1;
			} else {
				if (repeatCount > 0) {
					if (bwtBlockLength + repeatCount > streamBlockSize) {
						throw new BZip2Exception ("BZip2 block exceeds declared block size");
					}
					final byte nextByte = huffmanSymbolMap[mtfValue];
					bwtByteCounts[nextByte & 0xff] += repeatCount;
					while (--repeatCount >= 0) {
						bwtBlock[bwtBlockLength++] = nextByte;
					}

					repeatCount = 0;
					repeatIncrement = 1;
				}

				if (nextSymbol == huffmanEndOfBlockSymbol)
					break;

				if (bwtBlockLength >= streamBlockSize) {
					throw new BZip2Exception ("BZip2 block exceeds declared block size");
				}

				mtfValue = symbolMTF.indexToFront (nextSymbol - 1) & 0xff;

				final byte nextByte = huffmanSymbolMap[mtfValue];
				bwtByteCounts[nextByte & 0xff]++;
				bwtBlock[bwtBlockLength++] = nextByte;

			}
		}

		this.bwtBlockLength = bwtBlockLength;

	}


	/**
	 * Set up the Inverse Burrows-Wheeler Transform merged pointer array
	 * @param bwtStartPointer The start pointer into the BWT array
	 * @throws IOException if the given start pointer is invalid
	 */
	private void initialiseInverseBWT (final int bwtStartPointer) throws IOException {

		final byte[] bwtBlock  = this.bwtBlock;
		final int[] bwtMergedPointers = new int[this.bwtBlockLength];
		final int[] characterBase = new int[256];

		if ((bwtStartPointer < 0) || (bwtStartPointer >= this.bwtBlockLength)) {
			throw new BZip2Exception ("BZip2 start pointer invalid");
		}

		// Cumulatise character counts
		System.arraycopy (this.bwtByteCounts, 0, characterBase, 1, 255);
		for (int i = 2; i <= 255; i++) {
			characterBase[i] += characterBase[i - 1];
		}

		// Merged-Array Inverse Burrows-Wheeler Transform
		// Combining the output characters and forward pointers into a single array here, where we
		// have already read both of the corresponding values, cuts down on memory accesses in the
		// final walk through the array
		for (int i = 0; i < this.bwtBlockLength; i++) {
			int value = bwtBlock[i] & 0xff;
			bwtMergedPointers[characterBase[value]++] = (i << 8) + value;
		}

		this.bwtBlock = null;
		this.bwtMergedPointers = bwtMergedPointers;
		this.bwtCurrentMergedPointer = bwtMergedPointers[bwtStartPointer];

	}


	/**
	 * Decodes a byte from the Burrows-Wheeler Transform stage. If the block has randomisation
	 * applied, reverses the randomisation
	 * @return The decoded byte
	 */
	private int decodeNextBWTByte() {

		int mergedPointer = this.bwtCurrentMergedPointer;
		int nextDecodedByte =  mergedPointer & 0xff;
		this.bwtCurrentMergedPointer = this.bwtMergedPointers[mergedPointer >>> 8];

		if (this.blockRandomised) {
			if (--this.randomCount == 0) {
				nextDecodedByte ^= 1;
				this.randomIndex = (this.randomIndex + 1) % 512;
				this.randomCount = RNUMS[this.randomIndex];
			}
		}

		this.bwtBytesDecoded++;

		return nextDecodedByte;

	}


	/**
	 * Decodes a byte from the final Run-Length Encoding stage, pulling a new byte from the
	 * Burrows-Wheeler Transform stage when required
	 * @return The decoded byte, or -1 if there are no more bytes
	 */
	public int read() {

		while (this.rleRepeat < 1) {

			if (this.bwtBytesDecoded == this.bwtBlockLength) {
				return -1;
			}

			int nextByte = decodeNextBWTByte();

			if (nextByte != this.rleLastDecodedByte) {
				// New byte, restart accumulation
				this.rleLastDecodedByte = nextByte;
				this.rleRepeat = 1;
				this.rleAccumulator = 1;
				this.crc.updateCRC (nextByte);
			} else {
				if (++this.rleAccumulator == 4) {
					// Accumulation complete, start repetition
					int rleRepeat = decodeNextBWTByte() + 1;
					this.rleRepeat = rleRepeat;
					this.rleAccumulator = 0;
					this.crc.updateCRC (nextByte, rleRepeat);
				} else {
					this.rleRepeat = 1;
					this.crc.updateCRC (nextByte);
				}
			}

		}

		this.rleRepeat--;

		return this.rleLastDecodedByte;

	}


	/**
	 * Decodes multiple bytes from the final Run-Length Encoding stage, pulling new bytes from the
	 * Burrows-Wheeler Transform stage when required
	 * @param destination The array to write to
	 * @param offset The starting position within the array
	 * @param length The number of bytes to read
	 * @return The number of bytes actually read, or -1 if there are no bytes left in the block
	 */
	public int read (final byte[] destination, int offset, final int length) {

		int i;
		for (i = 0; i < length; i++, offset++) {
			int decoded = read();
			if (decoded == -1) {
				return (i == 0) ? -1 : i;
			}
			destination[offset] = (byte)decoded;
		}
		return i;

	}


	/**
	 * Verify and return the block CRC. This method may only be called after all of the block's
	 * bytes have been read
	 * @return The block CRC
	 * @throws IOException if the CRC verification failed
	 */
	public int checkCRC() throws IOException {

		if (this.blockCRC != this.crc.getCRC()) {
			throw new BZip2Exception ("BZip2 block CRC error");
		}

		return this.crc.getCRC();

	}


	/**
	 * @param bitInputStream The BZip2BitInputStream to read from
	 * @param blockSize The maximum decoded size of the block
	 * @throws IOException If the block could not be decoded
	 */
	public BZip2BlockDecompressor (final BZip2BitInputStream bitInputStream, final int blockSize) throws IOException {

		this.bitInputStream = bitInputStream;
		this.bwtBlock = new byte[blockSize];

		final int bwtStartPointer;

		// Read block header
		this.blockCRC = this.bitInputStream.readInteger();
		this.blockRandomised = this.bitInputStream.readBoolean();
		bwtStartPointer = this.bitInputStream.readBits (24);

		// Read block data and decode through to the Inverse Burrows Wheeler Transform stage
		BZip2HuffmanStageDecoder huffmanDecoder = readHuffmanTables();
		decodeHuffmanData (huffmanDecoder);
		initialiseInverseBWT (bwtStartPointer);

	}


}
