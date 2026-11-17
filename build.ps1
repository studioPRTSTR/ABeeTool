param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$Feed = "",

    [string]$Project = "",

    # Name of the toolbar in Rhino.
    [string]$ToolbarName = "=^..^="
)

$ErrorActionPreference = "Stop"

if (-not $Project) {
    $found = @(Get-ChildItem $PSScriptRoot -Filter *.rhproj)
    if ($found.Count -ne 1) {
        throw "Expected exactly one .rhproj in '$PSScriptRoot', found $($found.Count). Pass -Project explicitly."
    }
    $Project = $found[0].FullName
}

$rhinoCode = "C:\Program Files\Rhino 8\System\RhinoCode.exe"

if ($Version -notmatch '^\d+\.\d+(\.\d+)?(-[0-9A-Za-z.]+)?$') {
    throw "Version '$Version' must look like 1.0, 1.0.3 or 1.1.0-beta."
}
if (-not (Test-Path $Project)) {
    throw "Project '$Project' not found. Create it once in Rhino's ScriptEditor (File > Create Project) and save it in this folder."
}

$versionPattern = "-" + [regex]::Escape($Version) + "(\+\d+)?-"
if ($Feed -and (Get-ChildItem $Feed -Filter *.yak -ErrorAction SilentlyContinue | Where-Object { $_.Name -match $versionPattern })) {
    throw "Version $Version already exists in '$Feed'. Bump the version number."
}

$buildPath = Join-Path $PSScriptRoot "build\$Version"
if (Test-Path $buildPath) {
    Remove-Item $buildPath -Recurse -Force
}

Write-Host "Building ABeeTool $Version ..."
& $rhinoCode project build $Project --buildversion $Version --buildtarget "8.*" --buildpath $buildPath
if ($LASTEXITCODE -ne 0) {
    throw "rhinocode build failed (exit code $LASTEXITCODE). Is Rhino 8 running?"
}

$yak = Get-ChildItem $buildPath -Recurse -Filter *.yak | Select-Object -First 1
if (-not $yak) {
    throw "Build finished but no .yak package was found in '$buildPath'."
}

# Fix up what rhinocode generates, inside the .yak (a zip).
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

function Edit-ZipEntry($zip, [string]$name, [scriptblock]$change) {
    $entry = $zip.Entries | Where-Object { $_.FullName -like $name } | Select-Object -First 1
    if (-not $entry) {
        throw "No '$name' in '$($yak.FullName)'."
    }
    $fullName = $entry.FullName
    $reader = New-Object System.IO.StreamReader($entry.Open())
    $text = $reader.ReadToEnd()
    $reader.Close()
    $text = & $change $text
    $entry.Delete()
    $writer = New-Object System.IO.StreamWriter($zip.CreateEntry($fullName).Open(), (New-Object System.Text.UTF8Encoding($false)))
    $writer.Write($text)
    $writer.Close()
}

$zip = [System.IO.Compression.ZipFile]::Open($yak.FullName, [System.IO.Compression.ZipArchiveMode]::Update)
try {
    # rhinocode adds a build number ("1.1.1+25311"). Package Manager would show it, so set the plain version.
    Edit-ZipEntry $zip "manifest.yml" { param($t) [regex]::Replace($t, '(?m)^version:\s*\S+', "version: $Version") }

    # Name of the toolbar (and its group / tab) instead of the plugin name.
    $toolbarXml = [System.Security.SecurityElement]::Escape($ToolbarName)
    Edit-ZipEntry $zip "*.rui" { param($t) $t.Replace("<locale_1033>ABeeTool</locale_1033>", "<locale_1033>$toolbarXml</locale_1033>") }
}
finally {
    $zip.Dispose()
}
$cleanName = [regex]::Replace($yak.Name, '\+\d+', '')
if ($cleanName -ne $yak.Name) {
    $cleanPath = Join-Path $yak.DirectoryName $cleanName
    Move-Item $yak.FullName $cleanPath -Force
    $yak = Get-Item $cleanPath
}
Write-Host "Package: $($yak.FullName)"

if ($Feed) {
    New-Item -ItemType Directory -Force -Path $Feed | Out-Null
    Copy-Item $yak.FullName $Feed
    Write-Host "Published to feed: $Feed"
}
