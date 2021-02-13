using System;
using System.Collections.Generic;
using System.IO;
using ArchiveDiag;
using CommandLine;
using ConLib.Console;
using ConLib.HTML;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace ICSharpCode.SharpZipLib.ArchiveDiag
{
	public class Program
	{
		public class Options
		{
			[Value(0, HelpText = "Input filename")]
			public string Filename { get; set; }

			[Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages")]
			public bool Verbose { get; set; }

			[Option('q', "quiet")]
			public bool Quiet { get; set; }

			[Option('h', "no-html-report")]
			public bool SkipHtmlReport { get; set; }

			[Option('e', "eval", HelpText = "Run the input file as a C# script and create a report from the resulting stream")]
			public bool Evaluate { get; set; }
		}


		static int Main(string[] args)
        {

	        Parser.Default.ParseArguments<Options>(args)
		        .WithParsed<Options>(o =>
		        {
			        Stream inputStream;
			        var outputFile = $"{o.Filename}.html";
			        var inputFile = Path.GetFileName(o.Filename);

					if (o.Evaluate)
					{
						inputFile = $"script:{inputFile}";
						try
						{
							using var fs = File.OpenRead(o.Filename);
							using var sr = new StreamReader(fs);

							var opts = ScriptOptions.Default
								.WithFilePath(o.Filename)
								.WithImports(
									"System",
									"System.IO",
									"System.Text",
									"System.Collections.Generic",
									"ICSharpCode.SharpZipLib", 
									"ICSharpCode.SharpZipLib.Core",
									"ICSharpCode.SharpZipLib.Zip")
								.WithReferences(typeof(ZipOutputStream).Assembly);

							var task =
								CSharpScript.EvaluateAsync<byte[]>(sr.ReadToEnd(), opts);
							if (task.Wait(TimeSpan.FromSeconds(30)))
							{
								inputStream = new MemoryStream(task.Result);
							}
							else throw new TimeoutException("Script evaluation timed out");
						}
						catch (Exception x)
						{
							Console.WriteLine($"Failed to evaluate input script: {x}");
							return;
						}

			        }
			        else
			        {
						inputStream = File.OpenRead(o.Filename);

					}

			        using var outputStream = File.Open(outputFile, FileMode.Create);
			        using var htmlWriter = new HTMLWriter(outputStream);

					new TarArchiveDiagRunner(inputStream, inputFile).Run(new ConsoleWriter(), htmlWriter);



				});

	        return 0;
        }

		static void Lala()
		{
			var dataBytes = new byte[] { 0x34, 0x68, 0xf2, 0x8d };

			using var ms = new MemoryStream(dataBytes);
			using var fs = File.Create("output.zip");
			using var zip = new ZipOutputStream(fs);
			zip.PutNextEntry(new ZipEntry("content-file.bin"));
			ms.WriteTo(zip);

		}

		public void UseCreateZipFileFromData()
		{
			var dataBytes = new byte[] { 0x49, 0xe2, 0x9d, 0xa4, 0x5a, 0x49, 0x50 };

			CreateZipFileFromData(File.Create("output.zip"), dataBytes);

			using (var ms = new MemoryStream())
			{
				CreateZipFileFromData(ms, dataBytes, closeStream: false, zipEntryName: "data.bin");
				var outputBytes = ms.ToArray();
			}
		}

		public void CreateZipFileFromData(Stream outputStream, byte[] inputData, bool closeStream = true, string zipEntryName = "-")
		{
			using (var zipStream = new ZipOutputStream(outputStream))
			{
				// Stop ZipStream.Dispose() from also Closing the underlying stream.
				zipStream.IsStreamOwner = closeStream;

				zipStream.PutNextEntry(new ZipEntry(zipEntryName));
				zipStream.Write(inputData);
			}
		}

	}



	static class ZipExtraDataExtensions
    {
	    public static IEnumerable<(ExtraDataType, Range)> EnumerateTags(this ZipExtraData zed)
	    {
		    var index = 0;

		    var data = zed.GetEntryData();

			while (index < data.Length - 3)
		    {
			    var tag = data[index++] + (data[index++] << 8);
			    var length = data[index++] + (data[index++] << 8);
			    yield return ((ExtraDataType)tag, new Range(index, index+length));
			    index += length;
		    }

	    }
    }


	internal static class StringExtensions
	{
		internal static string Ellipsis(this string source, int maxLength)
			=> source.Length > maxLength - 3
				? source.Substring(0, maxLength - 3) + "..."
				: source;
	}

}
