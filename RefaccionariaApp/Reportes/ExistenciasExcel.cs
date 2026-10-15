using System.Data;
using ClosedXML.Excel;

namespace RefaccionariaApp.Reportes
{
    /// <summary>
    /// Genera el archivo de Excel (.xlsx) del reporte de existencias: título,
    /// fecha y filtros aplicados, y la tabla con los registros. Usa ClosedXML
    /// (licencia MIT).
    /// </summary>
    public static class ExistenciasExcel
    {
        /// <param name="datos">Registros a exportar (ya filtrados y ordenados).</param>
        /// <param name="filtros">Descripción de los filtros aplicados, o vacío.</param>
        public static void Generar(DataTable datos, string filtros, string ruta)
        {
            using var libro = new XLWorkbook();
            var hoja = libro.Worksheets.Add("Existencias");

            hoja.Cell(1, 1).Value = "Reporte de existencias";
            hoja.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
            hoja.Cell(2, 1).Value = "Generado: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            hoja.Cell(3, 1).Value = "Filtros: " + (string.IsNullOrEmpty(filtros) ? "ninguno" : filtros);

            const int filaTabla = 5;
            var tabla = hoja.Cell(filaTabla, 1).InsertTable(datos, "Existencias", true);
            tabla.Theme = XLTableTheme.TableStyleLight9;

            hoja.Columns(1, datos.Columns.Count).AdjustToContents(filaTabla, filaTabla + datos.Rows.Count);
            hoja.SheetView.FreezeRows(filaTabla);

            libro.SaveAs(ruta);
        }
    }
}
