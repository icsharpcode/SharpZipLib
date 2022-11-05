using System;
using System.Security.Cryptography;

namespace ICSharpCode.SharpZipLib.Encryption
{
	/// <summary>
	/// Transforms stream using AES in CTR mode
	/// </summary>
	internal class ZipAESTransform : ICryptoTransform
	{
		private const int PWD_VER_LENGTH = 2;

		// WinZip use iteration count of 1000 for PBKDF2 key generation
		private const int KEY_ROUNDS = 1000;

		// For 128-bit AES (16 bytes) the encryption is implemented as expected.
		// For 256-bit AES (32 bytes) WinZip do full 256 bit AES of the nonce to create the encryption
		// block but use only the first 16 bytes of it, and discard the second half.
		private const int ENCRYPT_BLOCK = 16;

		private int _blockSize;
		private readonly ICryptoTransform _encryptor;
		private readonly byte[] _counterNonce;
		private byte[] _encryptBuffer;
		private int _encrPos;
		private byte[] _pwdVerifier;
		private IncrementalHash _hmacsha1;
		private byte[] _authCode = null;

		private bool _writeMode;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="key">Password string</param>
		/// <param name="saltBytes">Random bytes, length depends on encryption strength.
		/// 128 bits = 8 bytes, 192 bits = 12 bytes, 256 bits = 16 bytes.</param>
		/// <param name="blockSize">The encryption strength, in bytes eg 16 for 128 bits.</param>
		/// <param name="writeMode">True when creating a zip, false when reading. For the AuthCode.</param>
		///
		public ZipAESTransform(string key, byte[] saltBytes, int blockSize, bool writeMode)
		{
			if (blockSize != 16 && blockSize != 32) // 24 valid for AES but not supported by Winzip
				throw new Exception("Invalid blocksize " + blockSize + ". Must be 16 or 32.");
			if (saltBytes.Length != blockSize / 2)
				throw new Exception("Invalid salt len. Must be " + blockSize / 2 + " for blocksize " + blockSize);
			// initialise the encryption buffer and buffer pos
			_blockSize = blockSize;
			_encryptBuffer = new byte[_blockSize];
			_encrPos = ENCRYPT_BLOCK;

			// Performs the equivalent of derive_key in Dr Brian Gladman's pwd2key.c
#if NET472_OR_GREATER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
			var pdb = new Rfc2898DeriveBytes(key, saltBytes, KEY_ROUNDS, HashAlgorithmName.SHA1);
#else
			var pdb = new Rfc2898DeriveBytes(key, saltBytes, KEY_ROUNDS);
#endif
			var rm = Aes.Create();
			rm.Mode = CipherMode.ECB;           // No feedback from cipher for CTR mode
			_counterNonce = new byte[_blockSize];
			byte[] key1bytes = pdb.GetBytes(_blockSize);
			byte[] key2bytes = pdb.GetBytes(_blockSize);

			// Use empty IV for AES
			_encryptor = rm.CreateEncryptor(key1bytes, new byte[16]);
			_pwdVerifier = pdb.GetBytes(PWD_VER_LENGTH);
			//
			_hmacsha1 = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA1, key2bytes);
			_writeMode = writeMode;
		}

		/// <summary>
		/// Implement the ICryptoTransform method.
		/// </summary>
		public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
		{
			// Pass the data stream to the hash algorithm for generating the Auth Code.
			// This does not change the inputBuffer. Do this before decryption for read mode.
			if (!_writeMode)
			{
				_hmacsha1.AppendData(inputBuffer, inputOffset, inputCount);
			}
			// Encrypt with AES in CTR mode. Regards to Dr Brian Gladman for this.
			int ix = 0;
			while (ix < inputCount)
			{
				if (_encrPos == ENCRYPT_BLOCK)
				{
					/* increment encryption nonce   */
					int j = 0;
					while (++_counterNonce[j] == 0)
					{
						++j;
					}
					/* encrypt the nonce to form next xor buffer    */
					_encryptor.TransformBlock(_counterNonce, 0, _blockSize, _encryptBuffer, 0);
					_encrPos = 0;
				}
				outputBuffer[ix + outputOffset] = (byte)(inputBuffer[ix + inputOffset] ^ _encryptBuffer[_encrPos++]);
				//
				ix++;
			}
			if (_writeMode)
			{
				// This does not change the buffer.
				_hmacsha1.AppendData(outputBuffer, outputOffset, inputCount);
			}
			return inputCount;
		}

		/// <summary>
		/// Returns the 2 byte password verifier
		/// </summary>
		public byte[] PwdVerifier => _pwdVerifier;

		/// <summary>
		/// Returns the 10 byte AUTH CODE to be checked or appended immediately following the AES data stream.
		/// </summary>
		public byte[] GetAuthCode() => _authCode ?? (_authCode = _hmacsha1.GetHashAndReset());

		#region ICryptoTransform Members

		/// <summary>
		/// Transform final block and read auth code
		/// </summary>
		public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
		{
			var buffer = Array.Empty<byte>();

			// FIXME: When used together with `ZipAESStream`, the final block handling is done inside of it instead
			// This should not be necessary anymore, and the entire `ZipAESStream` class should be replaced with a plain `CryptoStream`
			if (inputCount != 0) {
				if (inputCount > ZipAESStream.AUTH_CODE_LENGTH)
				{
					// At least one byte of data is preceeding the auth code
					int finalBlock = inputCount - ZipAESStream.AUTH_CODE_LENGTH;
					buffer = new byte[finalBlock];
					TransformBlock(inputBuffer, inputOffset, finalBlock, buffer, 0);
				}
				else if (inputCount < ZipAESStream.AUTH_CODE_LENGTH)
					throw new Zip.ZipException("Auth code missing from input stream");

				// Read the authcode from the last 10 bytes
				_authCode = _hmacsha1.GetHashAndReset();
			}
			

			return buffer;
		}

		/// <summary>
		/// Gets the size of the input data blocks in bytes.
		/// </summary>
		public int InputBlockSize => _blockSize;

		/// <summary>
		/// Gets the size of the output data blocks in bytes.
		/// </summary>
		public int OutputBlockSize => _blockSize;

		/// <summary>
		/// Gets a value indicating whether multiple blocks can be transformed.
		/// </summary>
		public bool CanTransformMultipleBlocks => true;

		/// <summary>
		/// Gets a value indicating whether the current transform can be reused.
		/// </summary>
		public bool CanReuseTransform => true;

		/// <summary>
		/// Cleanup internal state.
		/// </summary>
		public void Dispose() => _encryptor.Dispose();

		#endregion ICryptoTransform Members
	}
}
