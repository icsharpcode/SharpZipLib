using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace ICSharpCode.SharpZipLib.Tests.TestSupport
{
	internal static class PerformanceTesting
	{
		private const double ByteToMB = 1000000;
		private const int PacifierOffset = 0x100000;

		public static void TestReadWrite(TestDataSize size, Func<Stream, Stream> input, Func<Stream, Stream> output, Action<Stream> outputClose = null)
			=> TestReadWrite((int)size, input, output);

		public static void TestWrite(TestDataSize size, Func<Stream, Stream> output, Action<Stream> outputClose = null)
			=> TestWrite((int)size, output, outputClose);

		public static void TestReadWrite(int size, Func<Stream, Stream> input, Func<Stream, Stream> output, Action<Stream> outputClose = null)
		{
			var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
			var window = new WindowedStream(size, cts.Token);

			var readerState = new PerfWorkerState()
			{
				bytesLeft = size,
				token = cts.Token,
				baseStream = window,
				streamCtr = input,
			};

			var writerState = new PerfWorkerState()
			{
				bytesLeft = size,
				token = cts.Token,
				baseStream = window,
				streamCtr = output,
				streamCls = outputClose
			};

			var reader = new Thread(stateObject =>
			{
				var state = (PerfWorkerState)stateObject;
				try
				{
					// Run output stream constructor
					state.InitStream();

					// Main read loop
					ReadTargetBytes(ref state);

					if (!state.token.IsCancellationRequested)
					{
						Assert.IsFalse(state.baseStream.CanRead, "Base Stream should be closed");

						// This shouldnt read any data but should read the footer
						var buffer = new byte[1];
						int readBytes = state.stream.Read(buffer, 0, 1);
						Assert.LessOrEqual(readBytes, 0, "Stream should be empty");
					}

					// Dispose of the input stream
					state.stream.Close();
				}
				catch (Exception x)
				{
					state.exception = x;
				}
			});

			Thread writer = new Thread(stateObject =>
			{
				var state = (PerfWorkerState)stateObject;
				try
				{
					// Run input stream constructor
					state.InitStream();

					// Main write loop
					WriteTargetBytes(ref state);

					state.DeinitStream();

					// Dispose of the input stream
					state.stream.Close();
				}
				catch (Exception x)
				{
					state.exception = x;
				}
			});

			var sw = Stopwatch.StartNew();

			writer.Name = "Writer";
			writer.Start(writerState);

			// Give the writer thread a couple of seconds to write headers
			Thread.Sleep(TimeSpan.FromSeconds(3));

			reader.Name = "Reader";
			reader.Start(readerState);

			bool writerJoined = false, readerJoined = false;
			const int timeout = 100;

			while (!writerJoined && !readerJoined)
			{
				writerJoined = writer.Join(timeout);
				if (writerJoined && writerState.exception != null)
					ExceptionDispatchInfo.Capture(writerState.exception).Throw();

				readerJoined = reader.Join(timeout);
				if (readerJoined && readerState.exception != null)
					ExceptionDispatchInfo.Capture(readerState.exception).Throw();

				if (cts.IsCancellationRequested) break;
			}

			//Assert.IsTrue(writerJoined, "Timed out waiting for reader thread to join");
			//Assert.IsTrue(readerJoined, "Timed out waiting for writer thread to join");

			Assert.IsFalse(cts.IsCancellationRequested, "Threads were cancelled before completing execution");

			var elapsed = sw.Elapsed;
			var testSize = size / ByteToMB;
			Console.WriteLine($"Time {elapsed:mm\\:ss\\.fff} throughput {testSize / elapsed.TotalSeconds:f2} MB/s (using test size: {testSize:f2} MB)");
		}

		public static void TestWrite(int size, Func<Stream, Stream> output, Action<Stream> outputClose = null)
		{
			var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

			var sw = Stopwatch.StartNew();
			var writerState = new PerfWorkerState()
			{
				bytesLeft = size,
				token = cts.Token,
				baseStream = new NullStream(),
				streamCtr = output,
			};

			writerState.InitStream();
			WriteTargetBytes(ref writerState);

			writerState.DeinitStream();

			writerState.stream.Close();

			var elapsed = sw.Elapsed;
			var testSize = size / ByteToMB;
			Console.WriteLine($"Time {elapsed:mm\\:ss\\.fff} throughput {testSize / elapsed.TotalSeconds:f2} MB/s (using test size: {testSize:f2} MB)");
		}

		internal static void WriteTargetBytes(ref PerfWorkerState state)
		{
			const int bufferSize = 8192;
			byte[] buffer = new byte[bufferSize];
			int bytesToWrite = bufferSize;

			while (state.bytesLeft > 0 && !state.token.IsCancellationRequested)
			{
				if (state.bytesLeft < bufferSize)
					bytesToWrite = bufferSize;

				state.stream.Write(buffer, 0, bytesToWrite);
				state.bytesLeft -= bytesToWrite;
			}
		}

		internal static void ReadTargetBytes(ref PerfWorkerState state)
		{
			const int bufferSize = 8192;
			byte[] buffer = new byte[bufferSize];
			int bytesRead, bytesToRead = bufferSize;

			int pacifierLevel = state.bytesLeft - PacifierOffset;

			while ((state.bytesLeft > 0) && !state.token.IsCancellationRequested)
			{
				if (state.bytesLeft < bufferSize)
					bytesToRead = bufferSize;

				bytesRead = state.stream.Read(buffer, 0, bytesToRead);
				state.bytesLeft -= bytesRead;

				if (state.bytesLeft <= pacifierLevel)
				{
					Debug.WriteLine($"Reader {state.bytesLeft} bytes remaining");
					pacifierLevel = state.bytesLeft - PacifierOffset;
				}

				if (bytesRead == 0) break;
			}
		}
	}

	internal class PerfWorkerState
	{
		public Stream stream;
		public Stream baseStream;
		public int bytesLeft;
		public Exception exception;
		public CancellationToken token;
		public Func<Stream, Stream> streamCtr;
		public Action<Stream> streamCls;

		public void InitStream()
		{
			stream = streamCtr(baseStream);
		}

		public void DeinitStream()
		{
			streamCls?.Invoke(stream);
		}
	}

	public enum TestDataSize : int
	{
		Large = 0x10000000,
		Medium = 0x5000000,
		Small = 0x1400000,
	}
}
