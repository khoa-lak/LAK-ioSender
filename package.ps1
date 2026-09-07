param (
    [string]$OutputDir = ".\Publish"
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   BAT DAU BUILD & DONG GOI BAO VE CHONG DICH NGUOC" -ForegroundColor Cyan
Write-Host "   UNG DUNG: LAK-CNC" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Build ca 2 solution o che do Release
$msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
Write-Host "-> 1. Dang build LAK-CNC (Release)..." -ForegroundColor Yellow
& $msbuild "ioSender\ioSender.sln" /t:Rebuild /p:Configuration=Release /v:m
if ($LASTEXITCODE -ne 0) { throw "Build ioSender.sln that bai!" }

Write-Host "-> 2. Dang build LAK-CNC XL (Release)..." -ForegroundColor Yellow
& $msbuild "ioSender XL\ioSender XL.sln" /t:Rebuild /p:Configuration=Release /v:m
if ($LASTEXITCODE -ne 0) { throw "Build ioSender XL.sln that bai!" }

# 2. Xoa thu muc Publish cu va tao thu muc moi
if (Test-Path $OutputDir) {
    Remove-Item -Path $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path "$OutputDir\LAK-CNC" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputDir\LAK-CNC_XL" -Force | Out-Null

# 3. Chay Obfuscar de ma hoa & lam roi code chong dich nguoc (dnSpy, ILSpy)
Write-Host "-> 3. Dang ma hoa & bao ve chong dich nguoc LAK-CNC..." -ForegroundColor Yellow
obfuscar.console .\obfuscar_lak_cnc.xml
if ($LASTEXITCODE -ne 0) { throw "Obfuscar LAK-CNC that bai!" }

Write-Host "-> 4. Dang ma hoa & bao ve chong dich nguoc LAK-CNC XL..." -ForegroundColor Yellow
obfuscar.console .\obfuscar_lak_cnc_xl.xml
if ($LASTEXITCODE -ne 0) { throw "Obfuscar LAK-CNC XL that bai!" }

# 4. Sao chep cac thu vien ben thu 3, config, va ngon ngu
$excludeExt = @(".pdb", ".cs", ".csproj", ".sln", ".user", ".suo", ".xml", ".txt")

Write-Host "-> 5. Sao chep dependencies vao goi phan phoi..." -ForegroundColor Yellow

# Copy third-party files cho LAK-CNC
$src1 = ".\ioSender\ioSender\bin\Release"
Get-ChildItem -Path $src1 | Where-Object { $excludeExt -notcontains $_.Extension } | ForEach-Object {
    if (-not (Test-Path "$OutputDir\LAK-CNC\$($_.Name)")) {
        Copy-Item -Path $_.FullName -Destination "$OutputDir\LAK-CNC" -Recurse -Force
    }
}
# Dam bao config file ton tai
if (Test-Path "$src1\LAK-CNC.exe.config") {
    Copy-Item -Path "$src1\LAK-CNC.exe.config" -Destination "$OutputDir\LAK-CNC\LAK-CNC.exe.config" -Force
}
if (Test-Path ".\sample_square.nc") {
    Copy-Item -Path ".\sample_square.nc" -Destination "$OutputDir\LAK-CNC\sample_square.nc" -Force
}

# Copy third-party files cho LAK-CNC XL
$src2 = ".\ioSender XL\ioSender XL\bin\Release"
Get-ChildItem -Path $src2 | Where-Object { $excludeExt -notcontains $_.Extension } | ForEach-Object {
    if (-not (Test-Path "$OutputDir\LAK-CNC_XL\$($_.Name)")) {
        Copy-Item -Path $_.FullName -Destination "$OutputDir\LAK-CNC_XL" -Recurse -Force
    }
}
if (Test-Path "$src2\LAK-CNC XL.exe.config") {
    Copy-Item -Path "$src2\LAK-CNC XL.exe.config" -Destination "$OutputDir\LAK-CNC_XL\LAK-CNC XL.exe.config" -Force
}
if (Test-Path ".\sample_square.nc") {
    Copy-Item -Path ".\sample_square.nc" -Destination "$OutputDir\LAK-CNC_XL\sample_square.nc" -Force
}

# 5. Xoa cac file log mapping obfuscar khoi goi khach hang
Get-ChildItem -Path "$OutputDir" -Include "Mapping.txt", "*.pdb" -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue

# 6. Dong goi file ZIP
Write-Host "-> 6. Dang tao file ZIP cho khach hang..." -ForegroundColor Yellow
Compress-Archive -Path "$OutputDir\LAK-CNC\*" -DestinationPath "$OutputDir\LAK-CNC_Portable.zip" -Force
Compress-Archive -Path "$OutputDir\LAK-CNC_XL\*" -DestinationPath "$OutputDir\LAK-CNC_XL_Portable.zip" -Force

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "   DONG GOI THANH CONG! TOAN BO FILE DA DUOC BAO VE" -ForegroundColor Green
Write-Host "   Thu muc: $OutputDir" -ForegroundColor Green
Write-Host "   File ZIP 1: $OutputDir\LAK-CNC_Portable.zip" -ForegroundColor Green
Write-Host "   File ZIP 2: $OutputDir\LAK-CNC_XL_Portable.zip" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
