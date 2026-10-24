param([switch]$Performance, [string]$OutputDirectory = 'dist')
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
./build.ps1 -Test -OutputDirectory $OutputDirectory
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$compiler = & $vswhere -latest -products '*' -find 'MSBuild\**\Bin\Roslyn\csc.exe' | Select-Object -First 1
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$refs = @(('/r:' + (Join-Path $OutputDirectory 'Stroll.exe')))
foreach ($assembly in @('System', 'System.Core', 'System.Drawing', 'System.Windows.Forms', 'System.Runtime.Serialization')) { $refs += '/r:' + (Join-Path $framework ($assembly + '.dll')) }
foreach ($test in @('AppSmokeTests', 'UiSmokeTests', 'V2DesktopTests')) {
    & $compiler /nologo /win32manifest:src\Stroll\app.manifest @refs "/out:$OutputDirectory\$test.exe" "tests\$test.cs"
    if ($LASTEXITCODE -ne 0) { throw "$test compilation failed" }
    $testArgs = @()
    if ($test -eq 'UiSmokeTests') { $testArgs = @((Join-Path $OutputDirectory 'smoke')) }
    if ($test -eq 'V2DesktopTests' -and $Performance) { $testArgs = @('--performance') }
    & (Join-Path (Resolve-Path $OutputDirectory) "$test.exe") @testArgs
    if ($LASTEXITCODE -ne 0) { throw "$test failed" }
}

