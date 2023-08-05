// The content of the classes is borrowed from DEFLATE64 support implementation for DotNetZip
// which on its part contains modified code from the .NET Core Libraries (CoreFX and System.IO.Compression/DeflateManaged)
// where deflate64 decompression is implemented.
// https://github.com/haf/DotNetZip.Semverd/blob/master/src/Zip.Shared/Deflate64/Deflate64Stream.cs
// https://github.com/haf/DotNetZip.Semverd/blob/master/src/Zip.Shared/Deflate64/InputBuffer.cs

using System;
using System.Diagnostics;
using System.IO;

namespace ICSharpCode.SharpZipLib.Zip.Deflate64
{
	/// <summary>
	/// Deflate64Stream supports decompression of Deflate64 format only
	/// </summary>
	public class Deflate64Stream : Stream
	{
		internal const int DefaultBufferSize = 8192;

		private Stream _stream;
		private long _compressedSize;
		private long _reachedSize = 0;
		private InflaterManaged inflater;
		private readonly byte[] _buffer;

		/// <summary>
		///A specific constructor to allow decompression of Deflate64
		/// </summary>
		public Deflate64Stream(Stream stream, long compressedSize, long uncompressedSize = -1)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (!stream.CanRead)
				throw new ArgumentException("NotSupported_UnreadableStream", nameof(stream));

			Inflater = new InflaterManaged(null, true, uncompressedSize);

			_compressedSize = compressedSize;
			_stream = stream;
			_buffer = new byte[DefaultBufferSize];
		}

		/// <summary>
		/// Gets a value indicating if the stream supports reading
		/// </summary>
		public override bool CanRead
		{
			get
			{
				if (_stream == null)
				{
					return false;
				}

				return _stream.CanRead;
			}
		}

		/// <summary>
		/// Gets a value indicating if the stream supports writing
		/// </summary>
		public override bool CanWrite
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the current stream supports seeking.
		/// This property always returns false
		/// </summary>
		public override bool CanSeek => false;

		/// <summary>
		/// Gets the length in bytes of the stream
		/// Setting the length is not supported and will throw a NotSupportException
		/// </summary>
		public override long Length
		{
			get { throw new NotSupportedException(); }
		}

		/// <summary>
		/// Gets or sets the streams position
		/// Setting/Getting the position is not supported and will throw a NotSupportException
		/// </summary>
		public override long Position
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		internal InflaterManaged Inflater { get => inflater; set => inflater = value; }

		/// <summary>
		/// Flushes the stream
		/// </summary>
		public override void Flush()
		{
			EnsureNotDisposed();
		}

		/// <summary>
		/// Set the streams position.  This operation is not supported and will throw a NotSupportedException
		/// </summary>
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Sets the length of this stream to the given value.
		/// This operation is not supported and will throw a NotSupportedExceptionortedException
		/// </summary>
		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Read a sequence of bytes and advances the read position by one byte.
		/// </summary>
		/// <param name="array">Array of bytes to store values in</param>
		/// <param name="offset">Offset in array to begin storing data</param>
		/// <param name="count">The maximum number of bytes to read</param>
		/// <returns>The total number of bytes read into the buffer. This might be less
		/// than the number of bytes requested if that number of bytes are not 
		/// currently available or zero if the end of the stream is reached.
		/// </returns>
		public override int Read(byte[] array, int offset, int count)
		{
			ValidateParameters(array, offset, count);
			EnsureNotDisposed();

			int bytesRead;
			int currentOffset = offset;
			int remainingCount = count;

			while (true)
			{
				bytesRead = Inflater.Inflate(array, currentOffset, remainingCount);
				currentOffset += bytesRead;
				remainingCount -= bytesRead;

				if (remainingCount == 0)
				{
					break;
				}

				if (Inflater.Finished())
				{
					// if we finished decompressing, we can't have anything left in the outputwindow.
					Debug.Assert(Inflater.AvailableOutput == 0, "We should have copied all stuff out!");
					break;
				}

				//Calculate the availble buffer size according to the file compressed size, otherwise additional data will be read
				int availableToRead = (_compressedSize - _reachedSize >= _buffer.Length) ? _buffer.Length : Convert.ToInt32(_compressedSize - _reachedSize);
				int bytes = _stream.Read(_buffer, 0, availableToRead > 0 ? availableToRead : 1);
				_reachedSize += bytes;

				if (bytes <= 0)
				{
					break;
				}
				else if (bytes > _buffer.Length)
				{
					// The stream is either malicious or poorly implemented and returned a number of
					// bytes larger than the buffer supplied to it.
					throw new InvalidDataException();
				}

				Inflater.SetInput(_buffer, 0, bytes);
			}

			return count - remainingCount;
		}

