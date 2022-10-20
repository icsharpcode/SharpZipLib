using System;
using System.Text;
using ICSharpCode.SharpZipLib.Core;

namespace ICSharpCode.SharpZipLib.Zip
{
	internal static class EncodingExtensions
	{
		public static bool IsZipUnicode(this Encoding e)
			=> e.Equals(StringCodec.UnicodeZipEncoding);
	}
	
	/// <summary>
	/// Deprecated way of setting zip encoding provided for backwards compability.
	/// Use <see cref="StringCodec"/> when possible.
	/// </summary>
	/// <remarks>
	/// If any ZipStrings properties are being modified, it will enter a backwards compatibility mode, mimicking the
	/// old behaviour where a single instance was shared between all Zip* instances.
	/// </remarks>
	public static class ZipStrings
	{
		static StringCodec CompatCodec = StringCodec.Default;

		private static bool compatibilityMode;
		
		/// <summary>
		/// Returns a new <see cref="StringCodec"/> instance or the shared backwards compatible instance.
		/// </summary>
		/// <returns></returns>
		public static StringCodec GetStringCodec() 
			=> compatibilityMode ? CompatCodec : StringCodec.Default;

		/// <inheritdoc cref="ZipStrings"/>
		[Obsolete("Use ZipFile/Zip*Stream StringCodec instead")]
		public static int CodePage
		{
			get => CompatCodec.CodePage;
			set
			{
				CompatCodec = new StringCodec(CompatCodec.ForceZipLegacyEncoding, Encoding.GetEncoding(value))
				{
					ZipArchiveCommentEncoding = CompatCodec.ZipArchiveCommentEncoding,
					ZipCryptoEncoding = CompatCodec.ZipCryptoEncoding,
				};
				compatibilityMode = true;
			}
		}

		/// <inheritdoc cref="ZipStrings"/>
		[Obsolete("Use ZipFile/Zip*Stream StringCodec instead")]
		public static int SystemDefaultCodePage => StringCodec.SystemDefaultCodePage;

		/// <inheritdoc cref="ZipStrings"/>
		[Obsolete("Use ZipFile/Zip*Stream StringCodec instead")]
		public static bool UseUnicode
		{
			get => !CompatCodec.ForceZipLegacyEncoding;
			set
			{
				CompatCodec = new StringCodec(!value, CompatCodec.LegacyEncoding)
				{
					ZipArchiveCommentEncoding = CompatCodec.ZipArchiveCommentEncoding,
					ZipCryptoEncoding = CompatCodec.ZipCryptoEncoding,
				};
				compatibilityMode = true;
			}
		}

		/// <inheritdoc cref="ZipStrings"/>
		[Obsolete("Use ZipFile/Zip*Stream StringCodec instead")]
		private static bool HasUnicodeFlag(int flags)
			=> ((GeneralBitFlags)flags).HasFlag(GeneralBitFlags.UnicodeText);
		
		/// <inheritdoc cref="ZipStrings"/>
		[Obsolete("Use ZipFile/Zip*Stream StringCodec instead")]
		public static string ConvertToString(byte[] data, int count)
			=> CompatCodec.ZipOutputEncoding.GetString(data, 0, count);

		/// <inheritdoc cref="ZipStrings"/>
		[Obsolete("Use ZipFile/Zip*Stream StringCodec instead")]
		public static string ConvertToString(byte[] data)
			=> CompatCodec.ZipOutputEncoding.GetString(data);
		
		/// <inheritdoc cref="ZipStrings"/>
		[Obsolete("Use ZipFile/Zip*Stream StringCodec instead")]
		public static string ConvertToStringExt(int flags, byte[] data, int count)
			=> CompatCodec.ZipEncoding(HasUnicodeFlag(flags)).GetString(data, 0, count);

		/// <inheritdoc cref="ZipStrings"/>
		[Obsolete("Use ZipFile/Zip*Stream StringCodec instead")]
		public static string ConvertToStringExt(int flags, byte[] data)
			=> CompatCodec.ZipEncoding(HasUnicodeFlag(flags)).GetString(data);

		/// <inheritdoc cref="ZipStrings"/>
		[Obsolete("Use ZipFile/Zip*Stream StringCodec instead")]
		public static byte[] ConvertToArray(string str)
			=> ConvertToArray(0, str);
		
		/// <inheritdoc cref="ZipStrings"/>
		[Obsolete("Use ZipFile/Zip*Stream StringCodec instead")]
		public static byte[] ConvertToArray(int flags, string str)
			=> (string.IsNullOrEmpty(str))
				? Empty.Array<byte>()
				: CompatCodec.ZipEncoding(HasUnicodeFlag(flags)).GetBytes(str);
	}

	/// <summary>
	/// Utility class for resolving the encoding used for reading and writing strings
	/// </summary>
	public class StringCodec
	{
		internal StringCodec(bool forceLegacyEncoding, Encoding legacyEncoding)
		{
			LegacyEncoding = legacyEncoding;
			ForceZipLegacyEncoding = forceLegacyEncoding;
			ZipArchiveCommentEncoding = legacyEncoding;
			ZipCryptoEncoding = legacyEncoding;
		}

		/// <summary>
		/// Creates a StringCodec that uses the system default encoder or UTF-8 depending on whether the zip entry Unicode flag is set
		/// </summary>
		public static StringCodec Default 
			=> new StringCodec(false, SystemDefaultEncoding);

