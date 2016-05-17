using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

class Cmd_ZipInfo
{
	static void ShowHelp()
	{
		Console.Error.WriteLine("Compress or uncompress FILEs (by default, compress FILES in-place).");
		Console.Error.WriteLine("Version {0} using SharpZipLib {1}",
			typeof(Cmd_ZipInfo).Assembly.GetName().Version,
			typeof(ZipFile).Assembly.GetName().Version);
		Console.Error.WriteLine("");
		Console.Error.WriteLine("Mandatory arguments to long options are mandatory for short options too.");
		Console.Error.WriteLine("");
		Console.Error.WriteLine("  -d, --decompress  decompress");
		Console.Error.WriteLine("  -h, --help        give this help");
		Console.Error.WriteLine("  -z, --compress    compress");
		Console.Error.WriteLine("  -1, --fast        compress faster");
		Console.Error.WriteLine("  -9, --best        compress better");
	}

	#region Command parsing
	enum Command
	{
		Nothing,
		Help,
		Compress,
		Decompress,
		Stop,
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
					case "-d":
					case "--decompress":
						SetCommand(Command.Decompress);
						break;
					case "-c": // for backwards compatibility
					case "-z":
					case "--compress":
						SetCommand(Command.Compress);
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
					command_ = Command.Compress;
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
		public Command Command {
			get { return command_; }
		}

		public int Level {
			get { return level_; }
		}

		#region Instance Fields
		Command command_ = Command.Nothing;
		string file_;
		int level_;
		#endregion
	}
	#endregion


	public static int Main(string[] args)
	{
		if (args.Length == 0) {
			ShowHelp();
			return 1;
		}

		if (!File.Exists(args[0])) {
			Console.Error.WriteLine("Cannot find file {0}", args[0]);
			ShowHelp();
			return 1;
		}

		var parser = new ArgumentParser(args);

		using (ZipFile zFile = new ZipFile(args[0])) {
			Console.WriteLine("Listing of : " + zFile.Name);
			Console.WriteLine("");
			if (false) {
				Console.WriteLine("Raw Size    Size       Date       Time     Name");
				Console.WriteLine("--------  --------  -----------  ------  ---------");
				foreach (ZipEntry e in zFile) {
					DateTime d = e.DateTime;
					Console.WriteLine("{0, -10}{1, -10}{2}  {3}   {4}", e.Size, e.CompressedSize,
																		d.ToString("dd MMM yyyy"), d.ToString("HH:mm"),
																		e.Name);
				}
			} else {
				Console.WriteLine("Raw Size,Size,Date,Time,Name");
				foreach (ZipEntry e in zFile) {
					DateTime d = e.DateTime;
					Console.WriteLine("{0, -10}{1, -10}{2}  {3}   {4}", e.Size, e.CompressedSize,
																		d.ToString("dd MMM yyyy"), d.ToString("HH:mm"),
																		e.Name);
				}

			}
		}

		return 0;
	}
}
