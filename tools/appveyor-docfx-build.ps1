if(-Not $env:APPVEYOR_PULL_REQUEST_TITLE -and $env:CONFIGURATION -eq "Release")
{
    pushd docs\help
    # & docfx metadata
    & docfx docfx.json
    if ($lastexitcode -ne 0){
      throw [System.Exception] "docfx build failed with exit code $lastexitcode."
    }
    popd
<#  
    ## Useful for automatically updating gh pages:
    
    git config --global credential.helper store
    Add-Content "$env:USERPROFILE\.git-credentials" "https://$($env:access_token):x-oauth-basic@github.com`n"
    git config --global user.email $env:op_build_user_email
    git config --global user.name $env:op_build_user
    git clone https://github.com/ICSharpCode/SharpZipLib.git -b gh-pages origin_site -q
    Copy-Item origin_site/.git _site -recurse
    CD _site
    git add -A 2>&1
    git commit -m "CI Updates" -q
    git push origin gh-pages -q
#>
}