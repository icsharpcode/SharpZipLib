# Describe from the lastest tag matching 'vX.Y.Z', removing the initial 'v'
$description = $(git describe --long --tags --match 'v[0-9]*.[0-9]*.[0-9]*').substring(1);

# Description is in the format of: TAG-COMMITS_SINCE_TAG-COMMIT_HASH
$dparts = $description -split('-');
$short_version = $dparts[0];
$commits_since_tag = $dparts[1];
$commit_hash = $dparts[2];

$masterBranches = @("master");

# If not in master branch, set branch variable
$av_branch = $env:APPVEYOR_REPO_BRANCH;
$branch = $(if ($masterBranches -contains $av_branch) { "" } else { "-$av_branch" });

# If this is a PR, add the PR suffix
$suffix = $(if ($env:APPVEYOR_PULL_REQUEST_NUMBER) { "-pr$env:APPVEYOR_PULL_REQUEST_NUMBER" } else { "" });

# Main build is when we're in the master branch and not a PR
$is_main_build = ($branch -eq "" -and $suffix -eq "")

# Use YYDDD as the build for main builds, otherwise use 99999
if($is_main_build) {
	$today = Get-Date
	$build = $today.ToString("yy") + $today.DayOfYear.ToString("d3")
} else {
	$build = "_${env:APPVEYOR_BUILD_NUMBER}"
}

$is_release_build = ($commits_since_tag -eq 0 -and $is_main_build) 

$version = $(if ($is_release_build) { $short_version } else { "$short_version-$commit_hash" })

write-host -n "Release type: ";
if ($is_release_build) {write-host -f green 'release'} else { write-host -f yellow 'pre-release'}

write-host -n "NuGet Package Version: ";
write-host -n -f green $short_version;
if (!$is_release_build) {
	write-host -n " (";
	write-host -n -f cyan $version;
	write-host ")";
} else {
	write-host "";
}


$av_version = "$version$branch$suffix$build";

$env:APPVEYOR_BUILD_VERSION=$av_version;
$env:SHORT_VERSION=$short_version;
$env:VERSION=$version;

write-host -n "AppVeyor Build Version: ";
write-host -f green $av_version;

appveyor UpdateBuild -Version $av_version