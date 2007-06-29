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

using ICSharpCode.SharpZipLib.Zip;

class MainClass
{
	
	public static void Main(string[] args)
	{
		// Perform some simple parameter checking.  More could be done
		// like checking the target file name is ok, disk space, and lots
		// of other things, but for a demo this covers some obvious traps.
		if ( args.Length < 2 ) {
			Console.WriteLine("Usage: CreateZipFile Path ZipFile");
			return;
		}

		if ( !Directory.Exists(args[0]) ) {
			Console.WriteLine("Cannot find directory '{0}'", args[0]);
			return;
		}

		try
		{
			// Depending on the directory this could be very large and would require more attention
			// in a commercial package.
			string[] filenames = Directory.GetFiles(args[0]);
			
			// 'using' statements gaurantee the stream is closed properly which is a big source
			// of problems otherwise.  Its exception safe as well which is great.
			using (ZipOutputStream s = new ZipOutputStream(File.Create(args[1]))) {
			
				s.SetLevel(9); // 0 - store only to 9 - means best compression
		
				byte[] buffer = new byte[4096];
				
				foreach (string file in filenames) {
					
					// Using GetFileName makes the result compatible with XP
					// as the resulting path is not absolute.
					ZipEntry entry = new ZipEntry(Path.GetFileName(file));
					
					// Setup the entry data as required.
					
					// Crc and size are handled by the library for seakable streams
					// so no need to do them here.

					// Could also use the last write time or similar for the file.
					entry.DateTime = DateTime.Now;
					s.PutNextEntry(entry);
					
					using ( FileStream fs = File.OpenRead(file) ) {
		
						// Using a fixed size buffer here makes no noticeable difference for output
						// but keeps a lid on memory usage.
						int sourceBytes;
						do {
							sourceBytes = fs.Read(buffer, 0, buffer.Length);
							s.Write(buffer, 0, sourceBytes);
						} while ( sourceBytes > 0 );
					}
				}
				
				// Finish/Close arent needed strictly as the using statement does this automatically
				
				// Finish is important to ensure trailing information for a Zip file is appended.  Without this
				// the created file would be invalid.
				s.Finish();
				
				// Close is important to wrap things up and unlock the file.
				s.Close();
			}
		}
		catch(Exception ex)
		{
			Console.WriteLine("Exception during processing {0}", ex);
			
			// No need to rethrow the exception as for our purposes its handled.
		}
	}
}
