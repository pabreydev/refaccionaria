# Genera el instalador de Refaccionaria.
#   1. Publica la app (Release, .NET Framework 4.8, AnyCPU) en instalador\publish.
#   2. Compila Refaccionaria.iss con Inno Setup 6 -> instalador\salida\*.exe
#
# Uso (desde cualquier carpeta):
#   powershell -ExecutionPolicy Bypass -File instalador\generar_instalador.ps1 [-Version 1.0.1] [-Win7]
#
# -Win7 genera Instalador_Refaccionaria_<versión>_Win7.exe, que incluye
# .NET Framework 4.8 y SQL Server 2014 Express SP3 (unos 400 MB más): en
# Windows 7 no se pueden descargar durante la instalación. Se descargan una
# vez a instalador\prerequisitos y se reutilizan.

param([string]$Version = "1.0.0", [switch]$Win7)

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

$definiciones = @("/DMyAppVersion=$Version")
if ($Win7) {
    $prerequisitos = Join-Path $aqui "prerequisitos"
    New-Item -ItemType Directory -Force $prerequisitos | Out-Null
    $descargas = [ordered]@{
        "NDP48-x86-x64-AllOS-ENU.exe" = "https://download.microsoft.com/download/f/3/a/f3a6af84-da23-40a5-8d1c-49cc10c8e76f/NDP48-x86-x64-AllOS-ENU.exe"
        "SQLEXPR_x86_ENU.exe"         = "https://download.microsoft.com/download/3/9/f/39f968fa-debb-4960-8f9e-0e7bb3035959/SQLEXPR_x86_ENU.exe"
    }
    foreach ($archivo in $descargas.Keys) {
        $destino = Join-Path $prerequisitos $archivo
        if (Test-Path $destino) { continue }
        Write-Host "Descargando $archivo..." -ForegroundColor Cyan
        $ProgressPreference = "SilentlyContinue"   # la barra de progreso hace muy lenta la descarga
        Invoke-WebRequest $descargas[$archivo] -OutFile "$destino.part"
        Move-Item "$destino.part" $destino
    }
    $definiciones += "/DWin7"
}

if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }

Write-Host "Publicando la aplicación ($Version)..." -ForegroundColor Cyan
dotnet publish $proyecto -c Release -o $publish -p:Version=$Version
if ($LASTEXITCODE -ne 0) { throw "Falló dotnet publish." }

# conexion.txt es propio de cada PC: lo escribe el instalador, nunca se empaqueta.
Remove-Item (Join-Path $publish "conexion.txt") -ErrorAction SilentlyContinue
Get-ChildItem $publish -Recurse -Filter *.pdb | Remove-Item
# DLL nativas de Linux/macOS que algunos paquetes copian igual: en Windows no sirven.
Get-ChildItem (Join-Path $publish "runtimes") -Directory -ErrorAction SilentlyContinue |
    Where-Object Name -Match '^(linux|osx)' | Remove-Item -Recurse -Force

Write-Host "Compilando el instalador..." -ForegroundColor Cyan
& $iscc @definiciones (Join-Path $aqui "Refaccionaria.iss")
if ($LASTEXITCODE -ne 0) { throw "Falló la compilación del instalador." }

Write-Host "Listo: $(Join-Path $aqui 'salida')" -ForegroundColor Green
