; Instalador de RefaccionariaApp (Inno Setup 6).
; No se compila directo: usa generar_instalador.ps1, que primero publica la app
; en instalador\publish y luego llama a ISCC con este script.
;
; Qué hace:
;  1. Si el servidor elegido es la instancia local SQLEXPRESS y no existe,
;     descarga SQL Server Express de Microsoft y lo instala en silencio.
;  2. Copia la app (publicada self-contained: no requiere .NET instalado)
;     y los scripts SQL.
;  3. Ejecuta "RefaccionariaApp.exe --instalar-bd", que crea/actualiza la base
;     de datos, carga el catálogo y da acceso a los usuarios de Windows.
;  4. Escribe conexion.txt con la cadena de conexión.
; Al desinstalar NO se borra la base de datos (los datos se conservan).

#define MyAppName "Refaccionaria"
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#define MyAppExeName "RefaccionariaApp.exe"
#define InstanciaSql "SQLEXPRESS"
#define UrlSqlExpress "https://aka.ms/sql2025express"

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
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=salida
OutputBaseFilename=Instalador_Refaccionaria_{#MyAppVersion}
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

function SqlExpressInstalado: Boolean;
var
  Valor: String;
begin
  Result := RegQueryStringValue(HKLM64, 'SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL',
                                '{#InstanciaSql}', Valor);
end;

function NecesitaInstalarSql: Boolean;
begin
  Result := EsInstanciaLocal and not SqlExpressInstalado;
end;

procedure InitializeWizard;
begin
  PaginaServidor := CreateInputQueryPage(wpSelectDir,
    'Base de datos', 'Servidor de SQL Server donde se creará la base de datos "Refaccionaria".',
    'Deja el valor sugerido para usar SQL Server Express en esta PC (si no está instalado, ' +
    'se descargará e instalará automáticamente; se requiere conexión a internet).' + #13#10#13#10 +
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

  { Descarga del instalador de SQL Server Express (solo si hace falta). }
  if (CurPageID = wpReady) and NecesitaInstalarSql then
  begin
    PaginaDescarga.Clear;
    PaginaDescarga.Add('{#UrlSqlExpress}', 'SQL-SSEI-Expr.exe', '');
    PaginaDescarga.Show;
    try
      try
        PaginaDescarga.Download;
      except
        MsgBox('No se pudo descargar SQL Server Express: ' + GetExceptionMessage + #13#10#13#10 +
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
  if NecesitaInstalarSql then
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

{ Instala SQL Server Express en silencio, antes de copiar los archivos.
  Pasos: el bootstrapper descarga el paquete "Core", se extrae y se ejecuta
  setup.exe con autenticación de Windows; quien instala queda como administrador. }
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
    if not FileExists(Extraccion + '\setup.exe') then
    begin
      Result := 'No se pudo extraer el paquete de SQL Server Express.';
      Exit;
    end;

    Progreso.SetText('Instalando SQL Server Express...', '');
    Codigo := Ejecutar(Extraccion + '\setup.exe',
      '/Q /IACCEPTSQLSERVERLICENSETERMS /SUPPRESSPRIVACYSTATEMENTNOTICE /ACTION=Install ' +
      '/FEATURES=SQLEngine /INSTANCENAME={#InstanciaSql} /INSTANCEID={#InstanciaSql} ' +
      '/ADDCURRENTUSERASSQLADMIN=True /SQLSVCSTARTUPTYPE=Automatic /UPDATEENABLED=False');
    { 3010 = instalado, pero pide reiniciar. }
    if ((Codigo <> 0) and (Codigo <> 3010)) or not SqlExpressInstalado then
      Result := 'La instalación de SQL Server Express falló (código ' + IntToStr(Codigo) + ').' + #13#10 +
                'El detalle está en C:\Program Files\Microsoft SQL Server\<versión>\Setup Bootstrap\Log.';
  finally
    Progreso.Hide;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if NecesitaInstalarSql then
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
           'Puedes volver a ejecutar el instalador para reintentarlo; los datos existentes se conservan.',
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
