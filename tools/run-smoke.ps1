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
    [switch] $Beta,
    # Run while someone plays: on a copy of the game (Il2CppPublicCopyPath for the public IL2CPP game),
    # as a small silent window kept behind the player's game, watching and stopping only its own
    # process. The folder of a running game is never used.
    [switch] $Parallel
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

[xml] $props = Get-Content (Join-Path $root "local.build.props")
$gamePath = if ($Runtime -eq "Il2Cpp") { $props.Project.PropertyGroup.Il2CppGamePath } else { $props.Project.PropertyGroup.MonoGamePath }
if (-not $gamePath -or -not (Test-Path (Join-Path $gamePath "Schedule I.exe"))) { throw "Game path for $Runtime is not configured in local.build.props" }
if (-not $Parallel -and (Get-Process "Schedule I" -ErrorAction SilentlyContinue)) { throw "Schedule I is already running. Close it first (or use -Parallel)." }
$dllName = "${Mod}_$Runtime.dll"
if ($Beta) {
    $gamePath = if ($Runtime -eq "Il2Cpp") { $props.Project.PropertyGroup.Il2CppBetaGamePath } else { $props.Project.PropertyGroup.MonoBetaGamePath }
    if (-not $gamePath -or -not (Test-Path (Join-Path $gamePath "Schedule I.exe"))) { throw "Beta game path for $Runtime is not configured in local.build.props" }
    New-Item -ItemType Directory -Force (Join-Path $gamePath "Mods") | Out-Null
}
elseif ($Parallel -and $Runtime -eq "Il2Cpp") {
    $gamePath = $props.Project.PropertyGroup.Il2CppPublicCopyPath
    if (-not $gamePath -or -not (Test-Path (Join-Path $gamePath "Schedule I.exe"))) { throw "Il2CppPublicCopyPath (a copy of the game for -Parallel) is not configured in local.build.props" }
}
if ($Parallel) {
    $full = (Resolve-Path $gamePath).Path.TrimEnd('\')
    foreach ($running in Get-Process "Schedule I" -ErrorAction SilentlyContinue) {
        if ($running.Path -and ((Split-Path -Parent $running.Path).TrimEnd('\') -eq $full)) { throw "The game in $full is running: -Parallel needs a copy nobody plays." }
    }
}

$project = Join-Path $root "mods/$Mod/$Mod.csproj"
# Built without deploying (that would go to Il2CppGamePath) and copied into the game folder the run uses.
dotnet build $project -c "${Runtime}Dev" -nologo -v q -p:AutomateLocalDeployment=false | Out-Host
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
Copy-Item (Join-Path $root "artifacts/bin/$Mod/${Runtime}Dev/$dllName") (Join-Path $gamePath "Mods/$dllName") -Force

$runId = "{0}-{1}{2}-{3}{4}-{5}" -f $Mod, $Scenario, $(if ($Arg) { "-$Arg" } else { "" }), $Runtime.ToLower(), $(if ($Beta) { "-beta" } else { "" }), (Get-Date -Format "yyyyMMdd-HHmmss")
$out = Join-Path $root "test-runs/$runId"
New-Item -ItemType Directory -Force $out | Out-Null

$gameArgs = @("--s1dev-mod", $Mod, "--s1dev-scenario", $Scenario, "--s1dev-out", "`"$out`"", "--s1dev-timeout", $TimeoutSeconds)
if ($Arg) { $gameArgs += @("--s1dev-arg", $Arg) }
if ($Save) {
    if (-not (Test-Path (Join-Path $Save "Game.json"))) { throw "$Save is not a save folder (no Game.json)" }
    $gameArgs += @("--s1dev-save", "`"$((Resolve-Path $Save).Path)`"")
}
if ($Parallel) {
    # A window, not the fullscreen the shared display settings ask for; no MelonLoader console.
    $gameArgs += @("--s1dev-background", "-screen-fullscreen", "0", "-screen-width", "1280", "-screen-height", "720", "--melonloader.hideconsole")
}

# Both games keep their settings in the same registry key; the test game must not leave its window size there.
$prefsKey = "Software\TVGS\Schedule I"
$prefsBefore = @{}
$key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($prefsKey)
if ($key) {
    foreach ($name in $key.GetValueNames()) { $prefsBefore[$name] = @($key.GetValueKind($name), $key.GetValue($name)) }
    $key.Close()
}

Add-Type -Namespace SmokeRun -Name Win32 -MemberDefinition @'
[DllImport("user32.dll")] public static extern System.IntPtr GetForegroundWindow();
[DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(System.IntPtr hWnd, out uint processId);
'@ -ErrorAction SilentlyContinue

Write-Host "Launching $Runtime game$(if ($Beta) { ' (beta copy)' })$(if ($Parallel) { ' next to the running game' }): $runId"
$process = Start-Process -FilePath (Join-Path $gamePath "Schedule I.exe") -WorkingDirectory $gamePath -ArgumentList $gameArgs -PassThru

$deadline = (Get-Date).AddSeconds($TimeoutSeconds + 90)
$samples = 0; $inFront = 0
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 2
    if ($Parallel) {
        # Only this run's own process: the other one is somebody playing.
        $samples++
        $owner = [uint32]0
        [void][SmokeRun.Win32]::GetWindowThreadProcessId([SmokeRun.Win32]::GetForegroundWindow(), [ref] $owner)
        if ($owner -eq $process.Id) { $inFront++ }
        if ($process.HasExited) { break }
        continue
    }
    $running = Get-Process "Schedule I" -ErrorAction SilentlyContinue
    if (-not $running -and (Test-Path (Join-Path $out "result.txt"))) { break }
    if (-not $running -and $process.HasExited -and ((Get-Date) - $process.StartTime).TotalSeconds -gt 20) { break }
}
if ($Parallel) {
    if (-not $process.HasExited) { Write-Warning "Test game still running after timeout, killing it"; Stop-Process -Id $process.Id -Force; Start-Sleep 2 }
    Write-Host "Test window had the focus in $inFront of $samples checks"
}
else {
    $left = Get-Process "Schedule I" -ErrorAction SilentlyContinue
    if ($left) { Write-Warning "Game still running after timeout, killing it"; $left | Stop-Process -Force; Start-Sleep 2 }
}

$key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($prefsKey, $true)
if ($key) {
    $restored = 0
    foreach ($name in $prefsBefore.Keys) {
        $kind, $value = $prefsBefore[$name]
        $now = $key.GetValue($name)
        $same = if ($value -is [byte[]] -and $now -is [byte[]]) { [Convert]::ToBase64String($value) -eq [Convert]::ToBase64String($now) } else { "$value" -eq "$now" }
        if (-not $same) { $key.SetValue($name, $value, $kind); $restored++ }
    }
    $key.Close()
    if ($restored) { Write-Host "Restored $restored game settings in the registry" }
}

Copy-Item (Join-Path $gamePath "MelonLoader/Latest.log") (Join-Path $out "MelonLoader.log") -ErrorAction SilentlyContinue
$result = Join-Path $out "result.txt"
$status = if (Test-Path $result) { (Get-Content $result -Raw).Trim() } else { "FAIL no result.txt (crash or harness not started)" }

if (-not $KeepDevBuild) {
    dotnet build $project -c $Runtime -nologo -v q -p:AutomateLocalDeployment=false | Out-Null
    Copy-Item (Join-Path $root "artifacts/bin/$Mod/$Runtime/$dllName") (Join-Path $gamePath "Mods/$dllName") -Force
}

Write-Host "---- $status"
Write-Host "---- output: $out"
$log = Join-Path $out "MelonLoader.log"
if (Test-Path $log) {
    Select-String -Path $log -Pattern "ERROR|Exception|\[$Mod\].*(Warning|failed)" | Select-Object -First 25 | ForEach-Object { $_.Line }
}
if ($status -notlike "PASS*") { exit 1 }
