using ICSharpCode.SharpZipLib.Checksum;
using NUnit.Framework;
using System;
using System.Diagnostics;

namespace ICSharpCode.SharpZipLib.Tests.Checksum
{
	public abstract class ChecksumTestBase
	{
		protected readonly
				// Represents ASCII string of "123456789"
				byte[] check = { 49, 50, 51, 52, 53, 54, 55, 56, 57 };

		protected readonly
				// Represents ASCII string of "123456789123456789123456789"
				byte[] longcheck = { 49, 50, 51, 52, 53, 54, 55, 56, 57, 49, 50, 51, 52, 53, 54, 55, 56, 57, 49, 50, 51, 52, 53, 54, 55, 56, 57 };



		protected void exceptionTesting(IChecksum crcUnderTest)
		{
			bool exception = false;

			try
			{
				crcUnderTest.Update(null);
			}
			catch (ArgumentNullException)
			{
				exception = true;
			}
			Assert.IsTrue(exception, "Passing a null buffer should cause an ArgumentNullException");

			// reset exception
			exception = false;
			try
			{
				crcUnderTest.Update(new ArraySegment<byte>(null, 0, 0));
			}
			catch (ArgumentNullException)
			{
				exception = true;
			}
			Assert.IsTrue(exception, "Passing a null buffer should cause an ArgumentNullException");

			// reset exception
			exception = false;
			try
			{
				crcUnderTest.Update(new ArraySegment<byte>(check, -1, 9));
			}
			catch (ArgumentOutOfRangeException)
			{
				exception = true;
			}
			Assert.IsTrue(exception, "Passing a negative offset should cause an ArgumentOutOfRangeException");

			// reset exception
			exception = false;
			try
			{
				crcUnderTest.Update(new ArraySegment<byte>(check, 10, 0));
			}
			catch (ArgumentException)
			{
				exception = true;
			}
			Assert.IsTrue(exception, "Passing an offset greater than buffer.Length should cause an ArgumentException");

			// reset exception
			exception = false;
			try
			{
				crcUnderTest.Update(new ArraySegment<byte>(check, 0, -1));
			}
			catch (ArgumentOutOfRangeException)
			{
				exception = true;
			}
			Assert.IsTrue(exception, "Passing a negative count should cause an ArgumentOutOfRangeException");

			// reset exception
			exception = false;
			try
			{
				crcUnderTest.Update(new ArraySegment<byte>(check, 0, 10));
			}
			catch (ArgumentException)
			{
				exception = true;
			}
			Assert.IsTrue(exception, "Passing a count + offset greater than buffer.Length should cause an ArgumentException");
		}
	}
}
