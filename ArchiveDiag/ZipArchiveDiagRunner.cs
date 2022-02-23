using System;
using System.IO;
using System.Linq;
using System.Text;
using ConLib;
using ICSharpCode.SharpZipLib.ArchiveDiag;
using ICSharpCode.SharpZipLib.Zip;

namespace ArchiveDiag
{
	class ZipArchiveDiagRunner: ArchiveDiagRunner
	{
		protected override string Parser => "ArchiveDiag.ZipParser";
		protected override string Tester => "ICSharpCode.SharpZipLib.ZipFile+ZipInputStream";

		public int MaxNameHex {get; set;} = 32;
		public int MaxNameString {get; set;} = 128;
		public int MaxCommentHex {get; set;} = 32;
		public int MaxCommentString {get; set;} = 128;
		public int MaxExtraDataHex {get; set;} = 64;

		public bool WarnUnknownSigns {get; set;} = false;

		public ZipArchiveDiagRunner(Stream archiveStream, string? fileName = null): base(archiveStream, fileName)
		{
		}

		protected override bool TestArchive(Stream archiveStream)
		{
			var zipFileTest = false;
			PrettyConsole.DoChore("[TEST] ZipFile.Test", () => {
				zipFileTest = new ZipFile(archiveStream).TestArchive(true, TestStrategy.FindAllErrors, (status, message) =>
				{
					if (status.Operation == TestOperation.EntryData) return;
					var pad = "";//.PadLeft(18 - status.Operation.ToString().Length);
					if (status.Entry is { } entry)
					{
						PrettyConsole.WriteLine($"[{status.Operation}{pad}] #{entry.ZipFileIndex} {entry.Name}");
					}
					else
					{
						PrettyConsole.WriteLine($"[{status.Operation}{pad}]");
					}

					if (!string.IsNullOrEmpty(message))
					{
						PrettyConsole.WriteLine($"[{status.Operation}{pad}] {message}");
					}
				});
			});

			var zisIterTest = false;
			zisIterTest = PrettyConsole.DoChore("[TEST] ZipInputStream.Iterate", () => {
				archiveStream.Seek(0, SeekOrigin.Begin);
				using(var zis = new ZipInputStream(archiveStream){IsStreamOwner = false}) {
					ZipEntry entry;
					var startEntry = 0l;
					var entryNum = 0;
					while ((entry = zis.GetNextEntry()) != null) {
						entryNum++;
						var startData = archiveStream.Position + (zis.inputBuffer.RawLength - zis.inputBuffer.Available);
						var entryName = entry.Name;

						// PrettyConsole.WriteLine($"- Entry #{entryNum,3} @ {startEntry,8} // {zis.inputBuffer.RawLength} of {zis.inputBuffer.Available}");

						// while(zis.ReadByte() >= 0) {
						// 	Console.Write(".");
						// }

						try {
							zis.CloseEntry();
						} catch {

							PrettyConsole.WriteLine($"Error position: {(archiveStream.CanRead ? archiveStream.Position : -1)} // {zis.inputBuffer.RawLength} of {zis.inputBuffer.Available}");
							throw;
						}
						var endEntry = archiveStream.Position + (zis.inputBuffer.RawLength - zis.inputBuffer.Available);
						var headSize = startData - startEntry;
						var dataSize = endEntry - startData;
						PrettyConsole.WriteLine($"- Entry #{entryNum,3} @ {startEntry,8}: {entryName} ({headSize} + {dataSize} byte(s))");
						startEntry = endEntry;
					}
				}
			}, continueOnFail: true);
			return zisIterTest && zipFileTest;
		}

		protected override void ParseArchive(Stream transportStream)
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
				PrettyConsole.WriteLine($"{transportStream.Position:x8} PK: {b1:x2} {b2:x2} {signId}");
				//}

