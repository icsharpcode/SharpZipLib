using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Core;

namespace ICSharpCode.SharpZipLib.Zip
{
	/// <summary>
	/// Holds data pertinent to a data descriptor.
	/// </summary>
	public class DescriptorData
	{
		private long _crc;

		/// <summary>
		/// Get /set the compressed size of data.
		/// </summary>
		public long CompressedSize { get; set; }

		/// <summary>
		/// Get / set the uncompressed size of data
		/// </summary>
		public long Size { get; set; }

		/// <summary>
		/// Get /set the crc value.
		/// </summary>
		public long Crc
		{
			get => _crc;
			set => _crc = (value & 0xffffffff);
		}
	}

	internal class EntryPatchData
	{
		public long SizePatchOffset { get; set; }

		public long CrcPatchOffset { get; set; }
	}

	/// <summary>
	/// This class assists with writing/reading from Zip files.
	/// </summary>
	internal static class ZipFormat
	{
		// Write the local file header
		// TODO: ZipFormat.WriteLocalHeader is not yet used and needs checking for ZipFile and ZipOuptutStream usage
		private static void WriteLocalHeader(Stream stream, ZipEntry entry, EntryPatchData patchData, 
			bool headerInfoAvailable, bool patchEntryHeader, int offset)
		{
			CompressionMethod method = entry.CompressionMethod;
			
			stream.WriteLEInt(ZipConstants.LocalHeaderSignature);

			stream.WriteLEShort(entry.Version);
			stream.WriteLEShort(entry.Flags);
			stream.WriteLEShort((byte)method);
			stream.WriteLEInt((int)entry.DosTime);

			if (headerInfoAvailable)
			{
				stream.WriteLEInt((int)entry.Crc);
				if (entry.LocalHeaderRequiresZip64)
				{
					stream.WriteLEInt(-1);
					stream.WriteLEInt(-1);
				}
				else
				{
					stream.WriteLEInt(entry.IsCrypted ? (int)entry.CompressedSize + ZipConstants.CryptoHeaderSize : (int)entry.CompressedSize);
					stream.WriteLEInt((int)entry.Size);
				}
			}
			else
			{
				if (patchData != null)
				{
					patchData.CrcPatchOffset = offset + stream.Position;
				}
				stream.WriteLEInt(0);  // Crc

				if (patchData != null)
				{
					patchData.SizePatchOffset = offset + stream.Position;
				}

				// For local header both sizes appear in Zip64 Extended Information
				if (entry.LocalHeaderRequiresZip64 && patchEntryHeader)
				{
					stream.WriteLEInt(-1);
					stream.WriteLEInt(-1);
				}
				else
				{
					stream.WriteLEInt(0);  // Compressed size
					stream.WriteLEInt(0);  // Uncompressed size
				}
			}

			byte[] name = ZipStrings.ConvertToArray(entry.Flags, entry.Name);

			if (name.Length > 0xFFFF)
			{
				throw new ZipException("Entry name too long.");
			}

			var ed = new ZipExtraData(entry.ExtraData);

			if (entry.LocalHeaderRequiresZip64 && (headerInfoAvailable || patchEntryHeader))
			{
				ed.StartNewEntry();
				if (headerInfoAvailable)
				{
					ed.AddLeLong(entry.Size);
					ed.AddLeLong(entry.CompressedSize);
				}
				else
				{
					ed.AddLeLong(-1);
					ed.AddLeLong(-1);
				}
				ed.AddNewEntry(1);

				if (!ed.Find(1))
				{
					throw new ZipException("Internal error cant find extra data");
				}

				if (patchData != null)
				{
					patchData.SizePatchOffset = ed.CurrentReadIndex;
				}
			}
			else
			{
				ed.Delete(1);
			}

			byte[] extra = ed.GetEntryData();

			stream.WriteLEShort(name.Length);
			stream.WriteLEShort(extra.Length);

			if (name.Length > 0)
			{
				stream.Write(name, 0, name.Length);
			}

			if (entry.LocalHeaderRequiresZip64 && patchEntryHeader && patchData != null)
			{
				patchData.SizePatchOffset += offset + stream.Position;
			}

			if (extra.Length > 0)
			{
				stream.Write(extra, 0, extra.Length);
			}
		}

