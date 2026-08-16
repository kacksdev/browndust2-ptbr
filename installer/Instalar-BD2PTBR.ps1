[CmdletBinding()]
param(
    [string]$GameRoot
)

$ErrorActionPreference = 'Stop'

function Resolve-BD2GameRoot {
    param([string]$RequestedRoot)

    if (-not [string]::IsNullOrWhiteSpace($RequestedRoot)) {
        return (Resolve-Path -LiteralPath $RequestedRoot).Path
    }

    $registryPath = 'HKCU:\Software\Neowiz\BrownDust2Starter\10000002'
    if (Test-Path -LiteralPath $registryPath) {
        $registeredRoot = (Get-ItemProperty -LiteralPath $registryPath -Name path -ErrorAction Stop).path
        if (-not [string]::IsNullOrWhiteSpace($registeredRoot) -and (Test-Path -LiteralPath $registeredRoot -PathType Container)) {
            return (Resolve-Path -LiteralPath $registeredRoot).Path
        }
    }

    throw 'A pasta do jogo não foi localizada automaticamente. Execute novamente com -GameRoot "C:\caminho\BrownDust2_10000002".'
}

function Assert-ChildPath {
    param(
        [string]$Parent,
        [string]$Child
    )

    $parentFullPath = [System.IO.Path]::GetFullPath($Parent).TrimEnd('\') + '\'
    $childFullPath = [System.IO.Path]::GetFullPath($Child)
    if (-not $childFullPath.StartsWith($parentFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Caminho fora da pasta autorizada: $childFullPath"
    }
}

function Test-PackageIntegrity {
    param(
        [string]$PackageRoot,
        [string]$ManifestPath
    )

    $manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
    foreach ($entry in $manifest.files) {
        $candidate = Join-Path $PackageRoot ($entry.path.Replace('/', '\'))
        Assert-ChildPath -Parent $PackageRoot -Child $candidate
        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            throw "Arquivo ausente no pacote: $($entry.path)"
        }

        $actualHash = (Get-FileHash -LiteralPath $candidate -Algorithm SHA256).Hash
        if ($actualHash -ne $entry.sha256) {
            throw "Falha de integridade em $($entry.path). Extraia o pacote novamente."
        }
    }
}

$packageRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$manifestPath = Join-Path $packageRoot 'manifest.json'
$sourceLoader = Join-Path $packageRoot 'BepInEx\core\BepInEx.dll'
$sourcePlugin = Join-Path $packageRoot 'BepInEx\plugins\BD2PTBR\BD2PTBR.dll'

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $sourceLoader -PathType Leaf) -or
    -not (Test-Path -LiteralPath $sourcePlugin -PathType Leaf)) {
    throw 'O pacote está incompleto. Extraia todo o arquivo ZIP antes de executar o instalador.'
}

Test-PackageIntegrity -PackageRoot $packageRoot -ManifestPath $manifestPath

$resolvedGameRoot = Resolve-BD2GameRoot -RequestedRoot $GameRoot
$gameExecutable = Join-Path $resolvedGameRoot 'BrownDust II.exe'
if (-not (Test-Path -LiteralPath $gameExecutable -PathType Leaf)) {
    throw "Brown Dust II.exe não foi encontrado em $resolvedGameRoot."
}

$runningGame = Get-Process -Name 'BrownDust II' -ErrorAction SilentlyContinue
if ($runningGame) {
    throw 'Feche Brown Dust 2 antes de instalar ou atualizar a tradução.'
}

$pluginDestination = Join-Path $resolvedGameRoot 'BepInEx\plugins\BD2PTBR'
$backupRoot = Join-Path $resolvedGameRoot ('BD2PTBR_Backups\' + [DateTime]::Now.ToString('yyyyMMdd-HHmmss'))
Assert-ChildPath -Parent $resolvedGameRoot -Child $pluginDestination
Assert-ChildPath -Parent $resolvedGameRoot -Child $backupRoot

$hasExistingInstallation = (Test-Path -LiteralPath $pluginDestination -PathType Container) -or
    (Test-Path -LiteralPath (Join-Path $resolvedGameRoot 'BepInEx\core\BepInEx.dll') -PathType Leaf)

if ($hasExistingInstallation) {
    New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null

    if (Test-Path -LiteralPath $pluginDestination -PathType Container) {
        Copy-Item -LiteralPath $pluginDestination -Destination (Join-Path $backupRoot 'BD2PTBR') -Recurse -Force
    }

    $loaderBackup = Join-Path $backupRoot 'loader'
    New-Item -ItemType Directory -Path $loaderBackup -Force | Out-Null
    foreach ($relativePath in @('.doorstop_version', 'doorstop_config.ini', 'winhttp.dll')) {
        $existingFile = Join-Path $resolvedGameRoot $relativePath
        if (Test-Path -LiteralPath $existingFile -PathType Leaf) {
            Copy-Item -LiteralPath $existingFile -Destination $loaderBackup -Force
        }
    }
}

if (Test-Path -LiteralPath $pluginDestination -PathType Container) {
    Remove-Item -LiteralPath $pluginDestination -Recurse -Force
}

Copy-Item -LiteralPath (Join-Path $packageRoot 'BepInEx') -Destination $resolvedGameRoot -Recurse -Force
foreach ($relativePath in @('.doorstop_version', 'doorstop_config.ini', 'winhttp.dll')) {
    Copy-Item -LiteralPath (Join-Path $packageRoot $relativePath) -Destination (Join-Path $resolvedGameRoot $relativePath) -Force
}

$installedFiles = Get-ChildItem -LiteralPath $pluginDestination -File -Recurse | Sort-Object FullName | ForEach-Object {
    [pscustomobject]@{
        path = $_.FullName.Substring($resolvedGameRoot.Length + 1).Replace('\', '/')
        bytes = $_.Length
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    }
}

$packageManifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$state = [ordered]@{
    schemaVersion = 1
    modVersion = $packageManifest.modVersion
    installedAtUtc = [DateTime]::UtcNow.ToString('o')
    gameRoot = $resolvedGameRoot
    backupRoot = if ($hasExistingInstallation) { $backupRoot } else { $null }
    files = @($installedFiles)
}

$statePath = Join-Path $pluginDestination 'install-state.json'
$state | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $statePath -Encoding UTF8

Write-Host ''
Write-Host "Brown Dust 2 PT-BR $($packageManifest.modVersion) instalado com sucesso."
Write-Host "Jogo: $resolvedGameRoot"
if ($hasExistingInstallation) {
    Write-Host "Backup anterior: $backupRoot"
}
Write-Host 'Abra o jogo normalmente. Nenhum arquivo proprietário foi substituído.'