				switch (sign)
				{
					case ZipConstants.LocalHeaderSignature:
						PrettyConsole.DoChore("Parse Local File Header", ()
							=> ParseLocalHeader(br), true);
						break;
					case ZipConstants.DataDescriptorSignature:
						PrettyConsole.DoChore("Parse Data Descriptor", ()
							=> ParseDataDescriptor(br), true);
						break;
					case ZipConstants.CentralHeaderSignature:
						PrettyConsole.DoChore("Parse Central Directory Header", ()
							=> ParseCentralHeader(br), true);
						break;
					case ZipConstants.Zip64CentralFileHeaderSignature:
						PrettyConsole.DoChore("Parse Zip64 End Of Central Directory Record", ()
							=> ParseZip64EndOfCentralDirectory(br), true);
						break;
					case ZipConstants.Zip64CentralDirLocatorSignature:
						PrettyConsole.DoChore("Parse Zip64 Central Directory Locator", ()
							=> ParseZip64CentralDirLocator(br), true);
						break;
					case ZipConstants.EndOfCentralDirectorySignature:
						PrettyConsole.DoChore("Parse End of Central Directory Record", ()
							=> ParseCentralDirectory(br), true);
						break;
					default:
						if (WarnUnknownSigns) PrettyConsole.WriteLine($"{"Skipping unrecognized signature!"}\n");
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

			PrettyConsole.WriteLine($"Extra data:");
			ParseExtraData(extraData);
		}

		private void ParseZip64CentralDirLocator(BinaryReader br)
		{
			var diskNumber = br.ReadUInt32();
			var relativeOffset = br.ReadUInt64();
			var diskTotal = br.ReadUInt64();

			PrettyConsole.WriteLine($"Zip64 End Record Disk Number: {diskNumber}");
			PrettyConsole.WriteLine($"Zip64 End Record Relative Offset: {relativeOffset} (0x{relativeOffset:x8})");
			PrettyConsole.WriteLine($"Total number of disks: {diskTotal}");
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

			PrettyConsole.WriteLine($"Size of record: {recordSize} (0x{recordSize:x16})");
			PrettyConsole.WriteLine($"Version Made By: {versionMadeBy} (0x{versionMadeBy.OSRaw:x2})");
			PrettyConsole.WriteLine($"Version Needed: {versionToExtract} (0x{versionToExtract.OSRaw:x2})");
			PrettyConsole.WriteLine($"Disk Number: {diskNumber}");
			PrettyConsole.WriteLine($"Start Central Directory Disk: {startCentralDirDisk:x8}");

			PrettyConsole.WriteLine($"Entries For Disk: {entriesForDisk}");
			PrettyConsole.WriteLine($"Entries For Central Directory: {entriesForWholeCentralDir}");
			PrettyConsole.WriteLine($"Central Directory Size: {centralDirSize} (0x{centralDirSize:x16})");
			PrettyConsole.WriteLine($"Central Directory Offset: {offsetOfCentralDir} (0x{offsetOfCentralDir:x16})");


			PrettyConsole.WriteLine($"Extensible data size: {extDataSize}");
			if (extDataSize <= 0) return;

			PrettyConsole.WriteLine($"Parsing of extensible data is not supported! Skipping.", ConCol.Yellow);
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


			PrettyConsole.WriteLine($"Version Made By: {versionMadeBy} (0x{versionMadeBy.OSRaw:x2})");
			PrettyConsole.WriteLine($"Version Needed: {versionToExtract} (0x{versionToExtract.OSRaw:x2})");
			PrettyConsole.WriteLine($"Bit flags: {bitFlags:F}");
			PrettyConsole.WriteLine($"Compression Method: {method:G}");
			PrettyConsole.WriteLine($"DOS Date: 0x{dosTime:x8}");
			PrettyConsole.WriteLine($"File Date: {dateTime:yyyy-MM-dd}");
			PrettyConsole.WriteLine($"File Time: {dateTime:HH:mm:ss}");
			PrettyConsole.WriteLine($"CRC: 0x{crc:x8}");
			WriteSize($"Compressed Size:", compressedSize);
			WriteSize($"Uncompressed Size:", size);
			PrettyConsole.WriteLine($"Name Length: {nameLen}");
			PrettyConsole.WriteLine($"Extra Data Length: {extraLen}");
			PrettyConsole.WriteLine($"Comment Length: {commentLen}");
			PrettyConsole.WriteLine($"Disk number start: {diskStartNo}");
			PrettyConsole.WriteLine($"Internal file attributes: {internalAttributes:x4}");
			PrettyConsole.WriteLine($"External file attributes: {externalAttributes:F}");
			PrettyConsole.WriteLine($"Relative offset of local header: {offset}");

			PrettyConsole.Write($"Name (raw): "); WriteHexBytes(nameData, MaxNameHex);
			PrettyConsole.Write($"Name: "); WriteString(nameData, MaxNameString);

			if (commentLen > 0)
			{
				PrettyConsole.Write($"Comment (raw): "); WriteHexBytes(commentData, MaxCommentHex);
				PrettyConsole.Write($"Comment: "); WriteString(commentData, MaxCommentString);
			}

			PrettyConsole.WriteLine($"Extra data:");
			ParseExtraData(extraData);

		}

