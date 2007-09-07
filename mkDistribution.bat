@echo "mkDistribution v1.3"

if exist current (
	rmdir /s /q current
)

mkdir current

mkdir current\netcf-10
nant -t:netcf-1.0 -D:build.output.dir=current\netcf-10 -buildfile:sharpZLib.build build

mkdir current\netcf-20
nant -t:netcf-2.0 -D:build.output.dir=current\netcf-20 -buildfile:sharpZLib.build build

mkdir current\net-11
nant -t:net-1.1 -D:build.output.dir=current\net-11 -buildfile:sharpZLib.build build

mkdir current\net-20
nant -t:net-2.0 -D:build.output.dir=current\net-20 -buildfile:sharpZLib.build build


mkdir current\doc
copy doc\readme.rtf current\doc
copy doc\Changes.txt current\doc
copy doc\Copying.txt current\doc
copy SharpZipLib.chm current\doc
copy installGAC.bat current
copy uninstallGAC.bat current

"c:\Program Files\EWSoftware\Sandcastle Help File Builder\SandcastleBuilderConsole.exe" .\SharpZipLib.shfb

samples\cs\bin\sz -rc current\SharpZipLib.zip current\*.dll

@echo TODO Compress source to SharpZipLib_SourceSamples.zip
@echo TODO Build Bin Zip files
