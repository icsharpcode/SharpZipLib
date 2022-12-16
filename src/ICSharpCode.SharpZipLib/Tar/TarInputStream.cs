using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Core;

namespace ICSharpCode.SharpZipLib.Tar
{
	/// <summary>
	/// The TarInputStream reads a UNIX tar archive as an InputStream.
	/// methods are provided to position at each successive entry in
	/// the archive, and the read each entry as a normal input stream
	/// using read().
	/// </summary>
	public class TarInputStream : Stream
	{
		#region Constructors

		/// <summary>
		/// Construct a TarInputStream with default block factor
		/// </summary>
		/// <param name="inputStream">stream to source data from</param>
		[Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
		public TarInputStream(Stream inputStream)
			: this(inputStream, TarBuffer.DefaultBlockFactor, null)
		{
		}

		/// <summary>
		/// Construct a TarInputStream with default block factor
		/// </summary>
		/// <param name="inputStream">stream to source data from</param>
		/// <param name="nameEncoding">The <see cref="Encoding"/> used for the Name fields, or null for ASCII only</param>
		public TarInputStream(Stream inputStream, Encoding nameEncoding)
			: this(inputStream, TarBuffer.DefaultBlockFactor, nameEncoding)
		{
		}

		/// <summary>
		/// Construct a TarInputStream with user specified block factor
		/// </summary>
		/// <param name="inputStream">stream to source data from</param>
		/// <param name="blockFactor">block factor to apply to archive</param>
		[Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
		public TarInputStream(Stream inputStream, int blockFactor)
		{
			this.inputStream = inputStream;
			tarBuffer = TarBuffer.CreateInputTarBuffer(inputStream, blockFactor);
			encoding = null;
		}

		/// <summary>
		/// Construct a TarInputStream with user specified block factor
		/// </summary>
		/// <param name="inputStream">stream to source data from</param>
		/// <param name="blockFactor">block factor to apply to archive</param>
		/// <param name="nameEncoding">The <see cref="Encoding"/> used for the Name fields, or null for ASCII only</param>
		public TarInputStream(Stream inputStream, int blockFactor, Encoding nameEncoding)
		{
			this.inputStream = inputStream;
			tarBuffer = TarBuffer.CreateInputTarBuffer(inputStream, blockFactor);
			encoding = nameEncoding;
		}

		#endregion Constructors

		/// <summary>
		/// Gets or sets a flag indicating ownership of underlying stream.
		/// When the flag is true <see cref="Stream.Dispose()" /> will close the underlying stream also.
		/// </summary>
		/// <remarks>The default value is true.</remarks>
		public bool IsStreamOwner
		{
			get { return tarBuffer.IsStreamOwner; }
			set { tarBuffer.IsStreamOwner = value; }
		}

		#region Stream Overrides

		/// <summary>
		/// Gets a value indicating whether the current stream supports reading
		/// </summary>
		public override bool CanRead
		{
			get { return inputStream.CanRead; }
		}

		/// <summary>
		/// Gets a value indicating whether the current stream supports seeking
		/// This property always returns false.
		/// </summary>
		public override bool CanSeek
		{
			get { return false; }
		}

		/// <summary>
		/// Gets a value indicating if the stream supports writing.
		/// This property always returns false.
		/// </summary>
		public override bool CanWrite
		{
			get { return false; }
		}

		/// <summary>
		/// The length in bytes of the stream
		/// </summary>
		public override long Length
		{
			get { return inputStream.Length; }
		}

		/// <summary>
		/// Gets or sets the position within the stream.
		/// Setting the Position is not supported and throws a NotSupportedExceptionNotSupportedException
		/// </summary>
		/// <exception cref="NotSupportedException">Any attempt to set position</exception>
		public override long Position
		{
			get { return inputStream.Position; }
			set { throw new NotSupportedException("TarInputStream Seek not supported"); }
		}

		/// <summary>
		/// Flushes the baseInputStream
		/// </summary>
		public override void Flush()
		{
			inputStream.Flush();
		}

		/// <summary>
		/// Flushes the baseInputStream
		/// </summary>
		/// <param name="cancellationToken"></param>
		public override async Task FlushAsync(CancellationToken cancellationToken)
		{
			await inputStream.FlushAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Set the streams position.  This operation is not supported and will throw a NotSupportedException
		/// </summary>
		/// <param name="offset">The offset relative to the origin to seek to.</param>
		/// <param name="origin">The <see cref="SeekOrigin"/> to start seeking from.</param>
		/// <returns>The new position in the stream.</returns>
		/// <exception cref="NotSupportedException">Any access</exception>
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException("TarInputStream Seek not supported");
		}

		/// <summary>
		/// Sets the length of the stream
		/// This operation is not supported and will throw a NotSupportedException
		/// </summary>
		/// <param name="value">The new stream length.</param>
		/// <exception cref="NotSupportedException">Any access</exception>
		public override void SetLength(long value)
		{
			throw new NotSupportedException("TarInputStream SetLength not supported");
		}

		/// <summary>
		/// Writes a block of bytes to this stream using data from a buffer.
		/// This operation is not supported and will throw a NotSupportedException
		/// </summary>
		/// <param name="buffer">The buffer containing bytes to write.</param>
		/// <param name="offset">The offset in the buffer of the frist byte to write.</param>
		/// <param name="count">The number of bytes to write.</param>
		/// <exception cref="NotSupportedException">Any access</exception>
		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException("TarInputStream Write not supported");
		}

		/// <summary>
		/// Writes a byte to the current position in the file stream.
		/// This operation is not supported and will throw a NotSupportedException
		/// </summary>
		/// <param name="value">The byte value to write.</param>
		/// <exception cref="NotSupportedException">Any access</exception>
		public override void WriteByte(byte value)
		{
			throw new NotSupportedException("TarInputStream WriteByte not supported");
		}

		/// <summary>
		/// Reads a byte from the current tar archive entry.
		/// </summary>
		/// <returns>A byte cast to an int; -1 if the at the end of the stream.</returns>
		public override int ReadByte()
		{
			var oneByteBuffer = ArrayPool<byte>.Shared.Rent(1);
			var num = Read(oneByteBuffer, 0, 1);
			if (num <= 0)
			{
				// return -1 to indicate that no byte was read.
				return -1;
			}

			var result = oneByteBuffer[0];
			ArrayPool<byte>.Shared.Return(oneByteBuffer);
			return result;
		}


		/// <summary>
		/// Reads bytes from the current tar archive entry.
		/// 
		/// This method is aware of the boundaries of the current
		/// entry in the archive and will deal with them appropriately
		/// </summary>
		/// <param name="buffer">
		/// The buffer into which to place bytes read.
		/// </param>
		/// <param name="offset">
		/// The offset at which to place bytes read.
		/// </param>
		/// <param name="count">
		/// The number of bytes to read.
		/// </param>
		/// <param name="cancellationToken"></param>
		/// <returns>
		/// The number of bytes read, or 0 at end of stream/EOF.
		/// </returns>
		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return ReadAsync(buffer.AsMemory().Slice(offset, count), cancellationToken, true).AsTask();
		}

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
		/// <summary>
		/// Reads bytes from the current tar archive entry.
		/// 
		/// This method is aware of the boundaries of the current
		/// entry in the archive and will deal with them appropriately
		/// </summary>
		/// <param name="buffer">
		/// The buffer into which to place bytes read.
		/// </param>
		/// <param name="cancellationToken"></param>
		/// <returns>
		/// The number of bytes read, or 0 at end of stream/EOF.
		/// </returns>
		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken =
			new CancellationToken())
		{
			return ReadAsync(buffer, cancellationToken, true);
		}
#endif

