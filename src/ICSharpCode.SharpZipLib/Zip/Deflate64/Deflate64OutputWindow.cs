// The content of the class is borrowed from DEFLATE64 support implementation for DotNetZip
// which on its part contains modified code from the .NET Core Libraries (CoreFX and System.IO.Compression/DeflateManaged)
// where deflate64 decompression is implemented.
// https://github.com/haf/DotNetZip.Semverd/blob/master/src/Zip.Shared/Deflate64/OutputWindow.cs

using System;
using System.Diagnostics;

namespace ICSharpCode.SharpZipLib.Zip.Deflate64
{
	/// <summary>
	/// This class maintains a window for decompressed output.
	/// We need to keep this because the decompressed information can be
	/// a literal or a length/distance pair. For length/distance pair,
	/// we need to look back in the output window and copy bytes from there.
	/// We use a byte array of WindowSize circularly.
	/// </summary>
	internal sealed class Deflate64OutputWindow
	{
		// With Deflate64 we can have up to a 65536 length as well as up to a 65538 distance. This means we need a Window that is at
		// least 131074 bytes long so we have space to retrieve up to a full 64kb in lookback and place it in our buffer without
		// overwriting existing data. Deflate64OutputWindow requires that the WindowSize be an exponent of 2, so we round up to 2^18.
		private const int WindowSize = 262144;
		private const int WindowMask = 262143;

		private readonly byte[] _window = new byte[WindowSize]; // The window is 2^18 bytes
		private int _end;       // this is the position to where we should write next byte
		private int _bytesUsed; // The number of bytes in the output window which is not consumed.

		internal void ClearBytesUsed()
		{
			_bytesUsed = 0;
		}

		/// <summary>Add a byte to output window.</summary>
		public void Write(byte b)
		{
			Debug.Assert(_bytesUsed < WindowSize, "Can't add byte when window is full!");
			_window[_end++] = b;
			_end &= WindowMask;
			++_bytesUsed;
		}

		public void WriteLengthDistance(int length, int distance)
		{
			Debug.Assert((_bytesUsed + length) <= WindowSize, "No Enough space");

			// move backwards distance bytes in the output stream,
			// and copy length bytes from this position to the output stream.
			_bytesUsed += length;
			int copyStart = (_end - distance) & WindowMask; // start position for coping.

			int border = WindowSize - length;
			if (copyStart <= border && _end < border)
			{
				if (length <= distance)
				{
					Array.Copy(_window, copyStart, _window, _end, length);
					_end += length;
				}
				else
				{
					// The referenced string may overlap the current
					// position; for example, if the last 2 bytes decoded have values
					// X and Y, a string reference with <length = 5, distance = 2>
					// adds X,Y,X,Y,X to the output stream.
					while (length-- > 0)
					{
						_window[_end++] = _window[copyStart++];
					}
				}
			}
			else
			{
				// copy byte by byte
				while (length-- > 0)
				{
					_window[_end++] = _window[copyStart++];
					_end &= WindowMask;
					copyStart &= WindowMask;
				}
			}
		}

		/// <summary>
		/// Copy up to length of bytes from input directly.
		/// This is used for uncompressed block.
		/// </summary>
		public int CopyFrom(InputBuffer input, int length)
		{
			length = Math.Min(Math.Min(length, WindowSize - _bytesUsed), input.AvailableBytes);
			int copied;

			// We might need wrap around to copy all bytes.
			int tailLen = WindowSize - _end;
			if (length > tailLen)
			{
				// copy the first part
				copied = input.CopyTo(_window, _end, tailLen);
				if (copied == tailLen)
				{
					// only try to copy the second part if we have enough bytes in input
					copied += input.CopyTo(_window, 0, length - tailLen);
				}
			}
			else
			{
				// only one copy is needed if there is no wrap around.
				copied = input.CopyTo(_window, _end, length);
			}

			_end = (_end + copied) & WindowMask;
			_bytesUsed += copied;
			return copied;
		}

		/// <summary>Free space in output window.</summary>
		public int FreeBytes => WindowSize - _bytesUsed;

		/// <summary>Bytes not consumed in output window.</summary>
		public int AvailableBytes => _bytesUsed;

		/// <summary>Copy the decompressed bytes to output array.</summary>
		public int CopyTo(byte[] output, int offset, int length)
		{
			int copy_end;

			if (length > _bytesUsed)
			{
				// we can copy all the decompressed bytes out
				copy_end = _end;
				length = _bytesUsed;
			}
			else
			{
				copy_end = (_end - _bytesUsed + length) & WindowMask; // copy length of bytes
			}

			int copied = length;

			int tailLen = length - copy_end;
			if (tailLen > 0)
			{
				// this means we need to copy two parts separately
				// copy tailLen bytes from the end of output window
				Array.Copy(_window, WindowSize - tailLen,
								  output, offset, tailLen);
				offset += tailLen;
				length = copy_end;
			}
			Array.Copy(_window, copy_end - length, output, offset, length);
			_bytesUsed -= copied;
			Debug.Assert(_bytesUsed >= 0, "check this function and find why we copied more bytes than we have");
			return copied;
		}
	}
}
