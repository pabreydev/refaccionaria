# Refaccionaria - Proyecto de Windows Forms (C#)

Aplicación de escritorio en C# / Windows Forms (.NET Framework 4.8) que
administra la base de datos `Refaccionaria` (SQL Server) creada por `script.sql`.
Corre en **Windows 7 SP1 en adelante, de 32 o 64 bits**.

## Requisitos

- Visual Studio 2022 (o superior) con la carga de trabajo ".NET desktop
  development", o el SDK de .NET 8 o posterior (compila el proyecto `net48`
  sin instalar el Developer Pack de .NET Framework).
- SQL Server **2014 o posterior** (Express, Developer o similar) con la base
  de datos `Refaccionaria` ya creada a partir de tu `script.sql`.

### Compatibilidad con Windows 7

- **.NET Framework 4.8** en lugar de .NET 8 (que no corre en Windows 7).
  En código no se pueden usar APIs exclusivas de .NET Core/5+ (por ejemplo
  `TextBox.PlaceholderText`: se usa `Tema.AsignarPlaceholder`).
- **QuestPDF fijo en 2026.6.0**: desde 2026.6.1 su DLL nativa importa
  `GetSystemTimePreciseAsFileTime` (Windows 8+) y no carga en Windows 7.
  Antes de actualizarlo, revisar eso en `runtimes\win-x86\native`.
- **Los scripts de `sql/` deben correr en SQL Server 2014**, lo último que se
  instala en Windows 7: nada de `DROP ... IF EXISTS`, `CREATE OR ALTER`,
  `STRING_AGG`, `CONCAT_WS`, `OPENJSON` ni `OPTIMIZE_FOR_SEQUENTIAL_KEY`. Se
  usan `IF OBJECT_ID(...) IS NOT NULL DROP ...`, `STUFF(... FOR XML PATH(''))`
  y parámetros tipo tabla.

## Puesta en marcha

1. Ejecuta tu `script.sql` en SQL Server Management Studio para crear la
   base de datos, las tablas y los procedimientos almacenados (si ya lo
   hiciste, omite este paso).
2. (Recomendado) Ejecuta `sql/correcciones_recomendadas.sql` — corrige
   varios procedimientos de borrado que tienen un error. Ver la sección
   "Errores encontrados en el script" más abajo.
3. Abre `RefaccionariaApp.sln` en Visual Studio.
4. Edita `RefaccionariaApp/Data/ConexionBD.cs` y ajusta el `Data Source`
   (nombre de tu servidor/instancia de SQL Server) si no usas
   `.\SQLEXPRESS`. (En una PC instalada, la cadena se lee de `conexion.txt`
   junto al ejecutable.)
5. Compila y ejecuta (F5). Restaura los paquetes NuGet
   `Microsoft.Data.SqlClient`, `QuestPDF` (PDF de cotizaciones, licencia
   Community) y `ClosedXML` (Excel) automáticamente al compilar.

## Instalador

Para instalar en otra PC se genera un solo `.exe` que instala la aplicación
**y** la base de datos.

**Generarlo** (requiere Inno Setup 6: `winget install JRSoftware.InnoSetup`):

```powershell
powershell -ExecutionPolicy Bypass -File instalador\generar_instalador.ps1 -Version 1.0.0
# Para Windows 7 (incluye .NET Framework 4.8 y SQL Server 2014 Express):
powershell -ExecutionPolicy Bypass -File instalador\generar_instalador.ps1 -Version 1.0.0 -Win7
```

El resultado queda en `instalador\salida\Instalador_Refaccionaria_<versión>.exe`
(o `..._Win7.exe`). El de Windows 7 pesa unos 400 MB más porque Windows 7 sin
actualizaciones no puede descargar de los servidores de Microsoft (exigen
TLS 1.2); sus prerrequisitos se descargan una vez a `instalador\prerequisitos`.
Sirve también en Windows 10/11.

**Qué hace al instalar** (requiere permisos de administrador):

1. Si falta **.NET Framework 4.8**, lo instala (viene en Windows 10 1903+;
   el instalador normal lo descarga, el `_Win7` lo trae incluido).
2. Pide el servidor de SQL Server (por defecto `.\SQLEXPRESS`). Si es esa
   instancia local y no existe, lo instala en silencio (tarda varios minutos):
   - Windows 10/11: descarga **SQL Server 2025 Express** de Microsoft.
   - Windows 7/8/8.1: **SQL Server 2014 Express SP3** (x86, sirve en 32 y
     64 bits). Solo lo trae el instalador `_Win7`.
