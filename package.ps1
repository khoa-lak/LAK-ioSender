param (
    [string]$OutputDir = ".\Publish"
)

$ErrorActionPreference = "Stop"

# 1. Đọc tên app từ file cấu hình trung tâm Directory.Build.props
$AppName = "LAK-CNC"
if (Test-Path ".\Directory.Build.props") {
    try {
        [xml]$props = Get-Content ".\Directory.Build.props"
        if ($props.Project.PropertyGroup.AppBaseName) {
            $AppName = $props.Project.PropertyGroup.AppBaseName.Trim()
        }
    } catch {
        Write-Warning "Khong the doc Directory.Build.props, su dung ten mac dinh: $AppName"
    }
}
$AppNameXL = "$AppName XL"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   BAT DAU BUILD & DONG GOI BAO VE CHONG DICH NGUOC" -ForegroundColor Cyan
Write-Host "   TEN UNG DUNG HIEN TAI: $AppName / $AppNameXL" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 2. Build ca 2 solution o che do Release
$msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
Write-Host "-> 1. Dang build $AppName (Release)..." -ForegroundColor Yellow
& $msbuild "ioSender\ioSender.sln" /t:Rebuild /p:Configuration=Release /v:m
if ($LASTEXITCODE -ne 0) { throw "Build ioSender.sln that bai!" }

Write-Host "-> 2. Dang build $AppNameXL (Release)..." -ForegroundColor Yellow
& $msbuild "ioSender XL\ioSender XL.sln" /t:Rebuild /p:Configuration=Release /v:m
if ($LASTEXITCODE -ne 0) { throw "Build ioSender XL.sln that bai!" }

