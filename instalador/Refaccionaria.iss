; Instalador de RefaccionariaApp (Inno Setup 6).
; No se compila directo: usa generar_instalador.ps1, que primero publica la app
; en instalador\publish y luego llama a ISCC con este script.
;
; Qué hace:
;  1. Si falta .NET Framework 4.8, lo instala (la app es .NET Framework 4.8
;     para que corra también en Windows 7).
;  2. Si el servidor elegido es la instancia local SQLEXPRESS y no existe,
;     instala SQL Server Express en silencio:
;       - Windows 10/11: descarga SQL Server 2025 Express de Microsoft.
;       - Windows 7/8/8.1: SQL Server 2014 Express SP3 (lo último que soporta
;         Windows 7), que solo trae el instalador generado con -Win7.
;  3. Copia la app y los scripts SQL.
;  4. Ejecuta "RefaccionariaApp.exe --instalar-bd", que crea/actualiza la base
;     de datos, carga el catálogo y da acceso a los usuarios de Windows.
;  5. Escribe conexion.txt con la cadena de conexión.
; Al desinstalar NO se borra la base de datos (los datos se conservan).
;
; Con /DWin7 (generar_instalador.ps1 -Win7) se incluyen dentro del instalador
; .NET Framework 4.8 y SQL Server 2014 Express: Windows 7 sin actualizaciones
; no puede descargarlos (los servidores de Microsoft exigen TLS 1.2).

#define MyAppName "Refaccionaria"
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#define MyAppExeName "RefaccionariaApp.exe"
#define InstanciaSql "SQLEXPRESS"
#define UrlSqlExpress "https://aka.ms/sql2025express"
#define UrlNet48 "https://go.microsoft.com/fwlink/?linkid=2088631"
#define InstaladorNet48 "NDP48-x86-x64-AllOS-ENU.exe"
; SQL Server 2014 SP3 Express x86: se instala en Windows de 32 y de 64 bits.
#define InstaladorSql2014 "SQLEXPR_x86_ENU.exe"

