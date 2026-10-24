#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PayloadRoot,
    [Parameter(Mandatory)][string]$GameRoot,
    [Parameter(Mandatory)][string]$ModVersion,
    [Parameter(Mandatory)][string]$OutputRoot
)
$ErrorActionPreference = 'Stop'
$version = [Version]::Parse($ModVersion)
$payload = (Resolve-Path -LiteralPath $PayloadRoot).Path
$game = (Resolve-Path -LiteralPath $GameRoot).Path
$output = [IO.Path]::GetFullPath($OutputRoot)
if (Test-Path -LiteralPath $output) { throw "A saída deve ser uma pasta nova: $output" }
if ($payload.StartsWith($game.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase) -or $payload -eq $game) { throw 'O payload deve ser um staging isolado, não a instalação do jogo.' }
if (Test-Path -LiteralPath (Join-Path $payload 'payload-manifest.json')) { throw 'Remova a ambiguidade: staging não pode conter manifesto prévio.' }
$plugin = Join-Path $payload 'BepInEx\plugins\BD2PTBR\BD2PTBR.dll'
if (-not (Test-Path -LiteralPath $plugin)) { throw 'Plugin compilado ausente.' }
if ([Reflection.AssemblyName]::GetAssemblyName($plugin).Version.ToString(3) -ne $ModVersion) { throw 'Versão do plugin difere da versão solicitada.' }
$files = @(Get-ChildItem -LiteralPath $payload -Recurse -File)
$records = foreach ($file in $files) {
    $relative = $file.FullName.Substring($payload.Length).TrimStart('\').Replace('\','/')
    if ($relative -notmatch '^(winhttp\.dll|doorstop_config\.ini|\.doorstop_version|changelog\.txt|BepInEx/core/[^:]+|BepInEx/plugins/BD2PTBR/[^:]+|BepInEx/config/BD2PTBR\.cfg)$') { throw "Arquivo fora do escopo do mod: $relative" }
    if (($file.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Links não são aceitos no pacote.' }
    $cursor = $file.Directory
    while ($cursor -and $cursor.FullName.Length -ge $payload.Length) {
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Junções não são aceitas no staging.' }
        $cursor = $cursor.Parent
    }
    [ordered]@{ path=$relative; bytes=$file.Length; sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash }
}
if (-not $records -or $records.Count -gt 5000 -or ($records | Measure-Object bytes -Sum).Sum -gt 1GB) { throw 'Tamanho do payload fora dos limites.' }
$required = @('winhttp.dll','doorstop_config.ini','BepInEx/core/BepInEx.dll','BepInEx/core/BepInEx.Preloader.dll','BepInEx/core/0Harmony.dll','BepInEx/plugins/BD2PTBR/BD2PTBR.dll','BepInEx/plugins/BD2PTBR/translations/complete.jsonl','BepInEx/plugins/BD2PTBR/translations/hardcoded.jsonl')
foreach ($path in $required) { if ($path -notin $records.path) { throw "Componente essencial ausente: $path" } }
$criticalPaths = @('BrownDust II.exe', 'BrownDust II_Data/Managed/Assembly-CSharp.dll', 'BrownDust II_Data/globalgamemanagers')
$identityFiles = foreach ($path in $criticalPaths) {
    $file = Get-Item -LiteralPath (Join-Path $game $path)
    [ordered]@{ path=$path; bytes=$file.Length; sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash }
}
$globalBytes = [IO.File]::ReadAllBytes((Join-Path $game 'BrownDust II_Data\globalgamemanagers'))
$clientVersion = [regex]::Match([Text.Encoding]::ASCII.GetString($globalBytes), '(?<![\d.])2\.\d{1,3}\.\d{1,3}(?![\d.])').Value
$package = Get-ChildItem -LiteralPath $game -File -Filter '*.tied.dat' | Sort-Object Name -Descending | Select-Object -First 1
if (-not $package -or -not $clientVersion) { throw 'Identidade do cliente incompleta.' }
$packageId = $package.Name -replace '\.tied\.dat$', ''
$identity = [ordered]@{ package=$packageId; version=$clientVersion; files=@($identityFiles) }
$manifest = [ordered]@{ schemaVersion=3; modVersion=$ModVersion; testedClientVersion=$clientVersion; testedGamePackage=$packageId; compatibleClients=@($identity); files=@($records) }
New-Item -ItemType Directory -Path $output | Out-Null
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zipPath = Join-Path $output "BD2PTBR-payload-$ModVersion.zip"
$encoding = [Text.UTF8Encoding]::new($false)
$zip = [IO.Compression.ZipFile]::Open($zipPath, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($record in $records) { [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $payload $record.path), $record.path, [IO.Compression.CompressionLevel]::Optimal) | Out-Null }
    $entry = $zip.CreateEntry('payload-manifest.json')
    $writer = [IO.StreamWriter]::new($entry.Open(), $encoding)
    try { $writer.Write(($manifest | ConvertTo-Json -Depth 10)) } finally { $writer.Dispose() }
} finally { $zip.Dispose() }
$release = [ordered]@{ modVersion=$ModVersion; minimumLauncherVersion='0.2.0'; url="https://github.com/kacksdev/browndust2-ptbr/releases/download/v$ModVersion/$([IO.Path]::GetFileName($zipPath))"; bytes=(Get-Item -LiteralPath $zipPath).Length; sha256=(Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash; compatibleClients=@($identity) }
if ($release.bytes -gt 512MB) { throw 'O arquivo excede o limite de download do atualizador.' }
[IO.File]::WriteAllText((Join-Path $output 'release-candidate.json'), ($release | ConvertTo-Json -Depth 10), $encoding)
[IO.File]::WriteAllText((Join-Path $output 'CANDIDATE-NOT-PUBLISHED.txt'), 'Este pacote ainda exige auditoria do catalogo, QA editorial autorizado e validacao no cliente real. Nao publicar este arquivo como feed antes desses gates.', $encoding)
Write-Output "Payload candidato: $zipPath"
