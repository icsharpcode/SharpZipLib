using ICSharpCode.SharpZipLib.Zip.Compression;

namespace ICSharpCode.SharpZipLib
{
	/// <summary>
	/// Global options to alter behavior.
	/// </summary>
	public static class SharpZipLibOptions
	{
		/// <summary>
		/// The max pool size allowed for reusing <see cref="Inflater"/> instances, defaults to 0 (disabled).
		/// </summary>
		public static int InflaterPoolSize { get; set; } = 0;
	}
}
