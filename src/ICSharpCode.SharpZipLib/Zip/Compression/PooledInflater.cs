using ICSharpCode.SharpZipLib.Core;

namespace ICSharpCode.SharpZipLib.Zip.Compression
{
	/// <summary>
	/// A marker type for pooled version of an inflator that we can return back to <see cref="InflaterPool"/>.
	/// </summary>
	internal sealed class PooledInflater : Inflater
	{
		public PooledInflater(bool noHeader) : base(noHeader)
		{
		}
	}
}
