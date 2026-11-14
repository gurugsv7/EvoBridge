$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$root = Split-Path $PSScriptRoot -Parent
$cache = Join-Path $root '.deps'
New-Item -ItemType Directory -Path $cache,"$root/backend/source" -Force | Out-Null
function Assert-Hash($Path, $Sha) {
    if ((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $Sha) { throw "SHA256 mismatch: $Path" }
}
function Fetch($Url, $Path, $Sha) {
    if (-not (Test-Path -LiteralPath $Path)) { Invoke-WebRequest -UseBasicParsing -Uri $Url -OutFile $Path }
    Assert-Hash $Path $Sha
}
Fetch 'https://github.com/Alia5/VIIPER/releases/download/v0.9.0/viiper-windows-amd64.exe' "$root/backend/viiper.exe" '142c144f6abee8b8a5f6a1abbc7018683444de71d2e45102cde6aaab9b2223e6'
Fetch 'https://github.com/Alia5/VIIPER/archive/refs/tags/v0.9.0.tar.gz' "$root/backend/source/VIIPER-v0.9.0.tar.gz" '5cddd9907fc68e069a17afe6254f18148bb72f8713fba558aff433f4e321b7ac'
Fetch 'https://github.com/libsdl-org/SDL/releases/download/release-3.4.20/SDL3-3.4.20-win32-x64.zip' "$cache/SDL3-3.4.20-win32-x64.zip" '368bed8b24e44ad9ea372990814f2343fcfb9e468abb7f8f4a3bc146c7304752'
Expand-Archive -LiteralPath "$cache/SDL3-3.4.20-win32-x64.zip" -DestinationPath "$cache/SDL3" -Force
$dll = @(Get-ChildItem -LiteralPath "$cache/SDL3" -Filter SDL3.dll -Recurse)
if ($dll.Count -ne 1) { throw 'Expected one SDL3.dll in the verified archive.' }
Assert-Hash $dll[0].FullName '66844f10251cc1c10619ed5d8e4e3224f564900ee6d69ba87d63de782addae40'
Copy-Item -LiteralPath $dll[0].FullName -Destination "$root/SDL3.dll" -Force
Copy-Item -LiteralPath "$root/licenses/VIIPER/LICENSE.txt" -Destination "$root/backend/LICENSE.txt" -Force
Write-Output 'Verified VIIPER 0.9.0, corresponding source, and SDL 3.4.20.'
