/*
 * Created by SharpDevelop.
 * User: JohnR
 * Date: 23/08/2004
 * Time: 12:04 a.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using ICSharpCode.SharpZipLib;

namespace ICSharpCode.SharpZipLib.Tar {
	/// <summary>
	/// TarExceptions are used for exceptions specific to tar classes and code.	
	/// </summary>
	public class TarException : SharpZipBaseException
	{
		public TarException()
		{
		}
		
		public TarException(string message) : base(message)
		{
		}
	}
}
