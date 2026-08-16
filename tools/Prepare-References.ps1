[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GameRoot
)

$ErrorActionPreference = 'Stop'

$resolvedGameRoot = (Resolve-Path -LiteralPath $GameRoot).Path
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$destination = Join-Path $repositoryRoot 'lib\references'

$searchDirectories = @(
    (Join-Path $resolvedGameRoot 'BepInEx\core'),
    (Join-Path $resolvedGameRoot 'BrownDust II_Data\Managed'),
    (Join-Path $resolvedGameRoot 'BrownDust2_Data\Managed'),
    (Join-Path $resolvedGameRoot 'BepInEx\unhollowed'),
    (Join-Path $resolvedGameRoot 'BepInEx\interop')
) | Where-Object { Test-Path -LiteralPath $_ -PathType Container }

if ($searchDirectories.Count -eq 0) {
    throw "Nenhuma pasta de referências foi encontrada em $resolvedGameRoot."
}

$requiredFiles = @(
    '0Harmony.dll',
    'BepInEx.dll',
    'Newtonsoft.Json.dll',
    'Unity.TextMeshPro.dll',
    'UnityEngine.CoreModule.dll',
    'UnityEngine.dll',
    'UnityEngine.TextRenderingModule.dll',
    'UnityEngine.TextCoreFontEngineModule.dll',
    'UnityEngine.UI.dll',
    'UnityEngine.UIModule.dll'
)

New-Item -ItemType Directory -Path $destination -Force | Out-Null

foreach ($fileName in $requiredFiles) {
    $source = $null
    foreach ($directory in $searchDirectories) {
        $candidate = Join-Path $directory $fileName
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            $source = $candidate
            break
        }
    }

    if ($null -eq $source) {
        throw "Referência obrigatória não encontrada: $fileName"
    }

    Copy-Item -LiteralPath $source -Destination (Join-Path $destination $fileName) -Force
    Write-Host "Referência preparada: $fileName"
}

Write-Host "Referências prontas em $destination"
