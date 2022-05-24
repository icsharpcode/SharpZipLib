using System;
using System.IO;
using System.Threading;

namespace ICSharpCode.SharpZipLib.Tests.TestSupport
{
	/// <summary>
	/// An extended <see cref="MemoryStream">memory stream</see>
	/// that tracks closing and disposing
	/// </summary>
	public class TrackedMemoryStream : MemoryStream
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TrackedMemoryStream"/> class.
		/// </summary>
		public TrackedMemoryStream()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TrackedMemoryStream"/> class.
		/// </summary>
		/// <param name="buffer">The buffer.</param>
		public TrackedMemoryStream(byte[] buffer)
			: base(buffer)
		{
		}

		/// <summary>
		/// Write a short value in Little Endian order
		/// </summary>
		/// <param name="value"></param>
		public void WriteLEShort(short value)
		{
			WriteByte(unchecked((byte)value));
			WriteByte(unchecked((byte)(value >> 8)));
		}

		/// <summary>
		/// Write an int value in little endian order.
		/// </summary>
		/// <param name="value"></param>
		public void WriteLEInt(int value)
		{
			WriteLEShort(unchecked((short)value));
			WriteLEShort(unchecked((short)(value >> 16)));
		}

		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="T:System.IO.MemoryStream"/> class and optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected override void Dispose(bool disposing)
		{
			isDisposed_ = true;
			base.Dispose(disposing);
		}

		/// <summary>
		/// Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream.
		/// </summary>
		public override void Close()
		{
			if (isClosed_)
			{
				throw new InvalidOperationException("Already closed");
			}

			isClosed_ = true;
			base.Close();
		}

		/// <summary>
		/// Gets a value indicating whether this instance is closed.
		/// </summary>
		/// <value><c>true</c> if this instance is closed; otherwise, <c>false</c>.</value>
		public bool IsClosed
		{
			get { return isClosed_; }
		}

		/// <summary>
		/// Gets a value indicating whether this instance is disposed.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is disposed; otherwise, <c>false</c>.
		/// </value>
		public bool IsDisposed
		{
			get { return isDisposed_; }
		}

		#region Instance Fields

		private bool isDisposed_;

		private bool isClosed_;

