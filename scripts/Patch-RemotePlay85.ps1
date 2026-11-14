# Creates a separate patched copy; does not install, launch or overwrite Remote Play.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$SourcePath,
    [Parameter(Mandatory=$true)][string]$OutputPath
)
$ErrorActionPreference = 'Stop'
$sourcePath = (Resolve-Path -LiteralPath $SourcePath).Path
$targetPath = [IO.Path]::GetFullPath($OutputPath)
if ($sourcePath -eq $targetPath) { throw 'OutputPath must differ from SourcePath.' }
if (Test-Path -LiteralPath $targetPath) { throw 'OutputPath already exists; choose a new file.' }
$expectedOriginal = 'ACD3C15A35206B287BA1B1FD372994B2488A6FF3C675339A695EA1F0EEA46BC5'
if ((Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash -ne $expectedOriginal) {
    throw 'Unsupported input. Expected the original tested 8.5.0.08070 RemotePlay.exe.'
}
$oldVersion = '8.5.0.08070'
$newVersion = '9.5.0.08040'
$sourceSignature = Get-AuthenticodeSignature -LiteralPath $sourcePath
if ($sourceSignature.Status -ne 'Valid' -or $sourceSignature.SignerCertificate.Subject -notlike '*Sony Interactive Entertainment*') { throw 'Original Sony executable signature is not valid.' }
$bytes = [IO.File]::ReadAllBytes($sourcePath)
$original = $bytes.Clone()
$peOffset = [BitConverter]::ToInt32($bytes, 0x3c)
if ([BitConverter]::ToUInt32($bytes, $peOffset) -ne 0x4550) { throw 'Invalid PE header.' }
$sectionCount = [BitConverter]::ToUInt16($bytes, $peOffset + 6)
$optionalHeaderSize = [BitConverter]::ToUInt16($bytes, $peOffset + 20)
$sectionTable = $peOffset + 24 + $optionalHeaderSize
$resourceStart = -1
$resourceSize = 0
for ($section = 0; $section -lt $sectionCount; $section++) {
    $entry = $sectionTable + 40 * $section
    $name = [Text.Encoding]::ASCII.GetString($bytes, $entry, 8).Trim([char]0)
    if ($name -eq '.rsrc') {
        $resourceSize = [BitConverter]::ToUInt32($bytes, $entry + 16)
        $resourceStart = [BitConverter]::ToUInt32($bytes, $entry + 20)
    }
}
if ($resourceStart -lt 0 -or $resourceStart + $resourceSize -gt $bytes.Length) { throw 'Invalid resource section.' }
$fixedRecords = @()
for ($index = $resourceStart; $index -le $resourceStart + $resourceSize - 52; $index++) {
    if ([BitConverter]::ToUInt32($bytes, $index) -eq 4277077181) { $fixedRecords += $index }
}
if ($fixedRecords.Count -ne 1) { throw 'Expected exactly one fixed version record.' }
$fixedOffset = $fixedRecords[0]
$oldMs = [uint32]((8 -shl 16) -bor 5)
$newMs = [uint32]((9 -shl 16) -bor 5)
foreach ($relativeOffset in @(8, 16)) {
    if ([BitConverter]::ToUInt32($bytes, $fixedOffset + $relativeOffset) -ne $oldMs -or [BitConverter]::ToUInt32($bytes, $fixedOffset + $relativeOffset + 4) -ne 8070) { throw 'Unexpected existing version values.' }
    [BitConverter]::GetBytes($newMs).CopyTo($bytes, $fixedOffset + $relativeOffset)
    [BitConverter]::GetBytes([uint32]8040).CopyTo($bytes, $fixedOffset + $relativeOffset + 4)
}
$oldText = [Text.Encoding]::Unicode.GetBytes($oldVersion)
$newText = [Text.Encoding]::Unicode.GetBytes($newVersion)
if ($oldText.Length -ne $newText.Length) { throw 'Version strings must have equal lengths.' }
$stringReplacements = 0
for ($index = $resourceStart; $index -le $resourceStart + $resourceSize - $oldText.Length; $index++) {
    $matches = $true
    for ($character = 0; $character -lt $oldText.Length; $character++) {
        if ($bytes[$index + $character] -ne $oldText[$character]) { $matches = $false; break }
    }
    if ($matches) { $newText.CopyTo($bytes, $index); $stringReplacements++ }
}
if ($stringReplacements -ne 2) { throw "Expected two version strings; found $stringReplacements." }
$changedBytes = 0
for ($index = 0; $index -lt $bytes.Length; $index++) {
    if ($bytes[$index] -ne $original[$index]) {
        if ($index -lt $resourceStart -or $index -ge $resourceStart + $resourceSize) { throw 'A change occurred outside the resource section.' }
        $changedBytes++
    }
}
[IO.File]::WriteAllBytes($targetPath, $bytes)
$versionInfo = [Diagnostics.FileVersionInfo]::GetVersionInfo($targetPath)
if ($versionInfo.FileVersion -ne $newVersion -or $versionInfo.ProductVersion -ne $newVersion) { throw 'Patched version validation failed.' }
[pscustomobject]@{FileVersion=$versionInfo.FileVersion;ProductVersion=$versionInfo.ProductVersion;ChangedBytes=$changedBytes;VersionStringsUpdated=$stringReplacements;ChangesRestrictedToResourceSection=$true;SHA256=(Get-FileHash -LiteralPath $targetPath -Algorithm SHA256).Hash} | ConvertTo-Json
