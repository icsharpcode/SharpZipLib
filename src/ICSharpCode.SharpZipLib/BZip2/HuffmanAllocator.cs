using System;
using System.IO;

namespace ICSharpCode.SharpZipLib.BZip2
{
	/**
	 * An in-place, length restricted Canonical Huffman code length allocator
	 * 
	 * Based on the algorithm proposed by R. L. Milidiú, A. A. Pessoa and E. S. Laber in "In-place
	 * Length-Restricted Prefix Coding" (see: http://www-di.inf.puc-rio.br/~laber/public/spire98.ps)
	 * and incorporating additional ideas from the implementation of "shcodec" by Simakov Alexander
	 * (see: http://webcenter.ru/~xander/)
	 */
	public static class HuffmanAllocator {

		/**
		 * FIRST() function
		 * @param array The code length array
		 * @param i The input position
		 * @param nodesToMove The number of internal nodes to be relocated
		 * @return The smallest {@code k} such that {@code nodesToMove <= k <= i} and
		 *         {@code i <= (array[k] % array.length)}
		 */
		private static int first(int[] array, int i, int nodesToMove) {

			int length = array.Length;
			int limit = i;
			int k = array.Length - 2;

			while ((i >= nodesToMove) && ((array[i] % length) > limit)) {
				k = i;
				i -= (limit - i + 1);
			}
			i = Math.Max(nodesToMove - 1, i);

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
		private static void setExtendedParentPointers(int[] array) {

			int length = array.Length;

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
		private static int findNodesToRelocate(int[] array, int maximumLength) {

			int currentNode = array.Length - 2;
			for (int currentDepth = 1; (currentDepth < (maximumLength - 1)) && (currentNode > 1); currentDepth++) {
				currentNode = first(array, currentNode - 1, 0);
			}

			return currentNode;

		}


		/**
		 * A allocation pass with no code length limit
		 * @param array The code length array
		 */
		private static void allocateNodeLengths(int[] array) {

			int firstNode = array.Length - 2;
			int nextNode = array.Length - 1;

			for (int currentDepth = 1, availableNodes = 2; availableNodes > 0; currentDepth++) {
				int lastNode = firstNode;
				firstNode = first(array, lastNode - 1, 0);

				for (int i = availableNodes - (lastNode - firstNode); i > 0; i--) {
					array[nextNode--] = currentDepth;
				}

				availableNodes = (lastNode - firstNode) << 1;
			}

		}


		/**
		 * A allocation pass that relocates nodes in order to achieve a maximum code length limit
		 * @param array The code length array
		 * @param nodesToMove The number of internal nodes to be relocated
		 * @param insertDepth The depth at which to insert relocated nodes
		 */
		private static void allocateNodeLengthsWithRelocation(int[] array, int nodesToMove, int insertDepth) {

			int firstNode = array.Length - 2;
			int nextNode = array.Length - 1;
			int currentDepth = (insertDepth == 1) ? 2 : 1;
			int nodesLeftToMove = (insertDepth == 1) ? nodesToMove - 2 : nodesToMove;

			for (int availableNodes = currentDepth << 1; availableNodes > 0; currentDepth++) {
				int lastNode = firstNode;
				firstNode = (firstNode <= nodesToMove) ? firstNode : first(array, lastNode - 1, nodesToMove);

				int offset = 0;
				if (currentDepth >= insertDepth) {
					offset = Math.Min(nodesLeftToMove, 1 << (currentDepth - insertDepth));
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
		public static void allocateHuffmanCodeLengths(int[] array, int maximumLength) {

			switch (array.Length) {
				case 2:
					array[1] = 1;
					array[0] = 1;
					return;
				case 1:
					array[0] = 1;
					return;
			}

			/* Pass 1 : Set extended parent pointers */
			setExtendedParentPointers(array);

			/* Pass 2 : Find number of nodes to relocate in order to achieve maximum code length */
			int nodesToRelocate = findNodesToRelocate(array, maximumLength);

			/* Pass 3 : Generate code lengths */
			if ((array[0] % array.Length) >= nodesToRelocate) {
				allocateNodeLengths(array);
			} else {
				int insertDepth = maximumLength - (32 - NumberOfLeadingZeros(nodesToRelocate - 1));
				allocateNodeLengthsWithRelocation(array, nodesToRelocate, insertDepth);
			}

		}

		/// <summary>
		/// Get the number of zero bits preceding the highest-order one-bit in two's complement binary representation of the 32-bit integer
		/// </summary>
		public static int NumberOfLeadingZeros(int v)
			=> 32 - Convert.ToString(v, 2).Length;
	}
}
