<#
.SYNOPSIS
  Builds release DLLs and packs one zip per mod and runtime into dist/.

  dist/<Mod>-<version>-IL2CPP.zip   -> Mods/<Mod>_Il2Cpp.dll   (Steam default branch)
  dist/<Mod>-<version>-Mono.zip     -> Mods/<Mod>_Mono.dll     (Steam "alternate" branch)

  Separate zips on purpose: MelonLoader tries to load every DLL in Mods, and the other
  runtime's build fails to load.

.EXAMPLE
  ./tools/package.ps1              # all mods
  ./tools/package.ps1 -Mods WorldRates
#>
[CmdletBinding()]
param(
    [string[]] $Mods = @("Polyglot", "WorldRates")
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $root "dist"
New-Item -ItemType Directory -Force $dist | Out-Null

foreach ($mod in $Mods) {
    $project = Join-Path $root "mods/$mod/$mod.csproj"
    $modInfo = Get-Content (Join-Path $root "mods/$mod/src/ModInfo.cs") -Raw
    $version = [regex]::Match($modInfo, 'Version = "([^"]+)"').Groups[1].Value

    foreach ($runtime in @("Il2Cpp", "Mono")) {
        dotnet build $project -c $runtime -nologo -v q -p:AutomateLocalDeployment=false | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "$mod $runtime build failed" }

        $dll = Join-Path $root "artifacts/bin/$mod/$runtime/${mod}_$runtime.dll"
        $stage = Join-Path $dist "stage/$mod-$runtime"
        Remove-Item -Recurse -Force $stage -ErrorAction SilentlyContinue
        New-Item -ItemType Directory -Force (Join-Path $stage "Mods") | Out-Null
        Copy-Item $dll (Join-Path $stage "Mods")
        Copy-Item (Join-Path $root "mods/$mod/README.md") $stage -ErrorAction SilentlyContinue
        if ($mod -eq "Polyglot") {
            Copy-Item (Join-Path $root "mods/Polyglot/fonts/OFL*.txt") $stage
        }

        $label = if ($runtime -eq "Il2Cpp") { "IL2CPP" } else { "Mono" }
        $zip = Join-Path $dist "$mod-$version-$label.zip"
        Remove-Item $zip -ErrorAction SilentlyContinue
        Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zip
        Write-Host "packed $zip"
    }
}
Remove-Item -Recurse -Force (Join-Path $dist "stage") -ErrorAction SilentlyContinue
