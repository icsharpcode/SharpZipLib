$describe = $(git describe --long) -split('-')

$short_version = $describe[0] + '.' + $describe[1]

$masterBranches = @("master");

if ($masterBranches -contains $env:APPVEYOR_REPO_BRANCH) {
	$branch = "";
} else {
	$branch = "-$env:APPVEYOR_REPO_BRANCH";
}

if ($env:APPVEYOR_PULL_REQUEST_NUMBER) {
	$suffix = "-pr$env:APPVEYOR_PULL_REQUEST_NUMBER";
} else {
	$suffix = "";
}

$release_build = ($describe[1] -eq 0 -and $branch -eq "" -and $suffix -eq "") 

$version = $(if ($release_build) { $short_version } else { $short_version + '-' + $describe[2] })

write-host -n "Release type: ";
if ($release_build) {write-host -f green 'release'} else { write-host -f yellow 'pre-release'}

write-host -n "NuGet Package Version: ";
write-host -n -f green $short_version;
if (!$release_build) {
	write-host -n " (";
	write-host -n -f cyan $version;
	write-host ")";
} else {
	write-host "";
}

$build = "_${env:APPVEYOR_BUILD_NUMBER}"
$av_version = "$version$branch$suffix$build";

$env:APPVEYOR_BUILD_VERSION=$av_version;
$env:SHORT_VERSION=$short_version;
$env:VERSION=$version;

write-host -n "AppVeyor Build Version: ";
write-host -f green $av_version;

appveyor UpdateBuild -Version $av_version