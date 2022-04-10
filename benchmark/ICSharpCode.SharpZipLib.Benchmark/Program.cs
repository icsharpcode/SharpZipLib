using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using ICSharpCode.SharpZipLib.Benchmark.Tar;

namespace ICSharpCode.SharpZipLib.Benchmark
{
	public class MultipleRuntimes : ManualConfig
	{
		public MultipleRuntimes()
		{
			AddJob(Job.Default.WithToolchain(CsProjClassicNetToolchain.Net461).AsBaseline()); // NET 4.6.1
			AddJob(Job.Default.WithToolchain(CsProjCoreToolchain.NetCoreApp21)); // .NET Core 2.1
			AddJob(Job.Default.WithToolchain(CsProjCoreToolchain.NetCoreApp31)); // .NET Core 3.1
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
			
			// var output = new TarOutputStream();
			// for (int i = 0; i < 1_000_000; i++)
			// {
			// 	output.WriteTarOutputStream();
			// }
		}
	}
}
