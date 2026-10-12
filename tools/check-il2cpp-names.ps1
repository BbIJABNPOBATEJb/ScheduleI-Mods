<#
.SYNOPSIS
  Name-level check of the IL2CPP builds against the game's Mono assemblies of another game version.

  IL2CPP builds are compiled against the proxy assemblies MelonLoader generates for the installed
  game. When no IL2CPP copy of another version is at hand, its Mono copy can still tell whether every
  game type and member a build uses exists there: the Il2Cpp prefix is removed from names and fields
  (exposed as properties by the proxies) are matched by name. Signatures are not compared.

.EXAMPLE
  ./tools/check-il2cpp-names.ps1 -MonoGame S:\S1Modding\ScheduleI_Mono
#>
[CmdletBinding()]
param(
    [string[]] $Mods = @("Polyglot", "WorldRates", "DamageIndicator", "GuideArrows", "QuietPause", "ElectricScooter"),
    [Parameter(Mandatory)] [string] $MonoGame,
    # Folder laid out like artifacts/bin (<Mod>/Il2Cpp/<Mod>_Il2Cpp.dll).
    [string] $BinRoot = ""
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Add-Type -Path (Join-Path $MonoGame "MelonLoader/net35/Mono.Cecil.dll")
$bin = if ($BinRoot) { $BinRoot } else { Join-Path $root "artifacts/bin" }

$managed = Join-Path $MonoGame "Schedule I_Data/Managed"
$types = @{}
foreach ($asm in "Assembly-CSharp", "Assembly-CSharp-firstpass", "ScheduleOne.Core", "FishNet.Runtime") {
    $m = [Mono.Cecil.ModuleDefinition]::ReadModule((Join-Path $managed "$asm.dll"))
    foreach ($t in $m.GetTypes()) { $types[$t.FullName] = $t }
}

function MonoName([string] $name) {
    # Il2CppScheduleOne.X -> ScheduleOne.X, Il2CppEPOOutline -> EPOOutline, Il2CppFishNet -> FishNet
    return ($name -replace '(^|[/.<,\[ ])Il2Cpp(?=[A-Z])', '$1')
}

function HasMember($type, [string] $name, [int] $params) {
    for ($t = $type; $null -ne $t; ) {
        if ($name -eq ".ctor") {
            if ($t.Methods | Where-Object { $_.IsConstructor -and -not $_.IsStatic -and $_.Parameters.Count -eq $params }) { return $true }
        }
        else {
            $plain = $name -replace '^(get_|set_)', ''
            if ($t.Fields | Where-Object { $_.Name -eq $plain }) { return $true }
            if ($t.Properties | Where-Object { $_.Name -eq $plain }) { return $true }
            if ($t.Methods | Where-Object { $_.Name -eq $name }) { return $true }
            if ($t.Events | Where-Object { $_.Name -eq $plain -or "add_$($_.Name)" -eq $name -or "remove_$($_.Name)" -eq $name }) { return $true }
        }
        if ($null -eq $t.BaseType) { break }
        $t = $types[(MonoName $t.BaseType.GetElementType().FullName)]
    }
    return $false
}

$gameScopes = "^(Assembly-CSharp|Assembly-CSharp-firstpass|Il2CppScheduleOne\.Core|Il2CppFishNet\.Runtime)$"
$failed = 0
foreach ($mod in $Mods) {
    $module = [Mono.Cecil.ModuleDefinition]::ReadModule((Join-Path $bin "$mod/Il2Cpp/${mod}_Il2Cpp.dll"))
    $missing = New-Object System.Collections.Generic.List[string]
    foreach ($tr in $module.GetTypeReferences()) {
        if ($tr.Scope.Name -notmatch $gameScopes) { continue }
        $n = MonoName $tr.FullName
        if (-not $types.ContainsKey($n)) { $missing.Add("type   $n") }
    }
    foreach ($mr in $module.GetMemberReferences()) {
        $decl = $mr.DeclaringType
        if ($null -eq $decl) { continue }
        $el = $decl.GetElementType()
        if ($null -eq $el.Scope -or $el.Scope.Name -notmatch $gameScopes) { continue }
        if ($mr.Name -like "NativeFieldInfoPtr_*" -or $mr.Name -like "NativeMethodInfoPtr_*" -or $mr.Name -eq "Il2CppClassPointer") { continue }
        $tn = MonoName $el.FullName
        $t = $types[$tn]
        if ($null -eq $t) { $missing.Add("type   $tn (for $($mr.Name))"); continue }
        $params = if ($mr -is [Mono.Cecil.MethodReference]) { $mr.Parameters.Count } else { 0 }
        # IL2CPP proxies have an extra (IntPtr) constructor.
        if ($mr.Name -eq ".ctor" -and $params -eq 1 -and $mr.Parameters[0].ParameterType.FullName -eq "System.IntPtr") { continue }
        if (-not (HasMember $t $mr.Name $params)) { $missing.Add("member $tn::$($mr.Name) ($params params)") }
    }
    $module.Dispose()
    $unique = $missing | Sort-Object -Unique
    if ($unique) { $failed++; "$mod : $(@($unique).Count) names not in $MonoGame"; $unique | ForEach-Object { "    $_" } }
    else { "$mod : OK" }
}
if ($failed) { exit 1 }
