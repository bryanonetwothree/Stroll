param([switch]$Test, [string]$OutputDirectory = 'dist')
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (!(Test-Path $compiler)) { throw 'Install .NET Framework 4.8, then try again.' }
$framework = Split-Path $compiler
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$sources = Get-ChildItem 'src\Stroll\*.cs' | ForEach-Object { $_.FullName }
# The Windows inbox compiler uses C# 5. Use the build toolchain documented below
# when its compiler is too old; Roslyn ships with Visual Studio Build Tools.
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vswhere) {
    $roslyn = & $vswhere -latest -products '*' -find 'MSBuild\**\Bin\Roslyn\csc.exe' | Select-Object -First 1
    if ($roslyn) { $compiler = $roslyn }
}
$common = @('/nologo', '/optimize+', '/platform:anycpu', '/nostdlib+')
foreach ($assembly in @('mscorlib', 'System', 'System.Core', 'System.Drawing', 'System.Windows.Forms', 'System.Runtime.Serialization', 'System.Xml')) {
    $common += '/r:' + (Join-Path $framework ($assembly + '.dll'))
}
& $compiler @common '/target:winexe' ('/out:' + (Join-Path $OutputDirectory 'Stroll.exe')) '/win32icon:assets\app.ico' '/win32manifest:src\Stroll\app.manifest' '/resource:assets\walk.png,Stroll.walk.png' '/resource:assets\jump.png,Stroll.jump.png' '/resource:assets/pets/john/walk.png,Stroll.john.walk.png' '/resource:assets/pets/john/John-jumping.png,Stroll.john.jump.png' '/resource:assets/pets/smooth-saint-bernard/walking.gif,Stroll.smooth.walk.gif' '/resource:assets/pets/smooth-saint-bernard/jumping.png,Stroll.smooth.jump.png' '/resource:assets/pets/j-yosef/walking.gif,Stroll.jyosef.walk.gif' '/resource:assets/pets/j-yosef/jumping.png,Stroll.jyosef.jump.png' @sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed. Install Visual Studio Build Tools with the .NET desktop build tools workload (C# 7.3+ compiler), then run again.' }
Copy-Item 'src\Stroll\Stroll.exe.config' (Join-Path $OutputDirectory 'Stroll.exe.config') -Force
Copy-Item 'Add-Stroll-to-Start.cmd' (Join-Path $OutputDirectory 'Add-Stroll-to-Start.cmd') -Force
if ($Test) {
    & $compiler @common '/target:exe' ('/out:' + (Join-Path $OutputDirectory 'Stroll.Tests.exe')) 'src\Stroll\Core.cs' 'src\Stroll\Settings.cs' 'tests\CoreTests.cs'
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
    & (Join-Path (Resolve-Path $OutputDirectory) 'Stroll.Tests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
Write-Host "Built $OutputDirectory\Stroll.exe"


