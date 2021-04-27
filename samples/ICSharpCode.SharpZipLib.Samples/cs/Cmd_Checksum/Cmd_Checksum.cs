using System;
using System.IO;
using ICSharpCode.SharpZipLib.Checksum;

class Cmd_Checksum
{
	static void ShowHelp()
	{
		Console.Error.WriteLine("Compress or uncompress FILEs (by default, compress FILES in-place).");
		Console.Error.WriteLine("Version {0} using SharpZipLib {1}",
			typeof(Cmd_Checksum).Assembly.GetName().Version,
			typeof(IChecksum).Assembly.GetName().Version);
		Console.Error.WriteLine("");
		Console.Error.WriteLine("Mandatory arguments to long options are mandatory for short options too.");
		Console.Error.WriteLine("");
		Console.Error.WriteLine("  -a, --adler       decompress");
		Console.Error.WriteLine("  -b, --bzip2       give this help");
		Console.Error.WriteLine("  -c, --crc32       compress");
		Console.Error.WriteLine("  -1, --fast        compress faster");
		Console.Error.WriteLine("  -9, --best        compress better");
	}

	#region Instance Fields
	private static Command command_ = Command.Nothing;
	private static string file_;
	#endregion

	#region Command parsing
	enum Command
	{
		Nothing,
		Help,
		Adler,
		BZip2,
		Crc32,
		Stop
	}

	class ArgumentParser
	{
		public ArgumentParser(string[] args)
		{
			foreach (string argument in args) {
				switch (argument) {
					case "-?": // for backwards compatibility
					case "-h":
					case "--help":
						SetCommand(Command.Help);
						break;
					case "--adler32":
						SetCommand(Command.Adler);
						break;
					case "--bzip2":
						SetCommand(Command.BZip2);
						break;
					case "--crc32":
						SetCommand(Command.Crc32);
						break;
					default:
						if (argument[0] == '-') {
							Console.Error.WriteLine("Unknown argument {0}", argument);
							command_ = Command.Stop;
						} else if (file_ == null) {
							file_ = argument;

							if (!System.IO.File.Exists(file_)) {
								Console.Error.WriteLine("File not found '{0}'", file_);
								command_ = Command.Stop;
							}
						} else {
							Console.Error.WriteLine("File has already been specified");
							command_ = Command.Stop;
						}
						break;
				}
			}

			if (command_ == Command.Nothing) {
				if (file_ == null) {
					command_ = Command.Help;
				} else {
					command_ = Command.Crc32;
				}
			}
		}

		void SetCommand(Command command)
		{
			if ((command_ != Command.Nothing) && (command_ != Command.Stop)) {
				Console.Error.WriteLine("Command already specified");
				command_ = Command.Stop;
			} else {
				command_ = command;
			}
		}

		public string Source {
			get { return file_; }
		}

		public Command Command {
			get { return command_; }
		}
	}
	#endregion

	public static int Main(string[] args)
	{
		if (args.Length == 0) {
			ShowHelp();
			return 1;
		}

		var parser = new ArgumentParser(args);

		if (!File.Exists(file_)) {
			Console.Error.WriteLine("Cannot find file {0}", file_);
			ShowHelp();
			return 1;
		}

		using (FileStream checksumStream = File.OpenRead(file_)) {

			byte[] buffer = new byte[4096];
			int bytesRead;

			switch (parser.Command) {
				case Command.Help:
					ShowHelp();
					break;

				case Command.Crc32:
					var currentCrc = new Crc32();
					while ((bytesRead = checksumStream.Read(buffer, 0, buffer.Length)) > 0) {
						currentCrc.Update(new ArraySegment<byte>(buffer, 0, bytesRead));
					}
					Console.WriteLine("CRC32 for {0} is 0x{1:X8}", args[0], currentCrc.Value);
					break;

				case Command.BZip2:
					var currentBZip2Crc = new BZip2Crc();
					while ((bytesRead = checksumStream.Read(buffer, 0, buffer.Length)) > 0) {
						currentBZip2Crc.Update(new ArraySegment<byte>(buffer, 0, bytesRead));
					}
					Console.WriteLine("BZip2CRC32 for {0} is 0x{1:X8}", args[0], currentBZip2Crc.Value);
					break;

				case Command.Adler:
					var currentAdler = new Adler32();
					while ((bytesRead = checksumStream.Read(buffer, 0, buffer.Length)) > 0) {
						currentAdler.Update(new ArraySegment<byte>(buffer, 0, bytesRead));
					}
					Console.WriteLine("Adler32 for {0} is 0x{1:X8}", args[0], currentAdler.Value);
					break;
			}
		}
		return 0;
	}
}

