<#
.SYNOPSIS
  Checks that everything a built mod DLL uses from the game still exists in a given copy of the game.

  A mod is compiled against one game version, but players also run it on others (the beta branch).
  A type, method or field that was renamed or changed its signature there only fails when that code
  first runs. This lists such references up front, without starting the game: every reference in the
  DLL to the game's assemblies is resolved against the assemblies of the given game folder.

.EXAMPLE
  ./tools/check-game-api.ps1 -Runtime Il2Cpp -GamePath S:\S1Modding\ScheduleI_Beta
  ./tools/check-game-api.ps1 -Runtime Mono -GamePath S:\S1Modding\ScheduleI_MonoBeta -Mods WorldRates
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [ValidateSet("Il2Cpp", "Mono")] [string] $Runtime,
    # Game folder to check against (MelonLoader installed; for IL2CPP launched once so that the proxy assemblies exist).
    [Parameter(Mandatory)] [string] $GamePath,
    [string[]] $Mods = @("Polyglot", "WorldRates", "DamageIndicator", "GuideArrows", "QuietPause", "ElectricScooter"),
    # Build configuration whose output is checked: the release one by default, "Dev" adds the smoke-test code.
    [string] $Suffix = ""
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

$cecil = Join-Path $GamePath "MelonLoader/net35/Mono.Cecil.dll"
if (-not (Test-Path $cecil)) { throw "Mono.Cecil not found in $GamePath\MelonLoader\net35" }
Add-Type -Path $cecil

$gameDirs = if ($Runtime -eq "Il2Cpp") { @("MelonLoader/Il2CppAssemblies", "MelonLoader/net6") } else { @("Schedule I_Data/Managed", "MelonLoader/net35") }
$resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
foreach ($dir in $gameDirs) {
    $path = Join-Path $GamePath $dir
    if (-not (Test-Path $path)) { throw "$path not found" }
    $resolver.AddSearchDirectory($path)
}
$parameters = New-Object Mono.Cecil.ReaderParameters
$parameters.AssemblyResolver = $resolver

# Only the game's own code changes between game versions; Unity, MelonLoader and the BCL do not.
$gameAssemblies = "^(Assembly-CSharp|Assembly-CSharp-firstpass|ScheduleOne\.Core|FishNet\.Runtime)$"
$failed = 0
foreach ($mod in $Mods) {
    $dll = Join-Path $root "artifacts/bin/$mod/$Runtime$Suffix/${mod}_$Runtime.dll"
    if (-not (Test-Path $dll)) { throw "$dll not found: build the mod first" }
    $module = [Mono.Cecil.ModuleDefinition]::ReadModule($dll, $parameters)
    $missing = New-Object System.Collections.Generic.List[string]
    foreach ($type in $module.GetTypeReferences()) {
        if ($type.Scope.Name -notmatch $gameAssemblies) { continue }
        $resolved = $null
        try { $resolved = $type.Resolve() } catch { }
        if ($null -eq $resolved) { $missing.Add("type   $($type.FullName)") }
    }
    foreach ($member in $module.GetMemberReferences()) {
        $declaring = $member.DeclaringType
        if ($null -eq $declaring) { continue }
        $scope = $declaring.GetElementType().Scope
        if ($null -eq $scope -or $scope.Name -notmatch $gameAssemblies) { continue }
        $resolved = $null
        try { $resolved = $member.Resolve() } catch { }
        if ($null -eq $resolved) { $missing.Add("member $($member.FullName)") }
    }
    $module.Dispose()
    $unique = $missing | Sort-Object -Unique
    if ($unique) {
        $failed++
        Write-Host "$mod ($Runtime$Suffix): $(@($unique).Count) references not found in $GamePath"
        $unique | ForEach-Object { Write-Host "    $_" }
    }
    else {
        Write-Host "$mod ($Runtime$Suffix): OK"
    }
}
if ($failed) { exit 1 }
