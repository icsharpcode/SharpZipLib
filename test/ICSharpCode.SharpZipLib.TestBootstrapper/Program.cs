using System;
using NUnit.Common;
using NUnitLite;
using System.Reflection;

namespace ICSharpCode.SharpZipLib.TestBootstrapper
{
	public class Program
    {
		static void Main(string[] args)
		{
			new AutoRun(typeof(Tests.Base.InflaterDeflaterTestSuite).GetTypeInfo().Assembly)
			.Execute(args);
		}

    }
}
