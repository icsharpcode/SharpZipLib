Write-Host "Installing .NET MicroFramework 4.3 ..."
$msiPath = "$($env:USERPROFILE)\MicroFrameworkSDK43.MSI"
(New-Object Net.WebClient).DownloadFile('http://download-codeplex.sec.s-msft.com/Download/Release?ProjectName=netmf&DownloadId=1423116&FileTime=130667921437670000&Build=21031', $msiPath)
cmd /c start /wait msiexec /i $msiPath /quiet
Write-Host "Installed" -ForegroundColor green

Write-Host "Installing .NET MicroFramework 4.4 ..."
$msiPath = "$($env:USERPROFILE)\MicroFrameworkSDK44.MSI"
(New-Object Net.WebClient).DownloadFile('https://github.com/NETMF/netmf-interpreter/releases/download/v4.4-RTW-20-Oct-2015/MicroFrameworkSDK.MSI', $msiPath)
cmd /c start /wait msiexec /i $msiPath /quiet
Write-Host "Installed" -ForegroundColor green
