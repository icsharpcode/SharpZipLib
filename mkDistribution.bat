@echo "mkDistribution v1.1"

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

@echo todo generate documentation and the rest of the distribution image.
samples\cs\bin\sz -rc current\SharpZipLib.zip current\*.dll

mkdir current\source
copy doc\readme.rtf current\source
copy doc\Changes.txt current\source
copy doc\Copying.txt current\source
rem copy doc\SharpZipLib.chm current\source
copy *.bat current\source
copy *.build current\source


REM Compress source to SharpZipLib_SourceSamples.zip
REM Build CHM file
REM Build Bin Zip files