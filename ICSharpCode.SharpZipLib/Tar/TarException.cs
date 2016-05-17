using System;
using System.Runtime.Serialization;

namespace ICSharpCode.SharpZipLib.Tar
{
	/// <summary>
	/// TarException represents exceptions specific to Tar classes and code.
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
		/// Initialise a new instance of <see cref="TarException" />.
		/// </summary>
		public TarException()
		{
		}

		/// <summary>
		/// Initialise a new instance of <see cref="TarException" /> with its message string.
		/// </summary>
		/// <param name="message">A <see cref="string"/> that describes the error.</param>
		public TarException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initialise a new instance of <see cref="TarException" />.
		/// </summary>
		/// <param name="message">A <see cref="string"/> that describes the error.</param>
		/// <param name="innerException">The <see cref="Exception"/> that caused this exception.</param>
		public TarException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
