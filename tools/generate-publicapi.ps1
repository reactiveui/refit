#requires -Version 5.1
<#
.SYNOPSIS
    (Re)generate PublicAPI baseline files for every shipped Refit library, across each
    target framework that builds on this machine.

.DESCRIPTION
    The Microsoft.CodeAnalysis.PublicApiAnalyzers (RS0016 / RS0017 / RS0037) require a
    per-TFM pair of tracking files:

        <Project>/PublicAPI/<tfm>/PublicAPI.Shipped.txt
        <Project>/PublicAPI/<tfm>/PublicAPI.Unshipped.txt

    This script seeds those files and uses `dotnet format analyzers` to capture the
    project's current public surface (RS0016), drop stale entries (RS0017), and record
    nullability (RS0037), then folds the surface into Shipped (this repo keeps the full
    surface in Shipped with Unshipped empty).

    Only projects with MSBuild property TrackPublicApi=true are processed; the tests/,
    benchmarks/, and examples/ trees, the source generators, and the AOT smoke app opt
    out centrally in src/Directory.Build.props.

    Each (project, TFM) pair is independent — `dotnet format` builds an in-memory
    MSBuildWorkspace and only writes its own PublicAPI/<tfm>/ files — so the pairs run
    in parallel (PowerShell 7+ runspaces; falls back to sequential on 5.1). Override the
    width with -Jobs <n> or $env:JOBS.

    Refit ships only cross-platform libraries (no Apple/Android/Windows-desktop TFMs), so
    every target framework — including the .NET Framework legs via EnableWindowsTargeting —
    builds on Windows, Linux, and macOS alike. The bash sibling (generate-publicapi.sh)
    produces byte-identical baselines.

.PARAMETER Filter
    Optional substring; only projects whose path contains it are processed.

.PARAMETER Jobs
    Maximum number of (project, TFM) pairs to generate concurrently.

.EXAMPLE
    ./tools/generate-publicapi.ps1
    Generates baselines for all tracked libraries across all buildable TFMs.

.EXAMPLE
    ./tools/generate-publicapi.ps1 -Filter HttpClient -Jobs 4
    Only projects whose path contains 'HttpClient', 4 at a time.
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Filter = '',
    [int]$Jobs = 0
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcDir = (Resolve-Path (Join-Path $scriptDir '..' 'src')).Path
Set-Location $srcDir

# MSBuild properties that `dotnet format` cannot accept via -p:; pass through the env
# (also inherited by the parallel runspaces, which share this process).
$env:EnableWindowsTargeting = 'true'
$env:CheckEolTargetFramework = 'false'
if (-not $env:MinVerVersionOverride) { $env:MinVerVersionOverride = '255.255.255-dev' }

if ($Jobs -le 0) {
    $Jobs = if ($env:JOBS) { [int]$env:JOBS } else { [Math]::Min([Environment]::ProcessorCount, 8) }
}

$diags = @('RS0016', 'RS0017', 'RS0037')

Write-Host 'PublicAPI baseline generation'
Write-Host "  src        : $srcDir"
Write-Host "  filter     : $(if ($Filter) { $Filter } else { '<none>' })"
Write-Host "  diagnostics: $($diags -join ' ')"
Write-Host "  MinVer     : $($env:MinVerVersionOverride)"
Write-Host "  jobs       : $Jobs"
Write-Host ''

function Get-MsBuildProperty {
    param([string]$Project, [string]$Name)
    $value = & dotnet msbuild $Project "-getProperty:$Name" -nologo 2>$null
    if ($LASTEXITCODE -ne 0 -or $null -eq $value) { return '' }
    return ($value | Out-String).Trim()
}

# Regenerate one (project, TFM) pair and fold the surface into Shipped. Returns a result
# object; also defined inside the parallel block below via its source text.
function Invoke-PublicApiOne {
    param($Item, [string[]]$Diags)
    $proj = $Item.Proj
    $tfm = $Item.Tfm
    $lf = "`n"
    $header = '#nullable enable'
    # Write LF-only so the baselines match the bash sibling's output byte-for-byte.
    $writeLf = { param($p, $lines) [IO.File]::WriteAllText($p, (($lines -join $lf) + $lf)) }
    # Back up any existing baseline so a build failure restores it instead of wiping it.
    $shippedBak = if (Test-Path $Item.Shipped) { (Get-Content -Raw $Item.Shipped) -replace "`r`n", "`n" } else { $null }
    $unshippedBak = if (Test-Path $Item.Unshipped) { (Get-Content -Raw $Item.Unshipped) -replace "`r`n", "`n" } else { $null }
    # Empty both to the bare header so the analyzer reports the entire current surface.
    & $writeLf $Item.Shipped @($header)
    & $writeLf $Item.Unshipped @($header)
    & dotnet format analyzers $proj -f $tfm --diagnostics $Diags --severity info -v quiet
    if ($LASTEXITCODE -eq 0) {
        # `dotnet format` records the surface in Unshipped; fold it into Shipped (ordinally
        # sorted+deduped, matching `LC_ALL=C sort -u`) and reset Unshipped to the bare header.
        $surface = [string[]]@(Get-Content $Item.Unshipped | Where-Object { $_ -ne $header -and $_.Trim() -ne '' } | Select-Object -Unique)
        [Array]::Sort($surface, [System.StringComparer]::Ordinal)
        & $writeLf $Item.Shipped (@($header) + $surface)
        & $writeLf $Item.Unshipped @($header)
        Write-Host "OK   [$tfm] $proj"
        return [pscustomobject]@{ Ok = $true }
    }
    # Restore the prior baseline (if any) so nothing is wiped for a TFM we can't build here.
    if ($null -ne $shippedBak) { [IO.File]::WriteAllText($Item.Shipped, $shippedBak) }
    if ($null -ne $unshippedBak) { [IO.File]::WriteAllText($Item.Unshipped, $unshippedBak) }
    Write-Host "FAIL [$tfm] $proj"
    return [pscustomobject]@{ Ok = $false }
}

