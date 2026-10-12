<#
.SYNOPSIS
  Runs the smoke scenarios of every mod one after another (tools/run-smoke.ps1) and prints one line
  per run plus the mods' own warnings from the game log.

.EXAMPLE
  ./tools/regression.ps1 -Runtimes Il2Cpp                         # the game in Il2CppGamePath / MonoGamePath
  ./tools/regression.ps1 -Beta -Only GuideArrows,Polyglot:sms     # beta copies, some mods or mod:scenario
  ./tools/regression.ps1 -Save "<backed-up SaveGame_N folder>"    # scenarios that need a progressed save
#>
[CmdletBinding()]
param(
    [string[]] $Runtimes = @("Il2Cpp", "Mono"),
    # Mods or Mod:scenario to run (default: all).
    [string[]] $Only = @(),
    [switch] $Beta,
    # A progressed save (customers, deals, several properties) for the scenarios marked below.
    # Without it those scenarios are skipped.
    [string] $Save = ""
)
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

# Mod, scenario, argument, needs a progressed save
$tests = @(
    @("QuietPause", "audio", "", $false),
    @("WorldRates", "ui", "", $false),
    @("WorldRates", "inspect", "", $false),
    @("WorldRates", "income", "", $false),
    @("WorldRates", "deals", "", $true),
    @("WorldRates", "complete-contract", "", $true),
    @("DamageIndicator", "combat", "", $false),
    @("DamageIndicator", "witness", "", $false),
    @("DamageIndicator", "always", "", $false),
    @("GuideArrows", "arrows", "", $false),
    @("GuideArrows", "outline", "", $true),
    @("ElectricScooter", "ride", "", $false),
    @("ElectricScooter", "shop", "", $false),
    @("Polyglot", "tour", "ru", $false),
    @("Polyglot", "sms", "", $false),
    @("Polyglot", "effects", "", $true)
)
$mods = "Polyglot|WorldRates|DamageIndicator|GuideArrows|QuietPause|ElectricScooter"
$results = @()
foreach ($rt in $Runtimes) {
    foreach ($t in $tests) {
        $mod, $scenario, $arg, $needsSave = $t
        if ($Only.Count -and ($Only -notcontains $mod) -and ($Only -notcontains "${mod}:$scenario")) { continue }
        if ($needsSave -and -not $Save) { $results += "$rt $mod ${scenario}: skipped (needs -Save)"; continue }
        $a = @{ Mod = $mod; Scenario = $scenario; Runtime = $rt; TimeoutSeconds = 400 }
        if ($Beta) { $a.Beta = $true }
        if ($arg) { $a.Arg = $arg }
        if ($needsSave) { $a.Save = $Save }
        $o = ./tools/run-smoke.ps1 @a *>&1 | Out-String
        $line = ($o -split "`n" | Where-Object { $_ -match "^---- (PASS|FAIL)" } | Select-Object -First 1)
        if (-not $line) { $line = "---- NO RESULT: " + (($o -split "`n" | Select-Object -Last 3) -join " | ") }
        $dir = Get-ChildItem test-runs -Directory | Sort-Object LastWriteTime | Select-Object -Last 1
        $warn = @()
        $log = Join-Path $dir.FullName "MelonLoader.log"
        if (Test-Path $log) {
            $warn = Select-String -Path $log -Pattern "not applied|\[WARNING\] \[($mods)\]|\[ERROR\] \[($mods)\]" |
                ForEach-Object { $_.Line -replace "^\[[0-9:.]+\] ", "" } | Sort-Object -Unique | Select-Object -First 4
        }
        $text = $line.Trim()
        if ($text.Length -gt 170) { $text = $text.Substring(0, 170) }
        $results += "$rt $mod ${scenario}: $text"
        foreach ($w in $warn) { $results += "      ! " + $(if ($w.Length -gt 200) { $w.Substring(0, 200) } else { $w }) }
        $results[-1 - @($warn).Count]
        foreach ($w in $warn) { "      ! $w" }
    }
}
"==== summary"
$results
