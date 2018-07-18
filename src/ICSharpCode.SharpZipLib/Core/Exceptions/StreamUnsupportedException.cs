using System;

namespace ICSharpCode.SharpZipLib
{
	/// <summary>
	/// Indicates that the input stream could not decoded due to known library incompability or missing features
	/// </summary>
	public class StreamUnsupportedException : StreamDecodingException
	{
		private const string GenericMessage = "Input stream is in a unsupported format";

		/// <summary>
		/// Initializes a new instance of the StreamUnsupportedException with a generic message
		/// </summary>
		public StreamUnsupportedException() : base(GenericMessage) { }

		/// <summary>
		/// Initializes a new instance of the StreamUnsupportedException class with a specified error message.
		/// </summary>
		/// <param name="message">A message describing the exception.</param>
		public StreamUnsupportedException(string message) : base(message) { }


		/// <summary>
		/// Initializes a new instance of the StreamUnsupportedException class with a specified
		/// error message and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="message">A message describing the exception.</param>
		/// <param name="innerException">The inner exception</param>
		public StreamUnsupportedException(string message, Exception innerException) : base(message, innerException) { }

	}
}
