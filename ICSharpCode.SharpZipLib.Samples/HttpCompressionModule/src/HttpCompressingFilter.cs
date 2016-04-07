using System;
using System.IO;

namespace blowery.Web.HttpModules {
  /// <summary>
  /// Base for any HttpFilter that performing compression
  /// </summary>
  /// <remarks>
  /// When implementing this class, you need to implement a <see cref="HttpOutputFilter"/>
  /// along with a NameOfContentEncoding property.  The latter corresponds to a 
  /// content coding (see http://www.w3.org/Protocols/rfc2616/rfc2616-sec3.html#sec3.5)
  /// that your implementation will support.
  /// </remarks>
  public abstract class HttpCompressingFilter : HttpOutputFilter {

    /// <summary>
    /// Protected constructor that sets up the underlying stream we're compressing into
    /// </summary>
    /// <param name="baseStream">The stream we're wrapping up</param>
    /// <param name="compressionLevel">The level of compression to use when compressing the content</param>
    protected HttpCompressingFilter(Stream baseStream, CompressionLevels compressionLevel) : base(baseStream) {
      _compressionLevel = compressionLevel;
    }

    /// <summary>
    /// The name of the content-encoding that's being implemented
    /// </summary>
    /// <remarks>
    /// See http://www.w3.org/Protocols/rfc2616/rfc2616-sec3.html#sec3.5 for more
    /// details on content codings.
    /// </remarks>
    public abstract string NameOfContentEncoding { get; }

    private CompressionLevels _compressionLevel;

    /// <summary>
    /// Allow inheriting classes to get access the the level of compression that should be used
    /// </summary>
    protected CompressionLevels CompressionLevel {
      get { return _compressionLevel; }
    }

  }
}
