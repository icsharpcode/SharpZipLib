// project created on 11.11.2001 at 15:19
using System;
using System.IO;

using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;

class MainClass
{
	static void ShowHelp()
	{
		Console.WriteLine("Usage: MiniGzip [options] filename");
		Console.WriteLine("");
		Console.WriteLine("Options:");
		Console.WriteLine("  -d decompress");
		Console.WriteLine("  -c compress (default)");
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
			foreach ( string argument in args )
			{
				if ( argument == "-d" ) {
					SetCommand(Command.Decompress);
				}
				else if ( argument == "-c" ) {
					SetCommand(Command.Compress);
				}
				else if ( argument == "-?" ) {
					SetCommand(Command.Help);
				}
				else if ( argument[0] == '-' ) {
					Console.WriteLine("Unknown argument {0}", argument);
					command_ = Command.Stop;
				}
				else
				{
					if ( file_ == null ) {
						file_ = argument;
						
						if ( !System.IO.File.Exists(file_) ) {
							Console.WriteLine("File not found '{0}'", file_);
							command_ = Command.Stop;
						}
					}
					else {
						Console.WriteLine("Too many arguments");
						command_ = Command.Stop;
					}
				}
			}
			
			if ( command_ == Command.Nothing ) {
				if ( file_ == null ) {
					command_ = Command.Help;
				}
				else {
					command_ = Command.Compress;
				}
			}
		}
		
		void SetCommand(Command command)
		{
			if ( (command_ != Command.Nothing) && (command_ != Command.Stop) ) {
				Console.WriteLine("Too many options");
				command_ = Command.Stop;
			}
			else {
				command_ = command;
			}
		}
		
		public string Source
		{
			get { return file_; }
		}
		
		public string Target
		{
			get {
				string result;
				if ( command_ == Command.Compress ) {
					result = file_ + ".gz";
				}
				else {
					result = Path.GetFileNameWithoutExtension(file_);
				}
				return result;
			}
		}
	
		public Command Command
		{
			get { return command_; }
		}
		
		#region Instance Fields
		Command command_ = Command.Nothing;
		string file_;
		#endregion
	}
	#endregion
	
	
	public static void Main(string[] args)
	{
		byte[] dataBuffer = new byte[4096];
		
		ArgumentParser parser = new ArgumentParser(args);
		
		switch ( parser.Command ) {
			case Command.Compress:
				Console.WriteLine("Compressing {0} to {1}", parser.Source, parser.Target);
				using (Stream s = new GZipOutputStream(File.Create(args[0] + ".gz")))
				using (FileStream fs = File.OpenRead(args[0])) {
					StreamUtils.Copy(fs, s, dataBuffer);
				}
				break;
				
			case Command.Decompress:
				Console.WriteLine("Decompressing {0} to {1}", parser.Source, parser.Target);
				using (Stream s = new GZipInputStream(File.OpenRead(args[1])))
				using (FileStream fs = File.Create(Path.GetFileNameWithoutExtension(args[1]))) {
					StreamUtils.Copy(s, fs, dataBuffer);
				}
				break;
				
			case Command.Help:
				ShowHelp();
				break;
		}
	}
}
