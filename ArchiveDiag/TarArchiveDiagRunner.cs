using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using ArchiveDiag.Tar;
using ConLib;
using static ConLib.PrettyConsole;

namespace ArchiveDiag
{
	class TarArchiveDiagRunner: ArchiveDiagRunner
	{
		protected override string Parser => "ArchiveDiag.TarParser";

		public TarArchiveDiagRunner(Stream archiveStream, string? fileName = null) : base(archiveStream, fileName)
		{
		}

		protected override bool TestArchive(Stream archiveStream)
		{
			return true;
		}

		protected override void ParseArchive(Stream archiveStream)
		{
			var block = new byte[512];
			while(archiveStream.Length > archiveStream.Position)
			{
				var readCount = 0;

				DoTask("Read Block", async () => readCount = await archiveStream.ReadAsync(block));

				var isEndBlock = block.All(b => b == 0);
				WriteBool("Block is end block? ", isEndBlock);

				if(isEndBlock) continue;

				long fileSize = 0;
				DoChore("Parse entry header", () => fileSize = ParseHeader(block), true);

				DoChore("Parse UStar header", () => ParseUstar(block), true);

				if (fileSize >= 0)
				{
					DoTask("Skip Content", async () => archiveStream.Seek(fileSize, SeekOrigin.Current));
				}
				else
				{
					throw new Exception("Cannot seek to next entry, since the previous entry didn't contain a valid size");
				}
			} 
		}

		private long ParseHeader(ReadOnlySpan<byte> block)
		{

			DumpStr("File name: ", block[0..100]);
			// DumpHex("File name (raw): ", block[0..100]);
			DumpDec("File mode: ", block[100..108]);
			DumpDec("Owner ID: ", block[108..116]);
			DumpDec("Group ID: ", block[116..124]);

			var fileSize = DumpDec("File size: ", block[124..136]);

			DumpTim("Modification time: ", block[136..148]); 

			DumpStr("Checksum: ", block[148..156]);

			var typeflag = block[156];
			var identifiedFlag = Enum.IsDefined(typeof(TypeFlag), typeflag) ? ((TypeFlag)typeflag).ToString("F") : "unknown";

			WriteLine($"File type: {identifiedFlag} (0x{typeflag} '{(char)typeflag}')");
			DumpStr("Linked file name: ", block[157..257]); 

			return fileSize;
		}

		private void ParseUstar(ReadOnlySpan<byte> block)
		{
			var magic = ReadStr(block.Slice(257, 6));

			WriteLine($"UStar indicator: {magic}");

			if (!magic.StartsWith("ustar"))
			{
				WriteLine($"UStar indicator not found, skipping!");
				return;
			}

			DumpDec("Version: ", block[263..265]);
			DumpStr("Owner user name: ", block[265..297]);
			DumpStr("Owner group name: ", block[297..329]);
			DumpDec($"Device major: ", block[329..337]);
			DumpDec($"Device minor: ", block[337..345]);
			DumpStr($"Filename prefix: ", block[345..500]);


		}

		private string ReadStr(ReadOnlySpan<byte> span) => Encoding.ASCII.GetString(span).Trim('\0', ' ');

		private void DumpStr(string prefix, ReadOnlySpan<byte> span)
		{
			Write(FormattableStringFactory.Create(prefix));
			WriteString(span.ToArray().Where(b => b != 0).ToArray(), 48);
		}

		private void DumpHex(string prefix, ReadOnlySpan<byte> span)
		{
			Write(FormattableStringFactory.Create(prefix));
			WriteHexBytes(span.ToArray(), 100);
		}

		private long DumpDec(string prefix, ReadOnlySpan<byte> span)
		{
			var dec = -1L;

			Write(FormattableStringFactory.Create(prefix));

			var str = ReadStr(span);
			if (string.IsNullOrWhiteSpace(str))
			{
				WriteColor("<empty>\n", ConCol.DarkGray);
				return -1;
			}
			try
			{
				dec = Convert.ToInt64(str, 8);
				WriteLine($"{dec}");
			}
			catch (Exception x)
			{
				WriteColor($"Invalid! ", ConCol.Red);
				WriteHexBytes(span.ToArray(), 16);
				WriteColor($" {x.GetType().Name}: {x.Message}\n", ConCol.DarkRed);
				return -1;
			}

			return dec;
		}

		private DateTimeOffset DumpTim(string prefix, ReadOnlySpan<byte> span)
		{
			var dto = DateTimeOffset.MinValue;

			Write(FormattableStringFactory.Create(prefix));

			var str = ReadStr(span);
			if (string.IsNullOrWhiteSpace(str))
			{
				WriteColor("<empty>\n", ConCol.DarkGray);
				return dto;
			}
			try
			{
				dto = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(str, 8));

				WriteLine($"{dto:s}");
			}
			catch (Exception x)
			{
				WriteColor($"Invalid! ", ConCol.Red);
				WriteHexBytes(span.ToArray(), 16);
				WriteColor($" {x.GetType().Name}: {x.Message}\n", ConCol.DarkRed);
			}

			return dto;
		}



	}
}
