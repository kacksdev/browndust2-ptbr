#Requires -Version 7.0
[CmdletBinding()]
param([string]$OutputRoot)
$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
if (-not $OutputRoot) { $OutputRoot = Join-Path $repository 'dist\universal-launcher-candidate' }
$output = [IO.Path]::GetFullPath($OutputRoot)
if (Test-Path -LiteralPath $output) { throw "A saída deve ser uma pasta nova: $output" }
& dotnet build (Join-Path $repository 'installer\BD2PTBR.Installer\BD2PTBR.Installer.csproj') -c Release -p:IncludeEmbeddedPayload=false --nologo
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o atualizador.' }
New-Item -ItemType Directory -Path $output | Out-Null
$executable = Join-Path $output 'BD2PTBR.exe'
Copy-Item -LiteralPath (Join-Path $repository 'installer\BD2PTBR.Installer\bin\Release\net472\BD2PTBR-Installer.exe') -Destination $executable
$metadata = [ordered]@{
    schemaVersion = 1
    launcherVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($executable).ProductVersion
    file = 'BD2PTBR.exe'
    bytes = (Get-Item -LiteralPath $executable).Length
    sha256 = (Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash
    channel = 'https://raw.githubusercontent.com/kacksdev/browndust2-ptbr/main/updates/stable.json'
    publicationStatus = 'candidate-awaiting-translation-integration-and-real-client-validation'
    builtAtUtc = [DateTime]::UtcNow.ToString('o')
}
$encoding = [Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText((Join-Path $output 'launcher-manifest.json'), ($metadata | ConvertTo-Json -Depth 5), $encoding)
[IO.File]::WriteAllText((Join-Path $output 'BD2PTBR.exe.sha256'), "$($metadata.sha256)  BD2PTBR.exe`n", [Text.Encoding]::ASCII)
Write-Output "Atualizador candidato: $executable"
Write-Output "SHA-256: $($metadata.sha256)"
Write-Output 'Não publicado. O canal público só deve ser ativado depois da integração e validação da nova tradução.'
