using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.SharpZipLib.Tests.TestSupport
{
	public static class StringTesting
	{
		static StringTesting()
		{
			TestSamples = new []
			{
				("Chinese", "測試.txt", "big5"),
				("Greek", "Ϗΰ.txt", "windows-1253"),
				("Nordic", "Åæ.txt", "windows-1252"),
				("Arabic", "ڀڅ.txt", "windows-1256"),
				("Russian", "Прйвёт.txt", "windows-1251"),
			};
		}

		public static (string language, string filename, string encoding)[] TestSamples { get; }

		public static IEnumerable<string> Languages => TestSamples.Select(s => s.language);
		public static IEnumerable<string> Filenames => TestSamples.Select(s => s.filename);
		public static IEnumerable<string> Encodings => TestSamples.Select(s => s.encoding);
	}
}
