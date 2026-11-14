$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$reports = Join-Path $root 'artifacts/tests'
New-Item -ItemType Directory -Path $reports -Force | Out-Null
$total = 0
foreach ($test in @('self-test','upgrade-self-test')) {
    $report = Join-Path $reports "$test.json"
    $process = Start-Process -FilePath "$root/EvoBridge.exe" -ArgumentList "--$test `"$report`"" -PassThru
    if (-not $process.WaitForExit(30000)) {
        $process.Kill()
        $process.WaitForExit()
        throw "$test timed out."
    }
    if ($process.ExitCode -ne 0) { throw "$test failed: $(Get-Content -LiteralPath $report -Raw)" }
    $result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    if (-not $result.success) { throw "$test did not report success." }
    $total += @($result.checks).Count
    Write-Output "$test passed ($(@($result.checks).Count) checks)."
}
if ($total -ne 34) { throw "Expected 34 checks; got $total." }
Write-Output 'All 34 software checks passed. No hardware session was started.'