		/// <summary>
		/// Locates a block with the desired <paramref name="signature"/>.
		/// </summary>
		/// <param name="stream" />
		/// <param name="signature">The signature to find.</param>
		/// <param name="endLocation">Location, marking the end of block.</param>
		/// <param name="minimumBlockSize">Minimum size of the block.</param>
		/// <param name="maximumVariableData">The maximum variable data.</param>
		/// <returns>Returns the offset of the first byte after the signature; -1 if not found</returns>
		internal static long LocateBlockWithSignature(Stream stream, int signature, long endLocation, int minimumBlockSize, int maximumVariableData)
		{
			long pos = endLocation - minimumBlockSize;
			if (pos < 0)
			{
				return -1;
			}

			long giveUpMarker = Math.Max(pos - maximumVariableData, 0);

			// TODO: This loop could be optimized for speed.
			do
			{
				if (pos < giveUpMarker)
				{
					return -1;
				}
				stream.Seek(pos--, SeekOrigin.Begin);
			} while (stream.ReadLEInt() != signature);

			return stream.Position;
		}

		/// <inheritdoc cref="WriteZip64EndOfCentralDirectory"/>
		public static async Task WriteZip64EndOfCentralDirectoryAsync(Stream stream, long noOfEntries, 
			long sizeEntries, long centralDirOffset, CancellationToken cancellationToken)
		{
			using (var ms = new MemoryStream())
			{
				WriteZip64EndOfCentralDirectory(ms, noOfEntries, sizeEntries, centralDirOffset);
				await ms.CopyToAsync(stream, 81920, cancellationToken);
			}
		}

		/// <summary>
		/// Write Zip64 end of central directory records (File header and locator).
		/// </summary>
		/// <param name="stream" />
		/// <param name="noOfEntries">The number of entries in the central directory.</param>
		/// <param name="sizeEntries">The size of entries in the central directory.</param>
		/// <param name="centralDirOffset">The offset of the central directory.</param>
		internal static void WriteZip64EndOfCentralDirectory(Stream stream, long noOfEntries, long sizeEntries, long centralDirOffset)
		{
			long centralSignatureOffset = centralDirOffset + sizeEntries;
			stream.WriteLEInt(ZipConstants.Zip64CentralFileHeaderSignature);
			stream.WriteLELong(44);    // Size of this record (total size of remaining fields in header or full size - 12)
			stream.WriteLEShort(ZipConstants.VersionMadeBy);   // Version made by
			stream.WriteLEShort(ZipConstants.VersionZip64);   // Version to extract
			stream.WriteLEInt(0);      // Number of this disk
			stream.WriteLEInt(0);      // number of the disk with the start of the central directory
			stream.WriteLELong(noOfEntries);       // No of entries on this disk
			stream.WriteLELong(noOfEntries);       // Total No of entries in central directory
			stream.WriteLELong(sizeEntries);       // Size of the central directory
			stream.WriteLELong(centralDirOffset);  // offset of start of central directory
												   // zip64 extensible data sector not catered for here (variable size)

			// Write the Zip64 end of central directory locator
			stream.WriteLEInt(ZipConstants.Zip64CentralDirLocatorSignature);

			// no of the disk with the start of the zip64 end of central directory
			stream.WriteLEInt(0);

			// relative offset of the zip64 end of central directory record
			stream.WriteLELong(centralSignatureOffset);

			// total number of disks
			stream.WriteLEInt(1);
		}

		/// <inheritdoc cref="WriteEndOfCentralDirectory"/>
		public static  async Task WriteEndOfCentralDirectoryAsync(Stream stream, long noOfEntries, long sizeEntries, 
			long start, byte[] comment, CancellationToken cancellationToken)
		{
			using (var ms = new MemoryStream())
			{
				WriteEndOfCentralDirectory(ms, noOfEntries, sizeEntries, start, comment);
				await ms.CopyToAsync(stream, 81920, cancellationToken);
			}
		}


		/// <summary>
		/// Write the required records to end the central directory.
		/// </summary>
		/// <param name="stream" />
		/// <param name="noOfEntries">The number of entries in the directory.</param>
		/// <param name="sizeEntries">The size of the entries in the directory.</param>
		/// <param name="start">The start of the central directory.</param>
		/// <param name="comment">The archive comment.  (This can be null).</param>

