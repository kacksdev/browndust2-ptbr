[CmdletBinding()]
param(
    [string]$Version = '0.1.0',
    [string]$GameClientVersion = 'v2.32.7 (pacote 20260814000)',
    [string]$OutputRoot,
    [string]$BepInExArchive
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$dependencyRoot = Join-Path $repositoryRoot 'work\dependencies'
New-Item -ItemType Directory -Path $dependencyRoot -Force | Out-Null

$bepInExVersion = '5.4.23.5'
$bepInExArchiveName = "BepInEx_win_x64_$bepInExVersion.zip"
$bepInExArchiveUrl = "https://github.com/BepInEx/BepInEx/releases/download/v$bepInExVersion/$bepInExArchiveName"
$bepInExArchiveHash = '82F9878551030F54657792C0740D9D51A09500EEAE1FBA21106B0C441E6732C4'
if ([string]::IsNullOrWhiteSpace($BepInExArchive)) {
    $BepInExArchive = Join-Path $dependencyRoot $bepInExArchiveName
}
if (-not (Test-Path -LiteralPath $BepInExArchive -PathType Leaf)) {
    Invoke-WebRequest -Uri $bepInExArchiveUrl -UseBasicParsing -OutFile $BepInExArchive
}
if ((Get-FileHash -LiteralPath $BepInExArchive -Algorithm SHA256).Hash -ne $bepInExArchiveHash) {
    throw 'O arquivo do BepInEx não corresponde ao pacote oficial esperado.'
}

$bepInExLicense = Join-Path $dependencyRoot 'BepInEx-LICENSE-v5.4.23.5.txt'
$bepInExLicenseUrl = 'https://raw.githubusercontent.com/BepInEx/BepInEx/v5.4.23.5/LICENSE'
$bepInExLicenseHash = 'E6E534EF6F4347B6449407EE046A3D09CB0174C6F688C996AD0BED94B74B3933'
if (-not (Test-Path -LiteralPath $bepInExLicense -PathType Leaf)) {
    Invoke-WebRequest -Uri $bepInExLicenseUrl -UseBasicParsing -OutFile $bepInExLicense
}
if ((Get-FileHash -LiteralPath $bepInExLicense -Algorithm SHA256).Hash -ne $bepInExLicenseHash) {
    throw 'A licença do BepInEx não corresponde ao arquivo oficial esperado.'
}
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repositoryRoot 'dist'
}

