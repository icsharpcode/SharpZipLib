using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ICSharpCode.SharpZipLib.Tests.TestSupport
{
	/// <summary>
	/// Provides support for testing in memory zip archives.
	/// </summary>
	internal static class ZipTesting
	{
		public static void AssertValidZip(Stream stream, string password = null, bool usesAes = true)
		{
			using var zipFile = new ZipFile(stream)
			{
				IsStreamOwner = false,
				Password = password,
			};
			
			Assert.That(zipFile, Does.PassTestArchive());

			if (!string.IsNullOrEmpty(password) && usesAes)
			{
				Assert.Ignore("ZipInputStream does not support AES");
			}
			
			stream.Seek(0, SeekOrigin.Begin);

			Assert.DoesNotThrow(() =>
			{
				using var zis = new ZipInputStream(stream){Password = password};
				while (zis.GetNextEntry() != null)
				{
					new StreamReader(zis).ReadToEnd();
				}
			}, "Archive could not be read by ZipInputStream");
		}
	}

	public class TestArchiveReport
	{
		internal const string PassingArchive = "Passing Archive";
		
		readonly List<string> _messages = new List<string>();
		public void HandleTestResults(TestStatus status, string message)
		{
			if (string.IsNullOrWhiteSpace(message)) return;
			_messages.Add(message);
		}

		public override string ToString() => _messages.Any() ? string.Join(", ", _messages) : PassingArchive;
	}
	
	public class PassesTestArchiveConstraint : Constraint
	{
		private readonly string _password;
		private readonly bool _testData;

		public PassesTestArchiveConstraint(string password = null, bool testData = true)
		{
			_password = password;
			_testData = testData;
		}

		public override string Description => TestArchiveReport.PassingArchive;
		
		public override ConstraintResult ApplyTo<TActual>(TActual actual)
		{
			MemoryStream ms = null;
			try
			{
				if (!(actual is ZipFile zipFile))
				{
					if (!(actual is byte[] rawArchive))
					{
						return new ConstraintResult(this, actual, ConstraintStatus.Failure);
					}
					
					ms = new MemoryStream(rawArchive);
					zipFile = new ZipFile(ms){Password = _password};
				}

				var report = new TestArchiveReport();

				return new ConstraintResult(
					this, report, zipFile.TestArchive(
						_testData,
						TestStrategy.FindAllErrors,
						report.HandleTestResults
					)
					? ConstraintStatus.Success
					: ConstraintStatus.Failure);
			}
			finally
			{
				ms?.Dispose();
			}
		}
	}

	public static class ZipTestingConstraintExtensions
	{
		public static IResolveConstraint PassTestArchive(this ConstraintExpression expression, string password = null, bool testData = true)
		{
			var constraint = new PassesTestArchiveConstraint(password, testData);
			expression.Append(constraint);
			return constraint;
		}
	}

	/// <inheritdoc />
	public class Does: NUnit.Framework.Does
	{
		public static IResolveConstraint PassTestArchive(string password = null, bool testData = true)
			=> new PassesTestArchiveConstraint(password, testData);
		
		public static IResolveConstraint PassTestArchive(bool testData)
			=> new PassesTestArchiveConstraint(password: null, testData);
	}
}
