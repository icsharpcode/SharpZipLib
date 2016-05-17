using System;
using System.Runtime.Serialization;

namespace ICSharpCode.SharpZipLib.Tar
{
	/// <summary>
	/// TarExceptions are used for exceptions specific to tar classes and code.	
	/// </summary>
	[Serializable]
	public class TarException : SharpZipBaseException
	{
		/// <summary>
		/// Deserialization constructor 
		/// </summary>
		/// <param name="info"><see cref="SerializationInfo"/> for this constructor</param>
		/// <param name="context"><see cref="StreamingContext"/> for this constructor</param>
		protected TarException(SerializationInfo info, StreamingContext context)
			: base(info, context)

		{
		}

		/// <summary>
		/// Initialises a new instance of the TarException class.
		/// </summary>
		public TarException()
		{
		}

		/// <summary>
		/// Initialises a new instance of the TarException class with a specified message.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public TarException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message">A message describing the error.</param>
		/// <param name="exception">The exception that is the cause of the current exception.</param>
		public TarException(string message, Exception exception)
			: base(message, exception)
		{
		}
	}
}
