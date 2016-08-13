# Define build command.
$buildCmd = "C:\Program Files (x86)\MSBuild\14.0\bin\msbuild.exe";
$buildArgs = @(
              "ICSharpCode.SharpZipLib.sln"
              "/l:C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll",
              "/m",
              "/p:UseSharedCompilation=false",
              "/p:Configuration=Release",
              "/p:Platform=Any CPU");

# If build is not a scheduled one, then simply build the project with MSBuild.
if ($env:APPVEYOR_SCHEDULED_BUILD -ne "True") {
  & $buildCmd $buildArgs
#  & nuget pack <project_file> -OutputDirectory <temp_path>
  return  # exit script
}

# Else, build project with Coverity Scan.
$publishCoverityExe = $env:APPVEYOR_BUILD_FOLDER + "\packages\PublishCoverity.0.11.0\tools\PublishCoverity.exe";
"Building project with Coverity Scan..."
& cov-build --dir Documentation\cov-int $buildCmd $buildArgs;

# Compress scan data.
& $publishCoverityExe compress -o Documentation\coverity.zip -i Documentation\cov-int;

# Upload scan data.
& $publishCoverityExe publish -z Documentation\coverity.zip -r McNeight/SharpZipLib -t $env:Coverity_Token -e $env:Coverity_Email -d "AppVeyor scheduled build";
