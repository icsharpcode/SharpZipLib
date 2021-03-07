using System;
using System.Text;

namespace ICSharpCode.SharpZipLib.Zip
{
	internal static class EncodingExtensions
	{
		public static bool IsZipUnicode(this Encoding e)
			=> e.Equals(StringCodec.UnicodeZipEncoding);
	}

	/// <summary>
	/// Utility class for resolving the encoding used for reading and writing strings
	/// </summary>
	public class StringCodec
	{
		static StringCodec()
		{
			try
			{
				var platformCodepage = Encoding.GetEncoding(0).CodePage;
				SystemDefaultCodePage = (platformCodepage == 1 || platformCodepage == 2 || platformCodepage == 3 || platformCodepage == 42) ? FallbackCodePage : platformCodepage;
			}
			catch
			{
				SystemDefaultCodePage = FallbackCodePage;
			}

			SystemDefaultEncoding = Encoding.GetEncoding(SystemDefaultCodePage);
		}

		/// <summary>
		/// If set, use the encoding set by <see cref="CodePage"/> for zip entries instead of the defaults
		/// </summary>
		public bool ForceZipLegacyEncoding { get; set; }

		/// <summary>
		/// The default encoding used for ZipCrypto passwords in zip files, set to <see cref="SystemDefaultEncoding"/>
		/// for greatest compability.
		/// </summary>
		public static Encoding DefaultZipCryptoEncoding => SystemDefaultEncoding;

		/// <summary>
		/// Returns the encoding for an output <see cref="ZipEntry"/>.
		/// Unless overriden by <see cref="ForceZipLegacyEncoding"/> it returns <see cref="UnicodeZipEncoding"/>.
		/// </summary>
		public Encoding ZipOutputEncoding => ZipEncoding(!ForceZipLegacyEncoding);

		/// <summary>
		/// Returns <see cref="UnicodeZipEncoding"/> if <paramref name="unicode"/> is set, otherwise it returns the encoding indicated by <see cref="CodePage"/>
		/// </summary>
		public Encoding ZipEncoding(bool unicode) => unicode ? UnicodeZipEncoding : _legacyEncoding;

		/// <summary>
		/// Returns the appropriate encoding for an input <see cref="ZipEntry"/> according to <paramref name="flags"/>.
		/// If overridden by <see cref="ForceZipLegacyEncoding"/>, it always returns the encoding indicated by <see cref="CodePage"/>.
		/// </summary>
		/// <param name="flags"></param>
		/// <returns></returns>
		public Encoding ZipInputEncoding(GeneralBitFlags flags) => ZipInputEncoding((int)flags);

		/// <inheritdoc cref="ZipInputEncoding(GeneralBitFlags)"/>
		public Encoding ZipInputEncoding(int flags) => ZipEncoding(!ForceZipLegacyEncoding && (flags & (int)GeneralBitFlags.UnicodeText) != 0);

		/// <summary>Code page encoding, used for non-unicode strings</summary>
		/// <remarks>
		/// The original Zip specification (https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT) states
		/// that file names should only be encoded with IBM Code Page 437 or UTF-8.
		/// In practice, most zip apps use OEM or system encoding (typically cp437 on Windows).
		/// Let's be good citizens and default to UTF-8 http://utf8everywhere.org/
		/// </remarks>
		private Encoding _legacyEncoding = SystemDefaultEncoding;

		private Encoding _zipArchiveCommentEncoding;

		/// <summary>
		/// Returns the UTF-8 code page (65001) used for zip entries with unicode flag set
		/// </summary>
		public static readonly Encoding UnicodeZipEncoding = Encoding.UTF8;

		/// <summary>
		/// Code page used for non-unicode strings and legacy zip encoding (if <see cref="ForceZipLegacyEncoding"/> is set).
		/// Default value is <see cref="SystemDefaultCodePage"/>
		/// </summary>
		public int CodePage
		{
			get => _legacyEncoding.CodePage;
			set => _legacyEncoding = (value < 4 || value > 65535 || value == 42)
				? throw new ArgumentOutOfRangeException(nameof(value))
				: Encoding.GetEncoding(value);
		}

		private const int FallbackCodePage = 437;

		/// <summary>
		/// Operating system default codepage, or if it could not be retrieved, the fallback code page IBM 437.
		/// </summary>
		public static int SystemDefaultCodePage { get; }

		/// <summary>
		/// The system default encoding, based on <see cref="SystemDefaultCodePage"/>
		/// </summary>
		public static Encoding SystemDefaultEncoding { get; }

		/// <summary>
		/// The encoding used for the zip archive comment. Defaults to the encoding for <see cref="CodePage"/>, since
		/// no unicode flag can be set for it in the files.
		/// </summary>
		public Encoding ZipArchiveCommentEncoding
		{
			get => _zipArchiveCommentEncoding ?? _legacyEncoding;
			set => _zipArchiveCommentEncoding = value;
		}

		/// <summary>
		/// The encoding used for the ZipCrypto passwords. Defaults to <see cref="DefaultZipCryptoEncoding"/>.
		/// </summary>
		public Encoding ZipCryptoEncoding
		{
			get => _zipArchiveCommentEncoding ?? DefaultZipCryptoEncoding;
			set => _zipArchiveCommentEncoding = value;
		}
	}
}