		private static void WriteSize(FormattableString fs, in long size)
		{
			PrettyConsole.Write(fs);

			if (size == uint.MaxValue)
			{
				PrettyConsole.WriteLine($" {size} ({"Zip64 Indicator"})", ConCol.DarkGray, ConCol.Blue);
			}
			else
			{
				PrettyConsole.WriteLine($" {size}");
			}
		}

		private void ParseExtraData(ZipExtraData extraData)
		{
			PrettyConsole.PushGroup("extra-data");
			var extraDataBytes = extraData.GetEntryData();

			foreach (var (tag, range) in extraData.EnumerateTags().ToList())
			{
				var length = range.End.Value - range.Start.Value;

				var knownId = Enum.IsDefined(typeof(ExtraDataType), tag);

				PrettyConsole.WriteLine($" - Type: {(knownId ? tag : ExtraDataType.Unknown):G} ({(uint)tag:x4}), Length: {length}");
				PrettyConsole.PushGroup("extra-raw");
				if (extraDataBytes.Length < range.End.Value)
				{
					PrettyConsole.WriteLine($" Invalid length! Skipping parse attemmpt!", ConCol.Red);
					PrettyConsole.PopGroup(); // extra-raw
					PrettyConsole.PopGroup(); // extra-data
					return;
				}

				PrettyConsole.Write($" Raw: ");
				WriteHexBytes(extraDataBytes[range], MaxExtraDataHex);

				PrettyConsole.PopGroup();


				switch (tag)
				{
					case ExtraDataType.NTFS:
					{
						PrettyConsole.PushGroup("extra-ntfs");
						var tagNtDate = extraData.GetData<NTTaggedData>();

						PrettyConsole.WriteLine($" Created: {tagNtDate.CreateTime:yyyy-MM-dd HH:mm:ss}");
						PrettyConsole.WriteLine($" Accessed: {tagNtDate.LastAccessTime:yyyy-MM-dd HH:mm:ss}");
						PrettyConsole.WriteLine($" Modified: {tagNtDate.LastModificationTime:yyyy-MM-dd HH:mm:ss}");
						PrettyConsole.PopGroup();

						break;
					}
					case ExtraDataType.UnixExtendedTime:
					{
						PrettyConsole.PushGroup("extra-unix-xtime");

						var tagUnixDate = extraData.GetData<ExtendedUnixData>();

						PrettyConsole.WriteLine($" Created: {tagUnixDate.CreateTime:yyyy-MM-dd HH:mm:ss}");
						PrettyConsole.WriteLine($" Accessed: {tagUnixDate.AccessTime:yyyy-MM-dd HH:mm:ss}");
						PrettyConsole.WriteLine($" Modified: {tagUnixDate.ModificationTime:yyyy-MM-dd HH:mm:ss}");
						PrettyConsole.PopGroup();
						break;
					}
					case ExtraDataType.UnicodeName:
					{
						PrettyConsole.PushGroup("extra-unicode-name");

						var nameRange = new Range(range.Start.Value + 5, range.End.Value);
						var unicodeName = Encoding.UTF8.GetString(extraDataBytes[nameRange]);
						File.WriteAllText("unicodetext.txt", unicodeName);
						PrettyConsole.WriteLine($" Unicode name: {unicodeName}");
						PrettyConsole.PopGroup();
						break;
					}
					case ExtraDataType.Zip64:
					{
						PrettyConsole.PushGroup("extra-zip64");
						using (var br = new BinaryReader(extraData.GetStreamForTag((int)tag)))
						{
							if (length < 16)
							{
								PrettyConsole.WriteLine($" Extra data length too small!", ConCol.Red);
								PrettyConsole.WriteLine($" Zip64 should be {16} bytes, but {length} was indicated!");
							}
							
							if (length < 8) break; 
							var usize64 = br.ReadUInt64();
							PrettyConsole.WriteLine($" Uncompressed Size: {usize64} (0x{usize64:x16})");
							
							if (length < 16) break;
							var csize64 = br.ReadUInt64();
							PrettyConsole.WriteLine($" Compressed Size: {csize64} (0x{csize64:x16})");
						}

						PrettyConsole.PopGroup();
						break;
					}
				}
			}

			PrettyConsole.PopGroup();
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

			PrettyConsole.WriteLine($"Disk Number: {diskNumber:x4}");
			PrettyConsole.WriteLine($"Start Central Directory Disk: {startCentralDirDisk:x4}");
			PrettyConsole.WriteLine($"Entries For Central Directory: {entriesForWholeCentralDir}");
			PrettyConsole.WriteLine($"Entries For Disk: {entriesForDisk}");
			PrettyConsole.WriteLine($"Central Directory Size: {centralDirSize}");
			PrettyConsole.WriteLine($"Central Directory Offset: {offsetOfCentralDir:x8}");
			PrettyConsole.WriteLine($"Comment Size: {commentSize}");

			if (commentData.Length > 0)
			{
				PrettyConsole.Write($"Comment (raw): "); WriteHexBytes(commentData, MaxCommentHex);
				PrettyConsole.Write($"Comment: "); WriteString(commentData, MaxCommentString);
			}

			var zip64 = diskNumber == 0xffff
			            || startCentralDirDisk == 0xffff
			            || entriesForDisk == 0xffff
			            || entriesForWholeCentralDir == 0xffff
			            || centralDirSize == 0xffffffff
			            || offsetOfCentralDir == 0xffffffff;

			WriteBool("Zip64 Indication: ", zip64);
		}