3. Copia la aplicación en `Archivos de programa\Refaccionaria`.
4. Crea la base `Refaccionaria` con `script.sql`, aplica
   `correcciones_recomendadas.sql`, carga el catálogo de `seed_catalogo.sql`
   y ejecuta `permisos_usuarios.sql`, que da acceso a todos los usuarios de
   Windows de la PC (grupo local *Usuarios*: `db_owner` en la base y
   `CREATE ANY DATABASE`, que SQL Server exige para verificar los respaldos).
   Todos los scripts se pueden correr varias veces: **reinstalar o actualizar
   conserva los datos**. El detalle queda en `instalacion_bd.log`, en la
   carpeta de la app.
5. Escribe `conexion.txt` con la cadena de conexión.

Al desinstalar se quita la aplicación, pero **la base de datos se conserva**.

## Estructura del proyecto

```
RefaccionariaApp/
  Program.cs                 Punto de entrada (y modo --instalar-bd del instalador)
  Data/
    ConexionBD.cs             Cadena de conexión (conexion.txt o la de por defecto)
    InstaladorBD.cs           Crea/actualiza la BD con los scripts de sql/
    BD.cs                     Helper genérico para llamar SPs / SQL directo
    RespaldoBD.cs             Respaldo completo (BACKUP DATABASE) verificado
  Forms/
    FormPrincipal.cs          Shell: barra lateral oscura + contenido embebido
    Tema.cs                   Paleta y helpers de estilo (acento rojo del logo, grids/botones planos)
    FormListaBase.cs          Base de listado: grid + buscador incremental + Nuevo/Editar/Eliminar
    FormEdicionBase.cs        Base del modal de alta/edición (Guardar/Cancelar)
    Paginador.cs              Barra de paginación reutilizable para los grids
    FormMarcas.cs             + FormMarcaEdicion.cs (modal)
    FormAnios.cs              + FormAnioEdicion.cs
    FormProveedores.cs        + FormProveedorEdicion.cs
    FormCategorias.cs         Tabla TipoRefaCat   + FormCategoriaEdicion.cs
    FormUnidades.cs           Tabla UnidadesCat   + FormUnidadEdicion.cs
    FormModelos.cs            Depende de Marca    + FormModeloEdicion.cs
    FormVersiones.cs          Depende de Modelo   + FormVersionEdicion.cs
    FormPartes.cs             Listado de refacciones (búsqueda en servidor) + FormParteEdicion.cs
    FormEquivalencias.cs      Refacciones equivalentes de una parte
    FormCotizaciones.cs       Listado + buscador incremental y filtro por fechas
    FormCotizacionNueva.cs    Modal de alta/edición de una cotización con su detalle y botón Generar PDF
    FormReporteExistencias.cs Reporte con filtros y exportación a Excel
    FormRespaldo.cs           Respaldo de la base de datos (menú Herramientas)
    FiltrosCombo.cs           Helpers de combos de filtro (Partes y Existencias)
  Reportes/
    CotizacionPdf.cs          PDF de una cotización (QuestPDF)
    ExistenciasExcel.cs       Excel del reporte de existencias (ClosedXML)
sql/
  script.sql                  Estructura y procedimientos almacenados
  correcciones_recomendadas.sql
  seed_catalogo.sql           Catálogo: años, marcas, modelos, versiones, categorías
  permisos_usuarios.sql       Acceso de los usuarios de Windows (lo usa el instalador)
instalador/
  Refaccionaria.iss           Script de Inno Setup
  generar_instalador.ps1      Publica la app y compila el instalador
```

## Patrón de las pantallas de entidad

Cada entidad se compone de dos piezas:

- **Listado** (`FormListaBase`): un grid con un **buscador incremental** arriba
  y los botones **Nuevo / Editar / Eliminar** abajo. Doble clic en una fila
  también edita. El grid se refresca al guardar en el modal. La búsqueda es en
  cliente por defecto (filtra en memoria); una pantalla con tablas grandes puede
  activar `BusquedaEnServidor` para re-consultar la base en cada cambio del texto
  (con un pequeño *debounce*), como hace **Partes**.
- **Modal de alta/edición** (`FormEdicionBase`): un diálogo con los campos y
  **Guardar / Cancelar**. La misma clase sirve para alta y edición: recibe la
  fila seleccionada (`DataRowView`); si es `null` es un alta, si trae datos es
  edición.

Para agregar una entidad nueva se definen dos clases pequeñas: una `: FormListaBase`
(datos, id, cómo crear el modal y cómo eliminar) y una `: FormEdicionBase` (los
controles, cómo cargar el registro y cómo guardar). Ejemplo mínimo: `FormMarcas` +
`FormMarcaEdicion`.

**Cotizaciones** no usa estas bases (tiene filtro por fechas y su alta/edición
es maestro-detalle), pero sigue el mismo patrón a mano: buscador incremental,
botón Editar y el alta/edición con su detalle en el modal `FormCotizacionNueva`.

