using System;
#if NET45
using System.Runtime.Serialization;
#endif

namespace ICSharpCode.SharpZipLib.Lzw
{
    /// <summary>
    /// LzwException represents exceptions specific to LZW classes and code.
    /// </summary>
#if NET45
    [Serializable]
#endif
	public class LzwException : SharpZipBaseException
	{
#if NET45
        /// <summary>
        /// Deserialization constructor 
        /// </summary>
        /// <param name="info"><see cref="SerializationInfo"/> for this constructor</param>
        /// <param name="context"><see cref="StreamingContext"/> for this constructor</param>
        protected LzwException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

#endif

		/// <summary>
		/// Initialise a new instance of <see cref="LzwException" />.
		/// </summary>
		public LzwException()
		{
		}

		/// <summary>
		/// Initialise a new instance of <see cref="LzwException" /> with its message string.
		/// </summary>
		/// <param name="message">A <see cref="string"/> that describes the error.</param>
		public LzwException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initialise a new instance of <see cref="LzwException" />.
		/// </summary>
		/// <param name="message">A <see cref="string"/> that describes the error.</param>
		/// <param name="innerException">The <see cref="Exception"/> that caused this exception.</param>
		public LzwException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