		internal static void WriteEndOfCentralDirectory(Stream stream, long noOfEntries, long sizeEntries, long start, byte[] comment)
		{
			if (noOfEntries >= 0xffff ||
			    start >= 0xffffffff ||
			    sizeEntries >= 0xffffffff)
			{
				WriteZip64EndOfCentralDirectory(stream, noOfEntries, sizeEntries, start);
			}

			stream.WriteLEInt(ZipConstants.EndOfCentralDirectorySignature);

			// TODO: ZipFile Multi disk handling not done
			stream.WriteLEShort(0);                    // number of this disk
			stream.WriteLEShort(0);                    // no of disk with start of central dir

			// Number of entries
			if (noOfEntries >= 0xffff)
			{
				stream.WriteLEUshort(0xffff);  // Zip64 marker
				stream.WriteLEUshort(0xffff);
			}
			else
			{
				stream.WriteLEShort((short)noOfEntries);          // entries in central dir for this disk
				stream.WriteLEShort((short)noOfEntries);          // total entries in central directory
			}

			// Size of the central directory
			if (sizeEntries >= 0xffffffff)
			{
				stream.WriteLEUint(0xffffffff);    // Zip64 marker
			}
			else
			{
				stream.WriteLEInt((int)sizeEntries);
			}

			// offset of start of central directory
			if (start >= 0xffffffff)
			{
				stream.WriteLEUint(0xffffffff);    // Zip64 marker
			}
			else
			{
				stream.WriteLEInt((int)start);
			}

			var commentLength = comment?.Length ?? 0;

			if (commentLength > 0xffff)
			{
				throw new ZipException($"Comment length ({commentLength}) is larger than 64K");
			}

			stream.WriteLEShort(commentLength);

			if (commentLength > 0)
			{
				stream.Write(comment, 0, commentLength);
			}
		}



		/// <summary>
		/// Write a data descriptor.
		/// </summary>
		/// <param name="stream" />
		/// <param name="entry">The entry to write a descriptor for.</param>
		/// <returns>Returns the number of descriptor bytes written.</returns>
		internal static int WriteDataDescriptor(Stream stream, ZipEntry entry)
		{
			if (entry == null)
			{
				throw new ArgumentNullException(nameof(entry));
			}

			int result = 0;

			// Add data descriptor if flagged as required
			if ((entry.Flags & (int)GeneralBitFlags.Descriptor) != 0)
			{
				// The signature is not PKZIP originally but is now described as optional
				// in the PKZIP Appnote documenting the format.
				stream.WriteLEInt(ZipConstants.DataDescriptorSignature);
				stream.WriteLEInt(unchecked((int)(entry.Crc)));

				result += 8;

				if (entry.LocalHeaderRequiresZip64)
				{
					stream.WriteLELong(entry.CompressedSize);
					stream.WriteLELong(entry.Size);
					result += 16;
				}
				else
				{
					stream.WriteLEInt((int)entry.CompressedSize);
					stream.WriteLEInt((int)entry.Size);
					result += 8;
				}
			}

			return result;
		}

		/// <summary>
		/// Read data descriptor at the end of compressed data.
		/// </summary>
		/// <param name="stream" />
		/// <param name="zip64">if set to <c>true</c> [zip64].</param>
		/// <param name="data">The data to fill in.</param>
		/// <returns>Returns the number of bytes read in the descriptor.</returns>
		internal static void ReadDataDescriptor(Stream stream, bool zip64, DescriptorData data)
		{
			int intValue = stream.ReadLEInt();

			// In theory this may not be a descriptor according to PKZIP appnote.
			// In practice its always there.
			if (intValue != ZipConstants.DataDescriptorSignature)
			{
				throw new ZipException("Data descriptor signature not found");
			}

			data.Crc = stream.ReadLEInt();

			if (zip64)
			{
				data.CompressedSize = stream.ReadLELong();
				data.Size = stream.ReadLELong();
			}
			else
			{
				data.CompressedSize = stream.ReadLEInt();
				data.Size = stream.ReadLEInt();
			}
		}
	}
}
