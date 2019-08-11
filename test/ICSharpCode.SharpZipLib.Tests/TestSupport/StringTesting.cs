using System.Collections.Generic;

namespace ICSharpCode.SharpZipLib.Tests.TestSupport
{
	public static class StringTesting
	{
		static StringTesting()
		{
			AddLanguage("Chinese", "測試.txt", "big5");
			AddLanguage("Greek", "Ϗΰ.txt", "windows-1253");
			AddLanguage("Nordic", "Åæ.txt", "windows-1252");
			AddLanguage("Arabic", "ڀڅ.txt", "windows-1256");
			AddLanguage("Russian", "Прйвёт.txt", "windows-1251");
		}

		private static void AddLanguage(string language, string filename, string encoding)
		{
			languages.Add(language);
			filenames.Add(filename);
			encodings.Add(encoding);
			entries++;
		}

		private static int entries = 0;
		private static List<string> languages = new List<string>();
		private static List<string> filenames = new List<string>();
		private static List<string> encodings = new List<string>();

		public static IEnumerable<string> Languages => filenames.AsReadOnly();
		public static IEnumerable<string> Filenames => filenames.AsReadOnly();
		public static IEnumerable<string> Encodings => filenames.AsReadOnly();

		public static IEnumerable<(string language, string filename, string encoding)> GetTestSamples()
		{
			for (int i = 0; i < entries; i++)
			{
				yield return (languages[i], filenames[i], encodings[i]);
			}
		}
	}
}
