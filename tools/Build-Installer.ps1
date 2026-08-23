[CmdletBinding()]
param(
    [string]$Version = '0.1.1',
    [string]$TestedClientVersion = '2.32.10',
    [string]$TestedGamePackage = '20260814000',
    [string]$OutputRoot,
    [string]$BepInExArchive
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repositoryRoot 'dist'
}

function Assert-PathInsideRepository {
    param([Parameter(Mandatory)][string]$Path)

    $resolved = [System.IO.Path]::GetFullPath($Path)
    $prefix = $repositoryRoot.TrimEnd('\') + '\'
    if (-not $resolved.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Caminho fora do repositório recusado: $resolved"
    }
    return $resolved
}

function Remove-BuildDirectory {
    param([Parameter(Mandatory)][string]$Path)

    $resolved = Assert-PathInsideRepository -Path $Path
    if (Test-Path -LiteralPath $resolved) {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}

$outputFullPath = Assert-PathInsideRepository -Path $OutputRoot
$workRoot = Assert-PathInsideRepository -Path (Join-Path $repositoryRoot 'work\installer-v011')
$payloadStage = Assert-PathInsideRepository -Path (Join-Path $workRoot 'payload')
$payloadZip = Assert-PathInsideRepository -Path (Join-Path $repositoryRoot 'installer\BD2PTBR.Installer\Payload\BD2PTBR-payload.zip')
$noticesAsset = Assert-PathInsideRepository -Path (Join-Path $repositoryRoot 'installer\BD2PTBR.Installer\Assets\THIRD-PARTY-NOTICES.txt')
$dependencyRoot = Assert-PathInsideRepository -Path (Join-Path $repositoryRoot 'work\dependencies')

$bepInExVersion = '5.4.23.5'
$bepInExArchiveName = "BepInEx_win_x64_$bepInExVersion.zip"
$bepInExArchiveUrl = "https://github.com/BepInEx/BepInEx/releases/download/v$bepInExVersion/$bepInExArchiveName"
$bepInExArchiveHash = '82F9878551030F54657792C0740D9D51A09500EEAE1FBA21106B0C441E6732C4'
$bepInExLicense = Join-Path $dependencyRoot 'BepInEx-LICENSE-v5.4.23.5.txt'
$bepInExLicenseUrl = 'https://raw.githubusercontent.com/BepInEx/BepInEx/v5.4.23.5/LICENSE'
$bepInExLicenseHash = 'E6E534EF6F4347B6449407EE046A3D09CB0174C6F688C996AD0BED94B74B3933'

New-Item -ItemType Directory -Path $dependencyRoot -Force | Out-Null
if ([string]::IsNullOrWhiteSpace($BepInExArchive)) {
    $BepInExArchive = Join-Path $dependencyRoot $bepInExArchiveName
}
$BepInExArchive = [System.IO.Path]::GetFullPath($BepInExArchive)
if (-not (Test-Path -LiteralPath $BepInExArchive -PathType Leaf)) {
    Invoke-WebRequest -Uri $bepInExArchiveUrl -UseBasicParsing -OutFile $BepInExArchive
}
if ((Get-FileHash -LiteralPath $BepInExArchive -Algorithm SHA256).Hash -ne $bepInExArchiveHash) {
    throw 'O arquivo do BepInEx não corresponde ao pacote oficial esperado.'
}
if (-not (Test-Path -LiteralPath $bepInExLicense -PathType Leaf)) {
    Invoke-WebRequest -Uri $bepInExLicenseUrl -UseBasicParsing -OutFile $bepInExLicense
}
if ((Get-FileHash -LiteralPath $bepInExLicense -Algorithm SHA256).Hash -ne $bepInExLicenseHash) {
    throw 'A licença do BepInEx não corresponde ao arquivo oficial esperado.'
}

Push-Location $repositoryRoot
try {
    & python 'tools\FullTranslator\audit_catalog.py'
    if ($LASTEXITCODE -ne 0) { throw 'Falha na auditoria do catálogo.' }

    $audit = Get-Content -LiteralPath 'catalog\audit-report.json' -Raw | ConvertFrom-Json
    if ($audit.effectiveRecords -ne 223743 -or
        $audit.bgmTitlesChanged -ne 0 -or
        $audit.bgmDisplayLabelsChanged -ne 0 -or
        $audit.editorialQueue -ne 0 -or
        $audit.hardFailures -ne 0 -or
        -not $audit.editorialReady) {
        throw 'O catálogo não atende aos critérios registrados para a distribuição pública.'
    }

    & dotnet run --project 'tools\RuntimeCatalogAudit\BD2PTBR.RuntimeCatalogAudit.csproj' -c Release -- 'catalog' 223743
    if ($LASTEXITCODE -ne 0) { throw 'O catálogo diverge do leitor usado pelo plugin.' }

    & dotnet build 'src\BD2PTBR.Plugin\BD2PTBR.csproj' -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o plugin.' }

    Remove-BuildDirectory -Path $workRoot
    New-Item -ItemType Directory -Path $payloadStage -Force | Out-Null
    Expand-Archive -LiteralPath $BepInExArchive -DestinationPath $payloadStage -Force

    $pluginStage = Join-Path $payloadStage 'BepInEx\plugins\BD2PTBR'
    $translationStage = Join-Path $pluginStage 'translations'
    New-Item -ItemType Directory -Path $translationStage -Force | Out-Null
    Copy-Item -LiteralPath 'src\BD2PTBR.Plugin\bin\Release\net472\BD2PTBR.dll' -Destination $pluginStage
    Get-ChildItem -LiteralPath 'catalog' -Filter '*.jsonl' -File |
        Sort-Object Name |
        ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $translationStage }

    $manifestFiles = Get-ChildItem -LiteralPath $payloadStage -File -Recurse |
        Sort-Object FullName |
        ForEach-Object {
            [ordered]@{
                path = $_.FullName.Substring($payloadStage.Length + 1).Replace('\', '/')
                bytes = $_.Length
                sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
            }
        }

    $payloadManifest = [ordered]@{
        schemaVersion = 2
        modVersion = $Version
        testedClientVersion = $TestedClientVersion
        testedGamePackage = $TestedGamePackage
        catalogLineageVersion = '0.9.6'
        generatedAtUtc = [DateTime]::UtcNow.ToString('o')
        files = @($manifestFiles)
    }
    $payloadManifestPath = Join-Path $payloadStage 'payload-manifest.json'
    [System.IO.File]::WriteAllText(
        $payloadManifestPath,
        ($payloadManifest | ConvertTo-Json -Depth 5),
        $utf8NoBom)

    New-Item -ItemType Directory -Path (Split-Path -Parent $payloadZip) -Force | Out-Null
    if (Test-Path -LiteralPath $payloadZip) {
        Remove-Item -LiteralPath $payloadZip -Force
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        $payloadStage,
        $payloadZip,
        [System.IO.Compression.CompressionLevel]::Optimal,
        $false)

    # Windows PowerShell 5.1 interpreta UTF-8 sem BOM como ANSI quando a
    # codificação não é declarada. Esses arquivos são UTF-8 e alimentam um
    # recurso incorporado ao executável, portanto a leitura precisa ser
    # explícita para preservar os acentos em qualquer máquina de build.
    $projectNotices = Get-Content -LiteralPath 'THIRD-PARTY-NOTICES.md' -Raw -Encoding UTF8
    $loaderLicense = Get-Content -LiteralPath $bepInExLicense -Raw -Encoding UTF8
    $combinedNotices = @"
$projectNotices

BEPINEX $bepInExVersion
Fonte: $bepInExArchiveUrl
SHA-256 do pacote oficial: $bepInExArchiveHash

$loaderLicense
"@
    [System.IO.File]::WriteAllText($noticesAsset, $combinedNotices, $utf8NoBom)

    & dotnet build 'installer\BD2PTBR.Installer\BD2PTBR.Installer.csproj' -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o instalador.' }

    New-Item -ItemType Directory -Path $outputFullPath -Force | Out-Null
    $builtExecutable = Join-Path $repositoryRoot 'installer\BD2PTBR.Installer\bin\Release\net472\BD2PTBR-Installer.exe'
    $releaseExecutable = Join-Path $outputFullPath "BD2PTBRv$Version.exe"
    $releaseChecksum = "$releaseExecutable.sha256"
    $releaseManifest = Join-Path $outputFullPath "BD2PTBRv$Version-manifest.json"
    Copy-Item -LiteralPath $builtExecutable -Destination $releaseExecutable -Force

    $executableHash = (Get-FileHash -LiteralPath $releaseExecutable -Algorithm SHA256).Hash
    Set-Content -LiteralPath $releaseChecksum -Value "$executableHash  $([System.IO.Path]::GetFileName($releaseExecutable))" -Encoding ascii
    $publicManifest = [ordered]@{
        schemaVersion = 1
        modVersion = $Version
        installerFile = [System.IO.Path]::GetFileName($releaseExecutable)
        installerBytes = (Get-Item -LiteralPath $releaseExecutable).Length
        installerSha256 = $executableHash
        testedClientVersion = $TestedClientVersion
        testedGamePackage = $TestedGamePackage
        catalog = [ordered]@{
            effectiveRecords = $audit.effectiveRecords
            editorialQueue = $audit.editorialQueue
            hardFailures = $audit.hardFailures
            bgmTitlesChanged = $audit.bgmTitlesChanged
        }
        embeddedPayloadSha256 = (Get-FileHash -LiteralPath $payloadZip -Algorithm SHA256).Hash
        generatedAtUtc = [DateTime]::UtcNow.ToString('o')
    }
    [System.IO.File]::WriteAllText(
        $releaseManifest,
        ($publicManifest | ConvertTo-Json -Depth 5),
        $utf8NoBom)

    Write-Host "Instalador criado: $releaseExecutable"
    Write-Host "SHA-256: $executableHash"
}
finally {
    Pop-Location
}
