using System;
using System.IO;

namespace ICSharpCode.SharpZipLib.BZip2
{

	/**
	 * A 256 entry Move To Front transform
	 */
	public class MoveToFront
	{

		/**
		 * The Move To Front list
		 */
		private readonly byte[] mtf = new byte[256];


		/**
		 * Moves a value to the head of the MTF list (forward Move To Front transform)
		 * @param value The value to move
		 * @return The position the value moved from
		 */
		public int valueToFront(byte value)
		{

			byte[] mtf = this.mtf;

			int index = 0;
			byte temp = mtf[0];
			if (value != temp)
			{
				mtf[0] = value;
				while (value != temp)
				{
					index++;
					byte temp2 = temp;
					temp = mtf[index];
					mtf[index] = temp2;
				}
			}

			return index;

		}


		/**
		 * Gets the value from a given index and moves it to the front of the MTF list (inverse Move To
		 * Front transform)
		 * @param index The index to move
		 * @return The value at the given index
		 */
		public byte indexToFront(int index)
		{

			byte[] mtf = this.mtf;

			byte value = mtf[index];
			Array.ConstrainedCopy(mtf, 0, mtf, 1, index);
			mtf[0] = value;

			return value;

		}

		/// <summary>
		/// Initialize new MTF list
		/// </summary>
		public MoveToFront()
		{
			byte b = 0;
			do mtf[b] = b;
			while (++b > 0);
		}

	}
}
