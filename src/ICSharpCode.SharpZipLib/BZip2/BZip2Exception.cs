using System;
using System.IO;

namespace ICSharpCode.SharpZipLib.BZip2
{

	/**
	 * Indicates that a data format error was encountered while attempting to decode bzip2 data
	 */
	public class BZip2Exception: Exception
	{

		/**
		 * @param reason The exception's reason string
		 */
		public BZip2Exception(string message) : base(message)
		{
		}

	}
}