$outputFullPath = [System.IO.Path]::GetFullPath($OutputRoot)
$repositoryPrefix = $repositoryRoot.TrimEnd('\') + '\'
if (-not $outputFullPath.StartsWith($repositoryPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'A pasta de saída precisa permanecer dentro do repositório.'
}

$stageName = "BD2PTBR-v$Version"
$stage = Join-Path $outputFullPath $stageName
$zipPath = Join-Path $outputFullPath "$stageName.zip"
$externalManifestPath = Join-Path $outputFullPath "$stageName-manifest.json"
$checksumPath = "$zipPath.sha256"

Push-Location $repositoryRoot
try {
    & python 'tools\FullTranslator\audit_catalog.py'
    if ($LASTEXITCODE -ne 0) { throw 'Falha na auditoria do catálogo público.' }

    $audit = Get-Content -LiteralPath 'catalog\audit-report.json' -Raw | ConvertFrom-Json
    if ($audit.effectiveRecords -ne 223743 -or
        $audit.bgmTitlesChanged -ne 0 -or
        $audit.bgmDisplayLabelsChanged -ne 0 -or
        $audit.editorialQueue -ne 0 -or
        $audit.hardFailures -ne 0 -or
        -not $audit.editorialReady) {
        throw 'O relatório de auditoria não atende aos critérios da versão pública.'
    }

    & dotnet run --project 'tools\RuntimeCatalogAudit\BD2PTBR.RuntimeCatalogAudit.csproj' -c Release -- 'catalog' 223743
    if ($LASTEXITCODE -ne 0) { throw 'O catálogo diverge do validador usado pelo plugin.' }

    & dotnet build 'src\BD2PTBR.Plugin\BD2PTBR.csproj' -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o plugin.' }

    New-Item -ItemType Directory -Path $outputFullPath -Force | Out-Null
    if (Test-Path -LiteralPath $stage) {
        $stageParent = [System.IO.Path]::GetFullPath((Split-Path -Parent $stage))
        if ($stageParent -ne $outputFullPath) { throw 'Destino temporário inválido.' }
        Remove-Item -LiteralPath $stage -Recurse -Force
    }

    New-Item -ItemType Directory -Path $stage -Force | Out-Null
    Expand-Archive -LiteralPath $BepInExArchive -DestinationPath $stage -Force

    $pluginStage = Join-Path $stage 'BepInEx\plugins\BD2PTBR'
    $translationStage = Join-Path $pluginStage 'translations'
    $documentationStage = Join-Path $stage 'documentacao'
    $installerStage = Join-Path $stage 'instalador'
    $licenseStage = Join-Path $stage 'licencas'
    New-Item -ItemType Directory -Path $translationStage -Force | Out-Null
    New-Item -ItemType Directory -Path $documentationStage -Force | Out-Null
    New-Item -ItemType Directory -Path $installerStage -Force | Out-Null
    New-Item -ItemType Directory -Path $licenseStage -Force | Out-Null

    Copy-Item -LiteralPath 'src\BD2PTBR.Plugin\bin\Release\net472\BD2PTBR.dll' -Destination $pluginStage
    Get-ChildItem -LiteralPath 'catalog' -Filter '*.jsonl' -File |
        Sort-Object Name |
        ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $translationStage }

    Copy-Item -LiteralPath 'README.md' -Destination $stage
    Copy-Item -LiteralPath 'LICENSE' -Destination $stage
    Copy-Item -LiteralPath 'THIRD-PARTY-NOTICES.md' -Destination $stage
    Copy-Item -LiteralPath 'docs\INSTALACAO.md' -Destination $documentationStage
    Copy-Item -LiteralPath 'installer\Instalar-BD2PTBR.ps1' -Destination $installerStage
    Copy-Item -LiteralPath 'installer\Remover-BD2PTBR.ps1' -Destination $installerStage
    Copy-Item -LiteralPath 'installer\INSTALAR-BD2-PTBR.cmd' -Destination $stage
    Copy-Item -LiteralPath 'installer\REMOVER-BD2-PTBR.cmd' -Destination $stage
    Copy-Item -LiteralPath $bepInExLicense -Destination (Join-Path $licenseStage 'BEPINEX-LICENSE.txt')
    if (Test-Path -LiteralPath 'docs\RELEASE-NOTES-0.1.0.md') {
        Copy-Item -LiteralPath 'docs\RELEASE-NOTES-0.1.0.md' -Destination (Join-Path $stage 'RELEASE-NOTES.md')
    }

    $manifestFiles = Get-ChildItem -LiteralPath $stage -File -Recurse | Sort-Object FullName | ForEach-Object {
        [pscustomobject]@{
            path = $_.FullName.Substring($stage.Length + 1).Replace('\', '/')
            bytes = $_.Length
            sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        }
    }

    $manifest = [ordered]@{
        schemaVersion = 1
        modVersion = $Version
        catalogLineageVersion = '0.9.6'
        gameClientVersion = $GameClientVersion
        generatedAtUtc = [DateTime]::UtcNow.ToString('o')
        loader = [ordered]@{
            name = 'BepInEx'
            version = $bepInExVersion
            platform = 'Windows x64'
            source = $bepInExArchiveUrl
            sourceSha256 = $bepInExArchiveHash
        }
        catalog = [ordered]@{
            effectiveRecords = $audit.effectiveRecords
            reviewedRecords = $audit.statusCounts.reviewed
            bgmTitles = $audit.bgmTitles
            bgmTitlesChanged = $audit.bgmTitlesChanged
            bgmDisplayLabels = $audit.bgmDisplayLabels
            bgmDisplayLabelsChanged = $audit.bgmDisplayLabelsChanged
            editorialQueue = $audit.editorialQueue
            hardFailures = $audit.hardFailures
            runtimeMarkerFailures = 0
        }
        runtime = [ordered]@{
            windowsFontAtlasGeneratedInMemory = $true
            bundledFontFiles = 0
            dynamicGlyphGuardHooks = 1
            centralTranslationHooks = 36
            safeVisualHooks = 11
            guidePaginationGuardHooks = 1
            continuousTextSweep = $false
            diagnosticCaptureEnabledByDefault = $false
        }
        files = @($manifestFiles)
    }

    $manifestJson = $manifest | ConvertTo-Json -Depth 6
    $internalManifestPath = Join-Path $stage 'manifest.json'
    Set-Content -LiteralPath $internalManifestPath -Value $manifestJson -Encoding UTF8
    Set-Content -LiteralPath $externalManifestPath -Value $manifestJson -Encoding UTF8

    foreach ($file in @($zipPath, $checksumPath)) {
        if (Test-Path -LiteralPath $file) {
            Remove-Item -LiteralPath $file -Force
        }
    }

    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zipPath -CompressionLevel Optimal
    $zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
    Set-Content -LiteralPath $checksumPath -Value "$zipHash  $([System.IO.Path]::GetFileName($zipPath))" -Encoding ASCII

    Write-Host "Pacote criado: $zipPath"
    Write-Host "SHA-256: $zipHash"
}
finally {
    Pop-Location
}
