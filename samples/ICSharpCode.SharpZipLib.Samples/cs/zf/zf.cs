//------------------------------------------------------------------------------
//
// zf - A command line archiver using the ZipFile class from SharpZipLib
// for compression
//
// Copyright © 2000-2016 SharpZipLib Contributors
//
//------------------------------------------------------------------------------
// Version History
// 1 Initial version ported from sz sample.  Some stuff is not used or commented still
// 2 Display files during extract. --env Now shows .NET version information.
// 3 Add usezip64 option as a testing aid.
// 4 Fix format bug in output, remove unused code and other minor refactoring


using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Globalization;
using System.Reflection;

using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;

namespace ICSharpCode.SharpZipLib.Samples.CS.ZF
{
	
	/// <summary>
	/// A command line archiver using the <see cref="ZipFile"/> class from the SharpZipLib compression library
	/// </summary>
	public class ZipFileArchiver 
	{
		#region Enumerations

		/// <summary>
		/// Options for handling overwriting of files.
		/// </summary>
		enum Overwrite 
		{
			Prompt,
			Never,
			Always
		}

		/// <summary>
		/// Operations that can be performed
		/// </summary>
		enum Operation 
		{
			Create,     // add files to new archive
			Extract,    // extract files from existing archive
			List,       // show contents of existing archive
			Delete,		// Delete from archive
			Add,		// Add to archive.
			Test,		// Test the archive for validity.
		}
		#endregion

		#region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ZipFileArchiver"/> class.
        /// </summary>
		public ZipFileArchiver()
		{
			// Do nothing.
		}

		#endregion

		#region Argument Parsing

