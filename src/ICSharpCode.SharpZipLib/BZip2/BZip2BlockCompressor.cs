using System;
using System.IO;

namespace ICSharpCode.SharpZipLib.BZip2
{


	/*
	 * Block encoding consists of the following stages:
	 * 1. Run-Length Encoding[1] - write()
	 * 2. Burrows Wheeler Transform - close() (through BZip2DivSufSort)
	 * 3. Write block header - close()
	 * 4. Move To Front Transform - close() (through BZip2HuffmanStageEncoder)
	 * 5. Run-Length Encoding[2] - close()  (through BZip2HuffmanStageEncoder)
	 * 6. Create and write Huffman tables - close() (through BZip2HuffmanStageEncoder)
	 * 7. Huffman encode and write data - close() (through BZip2HuffmanStageEncoder)
	 */
	/**
	 * Compresses and writes a single BZip2 block
	 */
	public class BZip2BlockCompressor
	{

		/**
		 * The stream to which compressed BZip2 data is written
		 */
		private BZip2BitOutputStream bitOutputStream;

		/**
		 * CRC builder for the block
		 */
		private CRC32 crc = new CRC32();

		/**
		 * The RLE'd block data
		 */
		private byte[] block;

		/**
		 * Current length of the data within the {@link block} array
		 */
		private int blockLength = 0;

		/**
		 * A limit beyond which new data will not be accepted into the block
		 */
		private int blockLengthLimit;

		/**
		 * The values that are present within the RLE'd block data. For each index, {@code true} if that
		 * value is present within the data, otherwise {@code false}
		 */
		private bool[] blockValuesPresent = new bool[256];

		/**
		 * The Burrows Wheeler Transformed block data
		 */
		private int[] bwtBlock;

		/**
		 * The current RLE value being accumulated (undefined when {@link #rleLength} is 0)
		 */
		private int rleCurrentValue = -1;

		/**
		 * The repeat count of the current RLE value
		 */
		private int rleLength = 0;


		/**
		 * Write the Huffman symbol to output byte map
		 * @throws IOException on any I/O error writing the data
		 */
		private void writeSymbolMap()
		{

			BZip2BitOutputStream bitOutputStream = this.bitOutputStream;

			bool[] blockValuesPresent = this.blockValuesPresent;
			bool[] condensedInUse = new bool[16];

			for (int i = 0; i < 16; i++)
			{
				for (int j = 0, k = i << 4; j < 16; j++, k++)
				{
					if (blockValuesPresent[k])
					{
						condensedInUse[i] = true;
					}
				}
			}

			for (int i = 0; i < 16; i++)
			{
				bitOutputStream.writeBoolean(condensedInUse[i]);
			}

			for (int i = 0; i < 16; i++)
			{
				if (condensedInUse[i])
				{
					for (int j = 0, k = i * 16; j < 16; j++, k++)
					{
						bitOutputStream.writeBoolean(blockValuesPresent[k]);
					}
				}
			}

		}


		/**
		 * Writes an RLE run to the block array, updating the block CRC and present values array as required
		 * @param value The value to write
		 * @param runLength The run length of the value to write
		 */
		private void writeRun(int value, int runLength)
		{

			blockValuesPresent[value] = true;
			crc.updateCRC((uint)value, runLength);

			byte byteValue = (byte)value;
			switch (runLength)
			{
				case 1:
					block[blockLength] = byteValue;
					blockLength += 1;
					break;

				case 2:
					block[blockLength] = byteValue;
					block[blockLength + 1] = byteValue;
					blockLength += 2;
					break;

				case 3:
					block[blockLength] = byteValue;
					block[blockLength + 1] = byteValue;
					block[blockLength + 2] = byteValue;
					blockLength += 3;
					break;

				default:
					runLength -= 4;
					blockValuesPresent[runLength] = true;
					block[blockLength] = byteValue;
					block[blockLength + 1] = byteValue;
					block[blockLength + 2] = byteValue;
					block[blockLength + 3] = byteValue;
					block[blockLength + 4] = (byte)runLength;
					blockLength += 5;
					break;
			}

		}