		private void ValidateParameters(byte[] array, int offset, int count)
		{
			if (array == null)
				throw new ArgumentNullException(nameof(array));

			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset));

			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (array.Length - offset < count)
				throw new ArgumentException("InvalidArgumentOffsetCount");
		}

		private void EnsureNotDisposed()
		{
			if (_stream == null)
				ThrowStreamClosedException();
		}

		private static void ThrowStreamClosedException()
		{
			throw new ObjectDisposedException(null, "ObjectDisposed_StreamClosed");
		}

		/// <summary>
		/// Asynchronous reads are not supported a NotSupportedException is always thrown
		/// </summary>
		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Asynchronous writes arent supported, a NotSupportedException is always thrown
		/// </summary>
		public override int EndRead(IAsyncResult asyncResult)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Writes bytes from an array to the decompressed stream
		/// The method is not supported
		/// </summary>
		public override void Write(byte[] array, int offset, int count)
		{
			throw new InvalidOperationException("CannotWriteToDeflateStream");
		}

		// This is called by Dispose:
		private void PurgeBuffers(bool disposing)
		{
			if (!disposing)
				return;

			if (_stream == null)
				return;

			Flush();
		}

		/// <summary>
		/// Stream disposal		
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			try
			{
				PurgeBuffers(disposing);
			}
			finally
			{
				// Close the underlying stream even if PurgeBuffers threw.
				// Stream.Close() may throw here (may or may not be due to the same error).
				// In this case, we still need to clean up internal resources, hence the inner finally blocks.
				try
				{
					if (disposing && _stream != null)
						_stream.Dispose();
				}
				finally
				{
					_stream = null;

					try
					{
						if (Inflater != null)
							Inflater.Dispose();
					}
					finally
					{
						Inflater = null;
						base.Dispose(disposing);
					}
				}
			}
		}
	}

	// This class can be used to read bits from an byte array quickly.
	// Normally we get bits from 'bitBuffer' field and bitsInBuffer stores
	// the number of bits available in 'BitBuffer'.
	// When we used up the bits in bitBuffer, we will try to get byte from
	// the byte array and copy the byte to appropiate position in bitBuffer.
	//
	// The byte array is not reused. We will go from 'start' to 'end'.
	// When we reach the end, most read operations will return -1,
	// which means we are running out of input.

	internal sealed class InputBuffer
	{
		private byte[] _buffer;           // byte array to store input
		private int _start;               // start poisition of the buffer
		private int _end;                 // end position of the buffer
		private uint _bitBuffer = 0;      // store the bits here, we can quickly shift in this buffer
		private int _bitsInBuffer = 0;    // number of bits available in bitBuffer

		/// <summary>Total bits available in the input buffer.</summary>
		public int AvailableBits => _bitsInBuffer;

		/// <summary>Total bytes available in the input buffer.</summary>
		public int AvailableBytes => (_end - _start) + (_bitsInBuffer / 8);

		/// <summary>Ensure that count bits are in the bit buffer.</summary>
		/// <param name="count">Can be up to 16.</param>
		/// <returns>Returns false if input is not sufficient to make this true.</returns>
		public bool EnsureBitsAvailable(int count)
		{
			Debug.Assert(0 < count && count <= 16, "count is invalid.");

			// manual inlining to improve perf
			if (_bitsInBuffer < count)
			{
				if (NeedsInput())
				{
					return false;
				}
				Debug.Assert(_buffer != null);
				// insert a byte to bitbuffer
				_bitBuffer |= (uint)_buffer[_start++] << _bitsInBuffer;
				_bitsInBuffer += 8;

				if (_bitsInBuffer < count)
				{
					if (NeedsInput())
					{
						return false;
					}
					// insert a byte to bitbuffer
					_bitBuffer |= (uint)_buffer[_start++] << _bitsInBuffer;
					_bitsInBuffer += 8;
				}
			}

			return true;
		}

		/// <summary>
		/// This function will try to load 16 or more bits into bitBuffer.
		/// It returns whatever is contained in bitBuffer after loading.
		/// The main difference between this and GetBits is that this will
		/// never return -1. So the caller needs to check AvailableBits to
		/// see how many bits are available.
		/// </summary>
		public uint TryLoad16Bits()
		{
			Debug.Assert(_buffer != null);
			if (_bitsInBuffer < 8)
			{
				if (_start < _end)
				{
					_bitBuffer |= (uint)_buffer[_start++] << _bitsInBuffer;
					_bitsInBuffer += 8;
				}

				if (_start < _end)
				{
					_bitBuffer |= (uint)_buffer[_start++] << _bitsInBuffer;
					_bitsInBuffer += 8;
				}
			}
			else if (_bitsInBuffer < 16)
			{
				if (_start < _end)
				{
					_bitBuffer |= (uint)_buffer[_start++] << _bitsInBuffer;
					_bitsInBuffer += 8;
				}
			}

			return _bitBuffer;
		}

		private uint GetBitMask(int count) => ((uint)1 << count) - 1;

		/// <summary>Gets count bits from the input buffer. Returns -1 if not enough bits available.</summary>
		public int GetBits(int count)
		{
			Debug.Assert(0 < count && count <= 16, "count is invalid.");

			if (!EnsureBitsAvailable(count))
			{
				return -1;
			}

			int result = (int)(_bitBuffer & GetBitMask(count));
			_bitBuffer >>= count;
			_bitsInBuffer -= count;
			return result;
		}

		/// <summary>
		/// Copies length bytes from input buffer to output buffer starting at output[offset].
		/// You have to make sure, that the buffer is byte aligned. If not enough bytes are
		/// available, copies fewer bytes.
		/// </summary>
		/// <returns>Returns the number of bytes copied, 0 if no byte is available.</returns>
		public int CopyTo(byte[] output, int offset, int length)
		{
			Debug.Assert(output != null);
			Debug.Assert(offset >= 0);
			Debug.Assert(length >= 0);
			Debug.Assert(offset <= output.Length - length);
			Debug.Assert((_bitsInBuffer % 8) == 0);

			// Copy the bytes in bitBuffer first.
			int bytesFromBitBuffer = 0;
			while (_bitsInBuffer > 0 && length > 0)
			{
				output[offset++] = (byte)_bitBuffer;
				_bitBuffer >>= 8;
				_bitsInBuffer -= 8;
				length--;
				bytesFromBitBuffer++;
			}

			if (length == 0)
			{
				return bytesFromBitBuffer;
			}

			int avail = _end - _start;
			if (length > avail)
			{
				length = avail;
			}

			Debug.Assert(_buffer != null);
			Array.Copy(_buffer, _start, output, offset, length);
			_start += length;
			return bytesFromBitBuffer + length;
		}

		/// <summary>
		/// Return true is all input bytes are used.
		/// This means the caller can call SetInput to add more input.
		/// </summary>
		public bool NeedsInput() => _start == _end;

		/// <summary>
		/// Set the byte array to be processed.
		/// All the bits remained in bitBuffer will be processed before the new bytes.
		/// We don't clone the byte array here since it is expensive.
		/// The caller should make sure after a buffer is passed in.
		/// It will not be changed before calling this function again.
		/// </summary>
		public void SetInput(byte[] buffer, int offset, int length)
		{
			Debug.Assert(buffer != null);
			Debug.Assert(offset >= 0);
			Debug.Assert(length >= 0);
			Debug.Assert(offset <= buffer.Length - length);

			if (_start == _end)
			{
				_buffer = buffer;
				_start = offset;
				_end = offset + length;
			}
		}

		/// <summary>Skip n bits in the buffer.</summary>
		public void SkipBits(int n)
		{
			Debug.Assert(_bitsInBuffer >= n, "No enough bits in the buffer, Did you call EnsureBitsAvailable?");
			_bitBuffer >>= n;
			_bitsInBuffer -= n;
		}

		/// <summary>Skips to the next byte boundary.</summary>
		public void SkipToByteBoundary()
		{
			_bitBuffer >>= (_bitsInBuffer % 8);
			_bitsInBuffer = _bitsInBuffer - (_bitsInBuffer % 8);
		}
	}
}
