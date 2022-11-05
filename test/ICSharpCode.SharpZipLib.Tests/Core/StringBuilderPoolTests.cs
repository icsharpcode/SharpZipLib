using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Core;
using NUnit.Framework;

namespace ICSharpCode.SharpZipLib.Tests.Core
{
	[TestFixture]
	public class StringBuilderPoolTests
	{
		[Test]
		[Category("Core")]
		public void RoundTrip()
		{
			var pool = new StringBuilderPool();
			var builder1 = pool.Rent();
			pool.Return(builder1);
			var builder2 = pool.Rent();
			Assert.AreEqual(builder1, builder2);
		}

		[Test]
		[Category("Core")]
		public void ReturnsClears()
		{
			var pool = new StringBuilderPool();
			var builder1 = pool.Rent();
			builder1.Append("Hello");
			pool.Return(builder1);
			Assert.AreEqual(0, builder1.Length);
		}

		[Test]
		[Category("Core")]
		public async Task ThreadSafeAsync()
		{
			// use a lot of threads to increase the likelihood of errors
			var concurrency = 100;
			
			var pool = new StringBuilderPool();
			var gate = new TaskCompletionSource<bool>();
			var startedTasks = new Task[concurrency];
			var completedTasks = new Task<string>[concurrency];
			for (int i = 0; i < concurrency; i++)
			{
				var started = new TaskCompletionSource<bool>();
				startedTasks[i] = started.Task;
				var captured = i;
				completedTasks[i] = Task.Run(async () =>
				{
					started.SetResult(true);
					await gate.Task;
					var builder = pool.Rent();
					builder.Append("Hello ");
					builder.Append(captured);
					var str = builder.ToString();
					pool.Return(builder);
					return str;
				});
			}

			// make sure all the threads have started
			await Task.WhenAll(startedTasks);
			
			// let them all loose at the same time
			gate.SetResult(true);

			// make sure every thread produces the expected string and hence had its own StringBuilder
			var results = await Task.WhenAll(completedTasks);
			for (int i = 0; i < concurrency; i++)
			{
				var result = results[i];
				Assert.AreEqual($"Hello {i}", result);
			}
		}
	}
}
