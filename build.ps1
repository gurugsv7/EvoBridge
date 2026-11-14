param([switch]$SkipDependencies)
$ErrorActionPreference = 'Stop'
if (-not $SkipDependencies) { & "$PSScriptRoot/scripts/Get-Dependencies.ps1" }
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework x64 C# compiler is missing.' }
$sources = @(Get-ChildItem -LiteralPath "$PSScriptRoot/src" -Filter '*.cs' | ForEach-Object FullName)
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /debug- /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll ("/out:" + (Join-Path $PSScriptRoot 'EvoBridge.exe')) @sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Write-Output 'Built EvoBridge.exe (Windows x64).'
