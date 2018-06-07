using System;
using System.IO;

namespace ICSharpCode.SharpZipLib.BZip2
{
	///<summary>
	/// BZip2 constants shared between the compressor and decompressor
	///</summary>
	public static class BZip2Constants
	{

		///<summary>
		/// First three bytes of the block header marker
		///</summary>
		public const int BLOCK_HEADER_MARKER_1 = 0x314159;

		///<summary>
		/// Last three bytes of the block header marker
		///</summary>
		public const int BLOCK_HEADER_MARKER_2 = 0x265359;

		///<summary>
		/// Number of symbols decoded after which a new Huffman table is selected
		///</summary>
		public const int HUFFMAN_GROUP_RUN_LENGTH = 50;

		///<summary>
		/// Maximum possible Huffman alphabet size
		///</summary>
		public const int HUFFMAN_MAXIMUM_ALPHABET_SIZE = 258;

		///<summary>
		/// The longest Huffman code length created by the encoder
		///</summary>
		public const int HUFFMAN_ENCODE_MAXIMUM_CODE_LENGTH = 20;

		///<summary>
		/// The longest Huffman code length accepted by the decoder
		///</summary>
		public const int HUFFMAN_DECODE_MAXIMUM_CODE_LENGTH = 23;

		///<summary>
		/// Minimum number of alternative Huffman tables
		///</summary>
		public const int HUFFMAN_MINIMUM_TABLES = 2;

		///<summary>
		/// Maximum number of alternative Huffman tables
		///</summary>
		public const int HUFFMAN_MAXIMUM_TABLES = 6;

		///<summary>
		/// Maximum possible number of Huffman table selectors
		///</summary>
		public const int HUFFMAN_MAXIMUM_SELECTORS = (900000 / HUFFMAN_GROUP_RUN_LENGTH) + 1;

		///<summary>
		/// Huffman symbol used for run-length encoding
		///</summary>
		public const ushort HUFFMAN_SYMBOL_RUNA = 0;

		///<summary>
		/// Huffman symbol used for run-length encoding
		///</summary>
		public const ushort HUFFMAN_SYMBOL_RUNB = 1;

		///<summary>
		/// First three bytes of the end of stream marker
		///</summary>
		public const int STREAM_END_MARKER_1 = 0x177245;

		///<summary>
		/// Last three bytes of the end of stream marker
		///</summary>
		public const int STREAM_END_MARKER_2 = 0x385090;

		///<summary>
		/// 'B' 'Z' that marks the start of a BZip2 stream
		///</summary>
		public const int STREAM_START_MARKER_1 = 0x425a;

		///<summary>
		/// 'h' that distinguishes BZip from BZip2
		///</summary>
		public const int STREAM_START_MARKER_2 = 0x68;

	}
}
