$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$project = Join-Path $PSScriptRoot "UniversalUnityAssemblyDumper.csproj"
$publish = Join-Path $PSScriptRoot "bin\Release\net10.0-windows\win-x64\publish"
$dist = Join-Path $PSScriptRoot "dist"
$obj = Join-Path $PSScriptRoot "obj"
$stage = Join-Path $obj "asset-runtime-stage"
$archive = Join-Path $obj "asset-runtime.zip"
$download = Join-Path $obj "python-3.12.10-embed-amd64.zip"
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "Install the .NET 10 SDK first." }
New-Item -ItemType Directory -Force -Path $obj | Out-Null
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
New-Item -ItemType Directory -Force -Path $stage | Out-Null
Write-Host "Preparing portable Python 3.12 x64 and UnityPy..." -ForegroundColor Cyan
if (-not (Test-Path $download)) {
    Invoke-WebRequest -UseBasicParsing -Uri "https://www.python.org/ftp/python/3.12.10/python-3.12.10-embed-amd64.zip" -OutFile "$download.partial"
    Move-Item -Force "$download.partial" $download
}
Expand-Archive -Path $download -DestinationPath $stage -Force
$pth = Join-Path $stage "python312._pth"
(Get-Content $pth) -replace '^\s*#\s*import site\s*$', 'import site' | Set-Content -Encoding ASCII $pth
Add-Content -Encoding ASCII $pth "Lib\site-packages"
$bootstrap = Join-Path $obj "get-pip.py"
Invoke-WebRequest -UseBasicParsing -Uri "https://bootstrap.pypa.io/get-pip.py" -OutFile $bootstrap
$python = Join-Path $stage "python.exe"
& $python $bootstrap --no-warn-script-location
if ($LASTEXITCODE -ne 0) { throw "Portable Python pip installation failed." }
Write-Host "Installing portable Python build dependencies..." -ForegroundColor Cyan
& $python -m pip install --disable-pip-version-check --no-warn-script-location "setuptools>=70" wheel
if ($LASTEXITCODE -ne 0) { throw "Python build dependencies installation failed." }
& $python -c "import setuptools.build_meta; import wheel; print('Python build backend import test passed')"
if ($LASTEXITCODE -ne 0) { throw "Python build backend import test failed." }
# Embedded Python uses an isolated _pth file, so pip's temporary build environment
# cannot reliably inject its setuptools path. Use the verified local backend.
& $python -m pip install --disable-pip-version-check --no-warn-script-location --no-build-isolation "UnityPy>=1.20,<2"
if ($LASTEXITCODE -ne 0) { throw "UnityPy installation failed." }
Copy-Item -Force (Join-Path $PSScriptRoot "export_assets.py") $stage
& $python -c "import UnityPy; from PIL import Image; import export_assets; print('Embedded asset runtime import test passed')"
if ($LASTEXITCODE -ne 0) { throw "Asset runtime import test failed." }
# Zip using Python so all files, including hidden files, are included.
& $python -c "import shutil,sys; shutil.make_archive(sys.argv[1], 'zip', sys.argv[2])" (Join-Path $obj "asset-runtime") $stage
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $archive)) { throw "Asset runtime packaging failed." }
Write-Host "Publishing self-contained win-x64 single EXE..." -ForegroundColor Cyan
dotnet publish $project -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile=true /p:PublishTrimmed=false
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }
New-Item -ItemType Directory -Force -Path $dist | Out-Null
Copy-Item -Force (Join-Path $publish "UniversalUnityAssemblyDumper.exe") (Join-Path $dist "UniversalUnityAssemblyDumper.exe")
Write-Host "Done. Portable EXE (asset runtime embedded):" -ForegroundColor Green
Write-Host (Join-Path $dist "UniversalUnityAssemblyDumper.exe")
