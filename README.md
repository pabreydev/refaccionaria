# Refaccionaria - Proyecto de Windows Forms (C#)

Aplicación de escritorio en C# / Windows Forms (.NET 8) que administra la
base de datos `Refaccionaria` (SQL Server) creada por `script.sql`.

## Requisitos

- Visual Studio 2022 (o superior) con la carga de trabajo ".NET desktop
  development".
- SDK de .NET 8.
- SQL Server (Express, Developer o similar) con la base de datos
  `Refaccionaria` ya creada a partir de tu `script.sql`.

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
   `.\SQLEXPRESS`.
5. Compila y ejecuta (F5). Restaura el paquete NuGet
   `Microsoft.Data.SqlClient` automáticamente al compilar.

## Estructura del proyecto

```
RefaccionariaApp/
  Program.cs                 Punto de entrada
  Data/
    ConexionBD.cs             Cadena de conexión
    BD.cs                     Helper genérico para llamar SPs / SQL directo
  Forms/
    FormPrincipal.cs          Shell: barra lateral oscura + contenido embebido
    Tema.cs                   Paleta y helpers de estilo (acento turquesa, grids/botones planos)
    FormListaBase.cs          Base de listado: grid + buscador incremental + Nuevo/Editar/Eliminar
    FormEdicionBase.cs        Base del modal de alta/edición (Guardar/Cancelar)
    FormMarcas.cs             + FormMarcaEdicion.cs (modal)
    FormAnios.cs              + FormAnioEdicion.cs
    FormProveedores.cs        + FormProveedorEdicion.cs
    FormCategorias.cs         Tabla TipoRefaCat   + FormCategoriaEdicion.cs
    FormUnidades.cs           Tabla UnidadesCat   + FormUnidadEdicion.cs
    FormModelos.cs            Depende de Marca    + FormModeloEdicion.cs
    FormVersiones.cs          Depende de Modelo   + FormVersionEdicion.cs
    FormPartes.cs             Listado de refacciones (búsqueda en servidor) + FormParteEdicion.cs
    FormEquivalencias.cs      Refacciones equivalentes de una parte
    FormCotizaciones.cs       Listado maestro-detalle + buscador incremental
    FormCotizacionNueva.cs    Modal de alta/edición de una cotización con su detalle
    FormReporteExistencias.cs
sql/
  correcciones_recomendadas.sql
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

**Cotizaciones** no usa estas bases porque es maestro-detalle (dos grids), pero
sigue el mismo patrón a mano: buscador incremental, botón Editar y el alta/edición
en el modal `FormCotizacionNueva`.

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
  foráneas, así que al seleccionar una parte para editar se hace una
  consulta directa a la tabla `Partes` (y a `RelPartesVersion`) para
  precargar los combos correctamente.
- **`spPartesInsertar`** no admite `unidad_id` (solo se agregó a
  `spPartesModificar`), así que al dar de alta una parte se completa la
  unidad con un `UPDATE` inmediatamente después del alta.

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
- `sp_ObtenerAnios` relaciona `Modelo` y `Año` comparando directamente
  `id_modelo = id_anio`, lo cual no corresponde con el modelo de datos
  (la relación real es a través de la tabla `RelAnioModelo`). Por esa
  razón no se construyó una pantalla de "compatibilidad Año/Modelo/Marca"
  automática; si me confirmas cómo debe funcionar esa relación, puedo
  agregarla.
- Hay procedimientos duplicados con distinto nombre para lo mismo (por
  ejemplo `spModeloInsertar` y `sp_ModeloInsertar`, o `spParteBuscar` y
  `sp_ParteBuscar`). La aplicación usa siempre la versión más completa.
