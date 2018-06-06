package org.itadaki.bzip2;

/**
 * An encoder for the BZip2 Move To Front Transform and Run-Length Encoding[2] stages<br>
 * Although conceptually these two stages are separate, it is computationally efficient to perform
 * them in one pass.
 */
public class BZip2MTFAndRLE2StageEncoder {

	/**
	 * The Burrows-Wheeler transformed block
	 */
	private final int[] bwtBlock;

	/**
	 * Actual length of the data in the {@link bwtBlock} array
	 */
	private int bwtLength;

	/**
	 * At each position, {@code true} if the byte value with that index is present within the block,
	 * otherwise {@code false} 
	 */
	private final boolean[] bwtValuesInUse;

	/**
	 * The output of the Move To Front Transform and Run-Length Encoding[2] stages
	 */
	private final char[] mtfBlock;

	/**
	 * The actual number of values contained in the {@link mtfBlock} array
	 */
	private int mtfLength;

	/**
	 * The global frequencies of values within the {@link mtfBlock} array
	 */
	private final int[] mtfSymbolFrequencies = new int[BZip2Constants.HUFFMAN_MAXIMUM_ALPHABET_SIZE];

	/**
	 * The encoded alphabet size
	 */
	private int alphabetSize;


	/**
	 * Performs the Move To Front transform and Run Length Encoding[1] stages
	 */
	public void encode() {

		final int bwtLength = this.bwtLength;
		final boolean[] bwtValuesInUse = this.bwtValuesInUse;
		final int[] bwtBlock = this.bwtBlock;
		final char[] mtfBlock = this.mtfBlock;
		final int[] mtfSymbolFrequencies = this.mtfSymbolFrequencies;
		final byte[] huffmanSymbolMap = new byte[256];
		final MoveToFront symbolMTF = new MoveToFront();

		int totalUniqueValues = 0;
		for (int i = 0; i < 256; i++) {
			if (bwtValuesInUse[i]) {
				huffmanSymbolMap[i] = (byte) totalUniqueValues++;
			}
		}

		final int endOfBlockSymbol = totalUniqueValues + 1;

		int mtfIndex = 0;
		int repeatCount = 0;
		int totalRunAs = 0;
		int totalRunBs = 0;

		for (int i = 0; i < bwtLength; i++) {

			// Move To Front
			final int mtfPosition = symbolMTF.valueToFront (huffmanSymbolMap[bwtBlock[i] & 0xff]);

			// Run Length Encode
			if (mtfPosition == 0) {
				repeatCount++;
			} else {
				if (repeatCount > 0) {
					repeatCount--;
					while (true) {
						if ((repeatCount & 1) == 0) {
							mtfBlock[mtfIndex++] = BZip2Constants.HUFFMAN_SYMBOL_RUNA;
							totalRunAs++;
						} else {
							mtfBlock[mtfIndex++] = BZip2Constants.HUFFMAN_SYMBOL_RUNB;
							totalRunBs++;
						}

						if (repeatCount <= 1) {
							break;
						}
						repeatCount = (repeatCount - 2) >>> 1;
					}
					repeatCount = 0;
				}

				mtfBlock[mtfIndex++] = (char) (mtfPosition + 1);
				mtfSymbolFrequencies[mtfPosition + 1]++;
			}

		}

		if (repeatCount > 0) {
			repeatCount--;
			while (true) {
				if ((repeatCount & 1) == 0) {
					mtfBlock[mtfIndex++] = BZip2Constants.HUFFMAN_SYMBOL_RUNA;
					totalRunAs++;
				} else {
					mtfBlock[mtfIndex++] = BZip2Constants.HUFFMAN_SYMBOL_RUNB;
					totalRunBs++;
				}

				if (repeatCount <= 1) {
					break;
				}
				repeatCount = (repeatCount - 2) >>> 1;
			}
		}

		mtfBlock[mtfIndex] = (char) endOfBlockSymbol;
		mtfSymbolFrequencies[endOfBlockSymbol]++;
		mtfSymbolFrequencies[BZip2Constants.HUFFMAN_SYMBOL_RUNA] += totalRunAs;
		mtfSymbolFrequencies[BZip2Constants.HUFFMAN_SYMBOL_RUNB] += totalRunBs;

		this.mtfLength = mtfIndex + 1;
		this.alphabetSize = endOfBlockSymbol + 1;

	}


	/**
	 * @return The encoded MTF block
	 */
	public char[] getMtfBlock() {

		return this.mtfBlock;

	}


	/**
	 * @return The actual length of the MTF block
	 */
	public int getMtfLength() {

		return this.mtfLength;

	}


	/**
	 * @return The size of the MTF block's alphabet
	 */
	public int getMtfAlphabetSize() {

		return this.alphabetSize;

	}


	/**
	 * @return The frequencies of the MTF block's symbols
	 */
	public int[] getMtfSymbolFrequencies() {

		return this.mtfSymbolFrequencies;

	}


	/**
	 * @param bwtBlock The Burrows Wheeler Transformed block data
	 * @param bwtLength The actual length of the BWT data
	 * @param bwtValuesPresent The values that are present within the BWT data. For each index,
	 *            {@code true} if that value is present within the data, otherwise {@code false}
	 */
	public BZip2MTFAndRLE2StageEncoder (final int[] bwtBlock, final int bwtLength, final boolean[] bwtValuesPresent) {

		this.bwtBlock = bwtBlock;
		this.bwtLength = bwtLength;
		this.bwtValuesInUse = bwtValuesPresent;
		this.mtfBlock = new char[bwtLength + 1];

	}

}
