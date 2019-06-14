$netfx3 = Get-ChildItem 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP' -recurse |
          Get-ItemProperty -name Version,Release -EA 0 |
          Where-Object { $_.PSChildName -eq 'v3.5'} |
          Select-Object PSChildName

Write-Host ".NET 3.5 Framework:" -NoNewline;
if($null -eq $netfx3){
  Write-Host "Not Installed" -ForegroundColor Red;   
  Write-Host "Installing .NET 3.5 Framework:" -ForegroundColor Yellow -NoNewline;
  $result = Add-WindowsCapability -Online -Name NetFX3~~~~
  Write-Host "$($result)" -ForegroundColor Green  
}
else {
  Write-Host " installed" -ForegroundColor Green;  
}


$vsconfing = New-TemporaryFile
Write-Host "Export Configuration to $($vsconfing.FullName)" 
$vsinstaller = [System.Environment]::ExpandEnvironmentVariables("%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vs_installer.exe")

Write-Host ".NET 3.5 Framework VS Tools:" -NoNewline;
#$exitCode = Start-Process -FilePath "$($vsinstaller)" -ArgumentList "--productId", "Microsoft.VisualStudio.Product.Community", "--channelId", "VisualStudio.15.Release", "export",  "--config", "$($vsconfing.FullName)"  , "--quiet" -Wait -PassThru

$startInfo = New-Object System.Diagnostics.ProcessStartInfo
$startInfo.FileName = $vsinstaller
$startInfo.Arguments = "--productId Microsoft.VisualStudio.Product.Community --channelId VisualStudio.15.Release export --config $($vsconfing.FullName) --quiet" 
$process = New-Object System.Diagnostics.Process
$process.StartInfo = $startInfo
$executed = $process.Start() 
$process.WaitForExit()

$netFx3Tool =  Get-Content $vsconfing.FullName |
ConvertFrom-Json |
Select-Object -expand components |
Where-Object {$_  -eq  "microsoft.net.component.3.5.developertools"}

if(($null -eq $netFx3Tool) -or ($netFx3Tool -eq ""  ))
{
  Write-Host " not installed" -ForegroundColor Red
  Write-Host "Add .NET 3.5 Framework target:" -ForegroundColor Yellow -NoNewline
  $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $vsinstaller
    $startInfo.Arguments = "--productId Microsoft.VisualStudio.Product.Community --channelId VisualStudio.15.Release modify --add microsoft.net.component.3.5.developertools;includeRecommended --quiet" 
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    $executed = $process.Start() 
    $process.WaitForExit()
    Write-Host "Exit code $($process.ExitCode)" -ForegroundColor Green
    if( ($executed -eq $true)  -and ($process.ExitCode -ne 0))
    {
      Write-Error "Failed to add .NET 3.5 Framework target exit code $($process.ExitCode)"
    }
    else {
      Write-Information "Installation successful";
    }
}
else {
  Write-Host " installed" -ForegroundColor Green
}



