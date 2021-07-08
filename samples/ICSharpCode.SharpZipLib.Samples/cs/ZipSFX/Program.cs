using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipSFX
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
	        // If launched with the arguments "--sfx", an input zip file, and an output exe file,
	        // create a self-extracting binary using a copy of the current binary with the zip-file appended
	        if (args.Length > 2 && args[0] == "--sfx")
			{
				using (var outStream = File.Open(args[2], FileMode.Create))
				{
					using (var exeStream = File.OpenRead(Application.ExecutablePath))
					{
						exeStream.CopyTo(outStream);
					}

					using (var zipStream = File.OpenRead(args[1]))
					{
						zipStream.CopyTo(outStream);
					}

				}

#if DEBUG
				Process.Start(args[2]);
#endif
			}
			else
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(new SfxForm());
			}
        }
    }
}