$projects = Get-ChildItem -Path . -Recurse -Filter '*.csproj' |
    Where-Object {
        $p = $_.FullName -replace '\\', '/'
        $p -notmatch '/tests/' -and $p -notmatch '/benchmarks/' -and $p -notmatch '/examples/'
    } |
    Sort-Object FullName

# Collect (project, TFM) work items; the worker seeds, generates, and folds each pair.
$items = [System.Collections.Generic.List[object]]::new()
$restoreSet = [System.Collections.Generic.List[string]]::new()
$skipped = 0

foreach ($projItem in $projects) {
    $proj = $projItem.FullName
    # Match the filter against a slash-normalized path so a forward-slash filter works on Windows too.
    if ($Filter -and (($proj -replace '\\', '/') -notlike "*$($Filter -replace '\\', '/')*")) { continue }

    $track = Get-MsBuildProperty -Project $proj -Name 'TrackPublicApi'
    if ($track -ne 'true') {
        Write-Host "skip  (TrackPublicApi != true): $proj"
        $skipped++
        continue
    }

    $tfms = Get-MsBuildProperty -Project $proj -Name 'TargetFrameworks'
    if (-not $tfms) { $tfms = Get-MsBuildProperty -Project $proj -Name 'TargetFramework' }
    if (-not $tfms) {
        Write-Host "skip  (no TargetFramework(s)): $proj"
        $skipped++
        continue
    }

    $projDir = Split-Path -Parent $proj
    Write-Host "queue $proj"
    Write-Host "    TFMs: $tfms"
    $restoreSet.Add($proj)

    foreach ($tfm in ($tfms -split ';')) {
        $tfm = $tfm.Trim()
        if (-not $tfm) { continue }

        $apiDir = Join-Path $projDir (Join-Path 'PublicAPI' $tfm)
        New-Item -ItemType Directory -Force -Path $apiDir | Out-Null

        $shipped = Join-Path $apiDir 'PublicAPI.Shipped.txt'
        $unshipped = Join-Path $apiDir 'PublicAPI.Unshipped.txt'
        $items.Add([pscustomobject]@{ Proj = $proj; Tfm = $tfm; Shipped = $shipped; Unshipped = $unshipped })
    }
}
Write-Host ''

if ($items.Count -eq 0) {
    Write-Host "Nothing to generate. projects skipped: $skipped"
    return
}

# Restore once per project so the parallel workers never race on restore (they each load
# a read-only workspace afterwards).
Write-Host "Restoring $($restoreSet.Count) project(s)..."
foreach ($proj in $restoreSet) {
    & dotnet restore $proj -v quiet
    if ($LASTEXITCODE -ne 0) { Write-Host "    WARN: restore reported issues for $proj" }
}
Write-Host ''

Write-Host "Generating $($items.Count) (project, TFM) baseline(s) across $Jobs job(s)..."
if ($PSVersionTable.PSVersion.Major -ge 7 -and $Jobs -gt 1) {
    $funcDef = ${function:Invoke-PublicApiOne}.ToString()
    $results = $items | ForEach-Object -ThrottleLimit $Jobs -Parallel {
        ${function:Invoke-PublicApiOne} = $using:funcDef
        Invoke-PublicApiOne -Item $_ -Diags $using:diags
    }
}
else {
    if ($Jobs -gt 1) { Write-Host '  (PowerShell 5.1: running sequentially — use pwsh 7+ for parallelism)' }
    $results = foreach ($it in $items) { Invoke-PublicApiOne -Item $it -Diags $diags }
}
Write-Host ''

$generated = @($results | Where-Object { $_.Ok }).Count
$failed = @($results | Where-Object { -not $_.Ok }).Count

Write-Host "Done. generated: $generated TFM baseline(s), failed: $failed, projects skipped: $skipped"
if ($failed -ne 0) { exit 1 }
