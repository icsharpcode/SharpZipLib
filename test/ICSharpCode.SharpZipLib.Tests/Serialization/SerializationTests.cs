using System;
using System.IO;
using System.Runtime.Serialization;

using NUnit.Framework;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Lzw;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;

namespace ICSharpCode.SharpZipLib.Tests.Serialization
{
	[TestFixture]
	public class SerializationTests
	{
        /// <summary>
        /// Test that SharpZipLib Custom Exceptions can be serialized.
        /// </summary>
        [Test]
        [Category("Core")]
        [Category("Serialization")]
        [TestCase(typeof(BZip2Exception))]
        [TestCase(typeof(GZipException))]
        [TestCase(typeof(InvalidHeaderException))]
        [TestCase(typeof(InvalidNameException))]
        [TestCase(typeof(LzwException))]
        [TestCase(typeof(SharpZipBaseException))]
        [TestCase(typeof(StreamDecodingException))]
        [TestCase(typeof(StreamUnsupportedException))]
        [TestCase(typeof(TarException))]
        [TestCase(typeof(UnexpectedEndOfStreamException))]
        [TestCase(typeof(ZipException))]
        public void SerializeException(Type exceptionType)
        {
            string message = $"Serialized {exceptionType.Name}";
            var exception = Activator.CreateInstance(exceptionType, message);

            var deserializedException = ExceptionSerialiseHelper(exception, exceptionType) as Exception;
            Assert.That(deserializedException, Is.InstanceOf(exceptionType), "deserialized object should have the correct type");
            Assert.That(deserializedException.Message, Is.EqualTo(message), "deserialized message should match original message");
        }

		/// <summary>
		/// Test that ValueOutOfRangeException can be serialized.
		/// </summary>
		[Test]
		[Category("Core")]
		[Category("Serialization")]
		public void SerializeValueOutOfRangeException()
		{
			string message = "Serialized ValueOutOfRangeException";
			var exception = new ValueOutOfRangeException(message);

			var deserializedException = ExceptionSerialiseHelper(exception, typeof(ValueOutOfRangeException)) as ValueOutOfRangeException;

			// ValueOutOfRangeException appends 'out of range' to the end of the message
			Assert.That(deserializedException.Message, Is.EqualTo($"{message} out of range"), "should have expected message");
		}

		// Shared serialization helper
		// round trips the specified exception using DataContractSerializer
		private static object ExceptionSerialiseHelper(object exception, Type exceptionType)
		{
			DataContractSerializer ser = new DataContractSerializer(exceptionType);

			using (var memoryStream = new MemoryStream())
			{
				ser.WriteObject(memoryStream, exception);

				memoryStream.Seek(0, loc: SeekOrigin.Begin);

				return ser.ReadObject(memoryStream);
			}
		}
	}
}

