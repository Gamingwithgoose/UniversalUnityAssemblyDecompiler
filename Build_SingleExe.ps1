$ErrorActionPreference = "Stop"

Write-Host "============================================================" -ForegroundColor Green
Write-Host "Universal Unity Assembly Dumper - Single EXE Build" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw "dotnet was not found. Install the .NET SDK, then run this again."
}

$project = Join-Path $PSScriptRoot "UniversalUnityAssemblyDumper.csproj"
$publish = Join-Path $PSScriptRoot "bin\Release\net10.0-windows\win-x64\publish"
$dist = Join-Path $PSScriptRoot "dist"

Write-Host "Publishing self-contained win-x64 single EXE..." -ForegroundColor Cyan

dotnet publish $project `
  -c Release `
  -r win-x64 `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:EnableCompressionInSingleFile=true `
  /p:PublishTrimmed=false

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

New-Item -ItemType Directory -Force -Path $dist | Out-Null
Copy-Item -Force (Join-Path $publish "UniversalUnityAssemblyDumper.exe") (Join-Path $dist "UniversalUnityAssemblyDumper.exe")

Write-Host ""
Copy-Item -Force (Join-Path $PSScriptRoot "export_assets.py") $dist
$venv = Join-Path $dist "asset-tools"
$python = Join-Path $venv "Scripts\python.exe"
if (-not (Test-Path $python)) {
    if (Get-Command py -ErrorAction SilentlyContinue) { & py -3 -m venv $venv }
    elseif (Get-Command python -ErrorAction SilentlyContinue) { & python -m venv $venv }
    else { throw "Install Python 3 (64-bit), then rerun build.bat for asset export." }
    if ($LASTEXITCODE -ne 0) { throw "Python environment creation failed." }
}
& $python -m pip install "UnityPy>=1.20,<2"
if ($LASTEXITCODE -ne 0) { throw "UnityPy installation failed." }
Write-Host "Done." -ForegroundColor Green
Write-Host "Single EXE:" -ForegroundColor Green
Write-Host (Join-Path $dist "UniversalUnityAssemblyDumper.exe")
Write-Host ""
