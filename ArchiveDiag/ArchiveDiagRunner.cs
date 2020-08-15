using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ConLib;
using ConLib.Console;
using ConLib.HTML;
using ICSharpCode.SharpZipLib.ArchiveDiag;
using ICSharpCode.SharpZipLib.Zip;
using static ConLib.PrettyConsole;

namespace ArchiveDiag
{
	class ArchiveDiagRunner: IDisposable
	{
		private readonly HTMLWriter? html = null;
		private readonly ColorFormatter? fmt = null;
		private Stream archiveStream;

		public int MaxNameHex {get; set;} = 32;
		public int MaxNameString {get; set;} = 128;
		public int MaxCommentHex {get; set;} = 32;
		public int MaxCommentString {get; set;} = 128;
		public int MaxExtraDataHex {get; set;} = 64;

		public bool WarnUnknownSigns {get; set;} = false;

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
				PrettyFormatters.Add(new ColorFormatter(writer));
			}

			PrettyConsole.ChoreOptions.StartedFormat = "{0} {1}\n";
			PrettyConsole.ChoreOptions.NameColor = ConCol.White;
			PrettyConsole.ChoreOptions.EndedFormat = "{0} {1} in ";

			var now = DateTime.UtcNow;

			WriteColor($"ArchiveDiag ", ConCol.White);
			WriteVersion<Program>();
			WriteLine($"\n");

			WriteLine($"File: {FileName ?? "<Unknown>"}");
			Write($"Size: {archiveStream.Length}");
			WriteColor(" byte(s)\n", ConCol.DarkGray);
			Write($"Generated at: {now:yyyy-MM-dd}");
			WriteColor("T", ConCol.DarkGray);
			Write($"{now:HH:mm:ss}");
			WriteColor("Z\n", ConCol.DarkGray);

			WriteLine();

#if DEBUG
			Console.Read();
#endif

			// Stream archiveStream = null;
			DoTask("Open File", async () =>
			{
				// archiveStream = File.OpenRead(file);
			}, continueOnFail: false);

			DoChore("Parse Archive", () =>
			{
				ParseArchive(archiveStream);
			}, continueOnFail: true);

			DoTask("Rewind File Stream", async () =>
			{
				archiveStream.Seek(0, SeekOrigin.Begin);
			}, continueOnFail: false);