		/// <summary>
		/// Creates a StringCodec that uses an encoding from the specified code page except for zip entries with the Unicode flag
		/// </summary>
		public static StringCodec FromCodePage(int codePage) 
			=> new StringCodec(false, Encoding.GetEncoding(codePage));

		/// <summary>
		/// Creates a StringCodec that uses an the specified encoding, except for zip entries with the Unicode flag
		/// </summary>
		public static StringCodec FromEncoding(Encoding encoding)
			=> new StringCodec(false, encoding);

		/// <summary>
		/// Creates a StringCodec that uses the zip specification encoder or UTF-8 depending on whether the zip entry Unicode flag is set
		/// </summary>
		public static StringCodec WithStrictSpecEncoding()
			=> new StringCodec(false, Encoding.GetEncoding(ZipSpecCodePage));

		/// <summary>
		/// If set, use the encoding set by <see cref="CodePage"/> for zip entries instead of the defaults
		/// </summary>
		public bool ForceZipLegacyEncoding { get; internal set; }

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
		public Encoding ZipEncoding(bool unicode) 
			=> unicode ? UnicodeZipEncoding : LegacyEncoding;

		/// <summary>
		/// Returns the appropriate encoding for an input <see cref="ZipEntry"/> according to <paramref name="flags"/>.
		/// If overridden by <see cref="ForceZipLegacyEncoding"/>, it always returns the encoding indicated by <see cref="CodePage"/>.
		/// </summary>
		/// <param name="flags"></param>
		/// <returns></returns>
		public Encoding ZipInputEncoding(GeneralBitFlags flags) 
			=> ZipEncoding(!ForceZipLegacyEncoding && flags.HasAny(GeneralBitFlags.UnicodeText));

		/// <inheritdoc cref="ZipInputEncoding(GeneralBitFlags)"/>
		public Encoding ZipInputEncoding(int flags) => ZipInputEncoding((GeneralBitFlags)flags);

		/// <summary>Code page encoding, used for non-unicode strings</summary>
		/// <remarks>
		/// The original Zip specification (https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT) states
		/// that file names should only be encoded with IBM Code Page 437 or UTF-8.
		/// In practice, most zip apps use OEM or system encoding (typically cp437 on Windows).
		/// </remarks>
		public Encoding LegacyEncoding { get; internal set; }

		/// <summary>
		/// Returns the UTF-8 code page (65001) used for zip entries with unicode flag set
		/// </summary>
		public static readonly Encoding UnicodeZipEncoding = Encoding.UTF8;

		/// <summary>
		/// Code page used for non-unicode strings and legacy zip encoding (if <see cref="ForceZipLegacyEncoding"/> is set).
		/// Default value is <see cref="SystemDefaultCodePage"/>
		/// </summary>
		public int CodePage => LegacyEncoding.CodePage;

		/// <summary>
		/// The non-unicode code page that should be used according to the zip specification
		/// </summary>
		public const int ZipSpecCodePage = 437;

		/// <summary>
		/// Operating system default codepage.
		/// </summary>
		public static int SystemDefaultCodePage => SystemDefaultEncoding.CodePage;

		/// <summary>
		/// The system default encoding.
		/// </summary>
		public static Encoding SystemDefaultEncoding => Encoding.GetEncoding(0);

		/// <summary>
		/// The encoding used for the zip archive comment. Defaults to the encoding for <see cref="CodePage"/>, since
		/// no unicode flag can be set for it in the files.
		/// </summary>
		public Encoding ZipArchiveCommentEncoding { get; internal set; }

		/// <summary>
		/// The encoding used for the ZipCrypto passwords. Defaults to <see cref="DefaultZipCryptoEncoding"/>.
		/// </summary>
		public Encoding ZipCryptoEncoding { get; internal set; }

		/// <summary>
		/// Create a copy of this StringCodec with the specified zip archive comment encoding
		/// </summary>
		/// <param name="commentEncoding"></param>
		/// <returns></returns>
		public StringCodec WithZipArchiveCommentEncoding(Encoding commentEncoding)
			=> new StringCodec(ForceZipLegacyEncoding, LegacyEncoding)
			{
				ZipArchiveCommentEncoding = commentEncoding,
				ZipCryptoEncoding = ZipCryptoEncoding
			};

		/// <summary>
		/// Create a copy of this StringCodec with the specified zip crypto password encoding
		/// </summary>
		/// <param name="cryptoEncoding"></param>
		/// <returns></returns>
		public StringCodec WithZipCryptoEncoding(Encoding cryptoEncoding)
			=> new StringCodec(ForceZipLegacyEncoding, LegacyEncoding)
			{
				ZipArchiveCommentEncoding = ZipArchiveCommentEncoding,
				ZipCryptoEncoding = cryptoEncoding
			};

		/// <summary>
		/// Create a copy of this StringCodec that ignores the Unicode flag when reading entries
		/// </summary>
		/// <returns></returns>
		public StringCodec WithForcedLegacyEncoding()
			=> new StringCodec(true, LegacyEncoding)
			{
				ZipArchiveCommentEncoding = ZipArchiveCommentEncoding,
				ZipCryptoEncoding = ZipCryptoEncoding
			};
	}
}
