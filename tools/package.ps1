<#
.SYNOPSIS
  Builds release DLLs and packs them into dist/:

  dist/<Mod>-<version>-IL2CPP.zip        Mods/<Mod>_Il2Cpp.dll + README/CHANGELOG   (GitHub / Nexus)
  dist/<Mod>-<version>-Mono.zip          Mods/<Mod>_Mono.dll   + README/CHANGELOG
  dist/thunderstore/<Mod>-<version>.zip       manifest.json, icon.png, README.md, CHANGELOG.md, Mods/<Mod>_Il2Cpp.dll
  dist/thunderstore/<Mod>_Mono-<version>.zip  the same for the Mono build

  One ZIP per runtime on purpose: MelonLoader tries to load every DLL in Mods, and the other
  runtime's build fails to load. On Thunderstore the IL2CPP build (default Steam branch) is the
  package <Mod>, the Mono build (alternate branch) the package <Mod>_Mono.
  Publish to Thunderstore with tools/publish_thunderstore.py.

.EXAMPLE
  ./tools/package.ps1              # all mods
  ./tools/package.ps1 -Mods WorldRates
#>
[CmdletBinding()]
param(
    [string[]] $Mods = @("Polyglot", "WorldRates", "DamageIndicator", "GuideArrows", "QuietPause")
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $root "dist"
$repoUrl = "https://github.com/BbIJABNPOBATEJb/ScheduleI-Mods"
$rawUrl = "https://raw.githubusercontent.com/BbIJABNPOBATEJb/ScheduleI-Mods/main"
$tsUrl = "https://thunderstore.io/c/schedule-i/p/BbIJABNPOBATEJb"
New-Item -ItemType Directory -Force (Join-Path $dist "thunderstore") | Out-Null

# README links are relative to mods/<Mod>/ in the repo; inside packages they must be absolute.
function Get-PackageReadme([string] $mod) {
    $text = Get-Content (Join-Path $root "mods/$mod/README.md") -Raw -Encoding UTF8
    $text = $text -replace '\.\./\.\./docs/', "$rawUrl/docs/"
    $text = $text -replace 'src="assets/', "src=`"$rawUrl/mods/$mod/assets/"
    $text = $text -replace '\]\((?!https?://|#)([^)]+)\)', "]($repoUrl/blob/main/mods/$mod/`$1)"
    $text = $text -replace "blob/main/mods/$mod/\.\./\.\./", "blob/main/"
    $text = $text -replace "blob/main/mods/$mod/\.\./", "blob/main/mods/"
    return $text
}

function Write-Utf8([string] $path, [string] $text) {
    [IO.File]::WriteAllText($path, $text, (New-Object Text.UTF8Encoding($false)))
}

# Compress-Archive in Windows PowerShell writes backslash entry names; mod managers and Linux need "/".
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
function New-Zip([string] $sourceDir, [string] $zipPath) {
    Remove-Item $zipPath -ErrorAction SilentlyContinue
    $zip = [IO.Compression.ZipFile]::Open($zipPath, [IO.Compression.ZipArchiveMode]::Create)
    try {
        $base = (Resolve-Path $sourceDir).Path.TrimEnd([char]92) + [char]92
        foreach ($file in Get-ChildItem $sourceDir -Recurse -File) {
            $entry = $file.FullName.Substring($base.Length).Replace([char]92, [char]47)
            [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file.FullName, $entry, [IO.Compression.CompressionLevel]::Optimal)
        }
    }
    finally { $zip.Dispose() }
}

foreach ($mod in $Mods) {
    $modDir = Join-Path $root "mods/$mod"
    $project = Join-Path $modDir "$mod.csproj"
    $modInfo = Get-Content (Join-Path $modDir "src/ModInfo.cs") -Raw
    $version = [regex]::Match($modInfo, 'Version = "([^"]+)"').Groups[1].Value
    $readme = Get-PackageReadme $mod

    foreach ($runtime in @("Il2Cpp", "Mono")) {
        dotnet build $project -c $runtime -nologo -v q -p:AutomateLocalDeployment=false | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "$mod $runtime build failed" }
        $dll = Join-Path $root "artifacts/bin/$mod/$runtime/${mod}_$runtime.dll"

        $stage = Join-Path $dist "stage/$mod-$runtime"
        Remove-Item -Recurse -Force $stage -ErrorAction SilentlyContinue
        New-Item -ItemType Directory -Force (Join-Path $stage "Mods") | Out-Null
        Copy-Item $dll (Join-Path $stage "Mods")
        Write-Utf8 (Join-Path $stage "README.md") $readme
        Copy-Item (Join-Path $modDir "CHANGELOG.md") $stage
        Copy-Item (Join-Path $root "LICENSE") $stage
        if ($mod -eq "Polyglot") { Copy-Item (Join-Path $modDir "fonts/OFL*.txt") $stage }

        $label = if ($runtime -eq "Il2Cpp") { "IL2CPP" } else { "Mono" }
        $zip = Join-Path $dist "$mod-$version-$label.zip"
        New-Zip $stage $zip
        Write-Host "packed $zip"

        # Thunderstore: one package per runtime. <Name> is the IL2CPP build, <Name>_Mono the Mono build.
        $ts = Join-Path $dist "stage/$mod-$runtime-thunderstore"
        Remove-Item -Recurse -Force $ts -ErrorAction SilentlyContinue
        Copy-Item -Recurse $stage $ts
        Copy-Item (Join-Path $modDir "assets/icon.png") $ts
        $meta = Get-Content (Join-Path $modDir "thunderstore.json") -Raw | ConvertFrom-Json
        $il2cppName = $meta.name
        $monoName = "$($meta.name)_Mono"
        if ($runtime -eq "Il2Cpp") {
            $tsName = $il2cppName
            $description = $meta.description
            $note = "> **IL2CPP build** for the default Steam branch. Playing on the ``alternate`` (Mono) branch? " +
                "Install [$monoName]($tsUrl/$monoName/) instead."
        }
        else {
            $tsName = $monoName
            $description = $meta.description -replace 'IL2CPP build\.', 'Mono build for the alternate branch.'
            $note = "> **Mono build** for the ``alternate`` Steam branch. Playing on the default branch? " +
                "Install [$il2cppName]($tsUrl/$il2cppName/) instead."
        }
        if ($description.Length -gt 250) { throw "$tsName Thunderstore description is longer than 250 chars" }
        $lines = $readme -split "`n", 2
        Write-Utf8 (Join-Path $ts "README.md") ($lines[0] + "`n`n" + $note + "`n" + $lines[1])
        $deps = ($meta.dependencies | ForEach-Object { '"' + $_ + '"' }) -join ", "
        $manifest = "{`n" +
            "    `"name`": `"$tsName`",`n" +
            "    `"version_number`": `"$version`",`n" +
            "    `"website_url`": `"$repoUrl`",`n" +
            "    `"description`": `"$description`",`n" +
            "    `"dependencies`": [$deps]`n}`n"
        Write-Utf8 (Join-Path $ts "manifest.json") $manifest
        $tsZip = Join-Path $dist "thunderstore/$tsName-$version.zip"
        New-Zip $ts $tsZip
        Write-Host "packed $tsZip"
    }
}
Remove-Item -Recurse -Force (Join-Path $dist "stage") -ErrorAction SilentlyContinue
