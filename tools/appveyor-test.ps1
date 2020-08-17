$proj = ".\test\ICSharpCode.SharpZipLib.TestBootstrapper\ICSharpCode.SharpZipLib.TestBootstrapper.csproj";
$resxml = ".\docs\nunit3-test-results-debug.xml";

# Nuget 3 Console runner:
#$tester = "nunit3-console .\test\ICSharpCode.SharpZipLib.Tests\bin\$($env:CONFIGURATION)\netcoreapp2.0\ICSharpCode.SharpZipLib.Tests.dll"

# Bootstrapper:
$tester = "dotnet run -f netcoreapp3.1 -p $proj -c $env:CONFIGURATION";
iex "$tester --explore=tests.xml";

[xml]$xml = Get-Content("tests.xml");
$assembly = select-xml "/test-suite[@type='Assembly']" $xml | select -f 1 -exp Node;
$testcases = select-xml "//test-case" $xml | % { Add-AppveyorTest -Name $_.Node.fullname -Framework NUnit -Filename $assembly.name };

iex "$tester --result=$resxml";

$wc = New-Object 'System.Net.WebClient';
$wc.UploadFile("https://ci.appveyor.com/api/testresults/nunit3/$($env:APPVEYOR_JOB_ID)", (Resolve-Path $resxml));