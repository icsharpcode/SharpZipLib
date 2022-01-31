using System;
using System.IO;
using System.Security.Cryptography;

namespace ICSharpCode.SharpZipLib.Encryption
{
	/// <summary>
	/// Encrypts AES ZIP entries.
	/// </summary>
	/// <remarks>
	/// Based on information from http://www.winzip.com/aes_info.htm
	/// and http://www.gladman.me.uk/cryptography_technology/fileencrypt/
	/// </remarks>
	internal class ZipAESEncryptionStream : Stream
	{
		// The transform to use for encryption.
		private ZipAESTransform transform;

		// The output stream to write the encrypted data to.
		private readonly Stream outputStream;

		// Static to help ensure that multiple files within a zip will get different random salt
		private static readonly RandomNumberGenerator _aesRnd = RandomNumberGenerator.Create();

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="stream">The stream on which to perform the cryptographic transformation.</param>
		/// <param name="rawPassword">The password used to encrypt the entry.</param>
		/// <param name="saltLength">The length of the salt to use.</param>
		/// <param name="blockSize">The block size to use for transforming.</param>
		public ZipAESEncryptionStream(Stream stream, string rawPassword, int saltLength, int blockSize)
		{
			// Set up stream.
			this.outputStream = stream;

			// Initialise the encryption transform.
			var salt = new byte[saltLength];

			// Salt needs to be cryptographically random, and unique per file
			_aesRnd.GetBytes(salt);
			
			this.transform = new ZipAESTransform(rawPassword, salt, blockSize, true);

			// File format for AES:
			// Size (bytes)   Content
			// ------------   -------
			// Variable       Salt value
			// 2              Password verification value
			// Variable       Encrypted file data
			// 10             Authentication code
			//
			// Value in the "compressed size" fields of the local file header and the central directory entry
			// is the total size of all the items listed above. In other words, it is the total size of the
			// salt value, password verification value, encrypted data, and authentication code.
			var pwdVerifier = this.transform.PwdVerifier;
			this.outputStream.Write(salt, 0, salt.Length);
			this.outputStream.Write(pwdVerifier, 0, pwdVerifier.Length);
		}

		// This stream is write only.
		public override bool CanRead => false;

		// We only support writing - no seeking about.
		public override bool CanSeek => false;

		// Supports writing for encrypting.
		public override bool CanWrite => true;

		// We don't track this.
		public override long Length => throw new NotImplementedException();

		// We don't track this, or support seeking.
		public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		/// <summary>
		/// When the stream is disposed, write the final blocks and AES Authentication code
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (this.transform != null)
			{
				this.WriteAuthCode();
				this.transform.Dispose();
				this.transform = null;
			}
		}

		// <inheritdoc/>
		public override void Flush()
		{
			this.outputStream.Flush();
		}

		// <inheritdoc/>
		public override int Read(byte[] buffer, int offset, int count)
		{
			// ZipAESEncryptionStream is only used for encryption.
			throw new NotImplementedException();
		}

		// <inheritdoc/>
		public override long Seek(long offset, SeekOrigin origin)
		{
			// We don't support seeking.
			throw new NotImplementedException();
		}

		// <inheritdoc/>
		public override void SetLength(long value)
		{
			// We don't support setting the length.
			throw new NotImplementedException();
		}

		// <inheritdoc/>
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (count == 0)
			{
				return;
			}

			var outputBuffer = new byte[count];
			var outputCount = this.transform.TransformBlock(buffer, offset, count, outputBuffer, 0);
			this.outputStream.Write(outputBuffer, 0, outputCount);
		}

		// Write the auth code for the encrypted data to the output stream
		private void WriteAuthCode()
		{
			// Transform the final block?

			// Write the AES Authentication Code (a hash of the compressed and encrypted data)
			var authCode = this.transform.GetAuthCode();
			this.outputStream.Write(authCode, 0, 10);
			this.outputStream.Flush();
		}
	}
}
