#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Launcher,
    [Parameter(Mandatory)][string]$Payload,
    [Parameter(Mandatory)][string]$GameRoot,
    [Parameter(Mandatory)][string]$OutputRoot,
    [Parameter(Mandatory)][string]$ModVersion,
    [string]$PreviousLauncher
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$app = (Resolve-Path -LiteralPath $Launcher).Path
$package = (Resolve-Path -LiteralPath $Payload).Path
$game = (Resolve-Path -LiteralPath $GameRoot).Path
$output = [IO.Path]::GetFullPath($OutputRoot)
if (Test-Path -LiteralPath $output) { throw 'Test output must be new.' }
if ($output.StartsWith($game.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Tests must run outside the game.' }
New-Item -ItemType Directory -Path $output | Out-Null
$payloadHash = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash
$critical = @('BrownDust II.exe','BrownDust II_Data/Managed/Assembly-CSharp.dll','BrownDust II_Data/globalgamemanagers')
$fixtureFiles = $critical + @('UnityPlayer.dll','BrownDust II_Data/Managed/Unity.TextMeshPro.dll','MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll')
$sourceBefore = @{}
foreach ($path in $critical) { $sourceBefore[$path] = (Get-FileHash -LiteralPath (Join-Path $game $path) -Algorithm SHA256).Hash }
$records = [Collections.Generic.List[object]]::new()
$assertions = 0
function Assert-Check([bool]$Condition, [string]$Label) {
    if (-not $Condition) { throw "Artifact assertion failed: $Label" }
    $script:assertions++
}
function New-Fixture([string]$Name) {
    $root = Join-Path $output "$Name/game"
    foreach ($path in $fixtureFiles) {
        $target = Join-Path $root $path
        New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $game $path) -Destination $target
    }
    $tied = Get-ChildItem -LiteralPath $game -Filter '*.tied.dat' -File | Sort-Object Name -Descending | Select-Object -First 1
    [IO.File]::WriteAllText((Join-Path $root $tied.Name), 'Synthetic package-name marker; not a runnable game copy.')
    [IO.File]::WriteAllText((Join-Path $root 'foreign-user-note.txt'), 'preserve')
    return $root
}
function Run-Operation([string]$Name, [string]$Root, [string]$Action, [bool]$ExpectedSuccess, [string]$Executable = $app, [int]$FaultAfter = 0) {
    $report = Join-Path $output "$Name.json"
    $state = Join-Path ([IO.Path]::GetDirectoryName($Root)) 'state'
    $arguments = "--headless --action $Action --game-root `"$Root`" --payload `"$package`" --payload-sha256 $payloadHash --mod-version $ModVersion --state-root `"$state`" --skip-process-check --report `"$report`""
    if ($FaultAfter -gt 0) { $arguments += " --fault-after $FaultAfter" }
    $process = Start-Process -FilePath $Executable -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(60000)) { $process.Kill(); throw "Installer test timed out: $Name" }
    Assert-Check (Test-Path -LiteralPath $report) "$Name report"
    $result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    Assert-Check ($result.Success -eq $ExpectedSuccess) "$Name outcome"
    Assert-Check (($process.ExitCode -eq 0) -eq $ExpectedSuccess) "$Name exit code"
    $records.Add([ordered]@{name=$Name;action=$Action;expectedSuccess=$ExpectedSuccess;actualSuccess=$result.Success;files=$result.FilesProcessed;rolledBack=$result.RolledBack;report=[IO.Path]::GetFileName($report)})
    return $result
}
$clean = New-Fixture 'clean'
Run-Operation '01-install' $clean Install $true | Out-Null
Run-Operation '02-verify' $clean Verify $true | Out-Null
$plugin = Join-Path $clean 'BepInEx/plugins/BD2PTBR/BD2PTBR.dll'
[IO.File]::WriteAllText($plugin, 'corrupted test plugin')
Run-Operation '03-detect-damage' $clean Verify $false | Out-Null
Run-Operation '04-repair' $clean Repair $true | Out-Null
Run-Operation '05-verify-repair' $clean Verify $true | Out-Null
Run-Operation '06-uninstall' $clean Uninstall $true | Out-Null
Assert-Check (-not (Test-Path -LiteralPath $plugin)) 'uninstall removed owned plugin'
Assert-Check ((Get-Content -LiteralPath (Join-Path $clean 'foreign-user-note.txt') -Raw) -eq 'preserve') 'foreign file preserved'
$fault = New-Fixture 'fault'
$rollback = Run-Operation '07-failed-install' $fault Install $false $app 4
Assert-Check $rollback.RolledBack 'failed clean installation rolled back'
Assert-Check (-not (Test-Path -LiteralPath (Join-Path $fault 'BepInEx/plugins/BD2PTBR/BD2PTBR.dll'))) 'rollback removed partial plugin'
$future = New-Fixture 'future'
[IO.File]::WriteAllText((Join-Path $future 'BrownDust II_Data/globalgamemanagers'), 'synthetic future metadata 2.99.12')
[IO.File]::WriteAllText((Join-Path $future '20991231000.tied.dat'), 'future package-name marker')
Run-Operation '08-older-catalog-new-client' $future Install $true | Out-Null
Run-Operation '09-verify-future-fixture' $future Verify $true | Out-Null
Run-Operation '10-uninstall-future-fixture' $future Uninstall $true | Out-Null
if ($PreviousLauncher) {
    $previous = (Resolve-Path -LiteralPath $PreviousLauncher).Path
    $legacy = New-Fixture 'old-app'
    Run-Operation '11-previous-app-installs-new-mod' $legacy Install $true $previous | Out-Null
    Run-Operation '12-previous-app-verifies-new-mod' $legacy Verify $true $previous | Out-Null
    Run-Operation '13-previous-app-uninstalls-new-mod' $legacy Uninstall $true $previous | Out-Null
}
foreach ($path in $critical) { Assert-Check ((Get-FileHash -LiteralPath (Join-Path $game $path) -Algorithm SHA256).Hash -eq $sourceBefore[$path]) "source client unchanged: $path" }
$summary = [ordered]@{status='PASS';launcherSha256=(Get-FileHash -LiteralPath $app -Algorithm SHA256).Hash;payloadSha256=$payloadHash;modVersion=$ModVersion;operations=$records.Count;assertions=$assertions;cases=$records;gameWritten=$false;realGameStarted=$false;scope='Production executables and payload, isolated partial fixtures; real-game validation remains separate.'}
[IO.File]::WriteAllText((Join-Path $output 'summary.json'), ($summary | ConvertTo-Json -Depth 7), [Text.UTF8Encoding]::new($false))
Write-Output "PASS: $($records.Count) artifact operations, $assertions assertions."
