using System;
using System.IO;

namespace blowery.Web.HttpModules {
  /// <summary>
  /// The base of anything you want to latch onto the Filter property of a <see cref="System.Web.HttpResponse"/>
  /// object.
  /// </summary>
  /// <remarks>
  /// <p></p>These are generally used with the <see cref="HttpCompressionModule"/> but you could really use them in
  /// other HttpModules.  This is a general, write-only stream that writes to some underlying stream.  When implementing
  /// a real class, you have to override void Write(byte[], int offset, int count).  Your work will be performed there.
  /// </remarks>
  public abstract class HttpOutputFilter : Stream {
    
    private Stream _sink;

    /// <summary>
    /// Subclasses need to call this on contruction to setup the underlying stream
    /// </summary>
    /// <param name="baseStream">The stream we're wrapping up in a filter</param>
    protected HttpOutputFilter(Stream baseStream) { 
      _sink = baseStream;
    }

    /// <summary>
    /// Allow subclasses access to the underlying stream
    /// </summary>
    protected Stream BaseStream {
      get{ return _sink; }
    }

    /// <summary>
    /// False.  These are write-only streams
    /// </summary>
    public override bool CanRead {
      get { return false; }
    }

    /// <summary>
    /// False.  These are write-only streams
    /// </summary>
    public override bool CanSeek {
      get { return false; }
    }

    /// <summary>
    /// True.  You can write to the stream.  May change if you call Close or Dispose
    /// </summary>
    public override bool CanWrite {
      get { return _sink.CanWrite; }
    }

    /// <summary>
    /// Not supported.  Throws an exception saying so.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown.  Always.</exception>
    public override long Length {
      get { throw new NotSupportedException(); }
    }

    /// <summary>
    /// Not supported.  Throws an exception saying so.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown.  Always.</exception>
    public override long Position {
      get { throw new NotSupportedException(); }
      set { throw new NotSupportedException(); }
    }

    /// <summary>
    /// Not supported.  Throws an exception saying so.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown.  Always.</exception>
    public override long Seek(long offset, System.IO.SeekOrigin direction) {
      throw new NotSupportedException();
    }

    /// <summary>
    /// Not supported.  Throws an exception saying so.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown.  Always.</exception>
    public override void SetLength(long length) {
      throw new NotSupportedException();
    }
    
    /// <summary>
    /// Closes this Filter and the underlying stream.
    /// </summary>
    /// <remarks>
    /// If you override, call up to this method in your implementation.
    /// </remarks>
    public override void Close() {
      _sink.Close();
    }

    /// <summary>
    /// Fluses this Filter and the underlying stream.
    /// </summary>
    /// <remarks>
    /// If you override, call up to this method in your implementation.
    /// </remarks>
    public override void Flush() {
      _sink.Flush();
    }

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <param name="buffer">The buffer to write into.</param>
    /// <param name="offset">The offset on the buffer to write into</param>
    /// <param name="count">The number of bytes to write.  Must be less than buffer.Length</param>
    /// <returns>An int telling you how many bytes were written</returns>
    public override int Read(byte[] buffer, int offset, int count) {
      throw new NotSupportedException();
    }

  }
}