		/// <summary>
		/// Reads bytes from the current tar archive entry.
		///
		/// This method is aware of the boundaries of the current
		/// entry in the archive and will deal with them appropriately
		/// </summary>
		/// <param name="buffer">
		/// The buffer into which to place bytes read.
		/// </param>
		/// <param name="offset">
		/// The offset at which to place bytes read.
		/// </param>
		/// <param name="count">
		/// The number of bytes to read.
		/// </param>
		/// <returns>
		/// The number of bytes read, or 0 at end of stream/EOF.
		/// </returns>
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}

			return ReadAsync(buffer.AsMemory().Slice(offset, count), CancellationToken.None, false).GetAwaiter()
				.GetResult();
		}

		private async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct, bool isAsync)
		{
			int offset = 0;
			int totalRead = 0;

			if (entryOffset >= entrySize)
			{
				return 0;
			}

			long numToRead = buffer.Length;

			if ((numToRead + entryOffset) > entrySize)
			{
				numToRead = entrySize - entryOffset;
			}

			if (readBuffer != null)
			{
				int sz = (numToRead > readBuffer.Memory.Length) ? readBuffer.Memory.Length : (int)numToRead;

				readBuffer.Memory.Slice(0, sz).CopyTo(buffer.Slice(offset, sz));

				if (sz >= readBuffer.Memory.Length)
				{
					readBuffer.Dispose();
					readBuffer = null;
				}
				else
				{
					int newLen = readBuffer.Memory.Length - sz;
					var newBuf = ExactMemoryPool<byte>.Shared.Rent(newLen);
					readBuffer.Memory.Slice(sz, newLen).CopyTo(newBuf.Memory);
					readBuffer.Dispose();
					
					readBuffer = newBuf;
				}

				totalRead += sz;
				numToRead -= sz;
				offset += sz;
			}

			var recLen = TarBuffer.BlockSize;
			var recBuf = ArrayPool<byte>.Shared.Rent(recLen);

			while (numToRead > 0)
			{
				await tarBuffer.ReadBlockIntAsync(recBuf, ct, isAsync).ConfigureAwait(false);

				var sz = (int)numToRead;

				if (recLen > sz)
				{
					recBuf.AsSpan().Slice(0, sz).CopyTo(buffer.Slice(offset, sz).Span);
					readBuffer?.Dispose();

					readBuffer = ExactMemoryPool<byte>.Shared.Rent(recLen - sz);
					recBuf.AsSpan().Slice(sz, recLen - sz).CopyTo(readBuffer.Memory.Span);
				}
				else
				{
					sz = recLen;
					recBuf.AsSpan().CopyTo(buffer.Slice(offset, recLen).Span);
				}

				totalRead += sz;
				numToRead -= sz;
				offset += sz;
			}

			ArrayPool<byte>.Shared.Return(recBuf);

			entryOffset += totalRead;

			return totalRead;
		}

		/// <summary>
		/// Closes this stream. Calls the TarBuffer's close() method.
		/// The underlying stream is closed by the TarBuffer.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				tarBuffer.Close();
			}
		}

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
		/// <summary>
		/// Closes this stream. Calls the TarBuffer's close() method.
		/// The underlying stream is closed by the TarBuffer.
		/// </summary>
		public override async ValueTask DisposeAsync()
		{
			await tarBuffer.CloseAsync(CancellationToken.None).ConfigureAwait(false);
		}
