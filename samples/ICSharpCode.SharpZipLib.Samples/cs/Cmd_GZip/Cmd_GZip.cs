using System;
using System.IO;
using ICSharpCode.SharpZipLib.GZip;

class Cmd_GZip
{
	static void ShowHelp()
	{
		Console.Error.WriteLine("Compress or uncompress FILEs (by default, compress FILES in-place).");
		Console.Error.WriteLine("Version {0} using SharpZipLib {1}",
			typeof(Cmd_GZip).Assembly.GetName().Version,
			typeof(GZip).Assembly.GetName().Version);
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
					case "-1":
					case "-2":
					case "-3":
					case "-4":
					case "-5":
					case "-6":
					case "-7":
					case "-8":
					case "-9":
						SetLevel((int)argument[1] - 48);
						break;
					case "--fast":
						SetLevel(1);
						break;
					case "--best":
						SetLevel(9);
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

		void SetLevel(int level)
		{
			if (level_ != 0) {
				Console.Error.WriteLine("Level already specified");
				level_ = 0;
			} else {
				level_ = level;
			}
		}

		public string Source {
			get { return file_; }
		}

		public string Target {
			get {
				string result;
				if (command_ == Command.Compress) {
					result = file_ + ".gz";
				} else {
					result = Path.GetFileNameWithoutExtension(file_);
				}
				return result;
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

		var parser = new ArgumentParser(args);

		switch (parser.Command) {
			case Command.Help:
				ShowHelp();
				break;

			case Command.Compress:
				Console.WriteLine("Compressing {0} to {1} at level {2}", parser.Source, parser.Target, parser.Level);
				GZip.Compress(File.OpenRead(parser.Source), File.Create(parser.Target), true, parser.Level);
				break;

			case Command.Decompress:
				Console.WriteLine("Decompressing {0} to {1}", parser.Source, parser.Target);
				GZip.Decompress(File.OpenRead(parser.Source), File.Create(parser.Target), true);
				break;
		}

		return 0;
	}
}
