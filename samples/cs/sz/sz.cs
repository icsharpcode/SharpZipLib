//------------------------------------------------------------------------------
//
// sz - A command line archiver using SharpZipLib for actual compression
//      Currently only creates Zip archives.
//
// Copyright 2004 John Reilly
//
//------------------------------------------------------------------------------

// Define test to add more detailed Console.WriteLine style information
// #define TEST

// Define this to get deeper detail on option handling
// #define OPTIONTEST

namespace SharpZip {

	using System;
	using System.IO;
	using System.Collections;
	using System.Text;
	using System.Globalization;
	using System.Diagnostics;
	using System.Reflection;

	using ICSharpCode.SharpZipLib.Zip;	
	using ICSharpCode.SharpZipLib.Zip.Compression;
	using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
	
	/// <summary>
	/// A command line archiver using the SharpZipLib compression library
	/// </summary>
	public class SharpZipArchiver {
		
		enum Overwrite {
			Prompt,
			Never,
			Always
		}
		
		/// <summary>
		/// Base constructor - initializes all fields to default values
		/// </summary>
		public SharpZipArchiver()
		{
		}

		/// <summary>
		/// Has user already seen help output?
		/// </summary>
		bool seenHelp;
		
		/// <summary>
		/// File specification possibly with wildcards from command line
		/// </summary>
		ArrayList fileSpecs = new ArrayList();
		
		/// <summary>
		/// Deflate compression level
		/// </summary>
		int    compressionLevel = Deflater.DEFAULT_COMPRESSION;
		
		/// <summary>
		/// Create entries for directories with no files
		/// </summary>
		bool   addEmptyDirectoryEntries;
		
		/// <summary>
		/// Apply operations recursively
		/// </summary>
		bool   recursive;

		/// <summary>
		/// Use ZipFile class for listing entries
		/// </summary>
		bool useZipFileWhenListing;
		
		/// <summary>
		/// Use relative path information
		/// </summary>
		bool   relativePathInfo;
		
		/// <summary>
		/// Operate silently
		/// </summary>
		bool   silent;
		
		/// <summary>
		/// Use store rather than deflate when adding files, not likely to be used much
		/// but it does exercise the option as the library supports it
		/// </summary>
		bool   useZipStored;

		/// <summary>
		/// Restore file date and time to that stored in zip file on extraction
		/// </summary>
		bool restoreDateTime;
		
		/// <summary>
		/// Overwrite files handling
		/// </summary>
		Overwrite   overwriteFiles = Overwrite.Prompt;

		/// <summary>
		/// Optional password for archive
		/// </summary>
		string password;
		
		/// <summary>
		/// prefix to remove when creating relative path names
		/// </summary>
		string removablePathPrefix;
		
		/// <summary>
		/// Where things will go
		/// </summary>
		string targetOutputDirectory;

		// TODO full CRUD really required for Zip files.

		/// <summary>
		/// Kinds of thing we know how to do, full CRUD would be great!!
		/// </summary>
		enum Operation {
			Create,     // add files to new archive
			Extract,    // extract files from existing archive
			List        // show contents of existing archive
		}
		
		/// <summary>
		/// What to do based on parsed command line arguments
		/// </summary>
		Operation operation = Operation.List;

