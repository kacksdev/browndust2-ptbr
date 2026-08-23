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

if (-not $SkipInstallerMatrix) {
    $effectiveOutputRoot = if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        Join-Path $repositoryRoot 'dist'
    }
    else {
        [System.IO.Path]::GetFullPath($OutputRoot)
    }
    $installer = Join-Path $effectiveOutputRoot "BD2PTBR-Installer-v$Version.exe"
    & $tester -InstallerPath $installer
    if ($LASTEXITCODE -ne 0) {
        throw 'A matriz de validação do instalador falhou.'
    }
}

Write-Host "Distribuição $Version compilada e validada."
