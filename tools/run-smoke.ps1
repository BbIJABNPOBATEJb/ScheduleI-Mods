<#
.SYNOPSIS
  Builds a mod in its Dev configuration, launches the game with the in-game smoke harness
  (shared/Dev/DevSmoke.cs) and collects the results into test-runs/<run-id>.

.EXAMPLE
  ./tools/run-smoke.ps1 -Mod Polyglot -Scenario inspect -Runtime Il2Cpp
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Mod,
    [Parameter(Mandatory)] [string] $Scenario,
    [ValidateSet("Il2Cpp", "Mono")] [string] $Runtime = "Il2Cpp",
    [int] $TimeoutSeconds = 300,
    # Passed to the scenario as DevSmoke.Arg (e.g. a language code).
    [string] $Arg = "",
    # A save folder to copy into the run and load instead of the game's default save
    # (e.g. a backed-up SaveGame_1). The original is never modified.
    [string] $Save = "",
    # Leave the Dev build in the game's Mods folder instead of redeploying the release build.
    [switch] $KeepDevBuild,
    # Run on the beta copy of the game (Il2CppBetaGamePath / MonoBetaGamePath in local.build.props).
    # The mod is still built against the public game - the same binary players get - and that DLL
    # is copied into the beta copy, so the run shows whether the released build works on the beta.
    [switch] $Beta
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

[xml] $props = Get-Content (Join-Path $root "local.build.props")
$gamePath = if ($Runtime -eq "Il2Cpp") { $props.Project.PropertyGroup.Il2CppGamePath } else { $props.Project.PropertyGroup.MonoGamePath }
if (-not $gamePath -or -not (Test-Path (Join-Path $gamePath "Schedule I.exe"))) { throw "Game path for $Runtime is not configured in local.build.props" }
if (Get-Process "Schedule I" -ErrorAction SilentlyContinue) { throw "Schedule I is already running. Close it first." }
$dllName = "${Mod}_$Runtime.dll"
if ($Beta) {
    $gamePath = if ($Runtime -eq "Il2Cpp") { $props.Project.PropertyGroup.Il2CppBetaGamePath } else { $props.Project.PropertyGroup.MonoBetaGamePath }
    if (-not $gamePath -or -not (Test-Path (Join-Path $gamePath "Schedule I.exe"))) { throw "Beta game path for $Runtime is not configured in local.build.props" }
    New-Item -ItemType Directory -Force (Join-Path $gamePath "Mods") | Out-Null
}

$project = Join-Path $root "mods/$Mod/$Mod.csproj"
dotnet build $project -c "${Runtime}Dev" -nologo -v q -p:AutomateLocalDeployment=true | Out-Host
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
if ($Beta) { Copy-Item (Join-Path $root "artifacts/bin/$Mod/${Runtime}Dev/$dllName") (Join-Path $gamePath "Mods/$dllName") -Force }

$runId = "{0}-{1}{2}-{3}{4}-{5}" -f $Mod, $Scenario, $(if ($Arg) { "-$Arg" } else { "" }), $Runtime.ToLower(), $(if ($Beta) { "-beta" } else { "" }), (Get-Date -Format "yyyyMMdd-HHmmss")
$out = Join-Path $root "test-runs/$runId"
New-Item -ItemType Directory -Force $out | Out-Null

$gameArgs = @("--s1dev-mod", $Mod, "--s1dev-scenario", $Scenario, "--s1dev-out", "`"$out`"", "--s1dev-timeout", $TimeoutSeconds)
if ($Arg) { $gameArgs += @("--s1dev-arg", $Arg) }
if ($Save) {
    if (-not (Test-Path (Join-Path $Save "Game.json"))) { throw "$Save is not a save folder (no Game.json)" }
    $gameArgs += @("--s1dev-save", "`"$((Resolve-Path $Save).Path)`"")
}
Write-Host "Launching $Runtime game$(if ($Beta) { ' (beta copy)' }): $runId"
$process = Start-Process -FilePath (Join-Path $gamePath "Schedule I.exe") -WorkingDirectory $gamePath -ArgumentList $gameArgs -PassThru

$deadline = (Get-Date).AddSeconds($TimeoutSeconds + 90)
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 2
    $running = Get-Process "Schedule I" -ErrorAction SilentlyContinue
    if (-not $running -and (Test-Path (Join-Path $out "result.txt"))) { break }
    if (-not $running -and $process.HasExited -and ((Get-Date) - $process.StartTime).TotalSeconds -gt 20) { break }
}
$left = Get-Process "Schedule I" -ErrorAction SilentlyContinue
if ($left) { Write-Warning "Game still running after timeout, killing it"; $left | Stop-Process -Force; Start-Sleep 2 }

Copy-Item (Join-Path $gamePath "MelonLoader/Latest.log") (Join-Path $out "MelonLoader.log") -ErrorAction SilentlyContinue
$result = Join-Path $out "result.txt"
$status = if (Test-Path $result) { (Get-Content $result -Raw).Trim() } else { "FAIL no result.txt (crash or harness not started)" }

if (-not $KeepDevBuild) {
    dotnet build $project -c $Runtime -nologo -v q -p:AutomateLocalDeployment=true | Out-Null
    if ($Beta) { Copy-Item (Join-Path $root "artifacts/bin/$Mod/$Runtime/$dllName") (Join-Path $gamePath "Mods/$dllName") -Force }
}

Write-Host "---- $status"
Write-Host "---- output: $out"
$log = Join-Path $out "MelonLoader.log"
if (Test-Path $log) {
    Select-String -Path $log -Pattern "ERROR|Exception|\[$Mod\].*(Warning|failed)" | Select-Object -First 25 | ForEach-Object { $_.Line }
}
if ($status -notlike "PASS*") { exit 1 }
