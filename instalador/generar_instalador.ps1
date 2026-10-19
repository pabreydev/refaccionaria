# Genera el instalador de Refaccionaria.
#   1. Publica la app (Release, win-x64, self-contained) en instalador\publish.
#   2. Compila Refaccionaria.iss con Inno Setup 6 -> instalador\salida\*.exe
#
# Uso (desde cualquier carpeta):
#   powershell -ExecutionPolicy Bypass -File instalador\generar_instalador.ps1 [-Version 1.0.1]

param([string]$Version = "1.0.0")

$ErrorActionPreference = "Stop"
$aqui = $PSScriptRoot
$proyecto = Join-Path $aqui "..\RefaccionariaApp\RefaccionariaApp.csproj"
$publish = Join-Path $aqui "publish"

$iscc = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw "No se encontró Inno Setup 6. Instálalo con: winget install JRSoftware.InnoSetup" }

if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }

Write-Host "Publicando la aplicación ($Version)..." -ForegroundColor Cyan
dotnet publish $proyecto -c Release -r win-x64 --self-contained true -o $publish -p:Version=$Version
if ($LASTEXITCODE -ne 0) { throw "Falló dotnet publish." }

# conexion.txt es propio de cada PC: lo escribe el instalador, nunca se empaqueta.
Remove-Item (Join-Path $publish "conexion.txt") -ErrorAction SilentlyContinue

Write-Host "Compilando el instalador..." -ForegroundColor Cyan
& $iscc "/DMyAppVersion=$Version" (Join-Path $aqui "Refaccionaria.iss")
if ($LASTEXITCODE -ne 0) { throw "Falló la compilación del instalador." }

Write-Host "Listo: $(Join-Path $aqui 'salida')" -ForegroundColor Green
