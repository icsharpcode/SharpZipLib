using System;
using System.IO;

namespace ICSharpCode.SharpZipLib.GZip
{
	/// <summary>
	/// An example class to demonstrate compression and decompression of GZip streams.
	/// </summary>
	public static class GZip
	{
		/// <summary>
		/// Decompress the <paramref name="inStream">input</paramref> writing 
		/// uncompressed data to the <paramref name="outStream">output stream</paramref>
		/// </summary>
		/// <param name="inStream">The readable stream containing data to decompress.</param>
		/// <param name="outStream">The output stream to receive the decompressed data.</param>
		/// <param name="isStreamOwner">Both streams are closed on completion if true.</param>
		/// <exception cref="ArgumentNullException">Input or output stream is null</exception>
		public static void Decompress(Stream inStream, Stream outStream, bool isStreamOwner)
		{
			if (inStream == null)
				throw new ArgumentNullException(nameof(inStream), "Input stream is null");

			if (outStream == null)
				throw new ArgumentNullException(nameof(outStream), "Output stream is null");

			try
			{
				using (GZipInputStream gzipInput = new GZipInputStream(inStream))
				{
					gzipInput.IsStreamOwner = isStreamOwner;
					Core.StreamUtils.Copy(gzipInput, outStream, new byte[4096]);
				}
			}
			finally
			{
				if (isStreamOwner)
				{
					// inStream is closed by the GZipInputStream if stream owner
					outStream.Dispose();
				}
			}
		}

		/// <summary>
		/// Compress the <paramref name="inStream">input stream</paramref> sending 
		/// result data to <paramref name="outStream">output stream</paramref>
		/// </summary>
		/// <param name="inStream">The readable stream to compress.</param>
		/// <param name="outStream">The output stream to receive the compressed data.</param>
		/// <param name="isStreamOwner">Both streams are closed on completion if true.</param>
		/// <param name="bufferSize">Deflate buffer size, minimum 512</param>
		/// <exception cref="ArgumentNullException">Input or output stream is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">Buffer Size is smaller than 512</exception>
		public static void Compress(Stream inStream, Stream outStream, bool isStreamOwner, int bufferSize = 512)
		{
			if (inStream == null)
				throw new ArgumentNullException(nameof(inStream), "Input stream is null");

			if(outStream == null)
				throw new ArgumentNullException(nameof(outStream), "Output stream is null");

			if (bufferSize < 512)
				throw new ArgumentOutOfRangeException(nameof(bufferSize), "Deflate buffer size must be >= 512");

			try
			{
				using (GZipOutputStream gzipOutput = new GZipOutputStream(outStream, bufferSize))
				{
					gzipOutput.IsStreamOwner = isStreamOwner;
					Core.StreamUtils.Copy(inStream, gzipOutput, new byte[bufferSize]);
				}
			}
			finally {
				if (isStreamOwner)
				{
					// outStream is closed by the GZipOutputStream if stream owner
					inStream.Dispose();
				}
			}
		}

	}
}