		private static void ParseDataDescriptor(BinaryReader br)
		{
			var crc = br.ReadInt32();

			var pos = br.BaseStream.Position;
			var csize = br.ReadInt32();
			var usize = br.ReadInt32();

			br.BaseStream.Seek(-8, SeekOrigin.Current);

			// var crc64 = br.ReadUInt32();
			var csize64 = br.ReadInt64();
			var usize64 = br.ReadInt64();

			// Revert stream back to non-zip64 descriptor to not risk skipping next entry
			br.BaseStream.Seek(-8, SeekOrigin.Current);

			PrettyConsole.WriteLine($"CRC: 0x{crc:x8}");
			PrettyConsole.WriteLine($"32-bit sizes:");
			PrettyConsole.PushGroup("descriptor32");
			PrettyConsole.WriteLine($"Compressed Size: {csize} (0x{csize:x8})");
			PrettyConsole.WriteLine($"Uncompressed Size: {usize} (0x{usize:x8})");
			PrettyConsole.PopGroup();

			PrettyConsole.WriteLine($"64-bit sizes (Zip64):");
			PrettyConsole.PushGroup("descriptor64");
			PrettyConsole.WriteLine($"Compressed Size: {csize64} (0x{csize64:x16})");
			PrettyConsole.WriteLine($"Uncompressed Size: {usize64} (0x{usize64:x16})");
			// PrettyConsole.WriteLine($"CRC: 0x{crc64:x8}");
			PrettyConsole.PopGroup();

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


			PrettyConsole.WriteLine($"Version Needed: {extractVersion} (0x{extractVersion.OperatingSystem:x})");
			PrettyConsole.WriteLine($"Local Flags: {flags:F}");
			PrettyConsole.WriteLine($"Compression Method: {method:G}");
			PrettyConsole.WriteLine($"DOS Date: 0x{dosTime:x8}");
			PrettyConsole.WriteLine($"File Date: {dateTime:yyyy-MM-dd}");
			PrettyConsole.WriteLine($"File Time: {dateTime:HH:mm:ss}");
			PrettyConsole.WriteLine($"CRC: 0x{crc:x8}");
			WriteSize($"Compressed Size:", compressedSize);
			WriteSize($"Uncompressed Size:", size);
			PrettyConsole.WriteLine($"Name Length: {storedNameLength}");
			PrettyConsole.WriteLine($"Extra Data Length: {extraDataLength}");
			PrettyConsole.WriteLine($"Name (raw): "); WriteHexBytes(nameData, MaxNameHex);
			PrettyConsole.Write($"Name: "); WriteString(nameData, MaxNameString);
			PrettyConsole.WriteLine($"Extra data:");
			ParseExtraData(extraData);

		}


		// Utils

		
	}
}
