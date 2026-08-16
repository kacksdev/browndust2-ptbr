[CmdletBinding()]
param(
    [string]$GameRoot
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($GameRoot)) {
    $registryPath = 'HKCU:\Software\Neowiz\BrownDust2Starter\10000002'
    if (Test-Path -LiteralPath $registryPath) {
        $GameRoot = (Get-ItemProperty -LiteralPath $registryPath -Name path -ErrorAction Stop).path
    }
}

if ([string]::IsNullOrWhiteSpace($GameRoot) -or -not (Test-Path -LiteralPath $GameRoot -PathType Container)) {
    throw 'A pasta do jogo não foi localizada. Execute novamente com -GameRoot "C:\caminho\BrownDust2_10000002".'
}

$resolvedGameRoot = (Resolve-Path -LiteralPath $GameRoot).Path
$pluginPath = Join-Path $resolvedGameRoot 'BepInEx\plugins\BD2PTBR'
$gamePrefix = $resolvedGameRoot.TrimEnd('\') + '\'
$pluginFullPath = [System.IO.Path]::GetFullPath($pluginPath)
if (-not $pluginFullPath.StartsWith($gamePrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Destino de remoção inválido.'
}

if (Get-Process -Name 'BrownDust II' -ErrorAction SilentlyContinue) {
    throw 'Feche Brown Dust 2 antes de remover a tradução.'
}

if (Test-Path -LiteralPath $pluginFullPath -PathType Container) {
    Remove-Item -LiteralPath $pluginFullPath -Recurse -Force
    Write-Host 'Brown Dust 2 PT-BR removido. O BepInEx foi preservado para não afetar outros mods.'
}
else {
    Write-Host 'A tradução não está instalada nessa pasta.'
}
