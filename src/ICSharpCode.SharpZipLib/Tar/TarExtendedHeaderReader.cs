using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.SharpZipLib.Tar
{
	/// <summary>
	/// Reads the extended header of a Tar stream
	/// </summary>
	public class TarExtendedHeaderReader
	{
		private const byte Length = 0;
		private const byte Key = 1;
		private const byte Value = 2;
		private const byte End = 3;

		private string[] _headerParts = new string[3];

		private int _bbIndex;
		private byte[] _byteBuffer = new byte[4];
		private char[] _charBuffer = new char[4];

		private readonly StringBuilder _sb = new StringBuilder();
		private readonly Decoder _decoder = Encoding.UTF8.GetDecoder();

		private int _state = Length;

		private static readonly byte[] StateNext = { (byte)' ', (byte)'=', (byte)'\n' };

		/// <summary>
		/// Creates a new <see cref="TarExtendedHeaderReader"/>.
		/// </summary>
		public TarExtendedHeaderReader()
		{
			ResetBuffers();
		}

		/// <summary>
		/// Read <paramref name="length"/> bytes from <paramref name="buffer"/>
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="length"></param>
		public void Read(byte[] buffer, int length)
		{
			for (int i = 0; i < length; i++)
			{
				byte next = buffer[i];

				if (next == StateNext[_state])
				{
					Flush();
					_headerParts[_state] = _sb.ToString();
					_sb.Clear();

					if (++_state == End)
					{
						Headers.Add(_headerParts[Key], _headerParts[Value]);
						_headerParts = new string[3];
						_state = Length;
					}
				}
				else
				{
					_byteBuffer[_bbIndex++] = next;
					if (_bbIndex == 4)
						Flush();
				}
			}
		}

		private void Flush()
		{
			_decoder.Convert(_byteBuffer, 0, _bbIndex, _charBuffer, 0, 4, false, out _, out var charsUsed, out _);

			_sb.Append(_charBuffer, 0, charsUsed);
			ResetBuffers();
		}

		private void ResetBuffers()
		{
			_charBuffer = new char[4];
			_byteBuffer = new byte[4];
			_bbIndex = 0;
		}

		/// <summary>
		/// Returns the parsed headers as key-value strings
		/// </summary>
		public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();
	}
}
