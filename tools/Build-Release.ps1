[CmdletBinding()]
param(
    [string]$Version = '0.1.1',
    [string]$TestedClientVersion = '2.32.10',
    [string]$TestedGamePackage = '20260814000',
    [string]$OutputRoot,
    [string]$BepInExArchive,
    [switch]$SkipInstallerMatrix
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$builder = Join-Path $PSScriptRoot 'Build-Installer.ps1'
$tester = Join-Path $PSScriptRoot 'Test-Installer.ps1'

$buildArguments = @{
    Version = $Version
    TestedClientVersion = $TestedClientVersion
    TestedGamePackage = $TestedGamePackage
}
if (-not [string]::IsNullOrWhiteSpace($OutputRoot)) {
    $buildArguments.OutputRoot = $OutputRoot
}
if (-not [string]::IsNullOrWhiteSpace($BepInExArchive)) {
    $buildArguments.BepInExArchive = $BepInExArchive
}

& $builder @buildArguments
if ($LASTEXITCODE -ne 0) {
    throw 'A compilação do instalador falhou.'
}

$effectiveOutputRoot = if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    Join-Path $repositoryRoot 'dist'
}
else {
    [System.IO.Path]::GetFullPath($OutputRoot)
}
$installer = Join-Path $effectiveOutputRoot "BD2PTBRv$Version.exe"

if (-not $SkipInstallerMatrix) {
    & $tester -InstallerPath $installer
    if ($LASTEXITCODE -ne 0) {
        throw 'A matriz de validação do instalador falhou.'
    }
}

$gameBananaPackage = Join-Path $effectiveOutputRoot "BD2PTBRv$Version.zip"
if (Test-Path -LiteralPath $gameBananaPackage) {
    Remove-Item -LiteralPath $gameBananaPackage -Force
}
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::Open($gameBananaPackage, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
        $archive,
        $installer,
        [System.IO.Path]::GetFileName($installer),
        [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
}
finally {
    $archive.Dispose()
}

$verificationArchive = [System.IO.Compression.ZipFile]::OpenRead($gameBananaPackage)
try {
    if ($verificationArchive.Entries.Count -ne 1 -or
        $verificationArchive.Entries[0].FullName -ne [System.IO.Path]::GetFileName($installer)) {
        throw 'O pacote do GameBanana deve conter somente o instalador gráfico.'
    }
}
finally {
    $verificationArchive.Dispose()
}

Write-Host "Distribuição $Version compilada e validada."
Write-Host "Pacote do GameBanana: $gameBananaPackage"