#endif

		#endregion Stream Overrides

		/// <summary>
		/// Set the entry factory for this instance.
		/// </summary>
		/// <param name="factory">The factory for creating new entries</param>
		public void SetEntryFactory(IEntryFactory factory)
		{
			entryFactory = factory;
		}

		/// <summary>
		/// Get the record size being used by this stream's TarBuffer.
		/// </summary>
		public int RecordSize
		{
			get { return tarBuffer.RecordSize; }
		}

		/// <summary>
		/// Get the record size being used by this stream's TarBuffer.
		/// </summary>
		/// <returns>
		/// TarBuffer record size.
		/// </returns>
		[Obsolete("Use RecordSize property instead")]
		public int GetRecordSize()
		{
			return tarBuffer.RecordSize;
		}

		/// <summary>
		/// Get the available data that can be read from the current
		/// entry in the archive. This does not indicate how much data
		/// is left in the entire archive, only in the current entry.
		/// This value is determined from the entry's size header field
		/// and the amount of data already read from the current entry.
		/// </summary>
		/// <returns>
		/// The number of available bytes for the current entry.
		/// </returns>
		public long Available
		{
			get { return entrySize - entryOffset; }
		}

		/// <summary>
		/// Skip bytes in the input buffer. This skips bytes in the
		/// current entry's data, not the entire archive, and will
		/// stop at the end of the current entry's data if the number
		/// to skip extends beyond that point.
		/// </summary>
		/// <param name="skipCount">
		/// The number of bytes to skip.
		/// </param>
		/// <param name="ct"></param>
		private Task SkipAsync(long skipCount, CancellationToken ct) => SkipAsync(skipCount, ct, true).AsTask();

		/// <summary>
		/// Skip bytes in the input buffer. This skips bytes in the
		/// current entry's data, not the entire archive, and will
		/// stop at the end of the current entry's data if the number
		/// to skip extends beyond that point.
		/// </summary>
		/// <param name="skipCount">
		/// The number of bytes to skip.
		/// </param>
		private void Skip(long skipCount) =>
			SkipAsync(skipCount, CancellationToken.None, false).GetAwaiter().GetResult();

		private async ValueTask SkipAsync(long skipCount, CancellationToken ct, bool isAsync)
		{
			// TODO: REVIEW efficiency of TarInputStream.Skip
			// This is horribly inefficient, but it ensures that we
			// properly skip over bytes via the TarBuffer...
			//
			var length = 8 * 1024;
			using (var skipBuf = ExactMemoryPool<byte>.Shared.Rent(length))
			{
				for (long num = skipCount; num > 0;)
				{
					int toRead = num > length ? length : (int)num;
					int numRead = await ReadAsync(skipBuf.Memory.Slice(0, toRead), ct, isAsync).ConfigureAwait(false);

					if (numRead == -1)
					{
						break;
					}

					num -= numRead;
				}
			}
		}

		/// <summary>
		/// Return a value of true if marking is supported; false otherwise.
		/// </summary>
		/// <remarks>Currently marking is not supported, the return value is always false.</remarks>
		public bool IsMarkSupported
		{
			get { return false; }
		}

		/// <summary>
		/// Since we do not support marking just yet, we do nothing.
		/// </summary>
		/// <param name ="markLimit">
		/// The limit to mark.
		/// </param>
		public void Mark(int markLimit)
		{
		}

		/// <summary>
		/// Since we do not support marking just yet, we do nothing.
		/// </summary>
		public void Reset()
		{
		}

		/// <summary>
		/// Get the next entry in this tar archive. This will skip
		/// over any remaining data in the current entry, if there
		/// is one, and place the input stream at the header of the
		/// next entry, and read the header and instantiate a new
		/// TarEntry from the header bytes and return that entry.
		/// If there are no more entries in the archive, null will
		/// be returned to indicate that the end of the archive has
		/// been reached.
		/// </summary>
		/// <returns>
		/// The next TarEntry in the archive, or null.
		/// </returns>
		public Task<TarEntry> GetNextEntryAsync(CancellationToken ct) => GetNextEntryAsync(ct, true).AsTask();

		/// <summary>
		/// Get the next entry in this tar archive. This will skip
		/// over any remaining data in the current entry, if there
		/// is one, and place the input stream at the header of the
		/// next entry, and read the header and instantiate a new
		/// TarEntry from the header bytes and return that entry.
		/// If there are no more entries in the archive, null will
		/// be returned to indicate that the end of the archive has
		/// been reached.
		/// </summary>
		/// <returns>
		/// The next TarEntry in the archive, or null.
		/// </returns>
		public TarEntry GetNextEntry() => GetNextEntryAsync(CancellationToken.None, false).GetAwaiter().GetResult();

		private async ValueTask<TarEntry> GetNextEntryAsync(CancellationToken ct, bool isAsync)
		{
			if (hasHitEOF)
			{
				return null;
			}

			if (currentEntry != null)
			{
				await SkipToNextEntryAsync(ct, isAsync).ConfigureAwait(false);
			}

			byte[] headerBuf = ArrayPool<byte>.Shared.Rent(TarBuffer.BlockSize);
			await tarBuffer.ReadBlockIntAsync(headerBuf, ct, isAsync).ConfigureAwait(false);

			if (TarBuffer.IsEndOfArchiveBlock(headerBuf))
			{
				hasHitEOF = true;

				// Read the second zero-filled block
				await tarBuffer.ReadBlockIntAsync(headerBuf, ct, isAsync).ConfigureAwait(false);
			}
			else
			{
				hasHitEOF = false;
			}

			if (hasHitEOF)
			{
				currentEntry = null;
				readBuffer?.Dispose();
			}
			else
			{
				try
				{
					var header = new TarHeader();
					header.ParseBuffer(headerBuf, encoding);
					if (!header.IsChecksumValid)
					{
						throw new TarException("Header checksum is invalid");
					}

					this.entryOffset = 0;
					this.entrySize = header.Size;

					string longName = null;

					if (header.TypeFlag == TarHeader.LF_GNU_LONGNAME)
					{
						using (var nameBuffer = ExactMemoryPool<byte>.Shared.Rent(TarBuffer.BlockSize))
						{
							long numToRead = this.entrySize;

							var longNameBuilder = StringBuilderPool.Instance.Rent();

							while (numToRead > 0)
							{
								var length = (numToRead > TarBuffer.BlockSize ? TarBuffer.BlockSize : (int)numToRead);
								int numRead = await ReadAsync(nameBuffer.Memory.Slice(0, length), ct, isAsync).ConfigureAwait(false);

								if (numRead == -1)
								{
									throw new InvalidHeaderException("Failed to read long name entry");
								}

								longNameBuilder.Append(TarHeader.ParseName(nameBuffer.Memory.Slice(0, numRead).Span,
									encoding));
								numToRead -= numRead;
							}

							longName = longNameBuilder.ToString();
							StringBuilderPool.Instance.Return(longNameBuilder);

							await SkipToNextEntryAsync(ct, isAsync).ConfigureAwait(false);
							await this.tarBuffer.ReadBlockIntAsync(headerBuf, ct, isAsync).ConfigureAwait(false);
						}
					}
					else if (header.TypeFlag == TarHeader.LF_GHDR)
					{
						// POSIX global extended header
						// Ignore things we dont understand completely for now
						await SkipToNextEntryAsync(ct, isAsync).ConfigureAwait(false);
						await this.tarBuffer.ReadBlockIntAsync(headerBuf, ct, isAsync).ConfigureAwait(false);
					}
					else if (header.TypeFlag == TarHeader.LF_XHDR)
					{
						// POSIX extended header
						byte[] nameBuffer = ArrayPool<byte>.Shared.Rent(TarBuffer.BlockSize);
						long numToRead = this.entrySize;

						var xhr = new TarExtendedHeaderReader();

						while (numToRead > 0)
						{
							var length = (numToRead > nameBuffer.Length ? nameBuffer.Length : (int)numToRead);
							int numRead = await ReadAsync(nameBuffer.AsMemory().Slice(0, length), ct, isAsync).ConfigureAwait(false);

							if (numRead == -1)
							{
								throw new InvalidHeaderException("Failed to read long name entry");
							}

							xhr.Read(nameBuffer, numRead);
							numToRead -= numRead;
						}

						ArrayPool<byte>.Shared.Return(nameBuffer);

						if (xhr.Headers.TryGetValue("path", out string name))
						{
							longName = name;
						}

						await SkipToNextEntryAsync(ct, isAsync).ConfigureAwait(false);
						await this.tarBuffer.ReadBlockIntAsync(headerBuf, ct, isAsync).ConfigureAwait(false);
					}
					else if (header.TypeFlag == TarHeader.LF_GNU_VOLHDR)
					{
						// TODO: could show volume name when verbose
						await SkipToNextEntryAsync(ct, isAsync).ConfigureAwait(false);
						await this.tarBuffer.ReadBlockIntAsync(headerBuf, ct, isAsync).ConfigureAwait(false);
					}
					else if (header.TypeFlag != TarHeader.LF_NORMAL &&
					         header.TypeFlag != TarHeader.LF_OLDNORM &&
					         header.TypeFlag != TarHeader.LF_LINK &&
					         header.TypeFlag != TarHeader.LF_SYMLINK &&
					         header.TypeFlag != TarHeader.LF_DIR)
					{
						// Ignore things we dont understand completely for now
						await SkipToNextEntryAsync(ct, isAsync).ConfigureAwait(false);
						await tarBuffer.ReadBlockIntAsync(headerBuf, ct, isAsync).ConfigureAwait(false);
					}

					if (entryFactory == null)
					{
						currentEntry = new TarEntry(headerBuf, encoding);
						readBuffer?.Dispose();

						if (longName != null)
						{
							currentEntry.Name = longName;
						}
					}
					else
					{
						currentEntry = entryFactory.CreateEntry(headerBuf);
						readBuffer?.Dispose();
					}

					// Magic was checked here for 'ustar' but there are multiple valid possibilities
					// so this is not done anymore.

					entryOffset = 0;

					// TODO: Review How do we resolve this discrepancy?!
					entrySize = this.currentEntry.Size;
				}
				catch (InvalidHeaderException ex)
				{
					entrySize = 0;
					entryOffset = 0;
					currentEntry = null;
					readBuffer?.Dispose();

					string errorText = string.Format("Bad header in record {0} block {1} {2}",
						tarBuffer.CurrentRecord, tarBuffer.CurrentBlock, ex.Message);
					throw new InvalidHeaderException(errorText);
				}
			}

			ArrayPool<byte>.Shared.Return(headerBuf);

			return currentEntry;
		}

		/// <summary>
		/// Copies the contents of the current tar archive entry directly into
		/// an output stream.
		/// </summary>
		/// <param name="outputStream">
		/// The OutputStream into which to write the entry's data.
		/// </param>
		/// <param name="ct"></param>
		public Task CopyEntryContentsAsync(Stream outputStream, CancellationToken ct) =>
			CopyEntryContentsAsync(outputStream, ct, true).AsTask();

		/// <summary>
		/// Copies the contents of the current tar archive entry directly into
		/// an output stream.
		/// </summary>
		/// <param name="outputStream">
		/// The OutputStream into which to write the entry's data.
		/// </param>
		public void CopyEntryContents(Stream outputStream) =>
			CopyEntryContentsAsync(outputStream, CancellationToken.None, false).GetAwaiter().GetResult();

		private async ValueTask CopyEntryContentsAsync(Stream outputStream, CancellationToken ct, bool isAsync)
		{
			byte[] tempBuffer = ArrayPool<byte>.Shared.Rent(32 * 1024);

			while (true)
			{
				int numRead = await ReadAsync(tempBuffer, ct, isAsync).ConfigureAwait(false);
				if (numRead <= 0)
				{
					break;
				}

				if (isAsync)
				{
					await outputStream.WriteAsync(tempBuffer, 0, numRead, ct).ConfigureAwait(false);
				}
				else
				{
					outputStream.Write(tempBuffer, 0, numRead);
				}
			}

			ArrayPool<byte>.Shared.Return(tempBuffer);
		}

		private async ValueTask SkipToNextEntryAsync(CancellationToken ct, bool isAsync)
		{
			long numToSkip = entrySize - entryOffset;

			if (numToSkip > 0)
			{
				await SkipAsync(numToSkip, ct, isAsync).ConfigureAwait(false);
			}

			readBuffer?.Dispose();
			readBuffer = null;
		}

		/// <summary>
		/// This interface is provided, along with the method <see cref="SetEntryFactory"/>, to allow
		/// the programmer to have their own <see cref="TarEntry"/> subclass instantiated for the
		/// entries return from <see cref="GetNextEntry"/>.
		/// </summary>
		public interface IEntryFactory
		{
			// This interface does not considering name encoding.
			// How this interface should be?
			/// <summary>
			/// Create an entry based on name alone
			/// </summary>
			/// <param name="name">
			/// Name of the new EntryPointNotFoundException to create
			/// </param>
			/// <returns>created TarEntry or descendant class</returns>
			TarEntry CreateEntry(string name);

			/// <summary>
			/// Create an instance based on an actual file
			/// </summary>
			/// <param name="fileName">
			/// Name of file to represent in the entry
			/// </param>
			/// <returns>
			/// Created TarEntry or descendant class
			/// </returns>
			TarEntry CreateEntryFromFile(string fileName);

			/// <summary>
			/// Create a tar entry based on the header information passed
			/// </summary>
			/// <param name="headerBuffer">
			/// Buffer containing header information to create an entry from.
			/// </param>
			/// <returns>
			/// Created TarEntry or descendant class
			/// </returns>
			TarEntry CreateEntry(byte[] headerBuffer);
		}

		/// <summary>
		/// Standard entry factory class creating instances of the class TarEntry
		/// </summary>
		public class EntryFactoryAdapter : IEntryFactory
		{
			Encoding nameEncoding;

			/// <summary>
			/// Construct standard entry factory class with ASCII name encoding
			/// </summary>
			[Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
			public EntryFactoryAdapter()
			{
			}

			/// <summary>
			/// Construct standard entry factory with name encoding
			/// </summary>
			/// <param name="nameEncoding">The <see cref="Encoding"/> used for the Name fields, or null for ASCII only</param>
			public EntryFactoryAdapter(Encoding nameEncoding)
			{
				this.nameEncoding = nameEncoding;
			}

			/// <summary>
			/// Create a <see cref="TarEntry"/> based on named
			/// </summary>
			/// <param name="name">The name to use for the entry</param>
			/// <returns>A new <see cref="TarEntry"/></returns>
			public TarEntry CreateEntry(string name)
			{
				return TarEntry.CreateTarEntry(name);
			}

			/// <summary>
			/// Create a tar entry with details obtained from <paramref name="fileName">file</paramref>
			/// </summary>
			/// <param name="fileName">The name of the file to retrieve details from.</param>
			/// <returns>A new <see cref="TarEntry"/></returns>
			public TarEntry CreateEntryFromFile(string fileName)
			{
				return TarEntry.CreateEntryFromFile(fileName);
			}

			/// <summary>
			/// Create an entry based on details in <paramref name="headerBuffer">header</paramref>
			/// </summary>
			/// <param name="headerBuffer">The buffer containing entry details.</param>
			/// <returns>A new <see cref="TarEntry"/></returns>
			public TarEntry CreateEntry(byte[] headerBuffer)
			{
				return new TarEntry(headerBuffer, nameEncoding);
			}
		}

		#region Instance Fields

		/// <summary>
		/// Flag set when last block has been read
		/// </summary>
		protected bool hasHitEOF;

		/// <summary>
		/// Size of this entry as recorded in header
		/// </summary>
		protected long entrySize;

		/// <summary>
		/// Number of bytes read for this entry so far
		/// </summary>
		protected long entryOffset;

		/// <summary>
		/// Buffer used with calls to <code>Read()</code>
		/// </summary>
		protected IMemoryOwner<byte> readBuffer;

		/// <summary>
		/// Working buffer
		/// </summary>
		protected TarBuffer tarBuffer;

		/// <summary>
		/// Current entry being read
		/// </summary>
		private TarEntry currentEntry;

		/// <summary>
		/// Factory used to create TarEntry or descendant class instance
		/// </summary>
		protected IEntryFactory entryFactory;

		/// <summary>
		/// Stream used as the source of input data.
		/// </summary>
		private readonly Stream inputStream;

		private readonly Encoding encoding;

		#endregion Instance Fields
	}
}
