param(
    [string]$Subject = "CN=BuildInPlugins Local Signing",
    [string]$OutputDirectory = "",
    [string]$PfxFileName = "BuildInPluginsSigning.pfx",
    [string]$Base64FileName = "BuildInPluginsSigning.base64.txt",
    [string]$CerFileName = "BuildInPluginsSigning.cer",
    [string]$Password = "",
    [switch]$TrustCert
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $env:USERPROFILE "Desktop"
}

if ([string]::IsNullOrWhiteSpace($Password)) {
    $passwordInput = Read-Host "Enter a password to protect the exported PFX"
    if ([string]::IsNullOrWhiteSpace($passwordInput)) {
        throw "A non-empty password is required to export the PFX."
    }

    $Password = $passwordInput
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$pfxPath = Join-Path $OutputDirectory $PfxFileName
$base64Path = Join-Path $OutputDirectory $Base64FileName
$cerPath = Join-Path $OutputDirectory $CerFileName

$securePassword = ConvertTo-SecureString -String $Password -Force -AsPlainText

$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject $Subject `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -HashAlgorithm SHA256

Export-PfxCertificate `
    -Cert "Cert:\CurrentUser\My\$($cert.Thumbprint)" `
    -FilePath $pfxPath `
    -Password $securePassword | Out-Null

Export-Certificate `
    -Cert "Cert:\CurrentUser\My\$($cert.Thumbprint)" `
    -FilePath $cerPath | Out-Null

[Convert]::ToBase64String([IO.File]::ReadAllBytes($pfxPath)) |
    Set-Content -Path $base64Path -NoNewline

if ($TrustCert) {
    Import-Certificate `
        -FilePath $cerPath `
        -CertStoreLocation "Cert:\CurrentUser\Root" | Out-Null
}

Write-Host ""
Write-Host "Certificate created successfully."
Write-Host "Thumbprint: $($cert.Thumbprint)"
Write-Host "PFX: $pfxPath"
Write-Host "CER: $cerPath"
Write-Host "Base64: $base64Path"
Write-Host ""
Write-Host "GitHub Secrets:"
Write-Host "BUILD_SIGNING_CERT_BASE64 = contents of $base64Path"
Write-Host "BUILD_SIGNING_CERT_PASSWORD = $Password"
