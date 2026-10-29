param(
    [string]$Source = (Join-Path $PSScriptRoot "..\assets\ui\lucide"),
    [string]$Destination = (Join-Path $PSScriptRoot "..\assets\ui\lucide\png-dark"),
    [string]$Color = "#f7f9f8"
)

$ErrorActionPreference = "Stop"
$edge = "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
if (-not (Test-Path -LiteralPath $edge)) {
    $edge = "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe"
}
if (-not (Test-Path -LiteralPath $edge)) {
    throw "Microsoft Edge was not found."
}

$sourcePath = (Resolve-Path -LiteralPath $Source).Path
New-Item -ItemType Directory -Path $Destination -Force | Out-Null
$destinationPath = (Resolve-Path -LiteralPath $Destination).Path
$tempRoot = [IO.Path]::GetTempPath()
$tempProfile = Join-Path $tempRoot ("creamykeys-edge-profile-" + [Guid]::NewGuid().ToString("N"))
$edgeError = Join-Path ([IO.Path]::GetTempPath()) "creamykeys-edge-render.log"

foreach ($icon in Get-ChildItem -LiteralPath $sourcePath -Filter "*.svg" -File) {
    $svg = [IO.File]::ReadAllText($icon.FullName)
    $svg = $svg -replace 'width="24"', 'width="20"'
    $svg = $svg -replace 'height="24"', 'height="20"'
    $svg = $svg -replace 'stroke="currentColor"', ('stroke="' + $Color + '"')
    $svg = $svg -replace 'fill="currentColor"', ('fill="' + $Color + '"')
    $html = @"
<!doctype html><html><head><meta charset="utf-8"></head>
<body style="margin:0;width:32px;height:32px;display:flex;align-items:center;justify-content:center;color:$Color;background:transparent;overflow:hidden">$svg</body></html>
"@
    $tempHtml = Join-Path $tempRoot `
        ("creamykeys-" + $icon.BaseName + "-" + [Guid]::NewGuid().ToString("N") + ".html")
    [IO.File]::WriteAllText($tempHtml, $html, [Text.UTF8Encoding]::new($false))
    $output = Join-Path $destinationPath ($icon.BaseName + ".png")
    $tempOutput = Join-Path ([IO.Path]::GetTempPath()) `
        ("creamykeys-" + $icon.BaseName + "-" + [Guid]::NewGuid().ToString("N") + ".png")
    $uri = [Uri]::new($tempHtml).AbsoluteUri
    $arguments = @(
        "--headless",
        "--disable-gpu",
        "--hide-scrollbars",
        "--user-data-dir=$tempProfile",
        "--default-background-color=00000000",
        "--window-size=32,32",
        "--screenshot=$tempOutput",
        $uri
    )
    $process = Start-Process -FilePath $edge -ArgumentList $arguments -PassThru -Wait `
        -WindowStyle Hidden -RedirectStandardError $edgeError
    if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $tempOutput)) {
        throw "Failed to render $($icon.Name)"
    }
    Move-Item -LiteralPath $tempOutput -Destination $output -Force
    Remove-Item -LiteralPath $tempHtml -Force -ErrorAction SilentlyContinue
}

Remove-Item -LiteralPath $edgeError -Force -ErrorAction SilentlyContinue
Write-Host "Rendered UI icons to $destinationPath"
