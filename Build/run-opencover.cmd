..\packages\OpenCover.4.6.519\tools\OpenCover.Console.exe -target:run-nunit3-tests-release.cmd -register:user -filter:+[ICSharpCode.SharpZipLib]* -output:..\Documentation\opencover-results-release.xml
..\packages\ReportGenerator.2.4.4.0\tools\ReportGenerator.exe -reports:..\Documentation\opencover-results-release.xml -targetdir:..\Documentation\opencover
