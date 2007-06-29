// SharpZipLibrary samples
// Copyright (c) 2007, AlphaSierraPapa
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are
// permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, this list
//   of conditions and the following disclaimer.
//
// - Redistributions in binary form must reproduce the above copyright notice, this list
//   of conditions and the following disclaimer in the documentation and/or other materials
//   provided with the distribution.
//
// - Neither the name of the SharpDevelop team nor the names of its contributors may be used to
//   endorse or promote products derived from this software without specific prior written
//   permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS &AS IS& AND ANY EXPRESS
// OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER
// IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT
// OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.IO;

using ICSharpCode.SharpZipLib.BZip2;

class MainClass
{
	static void ShowHelp()
	{
		Console.WriteLine("Usage: MiniBzip [options] filename");
		Console.WriteLine("");
		Console.WriteLine("Options:");
		Console.WriteLine("  -d decompress");
		Console.WriteLine("  -c compress (default)");
		Console.WriteLine("  -? Show help");
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
						Console.WriteLine("File has already been specified");
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
				Console.WriteLine("Command already specified");
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
					result = file_ + ".bz";
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

		ArgumentParser parser = new ArgumentParser(args);

		switch ( parser.Command ) {
			case Command.Help:
				ShowHelp();
				break;
				
			case Command.Compress:
				Console.WriteLine("Compressing {0} to {1}", parser.Source, parser.Target);
				BZip2.Compress(File.OpenRead(parser.Source), File.Create(parser.Target), 4096);
				break;
				
			case Command.Decompress:
				Console.WriteLine("Decompressing {0} to {1}", parser.Source, parser.Target);
				BZip2.Decompress(File.OpenRead(parser.Source), File.Create(parser.Target));
				break;
		}
	}
}
