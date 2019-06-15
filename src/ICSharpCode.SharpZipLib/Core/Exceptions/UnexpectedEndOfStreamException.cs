﻿using System;

namespace ICSharpCode.SharpZipLib
{
	/// <summary>
	/// Indicates that the input stream could not decoded due to the stream ending before enough data had been provided
	/// </summary>
	public class UnexpectedEndOfStreamException : StreamDecodingException
	{
		private const string GenericMessage = "Input stream ended unexpectedly";

		/// <summary>
		/// Initializes a new instance of the UnexpectedEndOfStreamException with a generic message
		/// </summary>
		public UnexpectedEndOfStreamException() : base(GenericMessage) { }

		/// <summary>
		/// Initializes a new instance of the UnexpectedEndOfStreamException class with a specified error message.
		/// </summary>
		/// <param name="message">A message describing the exception.</param>
		public UnexpectedEndOfStreamException(string message) : base(message) { }

		/// <summary>
		/// Initializes a new instance of the UnexpectedEndOfStreamException class with a specified
		/// error message and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="message">A message describing the exception.</param>
		/// <param name="innerException">The inner exception</param>
		public UnexpectedEndOfStreamException(string message, Exception innerException) : base(message, innerException) { }
	}
}
