[CmdletBinding()]
param(
    [string]$InstallerPath,
    [string]$TestRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($InstallerPath)) {
    $InstallerPath = Join-Path $repositoryRoot 'dist\BD2PTBR-Installer-v0.1.1.exe'
}
if ([string]::IsNullOrWhiteSpace($TestRoot)) {
    $TestRoot = Join-Path $repositoryRoot 'work\installer-tests-v011'
}

function Assert-PathInsideRepository {
    param([Parameter(Mandatory)][string]$Path)

    $resolved = [System.IO.Path]::GetFullPath($Path)
    $prefix = $repositoryRoot.TrimEnd('\') + '\'
    if (-not $resolved.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Caminho de teste fora do repositório recusado: $resolved"
    }
    return $resolved
}

function Remove-TestDirectory {
    param([Parameter(Mandatory)][string]$Path)

    $resolved = Assert-PathInsideRepository -Path $Path
    $allowed = (Join-Path $repositoryRoot 'work').TrimEnd('\') + '\'
    if (-not $resolved.StartsWith($allowed, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Remoção recusada fora da pasta de trabalho: $resolved"
    }
    if (Test-Path -LiteralPath $resolved) {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}

function Assert-True {
    param(
        [Parameter(Mandatory)][bool]$Condition,
        [Parameter(Mandatory)][string]$Message
    )

    if (-not $Condition) {
        throw "FALHA: $Message"
    }
}

function New-FakeGame {
    param(
        [Parameter(Mandatory)][string]$Name,
        [switch]$PreExistingLoader
    )

    $gameRoot = Join-Path $script:testRootFull "$Name\game"
    New-Item -ItemType Directory -Path (Join-Path $gameRoot 'BrownDust II_Data') -Force | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $gameRoot 'BrownDust II.exe'), 'FAKE-BD2-EXECUTABLE')
    [System.IO.File]::WriteAllText((Join-Path $gameRoot 'UnityPlayer.dll'), 'FAKE-UNITY-PLAYER')
    [System.IO.File]::WriteAllText((Join-Path $gameRoot '20260814000.tied.dat'), 'FAKE-PACKAGE')

    if ($PreExistingLoader) {
        New-Item -ItemType Directory -Path (Join-Path $gameRoot 'BepInEx\core') -Force | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $gameRoot 'BepInEx\plugins\ForeignMod') -Force | Out-Null
        [System.IO.File]::WriteAllText((Join-Path $gameRoot 'BepInEx\core\BepInEx.dll'), 'ORIGINAL-BEPINEX')
        [System.IO.File]::WriteAllText((Join-Path $gameRoot 'BepInEx\plugins\ForeignMod\Foreign.dll'), 'FOREIGN-MOD')
        [System.IO.File]::WriteAllText((Join-Path $gameRoot 'winhttp.dll'), 'ORIGINAL-WINHTTP')
        [System.IO.File]::WriteAllText((Join-Path $gameRoot 'doorstop_config.ini'), 'ORIGINAL-DOORSTOP')
    }

    return $gameRoot
}

function Invoke-Installer {
    param(
        [Parameter(Mandatory)][string]$Scenario,
        [Parameter(Mandatory)][ValidateSet('install', 'verify', 'uninstall')][string]$Action,
        [Parameter(Mandatory)][string]$GameRoot,
        [int]$ExpectedExitCode = 0,
        [int]$FaultAfter = 0
    )

    $scenarioRoot = Join-Path $script:testRootFull $Scenario
    $stateRoot = Join-Path $scenarioRoot 'state'
    $reportPath = Join-Path $scenarioRoot ("report-{0}-{1}.json" -f $Action, ([Guid]::NewGuid().ToString('N')))
    $logPath = [System.IO.Path]::ChangeExtension($reportPath, '.log')
    New-Item -ItemType Directory -Path $scenarioRoot -Force | Out-Null

    $arguments = @(
        '--headless',
        '--action', $Action,
        '--game-root', $GameRoot,
        '--report', $reportPath,
        '--log', $logPath,
        '--state-root', $stateRoot,
        '--skip-process-check'
    )
    if ($FaultAfter -gt 0) {
        $arguments += @('--fault-after', $FaultAfter.ToString())
    }

    $process = Start-Process -FilePath $script:installerFull -ArgumentList $arguments -Wait -PassThru
    Assert-True -Condition ($process.ExitCode -eq $ExpectedExitCode) -Message "$Scenario/$Action retornou $($process.ExitCode), esperado $ExpectedExitCode."
    Assert-True -Condition (Test-Path -LiteralPath $reportPath -PathType Leaf) -Message "$Scenario/$Action não gerou relatório JSON."
    $report = Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8 | ConvertFrom-Json
    Assert-True -Condition ([bool]$report.Success -eq ($ExpectedExitCode -eq 0)) -Message "$Scenario/$Action produziu estado de sucesso incoerente."
    $script:results.Add([pscustomobject]@{
        Scenario = $Scenario
        Action = $Action
        Success = [bool]$report.Success
        ExitCode = $process.ExitCode
        RolledBack = [bool]$report.RolledBack
        FilesProcessed = [int]$report.FilesProcessed
        Message = [string]$report.Message
    }) | Out-Null
    return $report
}

$installerFull = [System.IO.Path]::GetFullPath($InstallerPath)
$testRootFull = Assert-PathInsideRepository -Path $TestRoot
Assert-True -Condition (Test-Path -LiteralPath $installerFull -PathType Leaf) -Message "Instalador não encontrado: $installerFull"
Remove-TestDirectory -Path $testRootFull
New-Item -ItemType Directory -Path $testRootFull -Force | Out-Null
$results = [System.Collections.Generic.List[object]]::new()

Write-Host '[1/10] Rejeição de pasta inválida'
$invalidRoot = Join-Path $testRootFull 'invalid\game'
New-Item -ItemType Directory -Path $invalidRoot -Force | Out-Null
$invalidReport = Invoke-Installer -Scenario 'invalid' -Action install -GameRoot $invalidRoot -ExpectedExitCode 1
Assert-True -Condition ([string]$invalidReport.ErrorType -like '*DirectoryNotFoundException') -Message 'Pasta inválida não foi classificada corretamente.'

Write-Host '[2/10] Instalação limpa e verificação criptográfica'
$cleanGame = New-FakeGame -Name 'clean'
$cleanInstall = Invoke-Installer -Scenario 'clean' -Action install -GameRoot $cleanGame
Assert-True -Condition ($cleanInstall.FilesProcessed -gt 10) -Message 'Instalação limpa processou poucos arquivos.'
$pluginPath = Join-Path $cleanGame 'BepInEx\plugins\BD2PTBR\BD2PTBR.dll'
$catalogPath = Join-Path $cleanGame 'BepInEx\plugins\BD2PTBR\translations\complete.jsonl'
Assert-True -Condition (Test-Path -LiteralPath $pluginPath -PathType Leaf) -Message 'Plugin não foi instalado.'
Assert-True -Condition (Test-Path -LiteralPath $catalogPath -PathType Leaf) -Message 'Catálogo principal não foi instalado.'
Assert-True -Condition (Test-Path -LiteralPath (Join-Path $cleanGame 'winhttp.dll') -PathType Leaf) -Message 'Carregador não foi instalado.'
$expectedPluginHash = (Get-FileHash -LiteralPath $pluginPath -Algorithm SHA256).Hash
Invoke-Installer -Scenario 'clean' -Action verify -GameRoot $cleanGame | Out-Null

Write-Host '[3/10] Detecção de corrupção e reparo'
[System.IO.File]::WriteAllText($pluginPath, 'CORRUPTED')
Invoke-Installer -Scenario 'clean' -Action verify -GameRoot $cleanGame -ExpectedExitCode 1 | Out-Null
Invoke-Installer -Scenario 'clean' -Action install -GameRoot $cleanGame | Out-Null
Assert-True -Condition ((Get-FileHash -LiteralPath $pluginPath -Algorithm SHA256).Hash -eq $expectedPluginHash) -Message 'Reparo não restaurou o plugin oficial.'
Invoke-Installer -Scenario 'clean' -Action verify -GameRoot $cleanGame | Out-Null

Write-Host '[4/10] Reinstalação idempotente'
Invoke-Installer -Scenario 'clean' -Action install -GameRoot $cleanGame | Out-Null
Invoke-Installer -Scenario 'clean' -Action verify -GameRoot $cleanGame | Out-Null

Write-Host '[5/10] Falha injetada e rollback de instalação limpa'
$faultGame = New-FakeGame -Name 'fault-install'
$faultReport = Invoke-Installer -Scenario 'fault-install' -Action install -GameRoot $faultGame -ExpectedExitCode 1 -FaultAfter 7
Assert-True -Condition ([bool]$faultReport.RolledBack) -Message 'Falha injetada não confirmou rollback.'
Assert-True -Condition (-not (Test-Path -LiteralPath (Join-Path $faultGame 'BepInEx'))) -Message 'Rollback deixou BepInEx parcial.'
Assert-True -Condition (-not (Test-Path -LiteralPath (Join-Path $faultGame 'winhttp.dll'))) -Message 'Rollback deixou winhttp.dll parcial.'
Assert-True -Condition (Test-Path -LiteralPath (Join-Path $faultGame 'BrownDust II.exe') -PathType Leaf) -Message 'Rollback alterou o executável do jogo.'

Write-Host '[6/10] Rollback de remoção interrompida'
$faultUninstallGame = New-FakeGame -Name 'fault-uninstall'
Invoke-Installer -Scenario 'fault-uninstall' -Action install -GameRoot $faultUninstallGame | Out-Null
$faultUninstallReport = Invoke-Installer -Scenario 'fault-uninstall' -Action uninstall -GameRoot $faultUninstallGame -ExpectedExitCode 1 -FaultAfter 5
Assert-True -Condition ([bool]$faultUninstallReport.RolledBack) -Message 'Remoção interrompida não confirmou rollback.'
Invoke-Installer -Scenario 'fault-uninstall' -Action verify -GameRoot $faultUninstallGame | Out-Null

Write-Host '[7/10] Preservação de carregador e mod preexistentes'
$preExistingGame = New-FakeGame -Name 'preexisting' -PreExistingLoader
Invoke-Installer -Scenario 'preexisting' -Action install -GameRoot $preExistingGame | Out-Null
Invoke-Installer -Scenario 'preexisting' -Action uninstall -GameRoot $preExistingGame | Out-Null
Assert-True -Condition ((Get-Content -LiteralPath (Join-Path $preExistingGame 'BepInEx\core\BepInEx.dll') -Raw) -eq 'ORIGINAL-BEPINEX') -Message 'BepInEx preexistente não foi restaurado.'
Assert-True -Condition ((Get-Content -LiteralPath (Join-Path $preExistingGame 'winhttp.dll') -Raw) -eq 'ORIGINAL-WINHTTP') -Message 'winhttp.dll preexistente não foi restaurado.'
Assert-True -Condition ((Get-Content -LiteralPath (Join-Path $preExistingGame 'doorstop_config.ini') -Raw) -eq 'ORIGINAL-DOORSTOP') -Message 'doorstop_config.ini preexistente não foi restaurado.'
Assert-True -Condition (Test-Path -LiteralPath (Join-Path $preExistingGame 'BepInEx\plugins\ForeignMod\Foreign.dll') -PathType Leaf) -Message 'Mod de terceiro preexistente foi removido.'
Assert-True -Condition (-not (Test-Path -LiteralPath (Join-Path $preExistingGame 'BepInEx\plugins\BD2PTBR'))) -Message 'Plugin PT-BR permaneceu após remoção.'

Write-Host '[8/10] Preservação de mod adicionado depois da instalação'
$foreignGame = New-FakeGame -Name 'foreign-after'
Invoke-Installer -Scenario 'foreign-after' -Action install -GameRoot $foreignGame | Out-Null
$foreignFile = Join-Path $foreignGame 'BepInEx\plugins\AnotherMod\Another.dll'
New-Item -ItemType Directory -Path (Split-Path -Parent $foreignFile) -Force | Out-Null
[System.IO.File]::WriteAllText($foreignFile, 'ANOTHER-MOD')
$foreignRemoval = Invoke-Installer -Scenario 'foreign-after' -Action uninstall -GameRoot $foreignGame
Assert-True -Condition (Test-Path -LiteralPath $foreignFile -PathType Leaf) -Message 'Mod adicionado posteriormente foi removido.'
Assert-True -Condition (Test-Path -LiteralPath (Join-Path $foreignGame 'BepInEx\core\BepInEx.dll') -PathType Leaf) -Message 'Carregador necessário a outro mod não foi preservado.'
Assert-True -Condition (-not (Test-Path -LiteralPath (Join-Path $foreignGame 'BepInEx\plugins\BD2PTBR'))) -Message 'Plugin PT-BR permaneceu ao preservar outro mod.'
Assert-True -Condition (@($foreignRemoval.Warnings).Count -gt 0) -Message 'Preservação de conteúdo externo não gerou aviso.'

Write-Host '[9/10] Remoção limpa e restauração integral'
$cleanRemoval = Invoke-Installer -Scenario 'clean' -Action uninstall -GameRoot $cleanGame
Assert-True -Condition (-not (Test-Path -LiteralPath (Join-Path $cleanGame 'BepInEx'))) -Message 'Remoção limpa deixou a pasta BepInEx.'
Assert-True -Condition (-not (Test-Path -LiteralPath (Join-Path $cleanGame 'winhttp.dll'))) -Message 'Remoção limpa deixou winhttp.dll.'
Assert-True -Condition (-not (Test-Path -LiteralPath (Join-Path $cleanGame 'doorstop_config.ini'))) -Message 'Remoção limpa deixou doorstop_config.ini.'
Assert-True -Condition (Test-Path -LiteralPath (Join-Path $cleanGame 'BrownDust II.exe') -PathType Leaf) -Message 'Remoção limpa afetou o executável do jogo.'
Assert-True -Condition (@($cleanRemoval.Warnings).Count -eq 0) -Message 'Remoção limpa gerou avisos inesperados.'

Write-Host '[10/10] Remoção após artefatos reais de execução'
$runtimeGame = New-FakeGame -Name 'runtime-generated'
Invoke-Installer -Scenario 'runtime-generated' -Action install -GameRoot $runtimeGame | Out-Null
$runtimeArtifacts = [ordered]@{
    'BepInEx\LogOutput.log' = 'RUNTIME-LOG'
    'BepInEx\config\BepInEx.cfg' = 'RUNTIME-BEPINEX-CONFIG'
    'BepInEx\config\br.com.bd2ptbr.translation.cfg' = 'RUNTIME-PLUGIN-CONFIG'
    'BepInEx\cache\chainloader_typeloader.dat' = 'RUNTIME-TYPE-CACHE'
    'BepInEx\cache\harmony_interop_cache.dat' = 'RUNTIME-HARMONY-CACHE'
}
foreach ($entry in $runtimeArtifacts.GetEnumerator()) {
    $artifactPath = Join-Path $runtimeGame $entry.Key
    New-Item -ItemType Directory -Path (Split-Path -Parent $artifactPath) -Force | Out-Null
    [System.IO.File]::WriteAllText($artifactPath, $entry.Value)
}
$runtimeRemoval = Invoke-Installer -Scenario 'runtime-generated' -Action uninstall -GameRoot $runtimeGame
Assert-True -Condition (-not (Test-Path -LiteralPath (Join-Path $runtimeGame 'BepInEx'))) -Message 'Artefatos criados pelo runtime foram confundidos com conteúdo externo.'
Assert-True -Condition (-not (Test-Path -LiteralPath (Join-Path $runtimeGame 'winhttp.dll'))) -Message 'Remoção após execução deixou winhttp.dll.'
Assert-True -Condition (-not (Test-Path -LiteralPath (Join-Path $runtimeGame 'doorstop_config.ini'))) -Message 'Remoção após execução deixou doorstop_config.ini.'
Assert-True -Condition (@($runtimeRemoval.Warnings).Count -eq 0) -Message 'Remoção após execução gerou aviso indevido de conteúdo externo.'

$summary = [ordered]@{
    schemaVersion = 1
    installer = $installerFull
    installerSha256 = (Get-FileHash -LiteralPath $installerFull -Algorithm SHA256).Hash
    generatedAtUtc = [DateTime]::UtcNow.ToString('o')
    totalOperations = $results.Count
    expectedSuccesses = @($results | Where-Object Success).Count
    expectedFailures = @($results | Where-Object { -not $_.Success }).Count
    rollbackTests = @($results | Where-Object RolledBack).Count
    scenarios = @($results)
    result = 'PASS'
}
$summaryPath = Join-Path $testRootFull 'test-summary.json'
[System.IO.File]::WriteAllText(
    $summaryPath,
    ($summary | ConvertTo-Json -Depth 6),
    $utf8NoBom)
Write-Host "Matriz concluída: $($results.Count) operações, resultado PASS."
Write-Host "Relatório: $summaryPath"
