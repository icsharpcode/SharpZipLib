﻿using System;
using BenchmarkDotNet;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;

namespace ICSharpCode.SharpZipLib.Benchmark
{
	public class MultipleRuntimes : ManualConfig
	{
		public MultipleRuntimes()
		{
			Add(Job.Default.With(CsProjClassicNetToolchain.Net461).AsBaseline()); // NET 4.6.1
			Add(Job.Default.With(CsProjCoreToolchain.NetCoreApp21)); // .NET Core 2.1
			//Add(Job.Default.With(CsProjCoreToolchain.NetCoreApp30)); // .NET Core 3.0
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
		}
	}
}
