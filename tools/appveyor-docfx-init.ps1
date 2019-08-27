if(-Not $env:APPVEYOR_PULL_REQUEST_TITLE -and $env:CONFIGURATION -eq "Release")
{
    git checkout $env:APPVEYOR_REPO_BRANCH -q
    choco install docfx -y
}