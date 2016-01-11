@echo off
cd /d "%~dp0src"
msbuild ICSharpCode.SharpZLib.csproj /nologo /t:Rebuild /p:Configuration=Release
nuget pack -symbols ICSharpCode.SharpZLib.csproj