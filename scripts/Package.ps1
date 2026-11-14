$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
# Recheck dependencies and rebuild so a stale or modified binary cannot enter a release.
& "$root/build.ps1"
& "$PSScriptRoot/Test.ps1"
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $dist -Force | Out-Null
$stage = Join-Path $dist ('stage-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path "$stage/backend/source" -Force | Out-Null
foreach ($file in @('EvoBridge.exe','SDL3.dll','README.md','LICENSE','THIRD_PARTY_NOTICES.md','Restore physical input.cmd')) {
    Copy-Item -LiteralPath (Join-Path $root $file) -Destination $stage
}
foreach ($dir in @('assets','licenses','docs')) { Copy-Item -LiteralPath (Join-Path $root $dir) -Destination $stage -Recurse }
foreach ($file in @('viiper.exe','LICENSE.txt')) { Copy-Item -LiteralPath "$root/backend/$file" -Destination "$stage/backend" }
Copy-Item -LiteralPath "$root/backend/source/VIIPER-v0.9.0.tar.gz" -Destination "$stage/backend/source"
$zip = Join-Path $dist 'EvoBridge-v0.4.1-windows-x64.zip'
Compress-Archive -Path "$stage/*" -DestinationPath $zip -Force
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $dist 'SHA256SUMS.txt'), "$hash  EvoBridge-v0.4.1-windows-x64.zip`n")
Write-Output "Packaged $zip"
Write-Output "Staging folder retained for inspection: $stage"
