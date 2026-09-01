param(
  [string]$OutputDirectory = (Join-Path (Split-Path $PSScriptRoot -Parent) "publish")
)

$ErrorActionPreference = "Stop"
$projectDirectory = Split-Path $PSScriptRoot -Parent
$certificateSubject = "CN=R10 Radar App"

$certificate = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
  Where-Object { $_.Subject -eq $certificateSubject -and $_.NotAfter -gt (Get-Date) } |
  Sort-Object NotAfter -Descending |
  Select-Object -First 1

if (-not $certificate) {
  $certificate = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject $certificateSubject `
    -FriendlyName "R10 Radar App Code Signing" `
    -CertStoreLocation Cert:\CurrentUser\My `
    -HashAlgorithm SHA256 `
    -KeyAlgorithm RSA `
    -KeyLength 3072 `
    -NotAfter (Get-Date).AddYears(5)
}

Import-Certificate -FilePath (Export-Certificate -Cert $certificate -FilePath (Join-Path $env:TEMP "R10RadarApp.cer") -Force).FullName -CertStoreLocation Cert:\CurrentUser\Root | Out-Null
Import-Certificate -FilePath (Join-Path $env:TEMP "R10RadarApp.cer") -CertStoreLocation Cert:\CurrentUser\TrustedPublisher | Out-Null

dotnet publish (Join-Path $projectDirectory "gspro-r10.csproj") `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false `
  -o $OutputDirectory

if ($LASTEXITCODE -ne 0) {
  throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$frameworkBuild = Join-Path $projectDirectory "bin\Release\net8.0-windows10.0.19041\win-x64"
Copy-Item -LiteralPath (Join-Path $frameworkBuild "R10RadarApp.dll") -Destination $OutputDirectory -Force
Copy-Item -LiteralPath (Join-Path $frameworkBuild "R10RadarApp.deps.json") -Destination $OutputDirectory -Force
Copy-Item -LiteralPath (Join-Path $frameworkBuild "R10RadarApp.runtimeconfig.json") -Destination $OutputDirectory -Force
Copy-Item -LiteralPath (Join-Path $projectDirectory "build\Run-R10RadarApp.cmd") -Destination $OutputDirectory -Force
Copy-Item -LiteralPath (Join-Path $projectDirectory "settings.json") -Destination $OutputDirectory -Force

$executablePath = Join-Path $OutputDirectory "R10RadarApp.exe"
$signature = Set-AuthenticodeSignature -FilePath $executablePath -Certificate $certificate -HashAlgorithm SHA256
if ($signature.Status -ne "Valid") {
  throw "Signing failed: $($signature.StatusMessage)"
}

Export-Certificate -Cert $certificate -FilePath (Join-Path $OutputDirectory "R10RadarApp.cer") -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "Install-R10RadarCertificate.ps1") -Destination $OutputDirectory -Force

Write-Host "Signed publish complete: $OutputDirectory"
Write-Host "Publisher: $($certificate.Subject)"
Write-Host "Thumbprint: $($certificate.Thumbprint)"