		/// <summary>
		/// Parse command line arguments.
		/// This is fairly flexible without using any custom classes.  Arguments and options can appear
		/// in any order and are case insensitive.  Arguments for options are signalled with an '='
		/// as in -demo=argument, sometimes the '=' can be omitted as well secretly.
		/// Grouping of single character options is supported.
		/// </summary>		
		/// <returns>
		/// true if arguments are valid such that processing should continue
		/// </returns>
		bool SetArgs(string[] args) 
		{
			bool result = true;
			int argIndex = 0;
			
			while (argIndex < args.Length) 
			{
				if (args[argIndex][0] == '-' || args[argIndex][0] == '/') 
				{
					
					string option = args[argIndex].Substring(1).ToLower();
					string optArg = "";
	
					int parameterIndex = option.IndexOf('=');
	
					if (parameterIndex >= 0)
					{
						if (parameterIndex < option.Length - 1) 
						{
							optArg = option.Substring(parameterIndex + 1);
						}
						option = option.Substring(0, parameterIndex);
					}

#if OPTIONTEST
					Console.WriteLine("args index [{0}] option [{1}] argument [{2}]", argIndex, option, optArg);
#endif
					if (option.Length == 0) 
					{
						Console.Error.WriteLine("Invalid argument '{0}'", args[argIndex]);
						result = false;
					}
					else 
					{
						int optionIndex = 0;
						while (optionIndex < option.Length) 
						{
#if OPTIONTEST
							Console.WriteLine("optionIndex {0}", optionIndex);
#endif
							switch(option[optionIndex]) 
							{
								case '-': // long option
									optionIndex = option.Length;
									
									switch (option) 
									{
										case "-add":
											operation_ = Operation.Add;
											break;
	
										case "-create":
											operation_ = Operation.Create;
											break;
											
										case "-list":
											operation_ = Operation.List;
											break;
		
										case "-extract":
											operation_ = Operation.Extract;
											if (optArg.Length > 0) 
											{
												targetOutputDirectory_ = optArg;
											}
											break;
	
										case "-delete":
											operation_ = Operation.Delete;
											break;
	
										case "-test":
											operation_ = Operation.Test;
											break;
	
                                        case "-dryrun":
									        dryRun_ = true;
									        break;

										case "-env":
											ShowEnvironment();
											break;
											
										case "-emptydirs":
											addEmptyDirectoryEntries_ = true;
											break;
		
										case "-data":
											testData_ = true;
											break;
	
										case "-zip64":
											if ( optArg.Length > 0 )
											{
												switch ( optArg )
												{
													case "on":
														useZip64_ = UseZip64.On;
														break;
														
													case "off":
														useZip64_ = UseZip64.Off;
														break;
														
													case "auto":
														useZip64_ = UseZip64.Dynamic;
														break;
												}
											}
											break;
											
										case "-encoding":
											if (optArg.Length > 0) 
											{
												if (IsNumeric(optArg)) 
												{
													try 
													{
														int enc = int.Parse(optArg);
														if (Encoding.GetEncoding(enc) != null) 
														{
	#if OPTIONTEST
															Console.WriteLine("Encoding set to {0}", enc);
	#endif
															ZipStrings.CodePage = enc;
														} 
														else 
														{
															result = false;
															Console.Error.WriteLine("Invalid encoding " + args[argIndex]);
														}
													}
													catch (Exception) 
													{
														result = false;
														Console.Error.WriteLine("Invalid encoding " + args[argIndex]);
													}
												} 
												else 
												{
													try 
													{
														ZipStrings.CodePage = Encoding.GetEncoding(optArg).CodePage;
													}
													catch (Exception) 
													{
														result = false;
														Console.Error.WriteLine("Invalid encoding " + args[argIndex]);
													}
												}
											} 
											else 
											{
												result = false;
												Console.Error.WriteLine("Missing encoding parameter");
											}
											break;
											
										case "-version":
											ShowVersion();
											break;
											
										case "-help":
											ShowHelp();
											break;
				
										case "-restore-dates":
											restoreDateTime_ = true;
											break;
	
										default:
											Console.Error.WriteLine("Invalid long argument " + args[argIndex]);
											result = false;
											break;
									}
									break;
								
								case '?':
									ShowHelp();
									break;
								
								case 's':
									if (optionIndex != 0) 
									{
										result = false;
										Console.Error.WriteLine("-s cannot be in a group");
									} 
									else 
									{
										if (optArg.Length > 0) 
										{
											password_ = optArg;
										} 
										else if (option.Length > 1) 
										{
											password_ = option.Substring(1);
										} 
										else 
										{
											Console.Error.WriteLine("Missing argument to " + args[argIndex]);
										}
									}
									optionIndex = option.Length;
									break;

								case 't':
									operation_ = Operation.Test;
									break;

								case 'c':
									operation_ = Operation.Create;
									break;
															
								case 'o':
									optionIndex += 1;
									overwriteFiles = optionIndex < option.Length ? (option[optionIndex] == '+') ? Overwrite.Always : Overwrite.Never : Overwrite.Never;
									break;
								
								case 'q':
									silent_ = true;
									if (overwriteFiles == Overwrite.Prompt) 
									{
										overwriteFiles = Overwrite.Never;
									}
									break;
	
								case 'r':
									recursive_ = true;
									break;
	
								case 'v':
									operation_ = Operation.List;
									break;
	
								case 'x':
									if (optionIndex != 0) 
									{
										result = false;
										Console.Error.WriteLine("-x cannot be in a group");
									} 
									else 
									{
										operation_ = Operation.Extract;
										if (optArg.Length > 0) 
										{
											targetOutputDirectory_ = optArg;
										}
									}
									optionIndex = option.Length;
									break;
								
								default:
									Console.Error.WriteLine("Invalid argument: " + args[argIndex]);
									result = false;
									break;
							}
							++optionIndex;
						}
					}
				}
				else 
				{
#if OPTIONTEST
					Console.WriteLine("file spec {0} = '{1}'", argIndex, args[argIndex]);
#endif
					fileSpecs_.Add(args[argIndex]);
				}
				++argIndex;
			}
			
			if (fileSpecs_.Count > 0) 
			{
				var checkPath = (string)fileSpecs_[0];
				int deviceCheck = checkPath.IndexOf(':');
#if NET_VER_1
				if (checkPath.IndexOfAny(Path.InvalidPathChars) >= 0
#else
				if (checkPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0
#endif
					|| checkPath.IndexOf('*') >= 0 || checkPath.IndexOf('?') >= 0
					|| ((deviceCheck >= 0) && (deviceCheck != 1))) 
				{
					Console.WriteLine("There are invalid characters in the specified zip file name");
					result = false;					
				}
			}
			return result && (fileSpecs_.Count > 0);
		}

		#endregion

		#region Show - Help/Environment/Version

		/// <summary>
		/// Show encoding/locale information
		/// </summary>
		void ShowEnvironment()
		{
			seenHelp_ = true;
			Console.Out.WriteLine("");
			Console.Out.WriteLine(
				"Current encoding is {0}, code page {1}, windows code page {2}",
				Console.Out.Encoding.EncodingName,
				Console.Out.Encoding.CodePage,
				Console.Out.Encoding.WindowsCodePage);

			Console.WriteLine("Default code page is {0}",
				Encoding.Default.CodePage);
			
			Console.WriteLine( "Current culture LCID 0x{0:X}, {1}", CultureInfo.CurrentCulture.LCID, CultureInfo.CurrentCulture.EnglishName);
			Console.WriteLine( "Current thread OEM codepage {0}", System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.OEMCodePage);
			Console.WriteLine( "Current thread Mac codepage {0}", System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.MacCodePage);
			Console.WriteLine( "Current thread Ansi codepage {0}", System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ANSICodePage);
			Console.WriteLine(".NET version {0}", Environment.Version);
		}
		
		/// <summary>
		/// Display version information
		/// </summary>		
		void ShowVersion() 
		{
			seenHelp_ = true;
			Console.Out.WriteLine("ZipFile Archiver v0.3");
			Console.Out.WriteLine("Copyright © 2000-2016 AlphaSierraPapa for the SharpZipLib Team");
			
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

			foreach (Assembly assembly in assemblies) 
			{
				if (assembly.GetName().Name == "ICSharpCode.SharpZipLib") 
				{
					Console.Out.WriteLine("#ZipLib v{0} {1}", assembly.GetName().Version,
						assembly.GlobalAssemblyCache ? "Running from GAC" : "Running from DLL"
						);
				}
			}
			Console.Out.WriteLine();
		}

		/// <summary>
		/// Show help on possible options and arguments
		/// </summary>
		void ShowHelp()
		{
			if (seenHelp_) 
			{
				return;
			}
			
			seenHelp_ = true;
			ShowVersion();
			Console.Out.WriteLine("usage zf {options} archive files");
			Console.Out.WriteLine("");
			Console.Out.WriteLine("Options:");

			Console.Out.WriteLine("--add                      Add files to archive");
			Console.Out.WriteLine("--create                   Create new archive");
			Console.Out.WriteLine("--data                     Test archive data");
            Console.Out.WriteLine("--delete                   Delete files from archive");
			Console.Out.WriteLine("--encoding=codepage|name   Set code page for encoding by name or number");
			Console.Out.WriteLine("--extract{=dir}            Extract archive contents to dir(default .)");
			Console.Out.WriteLine("--help                     Show this help");
			Console.Out.WriteLine("--env                      Show current environment information" );
			Console.Out.WriteLine("--list                     List archive contents extended format");
			Console.Out.WriteLine("--test                     Test archive for validity");
			Console.Out.WriteLine("--version                  Show version information");
			Console.Out.WriteLine("-r                         Recurse sub-folders");
			Console.Out.WriteLine("-s=password                Set archive password");
			Console.Out.WriteLine("--zip64=[on|off|auto]      Zip64 extension handling to use");


			/*
			Console.Out.WriteLine("--store                    Store entries (default=deflate)");
			Console.Out.WriteLine("--emptydirs                Create entries for empty directories");
			Console.Out.WriteLine("--restore-dates            Restore dates on extraction");
			Console.Out.WriteLine("-o+                        Overwrite files without prompting");
			Console.Out.WriteLine("-o-                        Never overwrite files");
			Console.Out.WriteLine("-q                         Quiet mode");
			*/			
			Console.Out.WriteLine("");
		}
		
		#endregion

		#region Archive Listing

		void ListArchiveContents(ZipFile zipFile, FileInfo fileInfo)
		{
			const string headerTitles    = "Name              Length Ratio Size           Date & time     CRC-32     Attr";
			const string headerUnderline = "------------  ---------- ----- ---------- ------------------- --------   ------";
				
			int entryCount = 0;
			long totalCompressedSize = 0;
			long totalSize  = 0;

			foreach (ZipEntry theEntry in zipFile) 
			{
						
				if ( theEntry.IsDirectory ) 
				{
					Console.Out.WriteLine("Directory {0}", theEntry.Name);
				}
				else if ( !theEntry.IsFile ) 
				{
					Console.Out.WriteLine("Non file entry {0}", theEntry.Name);
					continue;
				}
				else
				{
					if (entryCount == 0) 
					{
						Console.Out.WriteLine(headerTitles);
						Console.Out.WriteLine(headerUnderline);
					}
						
					++entryCount;
					int ratio = GetCompressionRatio(theEntry.CompressedSize, theEntry.Size);
					totalSize += theEntry.Size;
					totalCompressedSize += theEntry.CompressedSize;
					
					char cryptoDisplay = ( theEntry.IsCrypted ) ? '*' : ' ';

					if (theEntry.Name.Length > 12) 
					{
						Console.Out.WriteLine(theEntry.Name);
						Console.Out.WriteLine(
							"{0,-12}{7} {1,10:0}  {2,3}% {3,10:0} {4,10:d} {4:hh:mm:ss} {5,8:x}   {6,4}",
							"", theEntry.Size, ratio, theEntry.CompressedSize, theEntry.DateTime, theEntry.Crc,
							InterpretExternalAttributes(theEntry.HostSystem, theEntry.ExternalFileAttributes),
							cryptoDisplay);
					} 
					else 
					{
						Console.Out.WriteLine(
							"{0,-12}{7} {1,10:0}  {2,3}% {3,10:0} {4,10:d} {4:hh:mm:ss} {5,8:x}   {6,4}",
							theEntry.Name, theEntry.Size, ratio, theEntry.CompressedSize, theEntry.DateTime, theEntry.Crc, 
							InterpretExternalAttributes(theEntry.HostSystem, theEntry.ExternalFileAttributes),
							cryptoDisplay);
					}
				}
			}

			if (entryCount == 0) 
			{
				Console.Out.WriteLine("Archive is empty!");
			} 
			else 
			{
				Console.Out.WriteLine(headerUnderline);
				Console.Out.WriteLine(
					"{0,-12}  {1,10:0}  {2,3}% {3,10:0} {4,10:d} {4:hh:mm:ss}",
					entryCount + " entries", totalSize, GetCompressionRatio(totalCompressedSize, totalSize), fileInfo.Length, fileInfo.LastWriteTime);
			}
		}

		/// <summary>
		/// List zip file contents using ZipFile class
		/// </summary>
		/// <param name="fileName">File to list contents of</param>
		void ListArchiveContents(string fileName) 
		{
			try
			{
				var fileInfo = new FileInfo(fileName);
				
				if (!fileInfo.Exists) 
				{
					Console.Error.WriteLine("No such file exists {0}", fileName);
				}
				else
				{

					Console.Out.WriteLine(fileName);

					try
					{
						using (ZipFile zipFile = new ZipFile(fileName))
						{
							ListArchiveContents(zipFile, fileInfo);
						}
					}
					catch(Exception ex)
					{
						Console.Out.WriteLine("Problem reading archive - '{0}'", ex.Message);
					}
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
			foreach (string spec in fileSpecs) 
			{
				string pathName = Path.GetDirectoryName(spec);
					
				if ( string.IsNullOrEmpty(pathName)) 
				{
					pathName = @".\";
				}

				string[] names = Directory.GetFiles(pathName, Path.GetFileName(spec));
				
				if (names.Length == 0) 
				{
					Console.Error.WriteLine("No files found matching {0}", spec);
				}
				else 
				{
					foreach (string file in names) 
					{
						ListArchiveContents(file);
					}
					Console.Out.WriteLine("");
				}
			}
		}

		#endregion

		#region Creation

		/// <summary>
		/// Create archives based on specifications passed and internal state
		/// </summary>		
		void Create(ArrayList fileSpecs)
		{
			var zipFileName = fileSpecs[0] as string;
			if (Path.GetExtension(zipFileName).Length == 0) 
			{
				zipFileName = Path.ChangeExtension(zipFileName, ".zip");
			}
			
			fileSpecs.RemoveAt(0);

			if ( (overwriteFiles == Overwrite.Never) && File.Exists(zipFileName)) 
			{
				Console.Error.WriteLine("File {0} already exists", zipFileName);
				return;
			}

			try
			{
				using (ZipFile zf = ZipFile.Create(zipFileName) )
				{
					zf.Password = password_;
					zf.UseZip64 = useZip64_;
					
					zf.BeginUpdate();

					activeZipFile_ = zf;

					foreach (string spec in fileSpecs)
					{
						// This can fail with wildcards in spec...
						string path = Path.GetDirectoryName(Path.GetFullPath(spec));
						string fileSpec = Path.GetFileName(spec);

						zf.NameTransform = new ZipNameTransform(path);

						var scanner = new FileSystemScanner(WildcardToRegex(fileSpec));
						scanner.ProcessFile = new ProcessFileHandler(ProcessFile);
						scanner.ProcessDirectory += new EventHandler<DirectoryEventArgs>(ProcessDirectory);
						scanner.Scan(path, recursive_);
					}

					zf.CommitUpdate();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Problem creating archive - '{0}'", ex.Message);
			}
		}

		#endregion

		#region Extraction

		/// <summary>
		/// Extract a file storing its contents.
		/// </summary>
		/// <param name="inputStream">The input stream to source file contents from.</param>
		/// <param name="theEntry">The <see cref="ZipEntry"/> representing the stored file details </param>
		/// <param name="targetDir">The directory to store the output.</param>
		/// <returns>True if operation is successful; false otherwise.</returns>
		bool ExtractFile(Stream inputStream, ZipEntry theEntry, string targetDir)
		{
            if (inputStream == null)
            {
                throw new ArgumentNullException("inputStream");
            }

		    if (theEntry == null)
            {
                throw new ArgumentNullException("theEntry");
            }

            if (!theEntry.IsFile)
            {
                throw new ArgumentException("Not a file", "theEntry");
            }

		    if (targetDir == null)
            {
                throw new ArgumentNullException("targetDir");
            }

		    // try and sort out the correct place to save this entry

		    bool result = true;
            bool process = theEntry.Name.Length > 0;

            if (!process)
            {
                // A fuller program would generate or prompt for a filename here...
                if (!silent_)
                {
                    Console.WriteLine("Ignoring empty name");
                }

                return false;
            }

		    string entryFileName;

            if (Path.IsPathRooted(theEntry.Name))
            {
                string workName = Path.GetPathRoot(theEntry.Name);
                workName = theEntry.Name.Substring(workName.Length);
                entryFileName = Path.Combine(Path.GetDirectoryName(workName), Path.GetFileName(theEntry.Name));
            }
            else
            {
                entryFileName = theEntry.Name;
            }

            string targetName = Path.Combine(targetDir, entryFileName);
            string fullDirectoryName = Path.GetDirectoryName(Path.GetFullPath(targetName));

#if TEST
			Console.WriteLine("Decompress targetfile name " + entryFileName);
			Console.WriteLine("Decompress targetpath " + fullDirectoryName);
#endif

            // Could be an option or parameter to allow failure or try creation
            if (process)
            {
                if (Directory.Exists(fullDirectoryName) == false)
                {
                    try
                    {
                        Directory.CreateDirectory(fullDirectoryName);
                    }
                    catch (Exception ex)
                    {
                        if (!silent_)
                        {
                            Console.Write("Exception creating directory '{0}' - {1}", fullDirectoryName, ex.Message);
                        }

                        result = false;
                        process = false;
                    }
                }
                else if (overwriteFiles == Overwrite.Prompt)
                {
                    if (File.Exists(targetName))
                    {
                        Console.Write("File " + targetName + " already exists.  Overwrite? ");

                        string readValue;
                        try
                        {
                            readValue = Console.ReadLine();
                        }
                        catch
                        {
                            readValue = null;
                        }

                        if ((readValue == null) || (readValue.ToLower() != "y"))
                        {
                            process = false;
                        }
                    }
                }
            }

		    if (process)
			{
				if ( !silent_ )
				{
					Console.Write("{0}", targetName);
				}

                if (!dryRun_)
                {
                    using (FileStream outputStream = File.Create(targetName))
                    {
                        StreamUtils.Copy(inputStream, outputStream, GetBuffer());
                    }

                    if (restoreDateTime_)
                    {
                        File.SetLastWriteTime(targetName, theEntry.DateTime);
                    }
                }

			    if ( !silent_ )
				{
					Console.WriteLine(" OK");
				}
			}
			return result;
		}

		/// <summary>
		/// Decompress a file
		/// </summary>
		/// <param name="fileName">File to decompress</param>
		/// <param name="targetDir">Directory to create output in</param>
		/// <returns>true iff all has been done successfully</returns>
		bool DecompressArchive(string fileName, string targetDir)
		{
			bool result = true;

			try
			{
				using (ZipFile zf = new ZipFile(fileName))
				{
					zf.Password = password_;
					foreach ( ZipEntry entry in zf )
					{
                        if (entry.IsFile)
                        {
                            if (!ExtractFile(zf.GetInputStream(entry), entry, targetDir))
                            {
                                if (!silent_)
                                {
                                    Console.WriteLine("Extraction failed {0}", entry.Name);
                                }
                            }
                        }
                        else
                        {
                            if (!silent_)
                            {
                                Console.WriteLine("Skipping {0}", entry.Name);
                            }
                        }
					}
					
					if ( !silent_ )
					{
						Console.WriteLine("Done");
					}
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine("Exception decompressing - '{0}'", ex);
				result = false;
			}
			return result;
		}
		
		/// <summary>
		/// Extract archives based on user input
		/// Allows simple wildcards to specify multiple archives
		/// </summary>
		void Extract(ArrayList fileSpecs)
		{
			if ( string.IsNullOrEmpty(targetOutputDirectory_))
			{
				targetOutputDirectory_ = @".\";
			}
			
			foreach(string spec in fileSpecs) 
			{
				
				string [] names;
				if ( (spec.IndexOf('*') >= 0) || (spec.IndexOf('?') >= 0) ) 
				{
					string pathName = Path.GetDirectoryName(spec);
					
					if ( string.IsNullOrEmpty(pathName)) 
					{
						pathName = @".\";
					}
					names = Directory.GetFiles(pathName, Path.GetFileName(spec));
				} 
				else 
				{
					names = new string[] { spec };
				}

				foreach (string fileName in names) 
				{				
					if (File.Exists(fileName) == false) 
					{
						Console.Error.WriteLine("No such file exists {0}", fileName);
					} 
					else 
					{
						DecompressArchive(fileName, targetOutputDirectory_);
					}
				}
			}
		}

		#endregion

		#region Testing

		/// <summary>
		/// Handler for test result callbacks.
		/// </summary>
		/// <param name="status">The current <see cref="TestStatus"/>.</param>
		/// <param name="message">The message applicable for this result.</param>
		void TestResultHandler(TestStatus status, string message)
		{
			switch ( status.Operation )
			{
				case TestOperation.Initialising:
					Console.WriteLine("Testing");
					break;

				case TestOperation.Complete:
					Console.WriteLine("Testing complete");
					break;

				case TestOperation.EntryHeader:
					// Not an error if message is null.
					if ( message == null )
					{
						Console.Write("{0} - ", status.Entry.Name);
					}
					else
					{
						Console.WriteLine(message);
					}
					break;

				case TestOperation.EntryData:
					if ( message != null )
					{
						Console.WriteLine(message);
					}
					break;

				case TestOperation.EntryComplete:
					if ( status.EntryValid )
					{
						Console.WriteLine("OK");
					}
					break;

				case TestOperation.MiscellaneousTests:
					if ( message != null )
					{
						Console.WriteLine(message);
					}
					break;
			}
		}

		/// <summary>
		/// Test an archive to see if its valid.
		/// </summary>
		/// <param name="fileSpecs">The files to test.</param>
		void Test(ArrayList fileSpecs)
		{
			var zipFileName = fileSpecs[0] as string;
			if (Path.GetExtension(zipFileName).Length == 0) 
			{
				zipFileName = Path.ChangeExtension(zipFileName, ".zip");
			}

			try
			{
				using (ZipFile zipFile = new ZipFile(zipFileName))
				{
					zipFile.Password = password_;
					if ( zipFile.TestArchive(testData_, TestStrategy.FindAllErrors,
						new ZipTestResultHandler(TestResultHandler)) )
					{
						Console.Out.WriteLine("Archive test passed");
					}
					else
					{
						Console.Out.WriteLine("Archive test failure");
					}
				}
			}
			catch(Exception ex)
			{
				Console.Out.WriteLine("Error list files - '{0}'", ex.Message);
			}
		}

		#endregion

		#region Deleting

		/// <summary>
		/// Delete entries from an archive
		/// </summary>
		/// <param name="fileSpecs">The file specs to operate on.</param>
		void Delete(ArrayList fileSpecs)
		{
			var zipFileName = fileSpecs[0] as string;
			if (Path.GetExtension(zipFileName).Length == 0) 
			{
				zipFileName = Path.ChangeExtension(zipFileName, ".zip");
			}

			try
			{
				using (ZipFile zipFile = new ZipFile(zipFileName))
				{
					zipFile.BeginUpdate();
					for ( int i = 1; i < fileSpecs.Count; ++i )
					{
						zipFile.Delete((string)fileSpecs[i]);
					}
					zipFile.CommitUpdate();
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine("Problem deleting files - '{0}'", ex.Message);
			}
		}

		#endregion

		#region Adding

		/// <summary>
		/// Callback for adding a new file.
		/// </summary>
		/// <param name="sender">The scanner calling this delegate.</param>
		/// <param name="args">The event arguments.</param>
		void ProcessFile(object sender, ScanEventArgs args)
		{
			if ( !silent_ )
			{
				Console.WriteLine(args.Name);
			}
			activeZipFile_.Add(args.Name);
		}

		/// <summary>
		/// Callback for adding a new directory.
		/// </summary>
		/// <param name="sender">The scanner calling this delegate.</param>
		/// <param name="args">The event arguments.</param>
		/// <remarks>Directories are only added if they are empty and
		/// the user has specified that empty directories are to be added.</remarks>
		void ProcessDirectory(object sender, DirectoryEventArgs args)
		{
			if ( !args.HasMatchingFiles && addEmptyDirectoryEntries_ )
			{
				activeZipFile_.AddDirectory(args.Name);
			}
		}

		/// <summary>
		/// Add files to an archive
		/// </summary>
		/// <param name="fileSpecs">The specification for files to add.</param>
		void Add(ArrayList fileSpecs)
		{
			var zipFileName = fileSpecs[0] as string;
			if (Path.GetExtension(zipFileName).Length == 0) 
			{
				zipFileName = Path.ChangeExtension(zipFileName, ".zip");
			}

			fileSpecs.RemoveAt(0);

			ZipFile zipFile;

			try
			{
				if ( File.Exists(zipFileName) )
				{
					zipFile = new ZipFile(zipFileName);
				}
				else
				{
					zipFile = ZipFile.Create(zipFileName);
				}

				using (zipFile)
				{
					zipFile.Password = password_;
					zipFile.UseZip64 = useZip64_;
					
					zipFile.BeginUpdate();

					activeZipFile_ = zipFile;

					foreach (string spec in fileSpecs)
					{
						string path = Path.GetDirectoryName(Path.GetFullPath(spec));
						string fileSpec = Path.GetFileName(spec);

						zipFile.NameTransform = new ZipNameTransform(path);

						var scanner = new FileSystemScanner(WildcardToRegex(fileSpec));
						scanner.ProcessFile = new ProcessFileHandler(ProcessFile);
						scanner.ProcessDirectory += new EventHandler<DirectoryEventArgs>(ProcessDirectory);
						scanner.Scan(path, recursive_);
					}
					zipFile.CommitUpdate();
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine("Problem adding to archive - '{0}'", ex.Message);
			}
		}

		#endregion

		#region Class Execute Command

		/// <summary>
		/// Parse command line arguments and 'execute' them.
		/// </summary>		
		void Execute(string[] args) 
		{
			if (SetArgs(args)) 
			{
				if (fileSpecs_.Count == 0) 
				{
					if (!silent_) 
					{
						Console.Out.WriteLine("Nothing to do");
					}
				}
				else 
				{
					switch (operation_) 
					{
						case Operation.List:
							List(fileSpecs_);
							break;
						
						case Operation.Create:
							Create(fileSpecs_);
							break;
						
						case Operation.Extract:
							Extract(fileSpecs_);
							break;

						case Operation.Delete:
							Delete(fileSpecs_);
							break;

						case Operation.Add:
							Add(fileSpecs_);
							break;

						case Operation.Test:
							Test(fileSpecs_);
							break;
					}
				}
			} 
			else 
			{
				if ( !silent_ ) 
				{
					ShowHelp();
				}
			}
		}
		
		#endregion

		#region Support Routines

		byte[] GetBuffer()
		{
			if ( buffer_ == null )
			{
				buffer_ = new byte[bufferSize_];
			}

			return buffer_;
		}
		#endregion

		#region Static support routines

		///<summary>
		/// Calculate compression ratio as a percentage
		/// This wont allow for expansion (ratio > 100) as the resulting strings can get huge easily
		/// </summary>
		static int GetCompressionRatio(long packedSize, long unpackedSize)
		{
			int result = 0;
			if ( (unpackedSize > 0) && (unpackedSize >= packedSize) )
			{
				result = (int) Math.Round((1.0 - ((double)packedSize / (double)unpackedSize)) * 100.0);
			}
			return result;
		}

		/// <summary>
		/// Interpret attributes in conjunction with operatingSystem
		/// </summary>
		/// <param name="operatingSystem">The operating system.</param>
		/// <param name="attributes">The external attributes.</param>
		/// <returns>A string representation of the attributres passed.</returns>
		static string InterpretExternalAttributes(int operatingSystem, int attributes)
		{
			string result = string.Empty;
			if ((operatingSystem == 0) || (operatingSystem == 10))
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

				// Device
				if ((attributes & 0x4) != 0)
					result = result + "d";
				else
					result = result + "-";

				// OS is NTFS
				if ( operatingSystem == 10 )
				{
					// Encrypted
					if ( (attributes & 0x4000) != 0 ) 
					{
						result += "E";
					}
					else 
					{
						result += "-";
					}

					// Not content indexed
					if ( (attributes & 0x2000) != 0 ) 
					{
						result += "n";
					}
					else 
					{
						result += "-";
					}

					// Offline
					if ( (attributes & 0x1000) != 0 ) 
					{
						result += "O";
					}
					else 
					{
						result += "-";
					}

					// Compressed
					if ( (attributes & 0x0800) != 0 ) 
					{
						result += "C";
					}
					else 
					{
						result += "-";
					}

					// Reparse point
					if ( (attributes & 0x0400) != 0 ) 
					{
						result += "R";
					}
					else 
					{
						result += "-";
					}

					// Sparse
					if ( (attributes & 0x0200) != 0 ) 
					{
						result += "S";
					}
					else 
					{
						result += "-";
					}

					// Temporary
					if ( (attributes & 0x0100) != 0 ) 
					{
						result += "T";
					}
					else 
					{
						result += "-";
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Determine if string is numeric [0-9]+
		/// </summary>
		/// <param name="rhs">string to test</param>
		/// <returns>true iff rhs is numeric</returns>
		static bool IsNumeric(string rhs)
		{
			bool result;
			if (!string.IsNullOrEmpty(rhs)) 
			{
				result = true;
				for (int i = 0; i < rhs.Length; ++i) 
				{
					if (!char.IsDigit(rhs[i])) 
					{
						result = false;
						break;
					}
				}
			} 
			else 
			{
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Make external attributes suitable for a <see cref="ZipEntry"/>
		/// </summary>
		/// <param name="info">The <see cref="FileInfo"/> to convert</param>
		/// <returns>Returns External Attributes for Zip use</returns>
		static int MakeExternalAttributes(FileInfo info)
		{
			return (int)info.Attributes;
		}

		/// <summary>
		/// Convert a wildcard expression to a regular expression
		/// </summary>
		/// <param name="wildcard">The wildcard expression to convert.</param>
		/// <returns>A regular expression representing the converted wildcard expression.</returns>
		static string WildcardToRegex(string wildcard)
		{
			int dotPos = wildcard.IndexOf('.');
			bool dotted = (dotPos >= 0) && (dotPos < wildcard.Length - 1);
			string converted = wildcard.Replace(".", @"\.");
			converted = converted.Replace("?", ".");
			converted = converted.Replace("*", ".*");
			converted = converted.Replace("(", @"\(");
			converted = converted.Replace(")", @"\)");
			if ( dotted )
			{
				converted += "$";
			}

			return converted;
		}

		#endregion

		#region Main

		/// <summary>
		/// Entry point for program, creates archiver and runs it
		/// </summary>
		/// <param name="args">
		/// Command line argument to process
		/// </param>
		public static void Main(string[] args) 
		{
			var zf = new ZipFileArchiver();
			zf.Execute(args);
		}

		#endregion

		#region Instance Fields

		/// <summary>
		/// Has user already seen help output?
		/// </summary>
		bool seenHelp_;
		
		/// <summary>
		/// File specifications possibly with wildcards from command line
		/// </summary>
		ArrayList fileSpecs_ = new ArrayList();

	    /// <summary>
		/// Create entries for directories with no files
		/// </summary>
		bool addEmptyDirectoryEntries_;
		
		/// <summary>
		/// Apply operations recursively
		/// </summary>
		bool recursive_;

		/// <summary>
		/// Operate silently
		/// </summary>
		bool silent_;
		
		/// <summary>
		/// Restore file date and time to that stored in zip file on extraction
		/// </summary>
		bool restoreDateTime_;
		
		/// <summary>
		/// Overwrite files handling
		/// </summary>
		Overwrite overwriteFiles = Overwrite.Prompt;

		/// <summary>
		/// Optional password for archive
		/// </summary>
		string password_;
		
		/// <summary>
		/// Where things will go when decompressed.
		/// </summary>
		string targetOutputDirectory_;

		/// <summary>
		/// What to do based on parsed command line arguments
		/// </summary>
		Operation operation_ = Operation.List;

		/// <summary>
		/// Flag whose value is true if data should be tested; false if it should not.
		/// </summary>
		bool testData_;

        /// <summary>
        /// Dont extract or compress just display what would be done - testing
        /// </summary>
	    private bool dryRun_;

		/// <summary>
		/// The currently active <see cref="ZipFile"/>.
		/// </summary>
		/// <remarks>Used for callbacks/delegates</remarks>
		ZipFile activeZipFile_;

		/// <summary>
		/// Buffer used during some operations
		/// </summary>
		byte[] buffer_;

		/// <summary>
		/// The size of buffer to provide. <see cref="GetBuffer"></see>
		/// </summary>
		int bufferSize_ = 4096;
		
		/// <summary>
		/// The Zip64 extension use to apply.
		/// </summary>
		UseZip64 useZip64_ = UseZip64.Off;
		#endregion
	}
}
