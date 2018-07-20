﻿using System;
using System.Text;

namespace ICSharpCode.SharpZipLib.Zip
{
	/// <summary>
	/// This static class contains functions for encoding and decoding zip file strings
	/// </summary>
	public static class ZipStrings
	{

		static ZipStrings()
		{
			try
			{
				var codePage = Encoding.GetEncoding(0).CodePage;
				SystemDefaultCodePage = (codePage == 1 || codePage == 2 || codePage == 3 || codePage == 42) ? FallbackCodePage : codePage;
			}
			catch
			{
				SystemDefaultCodePage = FallbackCodePage;
			}
		}

		/// <summary>Default code page backing field</summary>
		/// <remarks>
		/// The original Zip specification (https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT) states 
		/// that file names should only be encoded with IBM Code Page 437 or UTF-8. 
		/// In practice, most zip apps use OEM or system encoding (typically cp437 on Windows). 
		/// Let's be good citizens and default to UTF-8 http://utf8everywhere.org/
		/// </remarks>
		static int defaultCodePage = Encoding.UTF8.CodePage;


		/// <summary>
		/// Default encoding used for string conversion.  0 gives the default system OEM code page.
		/// Using the default code page isnt the full solution neccessarily
		/// there are many variable factors, codepage 850 is often a good choice for
		/// European users, however be careful about compatability.
		/// </summary>
		public static int DefaultCodePage
		{
			get
			{
				return defaultCodePage;
			}
			set
			{
				if ((value < 0) || (value > 65535) ||
					(value == 1) || (value == 2) || (value == 3) || (value == 42))
				{
					throw new ArgumentOutOfRangeException(nameof(value));
				}

				defaultCodePage = value;
			}
		}


		private const int FallbackCodePage = 437;

		/// <summary>
		/// Attempt to get the operating system default codepage, or failing that, to
		/// the fallback code page IBM 437.
		/// </summary>
		public static int SystemDefaultCodePage { get; }

		/// <summary>
		/// Get wether the default codepage is set to UTF-8. Setting this property to false will
		/// set the <see cref="DefaultCodePage"/> to <see cref="SystemDefaultCodePage"/>
		/// </summary>
		/// <remarks>
		/// /// Get OEM codepage from NetFX, which parses the NLP file with culture info table etc etc.
		/// But sometimes it yields the special value of 1 which is nicknamed <c>CodePageNoOEM</c> in <see cref="Encoding"/> sources (might also mean <c>CP_OEMCP</c>, but Encoding puts it so).
		/// This was observed on Ukranian and Hindu systems.
		/// Given this value, <see cref="Encoding.GetEncoding(int)"/> throws an <see cref="ArgumentException"/>.
		/// So replace it with <see cref="FallbackCodePage"/>, (IBM 437 which is the default code page in a default Windows installation console.
		/// </remarks>
		public static bool UseUnicode
		{
			get
			{
				return defaultCodePage == Encoding.UTF8.CodePage;
			}
			set
			{
				if (value)
				{
					defaultCodePage = Encoding.UTF8.CodePage;
				}
				else
				{
					defaultCodePage = SystemDefaultCodePage;
				}
			}
		}

		/// <summary>
		/// Convert a portion of a byte array to a string using <see cref="DefaultCodePage"/>
		/// </summary>		
		/// <param name="data">
		/// Data to convert to string
		/// </param>
		/// <param name="count">
		/// Number of bytes to convert starting from index 0
		/// </param>
		/// <returns>
		/// data[0]..data[count - 1] converted to a string
		/// </returns>
		public static string ConvertToString(byte[] data, int count) 
			=> data == null
			? string.Empty
			: Encoding.GetEncoding(DefaultCodePage).GetString(data, 0, count);

		/// <summary>
		/// Convert a byte array to a string using <see cref="DefaultCodePage"/>
		/// </summary>
		/// <param name="data">
		/// Byte array to convert
		/// </param>
		/// <returns>
		/// <paramref name="data">data</paramref>converted to a string
		/// </returns>
		public static string ConvertToString(byte[] data)
			=> ConvertToString(data, data.Length);

		private static Encoding EncodingFromFlag(int flags)
			=> ((flags & (int)GeneralBitFlags.UnicodeText) != 0)
				? Encoding.UTF8
				: Encoding.GetEncoding(SystemDefaultCodePage);

		/// <summary>
		/// Convert a byte array to a string  using <see cref="DefaultCodePage"/>
		/// </summary>
		/// <param name="flags">The applicable general purpose bits flags</param>
		/// <param name="data">
		/// Byte array to convert
		/// </param>
		/// <param name="count">The number of bytes to convert.</param>
		/// <returns>
		/// <paramref name="data">data</paramref>converted to a string
		/// </returns>
		public static string ConvertToStringExt(int flags, byte[] data, int count)
			=> (data == null)
				? string.Empty
				: EncodingFromFlag(flags).GetString(data, 0, count);

		/// <summary>
		/// Convert a byte array to a string using <see cref="DefaultCodePage"/>
		/// </summary>
		/// <param name="data">
		/// Byte array to convert
		/// </param>
		/// <param name="flags">The applicable general purpose bits flags</param>
		/// <returns>
		/// <paramref name="data">data</paramref>converted to a string
		/// </returns>
		public static string ConvertToStringExt(int flags, byte[] data)
			=> ConvertToStringExt(flags, data, data.Length);

		/// <summary>
		/// Convert a string to a byte array using <see cref="DefaultCodePage"/>
		/// </summary>
		/// <param name="str">
		/// String to convert to an array
		/// </param>
		/// <returns>Converted array</returns>
		public static byte[] ConvertToArray(string str)
			=> str == null
			? new byte[0]
			: Encoding.GetEncoding(DefaultCodePage).GetBytes(str);

		/// <summary>
		/// Convert a string to a byte array using <see cref="DefaultCodePage"/>
		/// </summary>
		/// <param name="flags">The applicable <see cref="GeneralBitFlags">general purpose bits flags</see></param>
		/// <param name="str">
		/// String to convert to an array
		/// </param>
		/// <returns>Converted array</returns>
		public static byte[] ConvertToArray(int flags, string str)
			=> (string.IsNullOrEmpty(str))
				? new byte[0]
				: EncodingFromFlag(flags).GetBytes(str);
	}
}