## Qué usa cada pantalla

Cada pantalla llama a los procedimientos almacenados de tu script
(`spMarcaInsertar`, `spPartesModificar`, `spCotizacionesAlta`, etc.). Se
usó SQL directo (documentado con un comentario en el propio código) solo
en los puntos donde el script no traía el procedimiento necesario:

- **Proveedores**: no existe `spProveedoresEditar` → se actualiza con un
  `UPDATE` directo.
- **UnidadesCat**: no existen `spUnidadesCatMostrar` ni
  `spUnidadesCatEditar` → se listan y editan con SQL directo.
- **Modelo → Versión (combos en cascada)**: no hay un procedimiento para
  "versiones de un modelo" → se consulta la tabla `Version` filtrando por
  `modelo_id`.
- **Edición de Partes**: `spPartesMostrar`/`spPartesFiltro` solo devuelven
  nombres (Marca, Modelo, Categoría, etc.), no los ids de las llaves
  foráneas, así que el modal de edición carga la parte con `spPartePorId` y
  sus aplicaciones con `spParteAplicacionesMostrar`.

## Aplicaciones de partes (versión + años)

Una parte se liga a los vehículos en que sirve mediante **aplicaciones**: cada
renglón de `RelPartesVersion` dice "esta parte aplica a esta versión en los
años `anio_desde`–`anio_hasta`". Así se distinguen piezas que cambian por año
para la misma versión, por ejemplo:

| Parte          | Versión                | Años      |
|----------------|------------------------|-----------|
| Barra cardan A | Grand Cherokee Laredo  | 1999–2002 |
| Barra cardan B | Grand Cherokee Laredo  | 2003–2004 |

- En el alta/edición de partes se capturan con Marca → Modelo → Versión y
  Desde/Hasta (los años del modelo según `RelAnioModelo`, vía
  `sp_ObtenerAnios`). Una parte puede tener varias aplicaciones; para una
  misma versión los rangos no pueden empalmarse.
- `spParteAplicacionesGuardar` reemplaza todas las aplicaciones de la parte
  en una transacción (recibe un arreglo JSON). `spPartesModificar` ya solo
  actualiza los datos de la parte.
- `RelAnioModelo` sigue indicando en qué años existe cada modelo; ya no se
  usa para decidir a qué años aplica una parte.
- En el listado, la columna **Año** muestra los rangos reales de la parte y el
  filtro por año solo devuelve partes cuyo rango lo incluye **para esa misma
  versión**. El texto completo de las aplicaciones aparece en el panel de
  detalle.

## ⚠️ Errores encontrados en el script (importante)

Varios procedimientos de **borrado** comparan el parámetro consigo mismo
en vez de compararlo con la columna de la tabla, por ejemplo:

```sql
DELETE FROM Partes WHERE @id_parte = @id_parte   -- siempre verdadero
```

Esa condición siempre es verdadera, así que el procedimiento **borra
todas las filas de la tabla**, sin importar qué id le mandes. Están así en
tu script:

- `spPartesEliminar`
- `spRelAnioModeloEliminar`
- `spRelEquivalenciasEliminar`
- `spRelMarcaAnioEliminar`
- `spRelPartesVersionEliminar`
- `spRelVersionEliminar` (además, pese al nombre, borra de la tabla
  `Version`, no de una tabla de relación)
- `spUnidadesCatEliminar`

**La aplicación de Windows Forms no llama a ninguno de estos
procedimientos tal cual** — para esas operaciones de borrado usa una
instrucción SQL directa con el filtro correcto, así que es segura de
usar. Aun así, para que cualquier otra herramienta o procedimiento futuro
no borre la tabla completa por accidente, corrige los procedimientos en
SQL Server ejecutando `sql/correcciones_recomendadas.sql`.

Otras observaciones menores del script (no afectan a la aplicación, pero
vale la pena que las tengas en cuenta si sigues editando la base de
datos):

- `spModeloEditar` y `spVersionEditar` solo actualizan el `nombre`; no
  permiten reasignar la Marca de un Modelo ni el Modelo de una Versión
  una vez creados. Por eso, en las pantallas de Modelos y Versiones el
  combo de la relación se deshabilita al editar.
- `sp_ObtenerAnios` relacionaba `Modelo` y `Año` comparando directamente
  `id_modelo = id_anio`. Se corrigió en la sección de correcciones de
  `script.sql`: ahora devuelve los años del modelo a través de
  `RelAnioModelo`.
- Hay procedimientos duplicados con distinto nombre para lo mismo (por
  ejemplo `spModeloInsertar` y `sp_ModeloInsertar`, o `spParteBuscar` y
  `sp_ParteBuscar`). La aplicación usa siempre la versión más completa.
