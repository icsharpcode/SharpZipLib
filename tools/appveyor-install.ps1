# Since alpha1 used assembly version v1.0.0.0 we use that as a base line for build revisions
$v1threshold = "cbd05248c25de00bd6437d6217b91890af560496";

$revision = $(git rev-list "$v1threshold..HEAD" --count)

$commit = $(git rev-parse --short HEAD);
$tag = $(git describe --tags --abbrev=0);
$tagVersion = $tag.Substring(1);

$parts = $tagVersion.Split("-");
if ($parts.length > 1) {
    $preRelease = $parts[1];
}
else {
    $preRelease = "";
}

$parts = $parts[0].Split(".");

$major = $parts[0];
$minor = $parts[1];
$patch = $parts[2];

$changes = $(git rev-list "$tag..HEAD" --count);

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

$isRelease = -not ($changes -gt 0 -or $branch -or $suffix);

$build = "_${env:APPVEYOR_BUILD_NUMBER}";

if ($isRelease) {
    $version = "$tagVersion";
} else {
    $version = "$tagVersion-git$commit";
}

$av_version = "$version$branch$suffix$build";
$as_version = "$major.$minor.$patch.$revision";
$env:APPVEYOR_BUILD_VERSION=$av_version;
$env:AS_VERSION=$as_version;
$env:VERSION=$version;

write-host -n "Version: "
write-host -f magenta $version;

write-host -n "Assembly version: "
write-host -f magenta $env:AS_VERSION;

write-host -n "Pre-release: "
if($preRelease) {
    write-host -f green "No";
} else {
    write-host -f yellow $preRelease;
}

write-host -n "Release: "
if($isRelease) {
    write-host -f green $tag;
} else {
    write-host -f yellow "No";
    write-host -n "Branch: ";
    write-host -f magenta $env:APPVEYOR_REPO_BRANCH;
    write-host -n "Changes: ";
    write-host -f magenta $changes;
}

write-host -n "AppVeyor build version: ";
write-host -f green $av_version;

appveyor UpdateBuild -Version $av_version;