[Setup]
AppId={{B426C276-89B9-4039-BFC1-10221A36C14E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher=Mangueras y Refacciones
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
; Windows 7 SP1 o posterior, de 32 o 64 bits (la app es AnyCPU).
MinVersion=6.1sp1
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=salida
#ifdef Win7
OutputBaseFilename=Instalador_Refaccionaria_{#MyAppVersion}_Win7
#else
OutputBaseFilename=Instalador_Refaccionaria_{#MyAppVersion}
#endif
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
CloseApplications=yes

[Languages]
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "escritorio"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Accesos directos:"

[Files]
#ifdef Win7
; Ya vienen comprimidos; se extraen a {tmp} solo si hacen falta.
Source: "prerequisitos\{#InstaladorNet48}"; Flags: dontcopy nocompression
Source: "prerequisitos\{#InstaladorSql2014}"; Flags: dontcopy nocompression
#endif
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\sql\script.sql"; DestDir: "{app}\sql"; Flags: ignoreversion
Source: "..\sql\correcciones_recomendadas.sql"; DestDir: "{app}\sql"; Flags: ignoreversion
Source: "..\sql\seed_catalogo.sql"; DestDir: "{app}\sql"; Flags: ignoreversion
Source: "..\sql\permisos_usuarios.sql"; DestDir: "{app}\sql"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: escritorio

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{app}\conexion.txt"
Type: files; Name: "{app}\instalacion_bd.log"

[Code]
const
  ClaveInstanciasSql = 'SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL';
#ifdef Win7
  IncluyePrerequisitos = True;
#else
  IncluyePrerequisitos = False;
#endif

var
  PaginaServidor: TInputQueryWizardPage;
  PaginaDescarga: TDownloadWizardPage;

function ServidorElegido: String;
begin
  Result := Trim(PaginaServidor.Values[0]);
end;

{ True si el servidor es la instancia SQLEXPRESS de esta misma PC. }
function EsInstanciaLocal: Boolean;
var
  S: String;
begin
  S := Lowercase(ServidorElegido);
  Result := (S = '.\{#InstanciaSql}') or (S = 'localhost\{#InstanciaSql}') or
            (S = '(local)\{#InstanciaSql}') or
            (S = Lowercase(GetComputerNameString + '\{#InstanciaSql}'));
end;

{ SQL Server 2016 en adelante ya no se instala en Windows 7/8/8.1. }
function UsaSql2014: Boolean;
begin
  Result := GetWindowsVersion < $0A000000;
end;

{ SQL Server 2014 x86 en Windows de 64 bits se registra en la vista de 32 bits. }
function SqlExpressInstalado: Boolean;
var
  Valor: String;
begin
  Result := RegQueryStringValue(HKLM32, ClaveInstanciasSql, '{#InstanciaSql}', Valor);
  if not Result and IsWin64 then
    Result := RegQueryStringValue(HKLM64, ClaveInstanciasSql, '{#InstanciaSql}', Valor);
end;

function NecesitaInstalarSql: Boolean;
begin
  Result := EsInstanciaLocal and not SqlExpressInstalado;
end;

function NecesitaNet48: Boolean;
begin
  Result := not IsDotNetInstalled(net48, 0);
end;

procedure InitializeWizard;
begin
  PaginaServidor := CreateInputQueryPage(wpSelectDir,
    'Base de datos', 'Servidor de SQL Server donde se creará la base de datos "Refaccionaria".',
    'Deja el valor sugerido para usar SQL Server Express en esta PC (si no está instalado, ' +
    'se instalará automáticamente).' + #13#10#13#10 +
    'Si la base de datos ya existe, se conservan sus datos.');
  PaginaServidor.Add('Servidor:', False);
  PaginaServidor.Values[0] := '.\{#InstanciaSql}';

  PaginaDescarga := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), nil);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;

  if (CurPageID = PaginaServidor.ID) and (ServidorElegido = '') then
  begin
    MsgBox('Indica el servidor de SQL Server.', mbError, MB_OK);
    Result := False;
  end;

  if (CurPageID = PaginaServidor.ID) and Result and UsaSql2014 and NecesitaInstalarSql and not IncluyePrerequisitos then
  begin
    MsgBox('En esta versión de Windows se necesita SQL Server 2014 Express, que no viene en este instalador.' + #13#10#13#10 +
           'Usa el instalador para Windows 7 (Instalador_Refaccionaria_..._Win7.exe) o indica un servidor ' +
           'de SQL Server que ya exista.', mbError, MB_OK);
    Result := False;
  end;

  { Descarga lo que haga falta y no venga incluido en el instalador. }
  if (CurPageID = wpReady) and ((NecesitaNet48 and not IncluyePrerequisitos) or (NecesitaInstalarSql and not UsaSql2014)) then
  begin
    PaginaDescarga.Clear;
    if NecesitaNet48 and not IncluyePrerequisitos then
      PaginaDescarga.Add('{#UrlNet48}', '{#InstaladorNet48}', '');
    if NecesitaInstalarSql and not UsaSql2014 then
      PaginaDescarga.Add('{#UrlSqlExpress}', 'SQL-SSEI-Expr.exe', '');
    PaginaDescarga.Show;
    try
      try
        PaginaDescarga.Download;
      except
        MsgBox('No se pudo descargar un componente necesario: ' + GetExceptionMessage + #13#10#13#10 +
               'Revisa la conexión a internet e inténtalo de nuevo.', mbCriticalError, MB_OK);
        Result := False;
      end;
    finally
      PaginaDescarga.Hide;
    end;
  end;
end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo,
  MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
begin
  Result := MemoDirInfo + NewLine + NewLine +
            'Servidor de base de datos:' + NewLine + Space + ServidorElegido;
  if NecesitaNet48 then
    Result := Result + NewLine + Space + '(se instalará .NET Framework 4.8)';
  if NecesitaInstalarSql then
    if UsaSql2014 then
      Result := Result + NewLine + Space + '(se instalará SQL Server 2014 Express; puede tardar varios minutos)'
    else
      Result := Result + NewLine + Space + '(se instalará SQL Server Express; puede tardar varios minutos)';
  if MemoTasksInfo <> '' then
    Result := Result + NewLine + NewLine + MemoTasksInfo;
end;

{ Ejecuta un programa y devuelve su código de salida (-1 si no se pudo iniciar). }
function Ejecutar(const Archivo, Parametros: String): Integer;
begin
  if not Exec(Archivo, Parametros, '', SW_HIDE, ewWaitUntilTerminated, Result) then
    Result := -1;
end;

{ Instala .NET Framework 4.8 en silencio (desde la carpeta temporal: incluido o descargado). }
function InstalarNet48(var NeedsRestart: Boolean): String;
var
  Progreso: TOutputMarqueeProgressWizardPage;
  Codigo: Integer;
begin
  Result := '';
  if IncluyePrerequisitos then
    ExtractTemporaryFile('{#InstaladorNet48}');

  Progreso := CreateOutputMarqueeProgressPage('Instalando .NET Framework 4.8',
    'Esto puede tardar varios minutos. No cierres el instalador.');
  Progreso.Show;
  try
    Progreso.Animate;
    Codigo := Ejecutar(ExpandConstant('{tmp}\{#InstaladorNet48}'), '/q /norestart');
  finally
    Progreso.Hide;
  end;

  { 3010/1641 = instalado, pero pide reiniciar. }
  if (Codigo = 3010) or (Codigo = 1641) then
    NeedsRestart := True
  else if Codigo <> 0 then
    Result := 'La instalación de .NET Framework 4.8 falló (código ' + IntToStr(Codigo) + ').' + #13#10 +
              'En Windows 7 se necesita el Service Pack 1. El detalle está en %TEMP%\dd_NDP48*.log.';
end;

{ Ejecuta el setup.exe ya extraído de SQL Server Express con autenticación de
  Windows; quien instala queda como administrador. }
function EjecutarSetupSql(Progreso: TOutputMarqueeProgressWizardPage; const Extraccion, ParametrosExtra: String): String;
var
  Codigo: Integer;
begin
  Result := '';
  if not FileExists(Extraccion + '\setup.exe') then
  begin
    Result := 'No se pudo extraer el paquete de SQL Server Express.';
    Exit;
  end;

  Progreso.SetText('Instalando SQL Server Express...', '');
  Codigo := Ejecutar(Extraccion + '\setup.exe',
    '/Q /IACCEPTSQLSERVERLICENSETERMS /ACTION=Install ' +
    '/FEATURES=SQLEngine /INSTANCENAME={#InstanciaSql} /INSTANCEID={#InstanciaSql} ' +
    '/ADDCURRENTUSERASSQLADMIN=True /SQLSVCSTARTUPTYPE=Automatic /UPDATEENABLED=False' + ParametrosExtra);
  { 3010 = instalado, pero pide reiniciar. }
  if ((Codigo <> 0) and (Codigo <> 3010)) or not SqlExpressInstalado then
    Result := 'La instalación de SQL Server Express falló (código ' + IntToStr(Codigo) + ').' + #13#10 +
              'El detalle está en C:\Program Files\Microsoft SQL Server\<versión>\Setup Bootstrap\Log.';
end;

{ SQL Server 2025 Express (Windows 10/11): el bootstrapper descarga el paquete
  "Core", se extrae y se ejecuta setup.exe. }
function InstalarSqlExpress: String;
var
  Progreso: TOutputMarqueeProgressWizardPage;
  Medios, Extraccion, Paquete: String;
  Busqueda: TFindRec;
  Codigo: Integer;
begin
  Result := '';
  Medios := ExpandConstant('{tmp}\sqlmedia');
  Extraccion := ExpandConstant('{tmp}\sqlsetup');

  Progreso := CreateOutputMarqueeProgressPage('Instalando SQL Server Express',
    'Esto puede tardar varios minutos. No cierres el instalador.');
  Progreso.Show;
  try
    Progreso.SetText('Descargando los archivos de SQL Server Express...', '');
    Progreso.Animate;
    Codigo := Ejecutar(ExpandConstant('{tmp}\SQL-SSEI-Expr.exe'),
      '/ACTION=Download /MEDIAPATH="' + Medios + '" /MEDIATYPE=Core /QUIET');
    if Codigo <> 0 then
    begin
      Result := 'No se pudieron descargar los archivos de SQL Server Express (código ' + IntToStr(Codigo) + ').';
      Exit;
    end;

    if not FindFirst(Medios + '\SQLEXPR*.exe', Busqueda) then
    begin
      Result := 'No se encontró el paquete descargado de SQL Server Express.';
      Exit;
    end;
    Paquete := Medios + '\' + Busqueda.Name;
    FindClose(Busqueda);

    Progreso.SetText('Extrayendo SQL Server Express...', '');
    Ejecutar(Paquete, '/u /x:"' + Extraccion + '"');
    Result := EjecutarSetupSql(Progreso, Extraccion, ' /SUPPRESSPRIVACYSTATEMENTNOTICE');
  finally
    Progreso.Hide;
  end;
end;

{ SQL Server 2014 Express SP3 (Windows 7/8/8.1), incluido en el instalador -Win7. }
function InstalarSql2014: String;
var
  Progreso: TOutputMarqueeProgressWizardPage;
  Extraccion: String;
begin
  Extraccion := ExpandConstant('{tmp}\sqlsetup');

  Progreso := CreateOutputMarqueeProgressPage('Instalando SQL Server 2014 Express',
    'Esto puede tardar varios minutos. No cierres el instalador.');
  Progreso.Show;
  try
    Progreso.SetText('Extrayendo SQL Server Express...', '');
    Progreso.Animate;
    ExtractTemporaryFile('{#InstaladorSql2014}');
    Ejecutar(ExpandConstant('{tmp}\{#InstaladorSql2014}'), '/q /x:"' + Extraccion + '"');
    { Si .NET 4.8 acaba de instalarse puede quedar un reinicio pendiente, y el
      setup de SQL se negaría a continuar por esa regla. }
    Result := EjecutarSetupSql(Progreso, Extraccion, ' /SkipRules=RebootRequiredCheck');
  finally
    Progreso.Hide;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  { .NET primero: SQL Server 2014 también lo necesita. }
  if NecesitaNet48 then
    Result := InstalarNet48(NeedsRestart);
  if (Result = '') and NecesitaInstalarSql then
    if UsaSql2014 then
      Result := InstalarSql2014
    else
      Result := InstalarSqlExpress;
end;

procedure EscribirConexion;
begin
  SaveStringToFile(ExpandConstant('{app}\conexion.txt'),
    'Data Source=' + ServidorElegido + ';Initial Catalog=Refaccionaria;Integrated Security=True;' +
    'Pooling=False;Connect Timeout=30;TrustServerCertificate=True', False);
end;

procedure CrearBaseDatos;
var
  Progreso: TOutputMarqueeProgressWizardPage;
  Log: String;
  Codigo: Integer;
begin
  Log := ExpandConstant('{app}\instalacion_bd.log');
  Progreso := CreateOutputMarqueeProgressPage('Base de datos',
    'Creando la base de datos y cargando el catálogo...');
  Progreso.Show;
  try
    Progreso.Animate;
    Codigo := Ejecutar(ExpandConstant('{app}\{#MyAppExeName}'),
      '--instalar-bd "' + ServidorElegido + '" "' + ExpandConstant('{app}\sql') + '" "' + Log + '"');
  finally
    Progreso.Hide;
  end;

  if Codigo <> 0 then
    MsgBox('La aplicación se instaló, pero no se pudo crear la base de datos en "' + ServidorElegido + '".' + #13#10#13#10 +
           'Revisa el detalle en:' + #13#10 + Log + #13#10#13#10 +
           'Si se instaló .NET Framework 4.8 y pidió reiniciar, reinicia la PC y vuelve a ejecutar el instalador; ' +
           'los datos existentes se conservan.',
           mbError, MB_OK);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    EscribirConexion;
    CrearBaseDatos;
  end;
end;
