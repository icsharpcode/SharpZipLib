using System;
using System.Runtime.Serialization;

namespace ICSharpCode.SharpZipLib.BZip2
{
	/// <summary>
	/// BZip2Exception represents exceptions specific to BZip2 classes and code.
	/// </summary>
	[Serializable]
	public class BZip2Exception : SharpZipBaseException
	{
		/// <summary>
		/// Deserialization constructor 
		/// </summary>
		/// <param name="info"><see cref="SerializationInfo"/> for this constructor</param>
		/// <param name="context"><see cref="StreamingContext"/> for this constructor</param>
		protected BZip2Exception(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		/// <summary>
		/// Initialise a new instance of <see cref="BZip2Exception" />.
		/// </summary>
		public BZip2Exception()
		{
		}

		/// <summary>
		/// Initialise a new instance of <see cref="BZip2Exception" /> with its message string.
		/// </summary>
		/// <param name="message">A <see cref="string"/> that describes the error.</param>
		public BZip2Exception(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initialise a new instance of <see cref="BZip2Exception" />.
		/// </summary>
		/// <param name="message">A <see cref="string"/> that describes the error.</param>
		/// <param name="innerException">The <see cref="Exception"/> that caused this exception.</param>
		public BZip2Exception(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
