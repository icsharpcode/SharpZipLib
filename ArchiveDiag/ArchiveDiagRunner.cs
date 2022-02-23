using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using ConLib;
using ConLib.Console;
using ConLib.HTML;
using ICSharpCode.SharpZipLib.ArchiveDiag;
using static ConLib.PrettyConsole;

namespace ArchiveDiag
{
	abstract class ArchiveDiagRunner : IDisposable
	{
		private readonly HTMLWriter? html = null;
		private readonly ColorFormatter? fmt = null;
		private Stream archiveStream;

		public int MaxNameHex { get; set; } = 32;
		public int MaxNameString { get; set; } = 128;
		public int MaxCommentHex { get; set; } = 32;
		public int MaxCommentString { get; set; } = 128;
		public int MaxExtraDataHex { get; set; } = 64;

		public bool WarnUnknownSigns { get; set; } = false;

		public string? FileName { get; }

		public ArchiveDiagRunner(Stream archiveStream, string? fileName = null)
		{
			this.archiveStream = archiveStream;
			FileName = fileName;
		}

		public void Run()
		{
			Run(new ConsoleWriter());
		}

		public void Run(params ColorWriter[] writers)
		{
			PrettyFormatters.Clear();

			foreach (var writer in writers)
			{
				var fmt = new ColorFormatter(writer);
				PrettyFormatters.Add(fmt);
			}

			PrettyConsole.ChoreOptions.StartedFormat = "{0} {1}\n";
			PrettyConsole.ChoreOptions.NameColor = ConCol.White;
			PrettyConsole.ChoreOptions.EndedFormat = "{0} {1} in ";

			var now = DateTime.UtcNow;

			WriteColor($"ArchiveDiag ", ConCol.White);
			WriteVersion<Program>();
			WriteLine($"\n");

			
			WriteLine($"Runtime: {RuntimeInformation.FrameworkDescription} ({RuntimeInformation.OSDescription}/{RuntimeInformation.OSArchitecture})");
			WriteLine($"File: {FileName ?? "<Unknown>"}");
			Write($"Size: {archiveStream.Length}");
			WriteColor(" byte(s)\n", ConCol.DarkGray);
			Write($"Generated at: {now:yyyy-MM-dd}");
			WriteColor("T", ConCol.DarkGray);
			Write($"{now:HH:mm:ss}");
			WriteColor("Z\n", ConCol.DarkGray);
			WriteLine($"Parser: {Parser}");
			WriteLine($"Tester: {Tester}");

			WriteLine();

			// Stream archiveStream = null;
			DoTask("Open File", async () =>
			{
				// archiveStream = File.OpenRead(file);
			}, continueOnFail: false);

			DoChore("Parse Archive", () => { ParseArchive(archiveStream); }, continueOnFail: true);

			// DoTask("Rewind File Stream", async () => { archiveStream.Seek(0, SeekOrigin.Begin); },
			//	continueOnFail: false);


			DoChore("Test Archive", () =>
			{
				var result = TestArchive(archiveStream);


				WriteLine($"Test result {(result ? "PASSED" : "FAILED")}", result ? ConCol.Green : ConCol.Red);
			}, continueOnFail: true);

			foreach (var writer in writers)
			{
				//writer.Flush();
				// writer.Close();
			}

#if DEBUG
			// Console.Read();
#endif
		}

		protected static void WriteHexBytes(IEnumerable<byte> data, int maxNumber, bool endLine = true)
		{
			int bytesWritten = 0;
			foreach (var b in data)
			{
				Write($"{b:x2} ");
				if (++bytesWritten >= maxNumber)
				{
					WriteEllipsis(endLine);
					return;
				}
			}

			if (endLine) WriteLine();
		}


		protected static void WriteEllipsis(bool endLine = true) => WriteColor($" [...]{(endLine ? "\n" : "")}", ConCol.DarkGray);

		protected static void WriteBool(bool value) => WriteColor(value ? "Yes" : "No", value ? ConCol.Green : ConCol.Red);
		protected static void WriteBool(string prefix, bool value, bool endLine = true)
		{
			Write(FormattableStringFactory.Create(prefix));
			WriteBool(value);
			if (endLine) WriteLine();
		}

		protected static void WriteString(byte[] rawBytes, int maxLength, bool endLine = true)
		{
			var chars = Encoding.UTF8.GetChars(rawBytes);
			var length = Math.Min(maxLength, chars.Length);

			var firstPrintableIndex = -1;
			for (var i = 0; i < length; i++)
			{
				if (char.IsControl(chars[i]) || chars[i] == '�')
				{
					if (firstPrintableIndex != -1)
					{
						// Write the string up until this position
						Write($"{new string(chars, firstPrintableIndex, i - firstPrintableIndex)}");
						firstPrintableIndex = -1;
					}

					if (chars[i] == '�')
					{
						WriteColor("�", ConCol.Red);
						continue;
					}

					var charRaw = (short)chars[i];
					WriteColor(charRaw > 255 ? $"\\u{charRaw:x4}" : $"\\x{charRaw:x2}", ConCol.Cyan);

				}
				else
				{
					if (firstPrintableIndex == -1)
					{
						firstPrintableIndex = i;
					}

				}
			}

			if (firstPrintableIndex != -1)
			{
				Write($"{new string(chars, firstPrintableIndex, length - firstPrintableIndex)}");
			}

			if (chars.Length > maxLength)
			{
				WriteEllipsis(endLine);
			}
			else if (endLine)
			{
				WriteLine();
			}
		}

		public void Dispose()
		{
			html?.Dispose();
			fmt?.Dispose();
		}

		protected abstract bool TestArchive(Stream archiveStream);
		protected abstract void ParseArchive(Stream archiveStream);

		protected virtual string Parser => "None";
		protected virtual string Tester => "None";
	}
}