		/// <summary>
		/// Determine if string is numeric [0-9]+
		/// </summary>
		/// <param name="rhs">string to test</param>
		/// <returns>true iff rhs is numeric</returns>
		bool IsNumeric(string rhs)
		{
			bool result;
			if (rhs != null && rhs.Length > 0) {
				result = true;
				for (int i = 0; i < rhs.Length; ++i) {
					if (char.IsDigit(rhs[i]) == false) {
						result = false;
						break;
					}
				}
			} else {
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Parse command line arguments.
		/// This is fairly flexible without using any custom classes.  Arguments and options can appear
		/// in any order and are case insensitive.  Arguments for options are signalled with an '='
		/// as in -demo=argument, sometimes the '=' can be omitted as well secretly.
		/// Grouping of single character options is supported.
		/// 
		/// The actual arguments and their handling is however a grab bag of ad-hoc things and its a bit messy.  Could be a 
		/// bit more rigorous about how things are done.  Up side is almost anything is/can be allowed
		/// </summary>		
		/// <returns>
		/// true if arguments are valid such that processing should continue
		/// </returns>
		bool SetArgs(string[] args) {
			bool result = true;
			int argIndex = 0;
			
			while (argIndex < args.Length) {
				if (args[argIndex][0] == '-' || args[argIndex][0] == '/') {
					
					string option = args[argIndex].Substring(1).ToLower();
					string optArg = "";
	
					int parameterIndex = option.IndexOf('=');
	
					if (parameterIndex >= 0)
					{
						if (parameterIndex < option.Length - 1) {
							optArg = option.Substring(parameterIndex + 1);
						}
						option = option.Substring(0, parameterIndex);
					}

#if OPTIONTEST
					Console.WriteLine("args index [{0}] option [{1}] argument [{2}]", argIndex, option, optArg);
#endif				
					if (option.Length == 0) {
						System.Console.Error.WriteLine("Invalid argument (0}", args[argIndex]);
						result = false;
					}
					else {
						int optionIndex = 0;
						while (optionIndex < option.Length) {
#if OPTIONTEST
							Console.WriteLine("optionIndex {0}", optionIndex);
#endif				
							switch(option[optionIndex]) {
								case '-': // long option
									optionIndex = option.Length;
									
									switch (option) {
										case "-create":
											operation = Operation.Create;
											break;
										
										case "-list":
											operation = Operation.List;
											useZipFileWhenListing = true;
											break;
	
										case "-extract":
											operation = Operation.Extract;
											if (optArg.Length > 0) {
												targetOutputDirectory = optArg;
											}
											break;
	
										case "-info":
											ShowEnvironment();
											break;
										
										case "-emptydirs":
											addEmptyDirectoryEntries = true;
											break;
	
										case "-extractdir":
											if (optArg.Length > 0) {
												targetOutputDirectory = optArg;
											} else {
												result = false;
												System.Console.Error.WriteLine("Invalid extractdir " + args[argIndex]);
											}
											break;
										
										case "-encoding":
											if (optArg.Length > 0) {
												if (IsNumeric(optArg)) {
													try {
														int enc = int.Parse(optArg);
														if (Encoding.GetEncoding(enc) != null) {
#if OPTIONTEST
															Console.WriteLine("Encoding set to {0}", enc);
#endif				
															ZipConstants.DefaultCodePage = enc;
														} else {
															result = false;
															System.Console.Error.WriteLine("Invalid encoding " + args[argIndex]);
														}
													}
													catch (Exception) {
														result = false;
														System.Console.Error.WriteLine("Invalid encoding " + args[argIndex]);
													}
												} else {
													try {
														ZipConstants.DefaultCodePage = Encoding.GetEncoding(optArg).CodePage;
													}
													catch (Exception) {
														result = false;
														System.Console.Error.WriteLine("Invalid encoding " + args[argIndex]);
													}
												}
											} else {
												result = false;
												System.Console.Error.WriteLine("Missing encoding parameter");
											}
											break;
										
										case "-store":
											useZipStored = true;
											break;
										
										case "-deflate":
											useZipStored = false;
											break;
										
										case "-version":
											ShowVersion();
											break;
										
										case "-help":
											ShowHelp();
											break;
			
										case "-restore-dates":
											restoreDateTime = true;
											break;
										
										default:
											System.Console.Error.WriteLine("Invalid long argument " + args[argIndex]);
											result = false;
											break;
									}
									break;
								
								case '?':
									ShowHelp();
									break;
								
								case 's':
									if (optionIndex != 0) {
										result = false;
										System.Console.Error.WriteLine("-s cannot be in a group");
									} else {
										if (optArg.Length > 0) {
											password = optArg;
										} else if (option.Length > 1) {
											password = option.Substring(1);
										} else {
											System.Console.Error.WriteLine("Missing argument to " + args[argIndex]);
										}
									}
									optionIndex = option.Length;
									break;
	
								case 'c':
									operation = Operation.Create;
									break;
								
								case 'e':
									if (optionIndex != 0) {
										result = false;
										System.Console.Error.WriteLine("-e cannot be in a group");
									} else {
										optionIndex = option.Length;
										if (optArg.Length > 0) {
											try {
												compressionLevel = int.Parse(optArg);
											}
											catch (Exception) {
												System.Console.Error.WriteLine("Level invalid");
											}
										}
									}
									optionIndex = option.Length;
									break;
								
								case 'o':
									optionIndex += 1;
									overwriteFiles = optionIndex < option.Length ? (option[optionIndex] == '+') ? Overwrite.Always : Overwrite.Never : Overwrite.Never;
									break;
								
								case 'p':
									relativePathInfo = true;
									break;
								
								case 'q':
									silent = true;
									if (overwriteFiles == Overwrite.Prompt) {
										overwriteFiles = Overwrite.Never;
									}
									break;
	
								case 'r':
									recursive = true;
									break;
	
								case 'v':
									operation = Operation.List;
									break;
	
								case 'x':
									if (optionIndex != 0) {
										result = false;
										System.Console.Error.WriteLine("-x cannot be in a group");
									} else {
										operation = Operation.Extract;
										if (optArg.Length > 0) {
											targetOutputDirectory = optArg;
										}
									}
									optionIndex = option.Length;
									break;
								
								default:
									System.Console.Error.WriteLine("Invalid argument: " + args[argIndex]);
									result = false;
									break;
							}
							++optionIndex;
						}
					}
				}
				else {
#if OPTIONTEST
					Console.WriteLine("file spec {0} = '{1}'", argIndex, args[argIndex]);
#endif				
					fileSpecs.Add(args[argIndex]);
				}
				++argIndex;
			}
			
			if (fileSpecs.Count > 0 && operation == Operation.Create) {
				string checkPath = (string)fileSpecs[0];
				int deviceCheck = checkPath.IndexOf(':');
				if (checkPath.IndexOfAny(Path.InvalidPathChars) >= 0
				    || checkPath.IndexOf('*') >= 0 || checkPath.IndexOf('?') >= 0
				    || (deviceCheck >= 0 && deviceCheck != 1)) {
					Console.WriteLine("There are invalid characters in the specified zip file name");
					result = false;					
				}
			}
			return result && fileSpecs.Count > 0;
		}

		/// <summary>
		/// Show encoding/locale information
		/// </summary>
		void ShowEnvironment()
		{
			seenHelp = true;
			System.Console.Out.WriteLine(
			   "Current encoding is {0}, code page {1}, windows code page {2}",
			   System.Console.Out.Encoding.EncodingName,
			   System.Console.Out.Encoding.CodePage,
			   System.Console.Out.Encoding.WindowsCodePage);

			System.Console.WriteLine("Default code page is {0}",
			   Encoding.Default.CodePage);
			
			Console.WriteLine( "Current culture LCID 0x{0:X}, {1}", CultureInfo.CurrentCulture.LCID, CultureInfo.CurrentCulture.EnglishName);
			Console.WriteLine( "Current thread OEM codepage {0}", System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.OEMCodePage);
			Console.WriteLine( "Current thread Mac codepage {0}", System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.MacCodePage);
			Console.WriteLine( "Current thread Ansi codepage {0}", System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ANSICodePage);
		}
		
		/// <summary>
		/// Display version information
		/// </summary>		
		void ShowVersion() {
			seenHelp = true;
			Console.Out.WriteLine("SharpZip Archiver v0.34   Copyright 2004 John Reilly");
			
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

			foreach (Assembly assembly in assemblies) {
				if (assembly.GetName().Name == "ICSharpCode.SharpZipLib") {
					Console.Out.WriteLine("#ZipLib v{0} {1}", assembly.GetName().Version,
						assembly.GlobalAssemblyCache == true ? "Running from GAC" : "Running from DLL"
					);
				}
			}
		}

		/// <summary>
		/// Show help on possible options and arguments
		/// </summary>
		void ShowHelp()
		{
			if (seenHelp == true) {
				return;
			}
			
			seenHelp = true;
			ShowVersion();
			Console.Out.WriteLine("usage sz {options} archive files");
			Console.Out.WriteLine("");
			Console.Out.WriteLine("Options:");
			Console.Out.WriteLine("-?,        --help          Show this help");
			Console.Out.WriteLine("-c         --create        Create new archive");
			Console.Out.WriteLine("-v                         List archive contents (default)");
			Console.Out.WriteLine("--list                     List archive contents extended format");
			Console.Out.WriteLine("-x{=dir},  --extract{=dir} Extract archive contents to dir(default .)");
			Console.Out.WriteLine("--extractdir=path          Set extract directory (default .)");
			Console.Out.WriteLine("--info                     Show current environment information" );
			Console.Out.WriteLine("--store                    Store entries (default=deflate)");
			Console.Out.WriteLine("--version                  Show version information");
			Console.Out.WriteLine("--emptydirs                Create entries for empty directories");
			Console.Out.WriteLine("--encoding=codepage|name   Set code page for encoding by name or number");
			Console.Out.WriteLine("--restore-dates            Restore dates on extraction");
			Console.Out.WriteLine("-o+                        Overwrite files without prompting");
			Console.Out.WriteLine("-o-                        Never overwrite files");
			Console.Out.WriteLine("-p                         Store relative path info");
			Console.Out.WriteLine("-r                         Recurse sub-folders");
			Console.Out.WriteLine("-q                         Quiet mode");
			Console.Out.WriteLine("-s=password                Set archive password");
			Console.Out.WriteLine("");
		
		}
		
		///<summary>
		/// Calculate compression ratio as a percentage
		/// Doesnt allow for expansion (ratio > 100) as the resulting strings can get huge easily
		/// </summary>
		int GetCompressionRatio(long packedSize, long unpackedSize)
		{
			int result = 0;
			if (unpackedSize > 0 && unpackedSize >= packedSize) {
				result = (int) Math.Round((1.0 - ((double)packedSize / (double)unpackedSize)) * 100.0);
			}
			return result;
		}

		/// <summary>
		/// List zip file contents using stream
		/// </summary>
		/// <param name="fileName">File to list contents of</param>
		void ListZip(string fileName) {
			try
			{
				// TODO for asian/non-latin/non-proportional fonts string lengths dont work so output wont line up
				
				const string headerTitles    = "Name                 Length Ratio Size         Date & time       CRC-32";
				const string headerUnderline = "---------------  ---------- ----- ---------- ------------------- --------";
				
				FileInfo fileInfo = new FileInfo(fileName);
				
				if (fileInfo.Exists == false) {
					Console.Error.WriteLine("No such file exists {0}", fileName);
					return;
				}

				Console.Out.WriteLine(fileName);

				using (FileStream fileStream = File.OpenRead(fileName)) {
					using (ZipInputStream stream = new ZipInputStream(fileStream)) {
						if (password != null && password.Length > 0)
							stream.Password = password;

						int entryCount = 0;
						long totalSize  = 0;
						
						ZipEntry theEntry;
						
						while ((theEntry = stream.GetNextEntry()) != null) {
							if (entryCount == 0) {
								Console.Out.WriteLine(headerTitles);
								Console.Out.WriteLine(headerUnderline);
							}
						
							++entryCount;
							int ratio = GetCompressionRatio(theEntry.CompressedSize, theEntry.Size);
							totalSize += theEntry.Size;
							
							if (theEntry.Name.Length > 15) {
								Console.Out.WriteLine(theEntry.Name);
								Console.Out.WriteLine(
								    "{0,-15}  {1,10:0}  {2,3}% {3,10:0} {4,10:d} {4:hh:mm:ss} {5,8:x}",
								    "", theEntry.Size, ratio, theEntry.CompressedSize, theEntry.DateTime, theEntry.Crc);
							} else {
								Console.Out.WriteLine(
								    "{0,-15}  {1,10:0}  {2,3}% {3,10:0} {4,10:d} {4:hh:mm:ss} {5,8:x}",
								    theEntry.Name, theEntry.Size, ratio, theEntry.CompressedSize, theEntry.DateTime, theEntry.Crc);
							}
						}
			
						if (entryCount == 0) {
							Console.Out.WriteLine("Archive is empty!");
						} else {
							Console.Out.WriteLine(headerUnderline);
							Console.Out.WriteLine(
								"{0,-15}  {1,10:0}  {2,3}% {3,10:0} {4,10:d} {4:hh:mm:ss}",
								entryCount.ToString() + " entries", totalSize, GetCompressionRatio(fileInfo.Length, totalSize), fileInfo.Length, fileInfo.LastWriteTime);
						}
					}
				}
			}
			catch(Exception exception)
			{
				Console.Error.WriteLine("Exception during list operation: {0}", exception.Message);
			}
		}
		
		string InterpretExternalAttributes(int os, int attributes)
		{
			string result = "";
			if (os == 0)
			{
				if ((attributes & 0x10) != 0)
					result = result + "D";
				else
					result = result + "-";

				if ((attributes & 0x08) != 0)
					result = result + "V";
				else
					result = result + "-";

				if ((attributes & 0x01) != 0)
					result = result + "r";
				else
					result = result + "-";

				if ((attributes & 0x20) != 0)
					result = result + "a";
				else
					result = result + "-";

				if ((attributes & 0x04) != 0)
					result = result + "s";
				else
					result = result + "-";

				if ((attributes & 0x02) != 0)
					result = result + "h";
				else
					result = result + "-";
			}
			return result;
		}

		/// <summary>
		/// List zip file contents using ZipFile class
		/// </summary>
		/// <param name="fileName">File to list contents of</param>
		void ListZipViaZipFile(string fileName) {
			try
			{
				const string headerTitles    = "Name              Length Ratio Size         Date & time       CRC-32     Attr";
				const string headerUnderline = "------------  ---------- ----- ---------- ------------------- --------   ------";
				
				FileInfo fileInfo = new FileInfo(fileName);
				
				if (fileInfo.Exists == false) {
					Console.Error.WriteLine("No such file exists {0}", fileName);
					return;
				}

				Console.Out.WriteLine(fileName);

				ZipFile zipFile = new ZipFile(fileName);
				int entryCount = 0;
				long totalSize  = 0;
				
		 		foreach (ZipEntry theEntry in zipFile) {
						
					if (entryCount == 0) {
						Console.Out.WriteLine(headerTitles);
						Console.Out.WriteLine(headerUnderline);
					}
						
					++entryCount;
					int ratio = GetCompressionRatio(theEntry.CompressedSize, theEntry.Size);
					totalSize += theEntry.Size;
							
					if (theEntry.Name.Length > 12) {
						Console.Out.WriteLine(theEntry.Name);
						Console.Out.WriteLine(
						    "{0,-12}  {1,10:0}  {2,3}% {3,10:0} {4,10:d} {4:hh:mm:ss} {5,8:x}   {6,4}",
						    "", theEntry.Size, ratio, theEntry.CompressedSize, theEntry.DateTime, theEntry.Crc,
						    InterpretExternalAttributes(theEntry.HostSystem, theEntry.ExternalFileAttributes));
					} else {
						Console.Out.WriteLine(
						    "{0,-12}  {1,10:0}  {2,3}% {3,10:0} {4,10:d} {4:hh:mm:ss} {5,8:x}   {6,4}",
						    theEntry.Name, theEntry.Size, ratio, theEntry.CompressedSize, theEntry.DateTime, theEntry.Crc, 
						    InterpretExternalAttributes(theEntry.HostSystem, theEntry.ExternalFileAttributes));
					}
				}
			
				if (entryCount == 0) {
					Console.Out.WriteLine("Archive is empty!");
				} else {
					Console.Out.WriteLine(headerUnderline);
					Console.Out.WriteLine(
						"{0,-12}  {1,10:0}  {2,3}% {3,10:0} {4,10:d} {4:hh:mm:ss}",
						entryCount.ToString() + " entries", totalSize, GetCompressionRatio(fileInfo.Length, totalSize), fileInfo.Length, fileInfo.LastWriteTime);
				}
			}
			catch(Exception exception)
			{
				Console.Error.WriteLine("Exception during list operation: {0}", exception.Message);
			}
		}
		
		/// <summary>
		/// Execute List operation
		/// Currently only Zip files are supported
		/// </summary>
		/// <param name="fileSpecs">Files to list</param>
		void List(ArrayList fileSpecs)
		{
			foreach (string spec in fileSpecs) {
				string [] names;
				string pathName = Path.GetDirectoryName(spec);
					
				if (pathName == null || pathName.Length == 0) {
					pathName = @".\";
				}
				names = Directory.GetFiles(pathName, Path.GetFileName(spec));
				
				if (names.Length == 0) {
					Console.Error.WriteLine("No files found matching {0}", spec);
				}
				else {
					foreach (string file in names) {
						if (useZipFileWhenListing) {
							ListZipViaZipFile(file);
						} else {
							ListZip(file);
						}
						Console.Out.WriteLine("");
					}
				}
			}
		}

		/// <summary>
		/// 'Cook' a name making it acceptable as a zip entry name.
		/// </summary>
		/// <param name="name">name to cook</param>
		/// <param name="stripPrefix">String to remove from front of name if present</param>
		/// <param name="relativePath">Make names relative if true or absolute if false</param>
		static public string CookZipEntryName(string name, string stripPrefix, bool relativePath)
		{
#if TEST
			Console.WriteLine("Cooking '{0}' prefix is '{1}'", name, stripPrefix);
#endif
			if (name == null) {
				return "";
			}
			
			if (stripPrefix != null && stripPrefix.Length > 0 && name.IndexOf(stripPrefix, 0) == 0) {
				name = name.Substring(stripPrefix.Length);
			}
		
			if (Path.IsPathRooted(name) == true) {
				// NOTE:
				// for UNC names...  \\machine\share\zoom\beet.txt gives \zoom\beet.txt
				name = name.Substring(Path.GetPathRoot(name).Length);
#if TEST
				Console.WriteLine("Removing root info {0}", name);
#endif
			}

			name = name.Replace(@"\", "/");
			
			if (relativePath == true) {
				if (name.Length > 0 && (name[0] == Path.AltDirectorySeparatorChar || name[0] == Path.DirectorySeparatorChar)) {
					name = name.Remove(0, 1);
				}
			} else {
				if (name.Length > 0 && name[0] != Path.AltDirectorySeparatorChar && name[0] != Path.DirectorySeparatorChar) {
					name = name.Insert(0, "/");
				}
			}
#if TEST
			Console.WriteLine("Cooked value '{0}'", name);
#endif
			return name;
		}
		
		/// <summary>
		/// Make string into something acceptable as an entry name
		/// </summary>
		/// <param name="name">Name to 'cook'</param>
		string CookZipEntryName(string name)
		{
			return CookZipEntryName(name, removablePathPrefix, relativePathInfo);
		}

		ZipOutputStream outputStream;

		/// <summary>
		/// Add a file assuming the output is seekable
		/// </summary>		
		// TODO Add equivalent for non-seekable output
		void AddFileSeekableOutput(string file, string entryPath, int maximumBufferSize)
		{
			ZipEntry entry = new ZipEntry(entryPath);
			FileInfo fileInfo = new FileInfo(file);
			entry.DateTime = fileInfo.LastWriteTime; // File.GetLastWriteTime(file); // or DateTime.Now or whatever, for now use the file
			entry.ExternalFileAttributes = (int)fileInfo.Attributes;
			entry.Size = fileInfo.Length;

			if (useZipStored) {
				entry.CompressionMethod = CompressionMethod.Stored;
			} else {
				entry.CompressionMethod = CompressionMethod.Deflated;
			}

			System.IO.FileStream fileStream = System.IO.File.OpenRead(file);

			try {
				byte[] transferBuffer;
				if (fileStream.Length > maximumBufferSize)
					transferBuffer = new byte[maximumBufferSize];
				else
					transferBuffer = new byte[fileStream.Length];

				outputStream.PutNextEntry(entry);

				int bytesRead;
				do {
					bytesRead = fileStream.Read(transferBuffer, 0, transferBuffer.Length);
					outputStream.Write(transferBuffer, 0, bytesRead);
				}
				while (bytesRead > 0);
			}
			finally {
				fileStream.Close();
			}
		}
		
		/// <summary>
		/// Add file to archive
		/// </summary>
		/// <param name="fileName">file to add</param>
		/// <param name="bufferSize">size of buffer to use with file</param>
		void AddFile(string fileName, int bufferSize) {
#if TEST
			Console.WriteLine("AddFile {0}", fileName);
#endif			
			
			if (File.Exists(fileName)) {
				string entryName = CookZipEntryName(fileName);
				
				if (silent == false) {
					Console.Write(" " + entryName);
				}
			
				AddFileSeekableOutput(fileName, entryName, bufferSize);
				
				if (silent == false) {
					Console.WriteLine("");
				}
			} else {
				Console.Error.WriteLine("No such file exists {0}", fileName);
			}
			
			
		}
	
		/// <summary>
		/// Add an entry for a folder or directory
		/// </summary>
		/// <param name="folderName">The name of the folder to add</param>
		void AddFolder(string folderName)
		{
#if TEST
			Console.WriteLine("AddFolder {0}", folderName);
#endif			
			folderName = CookZipEntryName(folderName);
			if (folderName.Length == 0 || folderName[folderName.Length - 1] != '/')
				folderName = folderName + '/';
	
			ZipEntry zipEntry = new ZipEntry(folderName);
			outputStream.PutNextEntry(zipEntry);
		}
	
		/// <summary>
		/// Compress contents of folder
		/// </summary>
		/// <param name="basePath">The folder to compress</param>
		/// <param name="recursive">If true process recursively</param>
		/// <param name="searchPattern">Pattern to match for files</param>
		/// <returns>Number of entries added</returns>
		int CompressFolder(string basePath, bool recursive, string searchPattern)
		{
			int result = 0;
#if TEST
			System.Console.Out.WriteLine("CompressFolder basepath {0}  pattern {1}", basePath, searchPattern);
#endif
			string [] names = Directory.GetFiles(basePath, searchPattern);
			
			foreach (string fileName in names) {
				AddFile(fileName, 8192);
				++result;
			}
		
			if (names.Length == 0 && addEmptyDirectoryEntries) {
				AddFolder(basePath);
				++result;
			}
		
			if (recursive) {
				names = Directory.GetDirectories(basePath);
				foreach (string folderName in names) {
					result += CompressFolder(folderName, recursive, searchPattern);
				}
			}
			return result;
		}
	
		/// <summary>
		/// Create archives based on specifications passed and internal state
		/// </summary>		
		void Create(ArrayList fileSpecs)
		{
			string zipFileName = fileSpecs[0] as string;
			if (Path.GetExtension(zipFileName).Length == 0)
				zipFileName = Path.ChangeExtension(zipFileName, ".zip");
			
			fileSpecs.RemoveAt(0);

			if (overwriteFiles == Overwrite.Never && File.Exists(zipFileName)) {
				System.Console.Error.WriteLine("File {0} already exists", zipFileName);
				return;
			}

			int totalEntries = 0;
			
			using (FileStream stream = File.Create(zipFileName)) {
				outputStream = new ZipOutputStream(stream);
				try {
					if (password != null && password.Length > 0) {
						outputStream.Password = password;
					}
					
					outputStream.SetLevel(compressionLevel);
					foreach(string spec in fileSpecs) {
						string fileName = Path.GetFileName(spec);
						string pathName = Path.GetDirectoryName(spec);
						
						if (pathName == null || pathName.Length == 0) {
							pathName = Path.GetFullPath(".");
							if (relativePathInfo == true)
								removablePathPrefix = pathName;
						} else {
							pathName = Path.GetFullPath(pathName);
							// TODO for paths like ./txt/*.txt the prefix should be fullpath for .
							// for z:txt/*.txt should be fullpath for z:.
							if (relativePathInfo == true) {
								removablePathPrefix = pathName;
							}
						}
						
						
						// TODO wildcards arent full supported by this
						if (recursive || fileName.IndexOf('*') >= 0 || fileName.IndexOf('?') >= 0) {
							
							// TODO this allows possible conflict in filenames that are added to Zip file
							// as part of different file specs.
							// how to resolve this, could leave for decompression to solve, or try and fix at create time
							
							totalEntries += CompressFolder(pathName, recursive, fileName);
						} else {
							AddFile(pathName + @"\" + fileName, 8192);
							++totalEntries;
						}
					}
					
					if (totalEntries == 0) {
						Console.Out.WriteLine("File created has no entries!");
					}
				} finally {
					outputStream.Close();
					outputStream = null;
				}
			}
		}

		/// <summary>
		/// Decompress a file
		/// </summary>
		/// <param name="fileName">File to decompress</param>
		/// <param name="targetDir">Directory to create output in</param>
		/// <returns>true iff all has been done successfully</returns>
		bool DecompressFile(string fileName, string targetDir)
		{
			bool result = true;
		
	
			try {
				using (ZipInputStream inputStream = new ZipInputStream(File.OpenRead(fileName))) {
					if (password != null)
						inputStream.Password = password;
		
					ZipEntry theEntry;
		
					while ((theEntry = inputStream.GetNextEntry()) != null) {
						
						// try and sort out the correct place to save this entry
						string entryFileName;
						
						if (Path.IsPathRooted(theEntry.Name)) {
							entryFileName = Path.Combine(Path.GetDirectoryName(theEntry.Name), Path.GetFileName(theEntry.Name));
						} else {
							entryFileName = theEntry.Name;
						}
						string targetName = Path.Combine(targetDir, entryFileName);
						
						string fullPath = Path.GetDirectoryName(Path.GetFullPath(targetName));
#if TEST
						Console.WriteLine("Decompress targetfile name " + entryFileName);
						Console.WriteLine("Decompress targetpath " + fullPath);
#endif						
						
						// Could be an option or parameter to allow failure or try creation
						if (Directory.Exists(fullPath) == false)
						{
							try {
								Directory.CreateDirectory(fullPath);
							}
							catch {
								return false;
							}
						} else if (overwriteFiles == Overwrite.Prompt) {
							if (File.Exists(targetName) == true) {
								Console.Write("File " + targetName + " already exists.  Overwrite? ");
								
								// TODO sort out the complexities of Read so single key presses can be used
								string readValue;
								try {
									readValue = Console.ReadLine();
								}
								catch {
									readValue = null;
								}
								
								if (readValue == null || readValue.ToLower() != "y") {
#if TEST
									Console.WriteLine("Skipped!");
#endif						
									continue;
								}
							}
						}
		
					
						if (entryFileName.Length > 0) {
#if TEST
							Console.WriteLine("Extracting...");
#endif						
							FileStream streamWriter = File.Create(targetName);
						
							try {
								byte[] data = new byte[4096];
								int size;
					
								do {
									size = inputStream.Read(data, 0, data.Length);
									streamWriter.Write(data, 0, size);
								} while (size > 0);
							}
							finally {
								streamWriter.Close();
							}
							
							if (restoreDateTime) {
								File.SetLastWriteTime(targetName, theEntry.DateTime);
							}
						}
					}
					inputStream.Close();
				}
			}
			catch (Exception except) {
				result = false;
				Console.Error.WriteLine(except.Message + " Failed to unzip file");
			}
		
			return result;
		}
		
		/// <summary>
		/// Extract archives based on user input
		/// Allows simple wildcards to specify multiple archives
		/// </summary>
		void Extract(ArrayList fileSpecs)
		{
			if (targetOutputDirectory == null || targetOutputDirectory.Length == 0) {
				targetOutputDirectory = @".\";
			}
			
			foreach(string spec in fileSpecs) {
				
				string [] names;
				if (spec.IndexOf('*') >= 0 || spec.IndexOf('?') >= 0) {
					string pathName = Path.GetDirectoryName(spec);
					
					if (pathName == null || pathName.Length == 0) {
						pathName = @".\";
					}
					names = Directory.GetFiles(pathName, Path.GetFileName(spec));
				} else {
					names = new string[] { spec };
				}

				foreach (string fileName in names) {				
					if (File.Exists(fileName) == false) {
						Console.Error.WriteLine("No such file exists {0}", spec);
					} else {
						DecompressFile(fileName, targetOutputDirectory);
					}
				}
			}
		}

		/// <summary>
		/// Parse command line arguments and 'execute' them.
		/// </summary>		
		void Execute(string[] args) {
			if (SetArgs(args)) {
				if (fileSpecs.Count == 0) {
					if (silent == false) {
						Console.Out.WriteLine("Nothing to do");
					}
				}
				else {
					switch (operation) {
						case Operation.List:
							List(fileSpecs);
							break;
						
						case Operation.Create:
							Create(fileSpecs);
							break;
						
						case Operation.Extract:
							Extract(fileSpecs);
							break;
					}
				}
			} else {
				if (silent == false) {
					ShowHelp();
				}
			}
		}
		
		/// <summary>
		/// Entry point for program, creates archiver and runs it
		/// </summary>
		/// <param name="args">
		/// Command line argument to process
		/// </param>
		public static void Main(string[] args) {
		
			SharpZipArchiver sza = new SharpZipArchiver();
			sza.Execute(args);
		}
	}
}

