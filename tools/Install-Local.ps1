[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GameRoot,
    [string]$Version = '0.1.0'
)

$ErrorActionPreference = 'Stop'

$resolvedGameRoot = (Resolve-Path -LiteralPath $GameRoot).Path
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$bepInExCore = Join-Path $resolvedGameRoot 'BepInEx\core\BepInEx.dll'
if (-not (Test-Path -LiteralPath $bepInExCore -PathType Leaf)) {
    throw "BepInEx não encontrado em $resolvedGameRoot."
}

$sourceDll = Join-Path $repositoryRoot 'src\BD2PTBR.Plugin\bin\Release\net472\BD2PTBR.dll'
$sourceCatalog = Join-Path $repositoryRoot 'catalog'
if (-not (Test-Path -LiteralPath $sourceDll -PathType Leaf)) {
    throw 'Compile o plugin em Release antes da instalação.'
}

$destination = Join-Path $resolvedGameRoot 'BepInEx\plugins\BD2PTBR'
$destinationTranslations = Join-Path $destination 'translations'
$backupRoot = Join-Path $repositoryRoot ('work\install-backups\' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))

if (Test-Path -LiteralPath $destination -PathType Container) {
    New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
    Copy-Item -LiteralPath $destination -Destination (Join-Path $backupRoot 'BD2PTBR') -Recurse
    Write-Host "Backup da instalação anterior: $backupRoot"
}

$missingRuntimeLog = Join-Path $resolvedGameRoot 'BepInEx\config\BD2PTBR\untranslated-runtime.jsonl'
if (Test-Path -LiteralPath $missingRuntimeLog -PathType Leaf) {
    New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
    Copy-Item -LiteralPath $missingRuntimeLog -Destination (Join-Path $backupRoot 'untranslated-runtime-before-install.jsonl') -Force
    Remove-Item -LiteralPath $missingRuntimeLog -Force
    Write-Host "Diagnóstico anterior preservado e reiniciado: $backupRoot"
}

New-Item -ItemType Directory -Path $destinationTranslations -Force | Out-Null
$destinationFullPath = [System.IO.Path]::GetFullPath($destination)
$translationsFullPath = [System.IO.Path]::GetFullPath($destinationTranslations)
$destinationPrefix = $destinationFullPath.TrimEnd('\') + '\'
if (-not $translationsFullPath.StartsWith($destinationPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Destino de traduções inválido.'
}

# O backup acima preserva a versão anterior. Remover somente as camadas JSONL evita
# que um arquivo obsoleto de outra versão seja carregado junto com o catálogo novo.
Get-ChildItem -LiteralPath $destinationTranslations -Filter '*.jsonl' -File |
    Remove-Item -Force

Copy-Item -LiteralPath $sourceDll -Destination (Join-Path $destination 'BD2PTBR.dll') -Force
Get-ChildItem -LiteralPath $sourceCatalog -Filter '*.jsonl' -File |
    Sort-Object Name |
    ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $destinationTranslations -Force }

$installedFiles = @(
    Get-Item -LiteralPath (Join-Path $destination 'BD2PTBR.dll')
    Get-ChildItem -LiteralPath $destinationTranslations -Filter '*.jsonl' -File
) | Sort-Object FullName | ForEach-Object {
    [pscustomobject]@{
        path = $_.FullName
        bytes = $_.Length
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    }
}

$manifest = [ordered]@{
    schemaVersion = 1
    modVersion = $Version
    installedAtUtc = [DateTime]::UtcNow.ToString('o')
    gameRoot = $resolvedGameRoot
    backupRoot = if (Test-Path -LiteralPath $backupRoot) { $backupRoot } else { $null }
    files = @($installedFiles)
}

$manifestPath = Join-Path $repositoryRoot "work\private-install-manifest-v$($Version.Replace('.', '')).json"
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
Write-Host "Brown Dust 2 PT-BR $Version instalado em $destination"
Write-Host "Manifesto: $manifestPath"
