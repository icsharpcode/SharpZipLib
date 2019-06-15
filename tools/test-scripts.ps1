$env:APPVEYOR_BUILD_NUMBER = 123;

function Test-Install {
 param( [string]$PR, [string]$Branch )
 Write-Host -n "-- Testing with PR "
 if($PR -eq "") { Write-Host -n -f red "No" } else { Write-Host -n -f cyan "#$PR" }
 Write-Host -n " and branch "
 if($Branch -eq "master") { Write-Host -f green "master" } else { Write-Host -f cyan "$Branch" }
 $env:APPVEYOR_PULL_REQUEST_NUMBER = $PR;
 $env:APPVEYOR_REPO_BRANCH = $Branch;
 .\appveyor-install.ps1
 Write-Host "---------------------------------------------------------------------";
 Write-Host;
}

function appveyor {
	# Dummy function for compability with AV
}

Write-Host;
Test-Install "" "master"
Test-Install 42 "master"
Test-Install "" "misc-feature"
Test-Install 36 "other-branch"