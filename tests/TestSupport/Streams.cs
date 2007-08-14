/*
 * Created by SharpDevelop.
 * User: JohnR
 * Date: 4/08/2007
 * Time: 7:09 a.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.IO;

namespace ICSharpCode.SharpZipLib.Tests.TestSupport
{
	/// <summary>
	/// An extended <see cref="MemoryStream">memory stream</see>
	/// that tracks closing and diposing
	/// </summary>
	public class MemoryStreamEx : MemoryStream
	{
		public MemoryStreamEx()
			: base()
		{
		}

		public MemoryStreamEx(byte[] buffer)
			: base(buffer)
		{
		}

		protected override void Dispose(bool disposing)
		{
			isDisposed_=true;
			base.Dispose(disposing);
		}

		public override void Close()
		{
			isClosed_=true;
			base.Close();
		}

		public bool IsClosed
		{
			get { return isClosed_; }
		}

		public bool IsDisposed
		{
			get { return isDisposed_; }
			set { isDisposed_=value; }
		}

		#region Instance Fields
		bool isDisposed_;

		bool isClosed_;
		#endregion
	}

    /// <summary>
    /// A stream that cannot seek.
    /// </summary>
	public class MemoryStreamWithoutSeek : MemoryStreamEx
	{
		public override bool CanSeek
		{
			get {
				return false;
			}
		}
	}

    public class NullStream : Stream
    {
        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
        }

        public override long Length
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

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

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override void SetLength(long value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
        }
    }

    public class WindowedStream : Stream
    {
        public WindowedStream(int size)
        {
            ringBuffer_ = new ReadWriteRingBuffer(size);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
            // Do nothing
        }

        public override long Length
        {
            // A bit of a HAK as its not true in the stream sense.
            get { return ringBuffer_.Count; }
        }

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

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;
            while (count > 0)
            {
                int value = ringBuffer_.ReadByte();
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

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override void SetLength(long value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                ringBuffer_.WriteByte(buffer[offset + i]);
            }
        }

        public bool IsClosed
        {
            get { return ringBuffer_.IsClosed; }
        }

        public override void Close()
        {
            ringBuffer_.Close();
        }

        public long BytesWritten
        {
            get { return ringBuffer_.BytesWritten; }
        }

        public long BytesRead
        {
            get { return ringBuffer_.BytesRead; }
        }

        #region Instance Fields
        ReadWriteRingBuffer ringBuffer_;

        #endregion
    }
}
