[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GameRoot,
    [string]$Version = '0.1.1',
    [string]$CatalogRoot,
    [ValidateRange(1, [int]::MaxValue)]
    [int]$ExpectedCatalogEntries = 223743,
    [ValidateRange(0, [int]::MaxValue)]
    [int]$ExpectedProtectedMusicSources = 288,
    [string]$ReferencePluginPath
)

$ErrorActionPreference = 'Stop'

$resolvedGameRoot = (Resolve-Path -LiteralPath $GameRoot).Path
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$sourceRoot = if ($CatalogRoot) { (Resolve-Path -LiteralPath $CatalogRoot).Path } else { Join-Path $repositoryRoot 'work\release-catalog-v096' }
$installedRoot = Join-Path $resolvedGameRoot 'BepInEx\plugins\BD2PTBR'
$installedDll = Join-Path $installedRoot 'BD2PTBR.dll'
$installedTranslations = Join-Path $installedRoot 'translations'
$sourceDll = if ($ReferencePluginPath) { (Resolve-Path -LiteralPath $ReferencePluginPath).Path } else { Join-Path $repositoryRoot 'src\BD2PTBR.Plugin\bin\Release\net472\BD2PTBR.dll' }

if (-not (Test-Path -LiteralPath $installedDll -PathType Leaf)) {
    throw 'BD2PTBR.dll não foi encontrado na instalação do jogo.'
}

$fileVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($installedDll).FileVersion
$expectedFileVersion = "$Version.0"
if ($fileVersion -ne $expectedFileVersion) {
    throw "Versão inesperada do plugin instalado: $fileVersion"
}
if (-not (Test-Path -LiteralPath $sourceDll -PathType Leaf)) {
    throw 'A DLL compilada de referência não foi encontrada.'
}

$sourceDllHash = (Get-FileHash -LiteralPath $sourceDll -Algorithm SHA256).Hash
$installedDllHash = (Get-FileHash -LiteralPath $installedDll -Algorithm SHA256).Hash
if ($sourceDllHash -ne $installedDllHash) {
    throw 'A DLL instalada não corresponde à compilação Release atual.'
}

$expectedFiles = @(Get-ChildItem -LiteralPath $sourceRoot -Filter '*.jsonl' -File | Sort-Object Name)
$installedFiles = @(Get-ChildItem -LiteralPath $installedTranslations -Filter '*.jsonl' -File | Sort-Object Name)
if ($installedFiles.Count -ne $expectedFiles.Count -or
    (Compare-Object -ReferenceObject $expectedFiles.Name -DifferenceObject $installedFiles.Name)) {
    throw 'O conjunto de camadas instalado diverge do catálogo público esperado.'
}

foreach ($source in $expectedFiles) {
    $installed = Join-Path $installedTranslations $source.Name
    if (-not (Test-Path -LiteralPath $installed -PathType Leaf)) {
        throw "Catálogo ausente na instalação: $($source.Name)"
    }

    $expectedHash = (Get-FileHash -LiteralPath $source.FullName -Algorithm SHA256).Hash
    $installedHash = (Get-FileHash -LiteralPath $installed -Algorithm SHA256).Hash
    if ($expectedHash -ne $installedHash) {
        throw "Hash divergente: $($source.Name)"
    }
}

$logPath = Join-Path $resolvedGameRoot 'BepInEx\LogOutput.log'
$runtimeValidated = $false
$runtimeChecks = [ordered]@{}
$failureMatches = @()
$missingRuntimeEntries = @()
if (Test-Path -LiteralPath $logPath -PathType Leaf) {
    $log = Get-Content -LiteralPath $logPath -Raw
    $missingRuntimePath = Join-Path $resolvedGameRoot 'BepInEx\config\BD2PTBR\untranslated-runtime.jsonl'
    if (Test-Path -LiteralPath $missingRuntimePath -PathType Leaf) {
        $missingRuntimeEntries = @(Get-Content -LiteralPath $missingRuntimePath | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    }
    $runtimeChecks = [ordered]@{
        logIsNewerThanPlugin = (Get-Item -LiteralPath $logPath).LastWriteTimeUtc -ge (Get-Item -LiteralPath $installedDll).LastWriteTimeUtc
        catalogCount = $log.Contains("Traduções carregadas: $ExpectedCatalogEntries.")
        musicTitlesProtected = $log.Contains("Títulos musicais protegidos: $ExpectedProtectedMusicSources.")
        awakeComplete = $log.Contains('[PTBR-CICLO] Awake concluído.')
        pluginEnabled = $log.Contains('[PTBR-CICLO] Plugin habilitado.')
        independentCoordinator = $log.Contains('[PTBR-RUNTIME] coordenador independente do ciclo do plugin ativo.')
        textSweep = $log.Contains('Varredura otimizada #')
        optimizedCoordinator = $log.Contains('Coordenador otimizado registrado;')
        visualSetter = $log.Contains('[PTBR-RUNTIME] setter visual de texto atingido.')
        translationCoverage = $log.Contains('[PTBR-RUNTIME] cobertura de tradução operacional.')
        fontAssetCreated = $log.Contains('Fonte auxiliar PT-BR criada em memória')
        fontCoverageAttached = $log.Contains('Cobertura PT-BR associada à fonte')
        guidePaginationGuard = $log.Contains('Proteções de paginação do guia: 1.')
        dynamicGlyphGuard = $log.Contains('Proteções de glifos dinâmicos: 1.')
        noUntranslatedRuntimeEntries = $missingRuntimeEntries.Count -eq 0
    }

    $failurePatterns = @(
        '\[Error\s*:',
        'Falha ao instalar a tradução',
        'A cobertura essencial de tradução ficou incompleta',
        'Catálogo parcial detectado',
        'Linha inválida em ',
        'Tradução ignorada por alterar marcadores',
        'Falha ao preparar a cobertura PT-BR',
        'ainda não cobre estes caracteres PT-BR',
        'Nenhum arquivo de fonte compatível foi encontrado',
        'de criar a fonte auxiliar PT-BR falhou',
        'Falha ao verificar os textos visíveis',
        'Falha ao salvar textos ausentes'
    )
    $failureMatches = @($failurePatterns | Where-Object { $log -match $_ })
    $runtimeValidated = -not ($runtimeChecks.Values -contains $false) -and $failureMatches.Count -eq 0
}

if (-not $runtimeValidated) {
    $failedChecks = @($runtimeChecks.GetEnumerator() | Where-Object { -not $_.Value } | ForEach-Object { $_.Key })
    if ($runtimeChecks.Count -eq 0) { $failedChecks = @('runtimeLogMissing') }
    throw "A validação de execução falhou. Verificações: $($failedChecks -join ', '); padrões de falha: $($failureMatches -join ', ')"
}

[pscustomobject]@{
    pluginVersion = $fileVersion
    pluginHash = $installedDllHash
    catalogFiles = $expectedFiles.Count
    hashesMatch = $true
    runtimeLogValidated = $runtimeValidated
    runtimeChecks = [pscustomobject]$runtimeChecks
    failureMatches = @($failureMatches)
    untranslatedRuntimeEntries = $missingRuntimeEntries.Count
    logPath = $logPath
}