		#endregion Instance Fields
	}

	/// <summary>
	/// An extended <see cref="FileStream">file stream</see>
	/// that tracks closing and disposing
	/// </summary>
	public class TrackedFileStream : FileStream
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TrackedMemoryStream"/> class.
		/// </summary>
		/// <param name="buffer">The buffer.</param>
		public TrackedFileStream(string path, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read)
			: base(path, mode, access)
		{
		}

		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="T:System.IO.MemoryStream"/> class and optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected override void Dispose(bool disposing)
		{
			isDisposed_ = true;
			base.Dispose(disposing);
		}

		/// <summary>
		/// Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream.
		/// </summary>
		public override void Close()
		{
			if (isClosed_)
			{
				throw new InvalidOperationException("Already closed");
			}

			isClosed_ = true;
			base.Close();
		}

		/// <summary>
		/// Gets a value indicating whether this instance is closed.
		/// </summary>
		/// <value><c>true</c> if this instance is closed; otherwise, <c>false</c>.</value>
		public bool IsClosed
		{
			get { return isClosed_; }
		}

		/// <summary>
		/// Gets a value indicating whether this instance is disposed.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is disposed; otherwise, <c>false</c>.
		/// </value>
		public bool IsDisposed
		{
			get { return isDisposed_; }
		}

		#region Instance Fields

		private bool isDisposed_;

		private bool isClosed_;

		#endregion Instance Fields
	}

	/// <summary>
	/// A <see cref="Stream"/> that cannot seek.
	/// </summary>
	public class MemoryStreamWithoutSeek : TrackedMemoryStream
	{
		/// <summary>
		/// Gets a value indicating whether the current stream supports seeking.
		/// </summary>
		/// <value></value>
		/// <returns>true if the stream is open.</returns>
		public override bool CanSeek => false;

		/// <inheritdoc />
		public override long Position
		{
			get => throw new NotSupportedException("Getting position is not supported");
			set => throw new NotSupportedException("Setting position is not supported");
		}

	}

	/// <summary>
	/// A <see cref="Stream"/> that cannot be read but supports infinite writes.
	/// </summary>
	public class NullStream : Stream
	{
		/// <summary>
		/// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
		/// </summary>
		/// <value></value>
		/// <returns>true if the stream supports reading; otherwise, false.</returns>
		public override bool CanRead
		{
			get { return false; }
		}

		/// <summary>
		/// When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
		/// </summary>
		/// <value></value>
		/// <returns>true if the stream supports seeking; otherwise, false.</returns>
		public override bool CanSeek
		{
			get { return false; }
		}

		/// <summary>
		/// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
		/// </summary>
		/// <value></value>
		/// <returns>true if the stream supports writing; otherwise, false.</returns>
		public override bool CanWrite
		{
			get { return true; }
		}

		/// <summary>
		/// When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
		/// </summary>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		public override void Flush()
		{
			// Do nothing.
		}

		/// <summary>
		/// When overridden in a derived class, gets the length in bytes of the stream.
		/// </summary>
		/// <value></value>
		/// <returns>A long value representing the length of the stream in bytes.</returns>
		/// <exception cref="T:System.NotSupportedException">A class derived from Stream does not support seeking. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override long Length
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		/// <summary>
		/// When overridden in a derived class, gets or sets the position within the current stream.
		/// </summary>
		/// <value></value>
		/// <returns>The current position within the stream.</returns>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.NotSupportedException">The stream does not support seeking. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override long Position
		{
			get
			{
				throw new Exception("The method or operation is not implemented.");
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		/// <summary>
		/// When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
		/// </summary>
		/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source.</param>
		/// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.</param>
		/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
		/// <returns>
		/// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">The sum of <paramref name="offset"/> and <paramref name="count"/> is larger than the buffer length. </exception>
		/// <exception cref="T:System.ArgumentNullException">
		/// 	<paramref name="buffer"/> is null. </exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">
		/// 	<paramref name="offset"/> or <paramref name="count"/> is negative. </exception>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.NotSupportedException">The stream does not support reading. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		/// When overridden in a derived class, sets the position within the current stream.
		/// </summary>
		/// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
		/// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
		/// <returns>
		/// The new position within the current stream.
		/// </returns>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.NotSupportedException">The stream does not support seeking, such as if the stream is constructed from a pipe or console output. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		/// When overridden in a derived class, sets the length of the current stream.
		/// </summary>
		/// <param name="value">The desired length of the current stream in bytes.</param>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.NotSupportedException">The stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override void SetLength(long value)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		/// When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies <paramref name="count"/> bytes from <paramref name="buffer"/> to the current stream.</param>
		/// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
		/// <exception cref="T:System.ArgumentException">The sum of <paramref name="offset"/> and <paramref name="count"/> is greater than the buffer length. </exception>
		/// <exception cref="T:System.ArgumentNullException">
		/// 	<paramref name="buffer"/> is null. </exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">
		/// 	<paramref name="offset"/> or <paramref name="count"/> is negative. </exception>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.NotSupportedException">The stream does not support writing. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override void Write(byte[] buffer, int offset, int count)
		{
			// Do nothing.
		}
	}

	/// <summary>
	/// A <see cref="Stream"/> that supports reading and writing from a fixed size memory buffer.
	/// This provides the ability to test writing and reading from very large streams
	/// without using any disk storeage
	/// </summary>
	public class WindowedStream : Stream
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="WindowedStream"/> class.
		/// </summary>
		/// <param name="size">The size.</param>
		public WindowedStream(int size, CancellationToken? token = null)
		{
			ringBuffer = new ReadWriteRingBuffer(size, token);
		}

		/// <summary>
		/// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
		/// </summary>
		/// <value></value>
		/// <returns>true if the stream is not closed.</returns>
		/// <remarks>If the stream is closed, this property returns false.</remarks>
		public override bool CanRead => !ringBuffer.IsClosed;

		/// <summary>
		/// Gets a value indicating whether the current stream supports seeking.
		/// </summary>
		/// <value></value>
		/// <returns>false</returns>
		public override bool CanSeek => false;

		/// <summary>
		/// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
		/// </summary>
		/// <value></value>
		/// <returns>true if the stream is not closed.</returns>
		/// <remarks>If the stream is closed, this property returns false.</remarks>
		public override bool CanWrite => !ringBuffer.IsClosed;

		/// <summary>
		/// When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
		/// </summary>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		public override void Flush()
		{
			// Do nothing
		}

		/// <summary>
		/// When overridden in a derived class, gets the length in bytes of the stream.
		/// </summary>
		/// <value></value>
		/// <returns>A long value representing the length of the stream in bytes.</returns>
		/// <exception cref="T:System.NotSupportedException">A class derived from Stream does not support seeking. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override long Length
		{
			get => throw new NotSupportedException();
		}

		/// <summary>
		/// When overridden in a derived class, gets or sets the position within the current stream.
		/// </summary>
		/// <value></value>
		/// <returns>The current position within the stream.</returns>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.NotSupportedException">The stream does not support seeking. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		/// <summary>
		/// When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
		/// </summary>
		/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source.</param>
		/// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.</param>
		/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
		/// <returns>
		/// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">The sum of <paramref name="offset"/> and <paramref name="count"/> is larger than the buffer length. </exception>
		/// <exception cref="T:System.ArgumentNullException">
		/// 	<paramref name="buffer"/> is null. </exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">
		/// 	<paramref name="offset"/> or <paramref name="count"/> is negative. </exception>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.NotSupportedException">The stream does not support reading. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override int Read(byte[] buffer, int offset, int count)
		{
			int bytesRead = 0;
			while (count > 0)
			{
				int value = ringBuffer.ReadByte();
				if (value >= 0)
				{
					buffer[offset] = (byte)(value & 0xff);
					offset++;
					bytesRead++;
					count--;
				}
				else
				{
					break;
				}
			}

			return bytesRead;
		}

		/// <summary>
		/// Not supported, throws <see cref="T:System.NotSupportedException"/>.
		/// </summary>
		/// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
		/// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
		/// <returns></returns>
		/// <exception cref="T:System.NotSupportedException">The stream does not support seeking, such as if the stream is constructed from a pipe or console output. </exception>
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

		/// <summary>
		/// Not supported, throws <see cref="T:System.NotSupportedException"/>.
		/// </summary>
		/// <param name="value">The desired length of the current stream in bytes.</param>
		/// <exception cref="T:System.NotSupportedException">The stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output. </exception>
		public override void SetLength(long value) => throw new NotSupportedException();

		/// <summary>
		/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies <paramref name="count"/> bytes from <paramref name="buffer"/> to the current stream.</param>
		/// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
		/// <exception cref="T:System.ArgumentException">The sum of <paramref name="offset"/> and <paramref name="count"/> is greater than the buffer length. </exception>
		/// <exception cref="T:System.ArgumentNullException">
		/// 	<paramref name="buffer"/> is null. </exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">
		/// 	<paramref name="offset"/> or <paramref name="count"/> is negative. </exception>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.NotSupportedException">The stream does not support writing. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override void Write(byte[] buffer, int offset, int count)
		{
			for (int i = 0; i < count; ++i)
			{
				ringBuffer.WriteByte(buffer[offset + i]);
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is closed.
		/// </summary>
		/// <value><c>true</c> if this instance is closed; otherwise, <c>false</c>.</value>
		public bool IsClosed
		{
			get { return ringBuffer.IsClosed; }
		}

		/// <summary>Releases the unmanaged resources used by the <see cref="Stream"></see> and optionally releases the managed resources.</summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && !ringBuffer.IsClosed)
			{
				ringBuffer.Close();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Gets the bytes written.
		/// </summary>
		/// <value>The bytes written.</value>
		public long BytesWritten => ringBuffer.BytesWritten;

		/// <summary>
		/// Gets the bytes read.
		/// </summary>
		/// <value>The bytes read.</value>
		public long BytesRead => ringBuffer.BytesRead;

		#region Instance Fields

		private readonly ReadWriteRingBuffer ringBuffer;

		#endregion Instance Fields
	}

	internal class SingleByteReadingStream : MemoryStream
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SingleByteReadingStream"/> class.
		/// </summary>
		public SingleByteReadingStream()
		{
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (count > 0)
				count = 1;

			return base.Read(buffer, offset, count);
		}
	}

	/// <summary>
	/// A stream that closes itself when all of its data is read.
	/// </summary>
	/// <remarks>
	/// Useful for testing issues such as https://github.com/icsharpcode/SharpZipLib/issues/379
	/// </remarks>
	internal class SelfClosingStream : MemoryStream
	{
		private bool isFullyRead = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="SelfClosingStream"/> class.
		/// </summary>
		public SelfClosingStream()
		{
		}

		// <inheritdoc/>
		public override int Read(byte[] buffer, int offset, int count)
		{
			var read = base.Read(buffer, offset, count);

			if (read == 0)
			{
				isFullyRead = true;
				Close();
			}

			return read;
		}

		/// <summary>
		/// CanRead is false if we're closed, or base.CanRead otherwise.
		/// </summary>
		public override bool CanRead
		{
			get
			{
				if (isFullyRead)
				{
					return false;
				}

				return base.CanRead;
			}
		}
	}
}
