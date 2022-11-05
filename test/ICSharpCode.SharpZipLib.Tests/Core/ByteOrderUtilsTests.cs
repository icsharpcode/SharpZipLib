using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using BO = ICSharpCode.SharpZipLib.Core.ByteOrderStreamExtensions;
using ICSharpCode.SharpZipLib.Core;

// ReSharper disable InconsistentNaming

namespace ICSharpCode.SharpZipLib.Tests.Core
{
	[TestFixture]
	[Category("Core")]
	public class ByteOrderUtilsTests
	{
		private const short native16 = 0x1234;
		private static readonly byte[] swapped16 = { 0x34, 0x12 };

		private const int native32 = 0x12345678;
		private static readonly byte[] swapped32 = { 0x78, 0x56, 0x34, 0x12 };

		private const long native64 = 0x123456789abcdef0;
		private static readonly byte[] swapped64 = { 0xf0, 0xde, 0xbc, 0x9a, 0x78, 0x56, 0x34, 0x12 };
		
		[Test]
		public void ToSwappedBytes()
		{
			Assert.AreEqual(swapped16, BO.SwappedBytes(native16));
			Assert.AreEqual(swapped16, BO.SwappedBytes((ushort)native16));

			Assert.AreEqual(swapped32, BO.SwappedBytes(native32));
			Assert.AreEqual(swapped32, BO.SwappedBytes((uint)native32));

			Assert.AreEqual(swapped64, BO.SwappedBytes(native64));
			Assert.AreEqual(swapped64, BO.SwappedBytes((ulong)native64));
		}

		[Test]
		public void FromSwappedBytes()
		{
			Assert.AreEqual(native16, BO.SwappedS16(swapped16));
			Assert.AreEqual(native16, BO.SwappedU16(swapped16));
			
			Assert.AreEqual(native32, BO.SwappedS32(swapped32));
			Assert.AreEqual(native32, BO.SwappedU32(swapped32));
			
			Assert.AreEqual(native64, BO.SwappedS64(swapped64));
			Assert.AreEqual(native64, BO.SwappedU64(swapped64));
		}

		[Test]
		public void ReadLESigned16() 
			=> TestReadLE(native16, 2, BO.ReadLEShort);

		[Test]
		public void ReadLESigned32() 
			=> TestReadLE(native32,4, BO.ReadLEInt);
		
		[Test]
		public void ReadLESigned64()
			=> TestReadLE(native64,8, BO.ReadLELong);

		[Test]
		public void WriteLESigned16() 
			=> TestWriteLE(swapped16, s => s.WriteLEShort(native16));
		
		[Test]
		public void WriteLESigned32() 
			=> TestWriteLE(swapped32, s => s.WriteLEInt(native32));	
		
		[Test]
		public void WriteLESigned64() 
			=> TestWriteLE(swapped64, s => s.WriteLELong(native64));
		
		[Test]
		public void WriteLEUnsigned16() 
			=> TestWriteLE(swapped16, s => s.WriteLEUshort((ushort)native16));
				
		[Test]
		public void WriteLEUnsigned32() 
			=> TestWriteLE(swapped32, s => s.WriteLEUint(native32));
				
		[Test]
		public void WriteLEUnsigned64() 
			=> TestWriteLE(swapped64, s => s.WriteLEUlong(native64));
		
		[Test]
		public async Task WriteLEAsyncSigned16() 
			=> await TestWriteLEAsync(swapped16, (int)native16, BO.WriteLEShortAsync);
		
		[Test]
		public async Task WriteLEAsyncUnsigned16() 
			=> await TestWriteLEAsync(swapped16, (ushort)native16, BO.WriteLEUshortAsync);
		
		[Test]
		public async Task WriteLEAsyncSigned32() 
			=> await TestWriteLEAsync(swapped32, native32, BO.WriteLEIntAsync);		
		[Test]
		public async Task WriteLEAsyncUnsigned32() 
			=> await TestWriteLEAsync(swapped32, (uint)native32, BO.WriteLEUintAsync);
				
		[Test]
		public async Task WriteLEAsyncSigned64() 
			=> await TestWriteLEAsync(swapped64, native64, BO.WriteLELongAsync);		
		[Test]
		public async Task WriteLEAsyncUnsigned64() 
			=> await TestWriteLEAsync(swapped64, (ulong)native64, BO.WriteLEUlongAsync);

		
		private static void TestReadLE<T>(T expected, int bytes, Func<Stream, T> read)
		{
			using (var ms = new MemoryStream(swapped64, 8 - bytes, bytes))
			{
				Assert.AreEqual(expected, read(ms));
			}
		}

		private static void TestWriteLE(byte[] expected, Action<Stream> write)
		{
			using (var ms = new MemoryStream())
			{
				write(ms);
				Assert.AreEqual(expected, ms.ToArray());
			}
		}

		private static async Task TestWriteLEAsync<T>(byte[] expected, T input, Func<Stream, T, CancellationToken, Task> write)
		{
			using (var ms = new MemoryStream())
			{
				await write(ms, input, CancellationToken.None);
				Assert.AreEqual(expected, ms.ToArray());
			}
		}
	}
}
