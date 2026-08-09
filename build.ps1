param(
    [string]$CreamyKeysJar = "C:\Users\DELL\Downloads\CreamyKeys-1.21.X.jar",
    [switch]$SkipAssets
)

$ErrorActionPreference = "Stop"

$root = (Resolve-Path -LiteralPath $PSScriptRoot).Path
$assets = Join-Path $root "assets"
$dist = Join-Path $root "dist"
$obj = Join-Path ([System.IO.Path]::GetTempPath()) "CreamyKeysDesktop-build"
$src = Join-Path $root "src\CreamyKeysDesktop.cs"
$manifest = Join-Path $root "app.manifest"
$pngIcon = Join-Path $root "app-icon.png"
$icon = Join-Path $root "app.ico"
$exe = Join-Path $dist "CreamyKeys.exe"
$oldExe = Join-Path $dist "CreamyKeysDesktop.exe"
$intermediateExe = Join-Path $obj "CreamyKeys.exe"
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path -LiteralPath $csc)) {
    throw "C# compiler not found: $csc"
}

if (-not $SkipAssets) {
    python (Join-Path $root "scripts\prepare_assets.py") --jar $CreamyKeysJar --out $assets
    if ($LASTEXITCODE -ne 0) {
        throw "Asset preparation failed with exit code $LASTEXITCODE"
    }
}

if (Test-Path -LiteralPath $CreamyKeysJar) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::OpenRead($CreamyKeysJar)
    try {
        $entry = $zip.GetEntry("assets/creamykeys/icon.png")
        if ($entry -ne $null) {
            $inputStream = $entry.Open()
            $outputStream = [IO.File]::Open($pngIcon, [IO.FileMode]::Create, [IO.FileAccess]::Write)
            try {
                $inputStream.CopyTo($outputStream)
            } finally {
                $outputStream.Dispose()
                $inputStream.Dispose()
            }
        }
    } finally {
        $zip.Dispose()
    }
}

if (-not (Test-Path -LiteralPath $pngIcon)) {
    throw "App icon source not found: $pngIcon"
}

$iconNeedsUpdate = -not (Test-Path -LiteralPath $icon)
if (-not $iconNeedsUpdate) {
    $iconNeedsUpdate = (Get-Item -LiteralPath $pngIcon).LastWriteTime -gt (Get-Item -LiteralPath $icon).LastWriteTime
}

if ($iconNeedsUpdate) {
    Add-Type -AssemblyName System.Drawing
    Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class CreamyKeysIconNative {
    [DllImport("user32.dll", SetLastError=true)]
    public static extern bool DestroyIcon(IntPtr hIcon);
}
"@
    $bitmap = [Drawing.Bitmap]::FromFile($pngIcon)
    $handle = $bitmap.GetHicon()
    $ico = [Drawing.Icon]::FromHandle($handle)
    $stream = [IO.File]::Open($icon, [IO.FileMode]::Create, [IO.FileAccess]::Write)
    try {
        $ico.Save($stream)
    } finally {
        $stream.Dispose()
        $ico.Dispose()
        [CreamyKeysIconNative]::DestroyIcon($handle) | Out-Null
        $bitmap.Dispose()
    }
}

New-Item -ItemType Directory -Force -Path $dist | Out-Null
New-Item -ItemType Directory -Force -Path $obj | Out-Null
if (Test-Path -LiteralPath $oldExe) {
    Remove-Item -LiteralPath $oldExe -Force -ErrorAction SilentlyContinue
}

& $csc `
    /nologo `
    /target:winexe `
    /platform:x64 `
    /optimize+ `
    /win32manifest:$manifest `
    /win32icon:$icon `
    /out:$intermediateExe `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Management.dll `
    /reference:System.Web.Extensions.dll `
    /reference:System.Windows.Forms.dll `
    $src

if ($LASTEXITCODE -ne 0) {
    throw "Compiler failed with exit code $LASTEXITCODE"
}

Copy-Item -LiteralPath $intermediateExe -Destination $exe -Force
Copy-Item -LiteralPath $icon -Destination (Join-Path $dist "app.ico") -Force
Copy-Item -LiteralPath $pngIcon -Destination (Join-Path $dist "app-icon.png") -Force

if (-not $SkipAssets) {
    $distAssets = Join-Path $dist "assets"
    if (Test-Path -LiteralPath $distAssets) {
        Write-Host "Updating existing dist assets in place."
    }
    Copy-Item -LiteralPath $assets -Destination $dist -Recurse -Force
} else {
    Write-Host "Skipped asset copy."
}

Write-Host "Built $exe"
Write-Host "Assets available at $(Join-Path $dist 'assets')"
