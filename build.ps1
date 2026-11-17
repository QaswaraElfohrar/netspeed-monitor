# Builds everything: tests -> self-contained single-file exe -> portable zip -> installer (if Inno Setup is installed).
# Usage:  powershell -ExecutionPolicy Bypass -File build.ps1
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$version = '1.0.0'
$dist = Join-Path $PSScriptRoot 'dist'
$publish = Join-Path $dist 'NetSpeedMonitor'

Write-Host '== 1/4 unit tests' -ForegroundColor Cyan
dotnet test tests/NetSpeedMonitor.Tests -c Release
if ($LASTEXITCODE -ne 0) { throw 'tests failed' }

Write-Host '== 2/4 publish (win-x64, self-contained, single file)' -ForegroundColor Cyan
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
dotnet publish src/NetSpeedMonitor.App -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none -p:DebugSymbols=false -o $publish
if ($LASTEXITCODE -ne 0) { throw 'publish failed' }

Write-Host '== 3/4 portable zip' -ForegroundColor Cyan
$portable = Join-Path $dist 'portable'
New-Item -ItemType Directory -Force $portable | Out-Null
Copy-Item (Join-Path $publish 'NetSpeedMonitor.exe') $portable
'Delete this file to store settings in %AppData% instead of the "data" folder.' | Set-Content (Join-Path $portable 'portable.txt')
Compress-Archive -Path (Join-Path $portable '*') -DestinationPath (Join-Path $dist "NetSpeedMonitor-$version-portable-win-x64.zip") -Force
Remove-Item $portable -Recurse -Force

Write-Host '== 4/4 installer' -ForegroundColor Cyan
$iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($iscc) {
    & $iscc "/DAppVersion=$version" "/DSourceExe=$publish\NetSpeedMonitor.exe" "/O$dist" installer\NetSpeedMonitor.iss
    if ($LASTEXITCODE -ne 0) { throw 'installer build failed' }
} else {
    Write-Warning 'Inno Setup 6 not found - installer skipped (install it from https://jrsoftware.org/isinfo.php and run build.ps1 again).'
}

Write-Host "`nDone. Output in $dist" -ForegroundColor Green
Get-ChildItem $dist -File -Recurse | Select-Object FullName, @{n = 'MB'; e = { [math]::Round($_.Length / 1MB, 1) } } | Format-Table -AutoSize
