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


/**
 * An in-place, length restricted Canonical Huffman code length allocator
 * 
 * Based on the algorithm proposed by R. L. Milidiú, A. A. Pessoa and E. S. Laber in "In-place
 * Length-Restricted Prefix Coding" (see: http://www-di.inf.puc-rio.br/~laber/public/spire98.ps)
 * and incorporating additional ideas from the implementation of "shcodec" by Simakov Alexander
 * (see: http://webcenter.ru/~xander/)
 */
public class HuffmanAllocator {

	/**
	 * FIRST() function
	 * @param array The code length array
	 * @param i The input position
	 * @param nodesToMove The number of internal nodes to be relocated
	 * @return The smallest {@code k} such that {@code nodesToMove <= k <= i} and
	 *         {@code i <= (array[k] % array.length)}
	 */
	private static int first (final int[] array, int i, final int nodesToMove) {

		final int length = array.length;
		final int limit = i;
		int k = array.length - 2;

		while ((i >= nodesToMove) && ((array[i] % length) > limit)) {
			k = i;
			i -= (limit - i + 1);
		}
		i = Math.max (nodesToMove - 1, i);

		while (k > (i + 1)) {
			int temp = (i + k) >> 1;
			if ((array[temp] % length) > limit) {
				k = temp;
			} else {
				i = temp;
			}
		}

		return k;

	}


	/**
	 * Fills the code array with extended parent pointers
	 * @param array The code length array
	 */
	private static void setExtendedParentPointers (final int[] array) {

		final int length = array.length;

		array[0] += array[1];

		for (int headNode = 0, tailNode = 1, topNode = 2; tailNode < (length - 1); tailNode++) {
			int temp;
			if ((topNode >= length) || (array[headNode] < array[topNode])) {
				temp = array[headNode];
				array[headNode++] = tailNode;
			} else {
				temp = array[topNode++];
			}

			if ((topNode >= length) || ((headNode < tailNode) && (array[headNode] < array[topNode]))) {
				temp += array[headNode];
				array[headNode++] = tailNode + length;
			} else {
				temp += array[topNode++];
			}

			array[tailNode] = temp;
		}

	}


	/**
	 * Finds the number of nodes to relocate in order to achieve a given code length limit
	 * @param array The code length array
	 * @param maximumLength The maximum bit length for the generated codes
	 * @return The number of nodes to relocate
	 */
	private static int findNodesToRelocate (final int[] array, final int maximumLength) {

		int currentNode = array.length - 2;
		for (int currentDepth = 1; (currentDepth < (maximumLength - 1)) && (currentNode > 1); currentDepth++) {
			currentNode =  first (array, currentNode - 1, 0);
		}

		return currentNode;

	}


	/**
	 * A final allocation pass with no code length limit
	 * @param array The code length array
	 */
	private static void allocateNodeLengths (final int[] array) {

		int firstNode = array.length - 2;
		int nextNode = array.length - 1;

		for (int currentDepth = 1, availableNodes = 2; availableNodes > 0; currentDepth++) {
			final int lastNode = firstNode;
			firstNode = first (array, lastNode - 1, 0);

			for (int i = availableNodes - (lastNode - firstNode); i > 0; i--) {
				array[nextNode--] = currentDepth;
			}

			availableNodes = (lastNode - firstNode) << 1;
		}

	}


	/**
	 * A final allocation pass that relocates nodes in order to achieve a maximum code length limit
	 * @param array The code length array
	 * @param nodesToMove The number of internal nodes to be relocated
	 * @param insertDepth The depth at which to insert relocated nodes
	 */
	private static void allocateNodeLengthsWithRelocation (final int[] array, final int nodesToMove, final int insertDepth) {

		int firstNode = array.length - 2;
		int nextNode = array.length - 1;
		int currentDepth = (insertDepth == 1) ? 2 : 1;
		int nodesLeftToMove = (insertDepth == 1) ? nodesToMove - 2 : nodesToMove;

		for (int availableNodes = currentDepth << 1; availableNodes > 0; currentDepth++) {
			final int lastNode = firstNode;
			firstNode = (firstNode <= nodesToMove) ? firstNode : first (array, lastNode - 1, nodesToMove);

			int offset = 0;
			if (currentDepth >= insertDepth) {
				offset = Math.min (nodesLeftToMove, 1 << (currentDepth - insertDepth));
			} else if (currentDepth == (insertDepth - 1)) {
				offset = 1;
				if ((array[firstNode]) == lastNode) {
					firstNode++;
				}
			}

			for (int i = availableNodes - (lastNode - firstNode + offset); i > 0; i--) {
				array[nextNode--] = currentDepth;
			}

			nodesLeftToMove -= offset;
			availableNodes = (lastNode - firstNode + offset) << 1;
		}

	}


	/**
	 * Allocates Canonical Huffman code lengths in place based on a sorted frequency array
	 * @param array On input, a sorted array of symbol frequencies; On output, an array of Canonical
	 *              Huffman code lengths
	 * @param maximumLength The maximum code length. Must be at least {@code ceil(log2(array.length))}
	 */
	public static void allocateHuffmanCodeLengths (final int[] array, final int maximumLength) {

		switch (array.length) {
			case 2:
				array[1] = 1;
			case 1:
				array[0] = 1;
				return;
		}

		/* Pass 1 : Set extended parent pointers */
		setExtendedParentPointers (array);

		/* Pass 2 : Find number of nodes to relocate in order to achieve maximum code length */
		int nodesToRelocate = findNodesToRelocate (array, maximumLength);

		/* Pass 3 : Generate code lengths */
		if ((array[0] % array.length) >= nodesToRelocate) {
			allocateNodeLengths (array);
		} else {
			int insertDepth = maximumLength - (32 - Integer.numberOfLeadingZeros (nodesToRelocate - 1));
			allocateNodeLengthsWithRelocation (array, nodesToRelocate, insertDepth);
		}

	}

	/**
	 * Non-instantiable
	 */
	private HuffmanAllocator() { }

}
