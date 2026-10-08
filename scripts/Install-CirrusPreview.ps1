# Experimental unsigned-origin fork build. Review files and the publisher before running.
# Requires an elevated Windows PowerShell terminal for LocalMachine certificate trust.
$ErrorActionPreference = "Stop"
$principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Open PowerShell as Administrator and run this installer from the extracted ZIP folder."
}
$certificate = Join-Path $PSScriptRoot "CirrusPreview.cer"
$package = Join-Path $PSScriptRoot "Cirrus-Preview-win-x64.msix"
if (!(Test-Path -LiteralPath $certificate) -or !(Test-Path -LiteralPath $package)) {
    throw "Extract the entire Cirrus-Preview-MSIX.zip and run the script from that folder."
}
Write-Host "This test installer will trust a TEMPORARY build certificate in LocalMachine/TrustedPeople."
Write-Host "The certificate is NOT installed into Trusted Root."
$cert = Import-Certificate -FilePath $certificate -CertStoreLocation "Cert:\LocalMachine\TrustedPeople"
Write-Host "Trusted publisher thumbprint: $($cert.Thumbprint)"
Add-AppxPackage -Path $package -ErrorAction Stop
Write-Host "Cirrus Preview installed. Launch it from the Windows Start menu."
Write-Host "To uninstall later, use: Get-AppxPackage -Name wadaxiyang.CirrusPreview | Remove-AppxPackage"
