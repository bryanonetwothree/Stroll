param([switch]$SkipBuild, [string]$BinaryDirectory = 'dist')
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
if (!$SkipBuild) { & ./build.ps1 -Test -OutputDirectory $BinaryDirectory }
$version = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $BinaryDirectory 'Stroll.exe')).FileVersion -replace '\.0$',''
$outputFolder = Join-Path $PSScriptRoot 'release'
New-Item -ItemType Directory -Path $outputFolder -Force | Out-Null
$stageFolder = Join-Path $outputFolder ('Stroll-v' + $version + '-Windows-portable')
New-Item -ItemType Directory -Path $stageFolder -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $stageFolder 'docs') -Force | Out-Null
$runtimeFiles = @((Join-Path $BinaryDirectory 'Stroll.exe'),(Join-Path $BinaryDirectory 'Stroll.exe.config'),'Add-Stroll-to-Start.cmd','README.md','LICENSE','ASSETS.md','CHANGELOG.md')
foreach ($file in $runtimeFiles) { Copy-Item -LiteralPath $file -Destination $stageFolder -Force }
foreach ($file in @('QUICKSTART.md','TESTING.md','PERFORMANCE.md')) { Copy-Item -LiteralPath (Join-Path 'docs' $file) -Destination (Join-Path $stageFolder 'docs') -Force }
$zipPath = Join-Path $outputFolder ('Stroll-v' + $version + '-Windows-portable.zip')
$packageFiles = @($runtimeFiles | ForEach-Object { Join-Path $stageFolder (Split-Path $_ -Leaf) }) + @(Join-Path $stageFolder 'docs')
Compress-Archive -LiteralPath $packageFiles -DestinationPath $zipPath -Force
$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
($hash + '  ' + (Split-Path $zipPath -Leaf)) | Set-Content -LiteralPath ($zipPath + '.sha256') -Encoding ascii
Write-Host "Packaged $zipPath"

