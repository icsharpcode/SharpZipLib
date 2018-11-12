using NUnitLite;
using System.Reflection;

namespace ICSharpCode.SharpZipLib.TestBootstrapper
{
	public class Program
	{
		private static void Main(string[] args)
		{
			new AutoRun(typeof(Tests.Base.InflaterDeflaterTestSuite).GetTypeInfo().Assembly)
			.Execute(args);
		}
	}
}
