@echo off
cd bin
echo installing ICSharpCode.SharpZipLib.dll into the GAC
gacutil /i ICSharpCode.SharpZipLib.dll
cd ..
                                
