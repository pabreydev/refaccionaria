# CLAUDE.md

Contexto del proyecto para Claude Code. La documentación general (puesta en
marcha, estructura, SPs con errores) está en [README.md](README.md); aquí solo
va lo que hace falta para trabajar en el código.

## Proyecto

- App de escritorio **Windows Forms (.NET 8, C#)** para la base de datos
  `Refaccionaria` en **SQL Server**. Todo el código, la UI y los comentarios
  están en **español**.
- Compilar: `dotnet build RefaccionariaApp/RefaccionariaApp.csproj`. Si la app
  está abierta, el build falla al copiar `RefaccionariaApp.exe` (MSB3021); para
  solo verificar el código compila con `-o <carpeta temporal>`.
- Cadena de conexión en `RefaccionariaApp/Data/ConexionBD.cs` (cambia según la
  PC; no subir cambios de servidor por accidente).

## Arquitectura y convenciones de código

- La UI se construye **en código**, sin diseñador (`*.Designer.cs`). Estilos
  centralizados en `Forms/Tema.cs` (`EstilizarGrid`, `BotonPrimario`,
  `BotonSecundario`, `EstilizarBuscador`); úsalos en controles nuevos.
- Acceso a datos con `Data/BD.cs`: siempre por **stored procedures**; SQL
  directo solo donde el SP no existe o tiene errores, documentado con un
  comentario. Varios SPs de borrado del script borran la tabla completa
  (ver README) — no llamarlos.
- Catálogos: una clase `: FormListaBase` (listado) + una `: FormEdicionBase`
  (modal de alta/edición). Ejemplo mínimo: `FormMarcas` + `FormMarcaEdicion`.
- **Paginación**: todo grid alimentado con un `DataTable` usa `Forms/Paginador.cs`
  (paginación en cliente). Cargar datos con `paginador.Cargar(dt, conservarPagina)`
  y filtrar con `paginador.Filtro` (sintaxis de `DataView.RowFilter`), no
  asignando `dgv.DataSource` directamente. El orden por columna lo maneja el
  paginador sobre todos los registros.
- Docking en WinForms: agregar primero el control `Fill` y después los
  `Top`/`Bottom` (el último agregado se acopla primero).

## Convención de versionado (commits)

Mensajes de commit en español, con una o varias etiquetas en minúsculas
seguidas de `:` y una descripción breve:

| Etiqueta   | Uso                                   |
|------------|---------------------------------------|
| `feat`     | Nuevas funcionalidades                |
| `refactor` | Refactorización de funcionalidades    |
| `fix`      | Corrección de errores                 |
| `docs`     | Cambios en la documentación del proyecto |

Ejemplos:

```
feat: se agregó paginación en los grids
refactor, fix: se centró el control de paginación y se arregló la alineación de componentes
```

Con varias etiquetas, se separan con `, ` en el mismo prefijo.
