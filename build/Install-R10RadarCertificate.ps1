param(
  [string]$CertificatePath = (Join-Path $PSScriptRoot "R10RadarApp.cer")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $CertificatePath)) {
  throw "Certificate not found: $CertificatePath"
}

$certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($CertificatePath)
Write-Host "Trusting publisher: $($certificate.Subject)"
Write-Host "Certificate thumbprint: $($certificate.Thumbprint)"

Import-Certificate -FilePath $CertificatePath -CertStoreLocation Cert:\CurrentUser\Root | Out-Null
Import-Certificate -FilePath $CertificatePath -CertStoreLocation Cert:\CurrentUser\TrustedPublisher | Out-Null

Write-Host "R10 Radar App is now trusted for the current Windows user."
Write-Host "Press Enter to close."
Read-Host | Out-Null
