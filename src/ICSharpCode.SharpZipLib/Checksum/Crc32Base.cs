using System;
using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.SharpZipLib.Checksum
{
	/// <summary>
	/// Base class for CRC32 checksums
	/// </summary>
	public abstract class Crc32Base : IChecksum
	{
		#region Instance Fields
		/// <summary>
		/// The CRC data checksum so far.
		/// </summary>
		private uint checkValue;

		#endregion


		/// <summary>
		/// Initialise a default instance of <see cref="Crc32"></see>
		/// </summary>
		public Crc32Base()
		{
			Reset();
		}


		internal abstract Crc32Proxy Proxy { get; }



		/// <summary>
		/// Resets the CRC data checksum as if no update was ever called.
		/// </summary>
		public void Reset()
		{
			checkValue = 0;
		}

		/// <summary>
		/// Returns the CRC data checksum computed so far.
		/// </summary>
		/// <remarks>Reversed Out = false</remarks>
		public long Value
		{
			get
			{
				return (long)(checkValue);
			}
		}

		/// <summary>
		/// Updates the checksum with the int bval.
		/// </summary>
		/// <param name = "bval">
		/// the byte is taken as the lower 8 bits of bval
		/// </param>
		/// <remarks>Reversed Data = true</remarks>
		public void Update(int bval)
		{
			checkValue = Proxy.Append(checkValue, new byte[] { (byte)bval }, 0, 1);
		}



		/// <summary>
		/// Updates the CRC data checksum with the bytes taken from
		/// a block of data.
		/// </summary>
		/// <param name="buffer">Contains the data to update the CRC with.</param>
		public void Update(byte[] buffer)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}

			Update(new ArraySegment<byte>(buffer, 0, buffer.Length));
		}

		/// <summary>
		/// Update CRC data checksum based on a portion of a block of data
		/// </summary>
		/// <param name = "segment">
		/// The chunk of data to add
		/// </param>
		public void Update(ArraySegment<byte> segment)
		{
			checkValue = Proxy.Append(checkValue, segment.Array, segment.Offset, segment.Count);
		}

	}
}
