[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SourcePng,
    [Parameter(Mandatory)][string]$OutputIco,
    [string]$PreviewRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Add-Type -AssemblyName System.Drawing

$sourcePath = [System.IO.Path]::GetFullPath($SourcePng)
$outputPath = [System.IO.Path]::GetFullPath($OutputIco)
if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
    throw "Imagem-fonte não encontrada: $sourcePath"
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$frames = [System.Collections.Generic.List[object]]::new()
$source = [System.Drawing.Image]::FromFile($sourcePath)

try {
    foreach ($size in $sizes) {
        $bitmap = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            try {
                $graphics.Clear([System.Drawing.Color]::Transparent)
                $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
                $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                $graphics.DrawImage($source, 0, 0, $size, $size)
            }
            finally {
                $graphics.Dispose()
            }

            $memory = New-Object System.IO.MemoryStream
            $bitmap.Save($memory, [System.Drawing.Imaging.ImageFormat]::Png)
            $bytes = $memory.ToArray()
            $memory.Dispose()
            $frames.Add([pscustomobject]@{ Size = $size; Bytes = $bytes }) | Out-Null

            if (-not [string]::IsNullOrWhiteSpace($PreviewRoot)) {
                $previewDirectory = [System.IO.Path]::GetFullPath($PreviewRoot)
                [System.IO.Directory]::CreateDirectory($previewDirectory) | Out-Null
                $bitmap.Save((Join-Path $previewDirectory "kacks-installer-$size.png"), [System.Drawing.Imaging.ImageFormat]::Png)
            }
        }
        finally {
            $bitmap.Dispose()
        }
    }
}
finally {
    $source.Dispose()
}

[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($outputPath)) | Out-Null
$stream = [System.IO.File]::Create($outputPath)
$writer = New-Object System.IO.BinaryWriter($stream)
try {
    $writer.Write([UInt16]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]$frames.Count)

    $offset = 6 + (16 * $frames.Count)
    foreach ($frame in $frames) {
        $dimension = if ($frame.Size -eq 256) { [byte]0 } else { [byte]$frame.Size }
        $writer.Write($dimension)
        $writer.Write($dimension)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]32)
        $writer.Write([UInt32]$frame.Bytes.Length)
        $writer.Write([UInt32]$offset)
        $offset += $frame.Bytes.Length
    }

    foreach ($frame in $frames) {
        $writer.Write($frame.Bytes)
    }
}
finally {
    $writer.Dispose()
    $stream.Dispose()
}

Write-Host "Ícone criado: $outputPath"
Write-Host "Tamanhos: $($sizes -join ', ') px"