		/**
		 * Writes a byte to the block, accumulating to an RLE run where possible
		 * @param value The byte to write
		 * @return {@code true} if the byte was written, or {@code false} if the block is already full
		 */
		public bool Write(int value)
		{

			if (blockLength > blockLengthLimit)
			{
				return false;
			}

			if (rleLength == 0)
			{
				rleCurrentValue = value;
				rleLength = 1;
			}
			else if (rleCurrentValue != value)
			{
				// This path commits us to write 6 bytes - one RLE run (5 bytes) plus one extra
				writeRun(rleCurrentValue & 0xff, rleLength);
				rleCurrentValue = value;
				rleLength = 1;
			}
			else
			{
				if (rleLength == 254)
				{
					writeRun(rleCurrentValue & 0xff, 255);
					rleLength = 0;
				}
				else
				{
					rleLength = rleLength + 1;
				}
			}

			return true;

		}


		/**
		 * Writes an array to the block
		 * @param data The array to write
		 * @param offset The offset within the input data to write from
		 * @param length The number of bytes of input data to write
		 * @return The actual number of input bytes written. May be less than the number requested, or
		 *         zero if the block is already full
		 */
		public int Write(byte[] data, int offset, int length)
		{

			int written = 0;

			while (length-- > 0)
			{
				if (!Write(data[offset++]))
				{
					break;
				}
				written++;
			}

			return written;

		}


		/**
		 * Compresses and writes out the block
		 * @throws IOException on any I/O error writing the data
		 */
		public void Close()
		{

			// If an RLE run is in progress, write it out
			if (rleLength > 0)
			{
				writeRun(rleCurrentValue & 0xff, rleLength);
			}

			// Apply a one byte block wrap required by the BWT implementation
			block[blockLength] = block[0];

			// Perform the Burrows Wheeler Transform
			BZip2DivSufSort divSufSort = new BZip2DivSufSort(block, bwtBlock, blockLength);
			var bwtStartPointer = divSufSort.bwt();

			// Write out the block header
			bitOutputStream.writeBits(24, BZip2Constants.BLOCK_HEADER_MARKER_1);
			bitOutputStream.writeBits(24, BZip2Constants.BLOCK_HEADER_MARKER_2);
			bitOutputStream.writeInteger(crc.CRC);
			bitOutputStream.writeBoolean(false); // Randomised block flag. We never create randomised blocks
			bitOutputStream.writeBits(24, (uint)bwtStartPointer);

			// Write out the symbol map
			writeSymbolMap();

			// Perform the Move To Front Transform and Run-Length Encoding[2] stages 
			BZip2MTFAndRLE2StageEncoder mtfEncoder = new BZip2MTFAndRLE2StageEncoder(bwtBlock, blockLength, blockValuesPresent);
			mtfEncoder.Encode();

			// Perform the Huffman Encoding stage and write out the encoded data
			BZip2HuffmanStageEncoder huffmanEncoder = new BZip2HuffmanStageEncoder(bitOutputStream, mtfEncoder.MtfBlock, 
				mtfEncoder.MtfLength, mtfEncoder.MtfAlphabetSize, mtfEncoder.MtfSymbolFrequencies);
			huffmanEncoder.encode();

		}


		/**
		 * Determines if any bytes have been written to the block
		 * @return {@code true} if one or more bytes has been written to the block, otherwise
		 *         {@code false}
		 */
		public bool isEmpty()
		{

			return ((blockLength == 0) && (rleLength == 0));

		}


		/**
		 * Gets the CRC of the completed block. Only valid after calling {@link #close()}
		 * @return The block's CRC
		 */
		public uint CRC => crc.CRC;


		/**
		 * @param bitOutputStream The BZip2BitOutputStream to which compressed BZip2 data is written
		 * @param blockSize The declared block size in bytes. Up to this many bytes will be accepted
		 *                  into the block after Run-Length Encoding is applied
		 */
		public BZip2BlockCompressor(BZip2BitOutputStream bitOutputStream, int blockSize)
		{

			this.bitOutputStream = bitOutputStream;

			// One extra byte is added to allow for the block wrap applied in close()
			block = new byte[blockSize + 1];
			bwtBlock = new int[blockSize + 1];
			blockLengthLimit = blockSize - 6; // 5 bytes for one RLE run plus one byte - see {@link #write(int)}

		}

	}

}
