#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Executable,
    [Parameter(Mandatory)][string]$OutputZip
)
$ErrorActionPreference = 'Stop'
$source = Get-Item -LiteralPath $Executable
$output = [IO.Path]::GetFullPath($OutputZip)
if (Test-Path -LiteralPath $output) { throw "A saída já existe: $output" }
# Preserve the release's real file date. Reusing 2000-01-01 across different
# application versions can leave Explorer's path/date icon cache stale.
$timestamp = $source.LastWriteTimeUtc
if ($timestamp.Year -lt 2020 -or $timestamp.Year -gt 2107) { throw 'Data do executável inválida para distribuição.' }
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($output)) | Out-Null
$archive = [IO.Compression.ZipFile]::Open($output, [IO.Compression.ZipArchiveMode]::Create)
try {
    $entry = $archive.CreateEntry('BD2PTBR.exe', [IO.Compression.CompressionLevel]::Optimal)
    $entry.LastWriteTime = [DateTimeOffset]::new($timestamp)
    $inputStream = $source.OpenRead()
    $outputStream = $entry.Open()
    try { $inputStream.CopyTo($outputStream) }
    finally { $outputStream.Dispose(); $inputStream.Dispose() }
}
finally { $archive.Dispose() }
Write-Output "Arquivo criado: $output"