			DoChore("Test Archive", () =>
			{
				var zf = new ZipFile(archiveStream);

				var result = zf.TestArchive(true, TestStrategy.FindAllErrors, (status, message) =>
				{
					if (status.Operation == TestOperation.EntryData) return;
					var pad = "";//.PadLeft(18 - status.Operation.ToString().Length);
					if (status.Entry is { } entry)
					{
						WriteLine($"[{status.Operation}{pad}] #{entry.ZipFileIndex} {entry.Name}");
					}
					else
					{
						WriteLine($"[{status.Operation}{pad}]");
					}

					if (!string.IsNullOrEmpty(message))
					{
						WriteLine($"[{status.Operation}{pad}] {message}");
					}
				});


				WriteLine($"Test result {(result ? "PASSED" : "FAILED")}", result ? ConCol.Green : ConCol.Red);
			}, continueOnFail: true);
		}


		private void ParseArchive(Stream transportStream)
		{

			transportStream.Seek(0, SeekOrigin.Begin);

			var br = new BinaryReader(transportStream);

			int readVal;
			while ((readVal = transportStream.ReadByte()) != -1)
			{
				if (readVal != 'P') continue;
				readVal = transportStream.ReadByte();

				if (readVal != 'K') continue;
				var b1 = (byte)transportStream.ReadByte();
				var b2 = (byte)transportStream.ReadByte();

				if (b1 > 0x08 || b2 > 0x08)
				{
					transportStream.Seek(-2, SeekOrigin.Current);
					continue;
				}

				var sign = 'P' | ('K' << 8) | (b1 << 16) | (b2 << 24);

				var signId = sign switch
				{
					ZipConstants.DataDescriptorSignature
						=> nameof(ZipConstants.DataDescriptorSignature),
					ZipConstants.CentralHeaderSignature
						=> nameof(ZipConstants.CentralHeaderSignature),
					ZipConstants.CentralHeaderDigitalSignature
						=> nameof(ZipConstants.CentralHeaderDigitalSignature),
					ZipConstants.EndOfCentralDirectorySignature
						=> nameof(ZipConstants.EndOfCentralDirectorySignature),
					ZipConstants.LocalHeaderSignature
						=> nameof(ZipConstants.LocalHeaderSignature),
					ZipConstants.SpanningTempSignature
						=> nameof(ZipConstants.SpanningTempSignature),
					ZipConstants.Zip64CentralDirLocatorSignature
						=> nameof(ZipConstants.Zip64CentralDirLocatorSignature),
					ZipConstants.Zip64CentralFileHeaderSignature
						=> nameof(ZipConstants.Zip64CentralFileHeaderSignature),
					_ => $"Unknown (0x{sign:x8})",
				};

				//if (warnUnknownSigns || signId.StartsWith("Unknown"))
				//{
				WriteLine($"{transportStream.Position:x8} PK: {b1:x2} {b2:x2} {signId}");
				//}

				switch (sign)
				{
					case ZipConstants.LocalHeaderSignature:
						DoChore("Parse Local File Header", ()
							=> ParseLocalHeader(br), true);
						break;
					case ZipConstants.DataDescriptorSignature:
						DoChore("Parse Data Descriptor", ()
							=> ParseDataDescriptor(br), true);
						break;
					case ZipConstants.CentralHeaderSignature:
						DoChore("Parse Central Directory Header", ()
							=> ParseCentralHeader(br), true);
						break;
					case ZipConstants.Zip64CentralFileHeaderSignature:
						DoChore("Parse Zip64 End Of Central Directory Record", ()
							=> ParseZip64EndOfCentralDirectory(br), true);
						break;
					case ZipConstants.Zip64CentralDirLocatorSignature:
						DoChore("Parse Zip64 Central Directory Locator", ()
							=> ParseZip64CentralDirLocator(br), true);
						break;
					case ZipConstants.EndOfCentralDirectorySignature:
						DoChore("Parse End of Central Directory Record", ()
							=> ParseCentralDirectory(br), true);
						break;
					default:
						if (WarnUnknownSigns) WriteLine($"{"Skipping unrecognized signature!"}\n");
						break;
				}

			}

		}

		private void ParseArchiveExtraData(BinaryReader br)
		{
			var extraLen = br.ReadUInt16();
			var extraDataBytes = new byte[extraLen];
			br.Read(extraDataBytes);
			var extraData = new ZipExtraData(extraDataBytes);

			WriteLine($"Extra data:");
			ParseExtraData(extraData);
		}

		private void ParseZip64CentralDirLocator(BinaryReader br)
		{
			var diskNumber = br.ReadUInt32();
			var relativeOffset = br.ReadUInt64();
			var diskTotal = br.ReadUInt64();

			WriteLine($"Zip64 End Record Disk Number: {diskNumber}");
			WriteLine($"Zip64 End Record Relative Offset: {relativeOffset} (0x{relativeOffset:x8})");
			WriteLine($"Total number of disks: {diskTotal}");
		}

		private void ParseZip64EndOfCentralDirectory(BinaryReader br)
		{
			var recordSize = br.ReadUInt64();
			var versionMadeBy = ZipVersion.From(br.ReadUInt16());
			var versionToExtract = ZipVersion.From(br.ReadUInt16());
			var diskNumber = br.ReadUInt32();
			var startCentralDirDisk = br.ReadUInt32();

			var entriesForDisk = br.ReadUInt64();
			var entriesForWholeCentralDir = br.ReadUInt64();
			var centralDirSize = br.ReadUInt64();
			var offsetOfCentralDir = br.ReadUInt64();

			var extDataSize = recordSize - 44;

			WriteLine($"Size of record: {recordSize} (0x{recordSize:x16})");
			WriteLine($"Version Made By: {versionMadeBy} (0x{versionMadeBy.OSRaw:x2})");
			WriteLine($"Version Needed: {versionToExtract} (0x{versionToExtract.OSRaw:x2})");
			WriteLine($"Disk Number: {diskNumber}");
			WriteLine($"Start Central Directory Disk: {startCentralDirDisk:x8}");

			WriteLine($"Entries For Disk: {entriesForDisk}");
			WriteLine($"Entries For Central Directory: {entriesForWholeCentralDir}");
			WriteLine($"Central Directory Size: {centralDirSize} (0x{centralDirSize:x16})");
			WriteLine($"Central Directory Offset: {offsetOfCentralDir} (0x{offsetOfCentralDir:x16})");


			WriteLine($"Extensible data size: {extDataSize}");
			if (extDataSize <= 0) return;

			WriteLine($"Parsing of extensible data is not supported! Skipping.", ConCol.Yellow);
			// Ignore skipping the data for now, since it is more likely to cause issues than it is to prevent them
			//br.BaseStream.Seek((long)extDataSize, SeekOrigin.Current);
		}

		private static DateTime DateTimeFromDosTime(uint dosTime)
		{
			var sec = Math.Min(59, 2 * (dosTime & 0x1f));
			var min = Math.Min(59, (dosTime >> 5) & 0x3f);
			var hrs = Math.Min(23, (dosTime >> 11) & 0x1f);
			var mon = Math.Max(1, Math.Min(12, ((dosTime >> 21) & 0xf)));
			var year = ((dosTime >> 25) & 0x7f) + 1980;
			var day = Math.Max(1, Math.Min(DateTime.DaysInMonth((int)year, (int)mon), (int)((dosTime >> 16) & 0x1f)));
			return new DateTime((int)year, (int)mon, day, (int)hrs, (int)min, (int)sec);
		}

		private void ParseCentralHeader(BinaryReader br)
		{
			var versionMadeBy = ZipVersion.From(br.ReadUInt16());
			var versionToExtract = ZipVersion.From(br.ReadUInt16());
			var bitFlags = (GeneralBitFlags)br.ReadUInt16();
			var method = (CompressionMethod)br.ReadUInt16();
			var dosTime = br.ReadUInt32();
			var dateTime = DateTimeFromDosTime(dosTime);
			var crc = br.ReadUInt32();
			var compressedSize = (long)br.ReadUInt32();
			var size = (long)br.ReadUInt32();
			var nameLen = br.ReadUInt16();
			var extraLen = br.ReadUInt16();
			var commentLen = br.ReadUInt16();
			var diskStartNo = br.ReadUInt16();  // Not currently used
			var internalAttributes = br.ReadUInt16();  // Not currently used

			var externalAttributes = (FileAttributes)br.ReadUInt32();
			var offset = br.ReadUInt32();

			var nameData = new byte[nameLen];
			br.Read(nameData);

			var extraDataBytes = new byte[extraLen];
			br.Read(extraDataBytes);
			var extraData = new ZipExtraData(extraDataBytes);

			var commentData = new byte[commentLen];
			br.Read(commentData);


			WriteLine($"Version Made By: {versionMadeBy} (0x{versionMadeBy.OSRaw:x2})");
			WriteLine($"Version Needed: {versionToExtract} (0x{versionToExtract.OSRaw:x2})");
			WriteLine($"Bit flags: {bitFlags:F}");
			WriteLine($"Compression Method: {method:G}");
			WriteLine($"DOS Date: 0x{dosTime:x8}");
			WriteLine($"File Date: {dateTime:yyyy-MM-dd}");
			WriteLine($"File Time: {dateTime:HH:mm:ss}");
			WriteLine($"CRC: 0x{crc:x8}");
			WriteSize($"Compressed Size:", compressedSize);
			WriteSize($"Uncompressed Size:", size);
			WriteLine($"Name Length: {nameLen}");
			WriteLine($"Extra Data Length: {extraLen}");
			WriteLine($"Comment Length: {commentLen}");
			WriteLine($"Disk number start: {diskStartNo}");
			WriteLine($"Internal file attributes: {internalAttributes:x4}");
			WriteLine($"External file attributes: {externalAttributes:F}");
			WriteLine($"Relative offset of local header: {offset}");

			Write($"Name (raw): "); WriteHexBytes(nameData, MaxNameHex);
			Write($"Name: "); WriteString(nameData, MaxNameString);

			if (commentLen > 0)
			{
				Write($"Comment (raw): "); WriteHexBytes(commentData, MaxCommentHex);
				Write($"Comment: "); WriteString(commentData, MaxCommentString);
			}

			WriteLine($"Extra data:");
			ParseExtraData(extraData);

		}

		private static void WriteSize(FormattableString fs, in long size)
		{
			Write(fs);

			if (size == uint.MaxValue)
			{
				WriteLine($" {size} ({"Zip64 Indicator"})", ConCol.DarkGray, ConCol.Blue);
			}
			else
			{
				WriteLine($" {size}");
			}
		}

		private void ParseExtraData(ZipExtraData extraData)
		{
			PushGroup("extra-data");
			var extraDataBytes = extraData.GetEntryData();

			foreach (var (tag, range) in extraData.EnumerateTags().ToList())
			{
				var length = range.End.Value - range.Start.Value;

				var knownId = Enum.IsDefined(typeof(ExtraDataType), tag);

				WriteLine($" - Type: {(knownId ? tag : ExtraDataType.Unknown):G} ({(uint)tag:x4}), Length: {length}");
				PushGroup("extra-raw");
				if (extraDataBytes.Length < range.End.Value)
				{
					WriteLine($" Invalid length! Skipping parse attemmpt!", ConCol.Red);
					PopGroup(); // extra-raw
					PopGroup(); // extra-data
					return;
				}

				Write($" Raw: ");
				WriteHexBytes(extraDataBytes[range], MaxExtraDataHex);

				PopGroup();


				switch (tag)
				{
					case ExtraDataType.NTFS:
						{
							PushGroup("extra-ntfs");
							var tagNtDate = extraData.GetData<NTTaggedData>();

							WriteLine($" Created: {tagNtDate.CreateTime:yyyy-MM-dd HH:mm:ss}");
							WriteLine($" Accessed: {tagNtDate.LastAccessTime:yyyy-MM-dd HH:mm:ss}");
							WriteLine($" Modified: {tagNtDate.LastModificationTime:yyyy-MM-dd HH:mm:ss}");
							PopGroup();

							break;
						}
					case ExtraDataType.UnixExtendedTime:
						{
							PushGroup("extra-unix-xtime");

							var tagUnixDate = extraData.GetData<ExtendedUnixData>();

							WriteLine($" Created: {tagUnixDate.CreateTime:yyyy-MM-dd HH:mm:ss}");
							WriteLine($" Accessed: {tagUnixDate.AccessTime:yyyy-MM-dd HH:mm:ss}");
							WriteLine($" Modified: {tagUnixDate.ModificationTime:yyyy-MM-dd HH:mm:ss}");
							PopGroup();
							break;
						}
					case ExtraDataType.UnicodeName:
						{
							PushGroup("extra-unicode-name");

							var nameRange = new Range(range.Start.Value + 5, range.End.Value);
							var unicodeName = Encoding.UTF8.GetString(extraDataBytes[nameRange]);
							File.WriteAllText("unicodetext.txt", unicodeName);
							WriteLine($" Unicode name: {unicodeName}");
							PopGroup();
							break;
						}
					case ExtraDataType.Zip64:
						{
							PushGroup("extra-zip64");
							using (var br = new BinaryReader(extraData.GetStreamForTag((int)tag)))
							{
								var usize64 = br.ReadUInt64();
								var csize64 = br.ReadUInt64();
								WriteLine($" Compressed Size: {csize64} (0x{csize64:x16})");
								WriteLine($" Uncompressed Size: {usize64} (0x{usize64:x16})");
							}

							PopGroup();
							break;
						}
				}
			}

			PopGroup();
		}

		private void ParseCentralDirectory(BinaryReader br)
		{
			var diskNumber = br.ReadUInt16();
			var startCentralDirDisk = br.ReadUInt16();
			var entriesForDisk = br.ReadUInt16();
			var entriesForWholeCentralDir = br.ReadUInt16();
			var centralDirSize = br.ReadUInt32();
			var offsetOfCentralDir = br.ReadUInt32();
			var commentSize = br.ReadUInt16();

			byte[] commentData = new byte[commentSize];
			br.Read(commentData);

			WriteLine($"Disk Number: {diskNumber:x4}");
			WriteLine($"Start Central Directory Disk: {startCentralDirDisk:x4}");
			WriteLine($"Entries For Central Directory: {entriesForWholeCentralDir}");
			WriteLine($"Entries For Disk: {entriesForDisk}");
			WriteLine($"Central Directory Size: {centralDirSize}");
			WriteLine($"Central Directory Offset: {offsetOfCentralDir:x8}");
			WriteLine($"Comment Size: {commentSize}");

			if (commentData.Length > 0)
			{
				Write($"Comment (raw): "); WriteHexBytes(commentData, MaxCommentHex);
				Write($"Comment: "); WriteString(commentData, MaxCommentString);
			}

			var zip64 = diskNumber == 0xffff
						|| startCentralDirDisk == 0xffff
						|| entriesForDisk == 0xffff
						|| entriesForWholeCentralDir == 0xffff
						|| centralDirSize == 0xffffffff
						|| offsetOfCentralDir == 0xffffffff;

			WriteLine($"Zip64 Indication: {(zip64 ? "Yes" : "No")}", zip64 ? ConCol.Green : ConCol.Red);
		}

		private static void ParseDataDescriptor(BinaryReader br)
		{
			var crc = br.ReadInt32();

			var pos = br.BaseStream.Position;
			var csize = br.ReadInt32();
			var usize = br.ReadInt32();
			var crc32 = br.ReadUInt32();

			br.BaseStream.Seek(-12, SeekOrigin.Current);

			var csize64 = br.ReadInt64();
			var usize64 = br.ReadInt64();
			var crc64 = br.ReadUInt32();

			// Revert stream back to non-zip64 descriptor to not risk skipping next entry
			br.BaseStream.Seek(-8, SeekOrigin.Current);

			WriteLine($"32-bit sizes:");
			PushGroup("descriptor32");
			WriteLine($"Compressed Size: {csize} (0x{csize:x8})");
			WriteLine($"Uncompressed Size: {usize} (0x{usize:x8})");
			WriteLine($"CRC: 0x{crc32:x8}");
			PopGroup();

			WriteLine($"64-bit sizes (Zip64):");
			PushGroup("descriptor64");
			WriteLine($"Compressed Size: {csize64} (0x{csize64:x16})");
			WriteLine($"Uncompressed Size: {usize64} (0x{usize64:x16})");
			WriteLine($"CRC: 0x{crc64:x8}");
			PopGroup();

		}

		private void ParseLocalHeader(BinaryReader br)
		{



			var extractVersion = ZipVersion.From(br.ReadUInt16());
			var flags = (GeneralBitFlags)br.ReadUInt16();
			var method = (CompressionMethod)br.ReadUInt16();
			var dosTime = br.ReadUInt32();
			var dateTime = DateTimeFromDosTime(dosTime);
			var crc = br.ReadUInt32();
			long compressedSize = br.ReadUInt32();
			long size = br.ReadUInt32();
			int storedNameLength = br.ReadUInt16();
			int extraDataLength = br.ReadUInt16();

			var nameData = new byte[storedNameLength];
			br.Read(nameData);

			var extraDataBytes = new byte[extraDataLength];
			br.Read(extraDataBytes);

			var extraData = new ZipExtraData(extraDataBytes);


			WriteLine($"Version Needed: {extractVersion} (0x{extractVersion.OperatingSystem:x})");
			WriteLine($"Local Flags: {flags:F}");
			WriteLine($"Compression Method: {method:G}");
			WriteLine($"DOS Date: 0x{dosTime:x8}");
			WriteLine($"File Date: {dateTime:yyyy-MM-dd}");
			WriteLine($"File Time: {dateTime:HH:mm:ss}");
			WriteLine($"CRC: 0x{crc:x8}");
			WriteSize($"Compressed Size:", compressedSize);
			WriteSize($"Uncompressed Size:", size);
			WriteLine($"Name Length: {storedNameLength}");
			WriteLine($"Extra Data Length: {extraDataLength}");
			WriteLine($"Name (raw): "); WriteHexBytes(nameData, MaxNameHex);
			Write($"Name: "); WriteString(nameData, MaxNameString);
			WriteLine($"Extra data:");
			ParseExtraData(extraData);

		}


		// Utils

		private static void WriteHexBytes(IEnumerable<byte> data, int maxNumber, bool endLine = true)
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


		private static void WriteEllipsis(bool endLine = true) => WriteColor($" [...]{(endLine ? "\n" : "")}", ConCol.DarkGray);

		private static void WriteString(byte[] rawBytes, int maxLength, bool endLine = true)
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
	}
}