# 3. Xoa thu muc Publish cu va tao thu muc moi
if (Test-Path $OutputDir) {
    Remove-Item -Path $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path "$OutputDir\$AppName" -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputDir\$($AppName)_XL" -Force | Out-Null

# 4. Tao file cau hinh Obfuscar dong theo ten AppName
$obfuscarXml1 = @"
<?xml version='1.0'?>
<Obfuscator>
  <Var name="InPath" value=".\ioSender\ioSender\bin\Release" />
  <Var name="OutPath" value="$OutputDir\$AppName" />
  <Var name="KeepPublicApi" value="true" />
  <Var name="HideStrings" value="true" />
  <Var name="RenameProperties" value="false" />
  <Var name="RenameEvents" value="false" />
  <Var name="RenameFields" value="true" />
  <Var name="OptimizeMethods" value="true" />
  <Var name="SuppressIldasm" value="true" />
  <Var name="XmlDocumentation" value="false" />

  <AssemblySearchPath path="C:\Windows\Microsoft.NET\Framework64\v4.0.30319" />
  <AssemblySearchPath path="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF" />
  <AssemblySearchPath path="C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.2" />

  <Module file="`$(InPath)\$AppName.exe">
    <SkipType name="*" skipProperties="true" skipEvents="true" skipMethods="true" />
  </Module>
  <Module file="`$(InPath)\CNC.Controls.WPF.dll">
    <SkipType name="*" skipProperties="true" skipEvents="true" skipMethods="true" />
  </Module>
  <Module file="`$(InPath)\CNC.Controls.Lathe.dll">
    <SkipType name="*" skipProperties="true" skipEvents="true" skipMethods="true" />
  </Module>
  <Module file="`$(InPath)\CNC.Controls.Probing.dll">
    <SkipType name="*" skipProperties="true" skipEvents="true" skipMethods="true" />
  </Module>
  <Module file="`$(InPath)\CNC.Controls.Viewer.dll">
    <SkipType name="*" skipProperties="true" skipEvents="true" skipMethods="true" />
  </Module>
  <Module file="`$(InPath)\CNC.Controls.DragKnife.dll">
    <SkipType name="*" skipProperties="true" skipEvents="true" skipMethods="true" />
  </Module>
  <Module file="`$(InPath)\CNC.Controls.Camera.dll">
    <SkipType name="*" skipProperties="true" skipEvents="true" skipMethods="true" />
  </Module>
  <Module file="`$(InPath)\CNC.Converters.dll">
    <SkipType name="*" skipProperties="true" skipEvents="true" skipMethods="true" />
  </Module>
  <Module file="`$(InPath)\CNC.Core.dll">
    <SkipType name="CNC.Core.GrblViewModel" skipProperties="true" skipEvents="true" skipMethods="true" />
    <SkipType name="CNC.Core.ViewModelBase" skipProperties="true" skipEvents="true" skipMethods="true" />
  </Module>
</Obfuscator>
"@
$obfuscarXml1 | Out-File -FilePath ".\obfuscar_lak_cnc.xml" -Encoding utf8

$obfuscarXml2 = @"
<?xml version='1.0'?>
<Obfuscator>
  <Var name="InPath" value=".\ioSender XL\ioSender XL\bin\Release" />
  <Var name="OutPath" value="$OutputDir\$($AppName)_XL" />
  <Var name="KeepPublicApi" value="true" />
  <Var name="HideStrings" value="true" />
  <Var name="RenameProperties" value="false" />
  <Var name="RenameEvents" value="false" />
  <Var name="RenameFields" value="true" />
  <Var name="OptimizeMethods" value="true" />
  <Var name="SuppressIldasm" value="true" />
  <Var name="XmlDocumentation" value="false" />

  <AssemblySearchPath path="C:\Windows\Microsoft.NET\Framework64\v4.0.30319" />
  <AssemblySearchPath path="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF" />
  <AssemblySearchPath path="C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.2" />

  <Module file="`$(InPath)\$AppNameXL.exe">
    <SkipType name="*" skipProperties="true" skipEvents="true" skipMethods="true" />
  </Module>
  <Module file="`$(InPath)\CNC.Controls.WPF.dll">
    <SkipType name="*" skipProperties="true" skipEvents="true" skipMethods="true" />
  </Module>
  <Module file="`$(InPath)\CNC.Controls.Lathe.dll">
    <SkipType name="*" skipProperties="true" skipEvents="true" skipMethods="true" />
  </Module>
  <Module file="`$(InPath)\CNC.Controls.Probing.dll">
    <SkipType name="*" skipProperties="true" skipEvents="true" skipMethods="true" />
  </Module>
  <Module file="`$(InPath)\CNC.Controls.Viewer.dll">
    <SkipType name="*" skipProperties="true" skipEvents="true" skipMethods="true" />
  </Module>
  <Module file="`$(InPath)\CNC.Controls.DragKnife.dll">
    <SkipType name="*" skipProperties="true" skipEvents="true" skipMethods="true" />
  </Module>
  <Module file="`$(InPath)\CNC.Controls.Camera.dll">
    <SkipType name="*" skipProperties="true" skipEvents="true" skipMethods="true" />
  </Module>
  <Module file="`$(InPath)\CNC.Converters.dll">
    <SkipType name="*" skipProperties="true" skipEvents="true" skipMethods="true" />
  </Module>
  <Module file="`$(InPath)\CNC.Core.dll">
    <SkipType name="CNC.Core.GrblViewModel" skipProperties="true" skipEvents="true" skipMethods="true" />
    <SkipType name="CNC.Core.ViewModelBase" skipProperties="true" skipEvents="true" skipMethods="true" />
  </Module>
</Obfuscator>
"@
$obfuscarXml2 | Out-File -FilePath ".\obfuscar_lak_cnc_xl.xml" -Encoding utf8

# 5. Chay Obfuscar
Write-Host "-> 3. Dang ma hoa & bao ve chong dich nguoc $AppName..." -ForegroundColor Yellow
obfuscar.console .\obfuscar_lak_cnc.xml
if ($LASTEXITCODE -ne 0) { throw "Obfuscar $AppName that bai!" }

Write-Host "-> 4. Dang ma hoa & bao ve chong dich nguoc $AppNameXL..." -ForegroundColor Yellow
obfuscar.console .\obfuscar_lak_cnc_xl.xml
if ($LASTEXITCODE -ne 0) { throw "Obfuscar $AppNameXL that bai!" }

# 6. Sao chep cac thu vien ben thu 3, config, va ngon ngu
$excludeExt = @(".pdb", ".cs", ".csproj", ".sln", ".user", ".suo", ".xml", ".txt")

Write-Host "-> 5. Sao chep dependencies vao goi phan phoi..." -ForegroundColor Yellow

# Copy third-party files cho AppName
$src1 = ".\ioSender\ioSender\bin\Release"
Get-ChildItem -Path $src1 | Where-Object { $excludeExt -notcontains $_.Extension } | ForEach-Object {
    if (-not (Test-Path "$OutputDir\$AppName\$($_.Name)")) {
        Copy-Item -Path $_.FullName -Destination "$OutputDir\$AppName" -Recurse -Force
    }
}
if (Test-Path "$src1\$AppName.exe.config") {
    Copy-Item -Path "$src1\$AppName.exe.config" -Destination "$OutputDir\$AppName\$AppName.exe.config" -Force
}
if (Test-Path ".\sample_square.nc") {
    Copy-Item -Path ".\sample_square.nc" -Destination "$OutputDir\$AppName\sample_square.nc" -Force
}

# Copy third-party files cho AppName XL
$src2 = ".\ioSender XL\ioSender XL\bin\Release"
Get-ChildItem -Path $src2 | Where-Object { $excludeExt -notcontains $_.Extension } | ForEach-Object {
    if (-not (Test-Path "$OutputDir\$($AppName)_XL\$($_.Name)")) {
        Copy-Item -Path $_.FullName -Destination "$OutputDir\$($AppName)_XL" -Recurse -Force
    }
}
if (Test-Path "$src2\$AppNameXL.exe.config") {
    Copy-Item -Path "$src2\$AppNameXL.exe.config" -Destination "$OutputDir\$($AppName)_XL\$AppNameXL.exe.config" -Force
}
if (Test-Path ".\sample_square.nc") {
    Copy-Item -Path ".\sample_square.nc" -Destination "$OutputDir\$($AppName)_XL\sample_square.nc" -Force
}

# 7. Xoa file log mapping
Get-ChildItem -Path "$OutputDir" -Include "Mapping.txt", "*.pdb" -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue

# 8. Dong goi file ZIP
Write-Host "-> 6. Dang tao file ZIP cho khach hang..." -ForegroundColor Yellow
$zip1 = "$OutputDir\$($AppName)_Portable.zip"
$zip2 = "$OutputDir\$($AppName)_XL_Portable.zip"
Compress-Archive -Path "$OutputDir\$AppName\*" -DestinationPath "$zip1" -Force
Compress-Archive -Path "$OutputDir\$($AppName)_XL\*" -DestinationPath "$zip2" -Force

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "   DONG GOI THANH CONG! TOAN BO FILE DA DUOC BAO VE" -ForegroundColor Green
Write-Host "   Thu muc: $OutputDir" -ForegroundColor Green
Write-Host "   File ZIP 1: $zip1" -ForegroundColor Green
Write-Host "   File ZIP 2: $zip2" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
