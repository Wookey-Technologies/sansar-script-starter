<#
.SYNOPSIS
    Compile-checks Sansar C# scripts locally, before you import them into Sansar.

.DESCRIPTION
    Compiles scripts against the Sansar API assemblies in ..\assemblies using the same
    settings as Sansar's own script importer (Roslyn, .NET Framework 4.7.2 target,
    C# 7.3, SERVERSCRIPT_1_1 defined). A script that fails here will fail on import;
    a script that passes here can still be rejected by Sansar's API whitelist
    (see api-docs\access.html), but that is rare in practice.

.PARAMETER Paths
    One or more things to check:
      - a .cs file        -> compiled on its own (like importing a single script)
      - a .json file      -> compiled as a script assembly (all files in its "source" list)
      - a directory       -> every .json assembly in it, plus every loose .cs file.
                             If loose files in one directory only compile together,
                             they are automatically retried as a group.

.PARAMETER All
    Check everything under examples\ and templates\.

.EXAMPLE
    .\tools\check.ps1 MyScript.cs

.EXAMPLE
    .\tools\check.ps1 examples\object-library\ObjectScriptLibrary.json

.EXAMPLE
    .\tools\check.ps1 -All
#>
param(
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [string[]]$Paths,
    [switch]$All
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$assembliesDir = Join-Path $repoRoot 'assemblies'

# --- Sanity: API assemblies present? -----------------------------------------
$sansarRefs = @(
    (Join-Path $assembliesDir 'Sansar.Script.dll'),
    (Join-Path $assembliesDir 'Sansar.Simulation.dll')
)
foreach ($ref in $sansarRefs) {
    if (-not (Test-Path $ref)) {
        Write-Host "ERROR: $ref not found." -ForegroundColor Red
        Write-Host "Copy the assemblies from your Sansar installation:" -ForegroundColor Yellow
        Write-Host '  C:\Program Files\Sansar\Client\ScriptApi\Assemblies\*  ->  assemblies\' -ForegroundColor Yellow
        exit 1
    }
}

# --- Find a C# compiler -------------------------------------------------------
function Find-Csc {
    # 1. vswhere (authoritative when Visual Studio / Build Tools are installed)
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $found = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild `
            -find 'MSBuild\**\Roslyn\csc.exe' 2>$null | Select-Object -First 1
        if ($found -and (Test-Path $found)) { return @{ Path = $found; Modern = $true } }
    }
    # 2. Glob for Roslyn csc in common VS install roots
    $globs = @(
        "$env:ProgramFiles\Microsoft Visual Studio\*\*\MSBuild\Current\Bin\Roslyn\csc.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\*\*\MSBuild\Current\Bin\Roslyn\csc.exe"
    )
    foreach ($g in $globs) {
        $found = Get-ChildItem $g -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) { return @{ Path = $found.FullName; Modern = $true } }
    }
    # 3. Legacy .NET Framework compiler (C# 5 only -- last resort)
    $legacy = "$env:windir\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    if (Test-Path $legacy) { return @{ Path = $legacy; Modern = $false } }
    return $null
}

$csc = Find-Csc
if ($null -eq $csc) {
    Write-Host 'ERROR: No C# compiler found.' -ForegroundColor Red
    Write-Host 'Install Visual Studio 2022+ or "Build Tools for Visual Studio" (free):' -ForegroundColor Yellow
    Write-Host '  https://visualstudio.microsoft.com/downloads/  (see "Build Tools")' -ForegroundColor Yellow
    exit 1
}
if (-not $csc.Modern) {
    Write-Host 'WARNING: Only the legacy .NET 4.0 compiler was found (C# 5 only).' -ForegroundColor Yellow
    Write-Host 'Sansar supports C# 7.3 - modern scripts may fail here that are actually fine.' -ForegroundColor Yellow
    Write-Host 'Install "Build Tools for Visual Studio" for accurate results.' -ForegroundColor Yellow
}

$tempDir = Join-Path $env:TEMP ("sansar-check-" + [System.IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Path $tempDir | Out-Null

# --- Compile one unit (one or more .cs files) ---------------------------------
$script:results = @()
function Invoke-Check {
    param([string]$Label, [string[]]$Files)

    $outDll = Join-Path $tempDir 'check.dll'
    if (Test-Path $outDll) { Remove-Item $outDll -Force }

    $cscArgs = @(
        '/nologo', '/target:library', "/out:$outDll",
        '/define:SERVERSCRIPT_1_1'
    )
    if ($csc.Modern) { $cscArgs += '/langversion:7.3' }
    foreach ($ref in $sansarRefs) { $cscArgs += "/reference:$ref" }
    $cscArgs += $Files

    $output = & $csc.Path $cscArgs
    $ok = ($LASTEXITCODE -eq 0)
    $warnings = @($output | Where-Object { $_ -match 'warning CS' })

    if ($ok) {
        $note = ''
        if ($warnings.Count -gt 0) { $note = " ($($warnings.Count) warning(s))" }
        Write-Host "  OK   $Label$note" -ForegroundColor Green
        foreach ($w in $warnings) { Write-Host "       $w" -ForegroundColor DarkYellow }
    }
    else {
        Write-Host "  FAIL $Label" -ForegroundColor Red
        foreach ($line in $output) { Write-Host "       $line" -ForegroundColor Red }
    }
    $script:results += [pscustomobject]@{ Label = $Label; Ok = $ok }
    return $ok
}

# --- Resolve a .json script assembly to its source files ----------------------
function Get-AssemblySources {
    param([string]$JsonPath)
    $json = Get-Content $JsonPath -Raw | ConvertFrom-Json
    $dir = Split-Path -Parent $JsonPath
    $files = @()
    foreach ($src in $json.source) {
        $resolved = Join-Path $dir $src
        if (Test-Path $resolved) { $files += (Resolve-Path $resolved).Path }
        else { Write-Host "  WARN missing source '$src' listed in $JsonPath" -ForegroundColor Yellow }
    }
    return $files
}

# --- Check a directory tree ----------------------------------------------------
function Invoke-CheckDirectory {
    param([string]$Dir)

    $dirFull = (Resolve-Path $Dir).Path
    $jsonFiles = @(Get-ChildItem $dirFull -Recurse -Filter '*.json' |
        Where-Object { (Get-Content $_.FullName -Raw) -match '"source"' })

    # Files covered by a .json assembly are checked as part of that assembly.
    $covered = @{}
    foreach ($jf in $jsonFiles) {
        foreach ($src in (Get-AssemblySources $jf.FullName)) { $covered[$src] = $true }
    }

    foreach ($jf in $jsonFiles) {
        $files = Get-AssemblySources $jf.FullName
        if ($files.Count -gt 0) {
            Invoke-Check -Label (Resolve-Path $jf.FullName -Relative) -Files $files | Out-Null
        }
    }

    # Group loose .cs files by directory.
    $loose = @(Get-ChildItem $dirFull -Recurse -Filter '*.cs' |
        Where-Object { -not $covered.ContainsKey($_.FullName) })
    $byDir = $loose | Group-Object { $_.DirectoryName }

    foreach ($group in $byDir) {
        $failed = @()
        foreach ($file in $group.Group) {
            $ok = Invoke-Check -Label (Resolve-Path $file.FullName -Relative) -Files @($file.FullName)
            if (-not $ok) { $failed += $file.FullName }
        }
        # If every loose file in a directory fails alone, they likely form an
        # implicit multi-file project; retry them as a single compilation unit.
        if ($group.Group.Count -gt 1 -and $failed.Count -eq $group.Group.Count) {
            Write-Host "  ...  retrying $($group.Name) as one unit" -ForegroundColor Cyan
            # Drop the individual failures from the tally before the group retry.
            $dropLabels = @{}
            foreach ($file in $group.Group) {
                $dropLabels[(Resolve-Path $file.FullName -Relative)] = $true
            }
            $script:results = @($script:results | Where-Object { -not $dropLabels.ContainsKey($_.Label) })
            $allFiles = @($group.Group | ForEach-Object { $_.FullName })
            Invoke-Check -Label ((Resolve-Path $group.Name -Relative) + '\* (compiled together)') -Files $allFiles | Out-Null
        }
    }
}

# --- Main ----------------------------------------------------------------------
Push-Location $repoRoot
try {
    Write-Host "Compiler: $($csc.Path)" -ForegroundColor Cyan

    if ($All) {
        $Paths = @()
        foreach ($d in @('examples', 'templates')) {
            if (Test-Path (Join-Path $repoRoot $d)) { $Paths += (Join-Path $repoRoot $d) }
        }
    }
    if (-not $Paths -or $Paths.Count -eq 0) {
        Write-Host 'Usage: check.ps1 <file.cs | assembly.json | directory>... | check.ps1 -All'
        exit 1
    }

    foreach ($p in $Paths) {
        if (-not (Test-Path $p)) {
            Write-Host "  FAIL $p (not found)" -ForegroundColor Red
            $script:results += [pscustomobject]@{ Label = $p; Ok = $false }
            continue
        }
        $item = Get-Item $p
        if ($item.PSIsContainer) {
            Invoke-CheckDirectory -Dir $item.FullName
        }
        elseif ($item.Extension -eq '.json') {
            $files = Get-AssemblySources $item.FullName
            if ($files.Count -gt 0) { Invoke-Check -Label $p -Files $files | Out-Null }
        }
        else {
            Invoke-Check -Label $p -Files @($item.FullName) | Out-Null
        }
    }

    $failCount = @($script:results | Where-Object { -not $_.Ok }).Count
    $okCount = @($script:results | Where-Object { $_.Ok }).Count
    Write-Host ''
    if ($failCount -eq 0) {
        Write-Host "All checks passed ($okCount unit(s))." -ForegroundColor Green
        exit 0
    }
    else {
        Write-Host "$failCount of $($okCount + $failCount) unit(s) FAILED." -ForegroundColor Red
        exit 1
    }
}
finally {
    Pop-Location
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
