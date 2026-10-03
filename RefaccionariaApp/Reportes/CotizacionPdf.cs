using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace RefaccionariaApp.Reportes
{
    /// <summary>
    /// Genera el PDF de una cotización: encabezado con logo, nombre del negocio,
    /// cliente y fechas; tabla de partes; y totales (Subtotal, IVA y Total).
    /// Usa QuestPDF (licencia Community).
    /// </summary>
    public static class CotizacionPdf
    {
        public class Linea
        {
            public string Codigo { get; set; }
            public string Producto { get; set; }
            public int Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal DescuentoPorc { get; set; }
            public decimal Subtotal { get; set; }
        }

        public class Datos
        {
            public int Folio { get; set; }
            public string Cliente { get; set; }
            public DateTime Fecha { get; set; }
            public DateTime Vigencia { get; set; }
            public List<Linea> Lineas { get; set; } = new();
            public decimal Subtotal { get; set; }
            public decimal Iva { get; set; }
            public decimal Total { get; set; }
        }

        private static readonly CultureInfo Mx = CultureInfo.GetCultureInfo("es-MX");
        private const string GrisEncabezado = "#E0E0E0";
        private const string GrisLinea = "#E6E6E6";

        public static void Generar(Datos d, string rutaPdf) => Crear(d).GeneratePdf(rutaPdf);

        public static IDocument Crear(Datos d)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            byte[] logo = null;
            var rutaLogo = Path.Combine(Application.StartupPath, "Imagenes", "logo.jpeg");
            if (File.Exists(rutaLogo)) logo = File.ReadAllBytes(rutaLogo);

            bool hayDescuentos = d.Lineas.Any(l => l.DescuentoPorc > 0);

            return Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(40);
                    page.DefaultTextStyle(t => t.FontSize(11));

                    // --- Encabezado ---
                    page.Header().PaddingBottom(30).Row(row =>
                    {
                        if (logo != null)
                            row.ConstantItem(110).Height(75).Image(logo).FitArea();

                        row.RelativeItem().PaddingLeft(15).Column(col =>
                        {
                            col.Item().Text("MANGUERAS Y REFACCIONES").FontSize(17).Bold();
                            col.Item().Text($"Atención a: {d.Cliente}");
                            col.Item().Text($"Fecha: {d.Fecha:dd/MM/yyyy}");
                        });

                        row.ConstantItem(150).PaddingLeft(10).Column(col =>
                        {
                            col.Item().Text($"COTIZACIÓN N° {d.Folio}").FontSize(15).Bold();
                            col.Item().Text($"Vigencia: {d.Vigencia:dd/MM/yyyy}");
                        });
                    });

                    // --- Tabla de partes y totales ---
                    page.Content().Column(col =>
                    {
                        col.Item().Table(tabla =>
                        {
                            tabla.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);   // Código
                                c.RelativeColumn(5);   // Producto
                                c.RelativeColumn(1.3f);  // Cant.
                                c.RelativeColumn(2);   // P. Unit
                                if (hayDescuentos) c.RelativeColumn(1.3f); // Desc.
                                c.RelativeColumn(2);   // Subtotal
                            });

                            tabla.Header(h =>
                            {
                                Encabezado(h.Cell(), "Código");
                                Encabezado(h.Cell(), "Producto");
                                Encabezado(h.Cell(), "Cant.");
                                Encabezado(h.Cell(), "P. Unit");
                                if (hayDescuentos) Encabezado(h.Cell(), "Desc.");
                                Encabezado(h.Cell(), "Subtotal");
                            });

                            foreach (var l in d.Lineas)
                            {
                                Celda(tabla.Cell(), l.Codigo);
                                Celda(tabla.Cell(), l.Producto);
                                Celda(tabla.Cell(), l.Cantidad.ToString(Mx));
                                Celda(tabla.Cell(), Moneda(l.PrecioUnitario));
                                if (hayDescuentos) Celda(tabla.Cell(), $"{l.DescuentoPorc:0.##}%");
                                Celda(tabla.Cell(), Moneda(l.Subtotal));
                            }
                        });

                        col.Item().PaddingTop(15).AlignRight().Column(t =>
                        {
                            t.Item().Text($"Subtotal: {Moneda(d.Subtotal)}");
                            t.Item().Text($"IVA (16%): {Moneda(d.Iva)}");
                            t.Item().Text($"TOTAL: {Moneda(d.Total)} MXN").FontSize(14).Bold();
                        });
                    });
                });
            });
        }

        private static string Moneda(decimal valor) => valor.ToString("C2", Mx);

        private static void Encabezado(IContainer celda, string texto) =>
            celda.Background(GrisEncabezado).PaddingVertical(8).PaddingHorizontal(8).Text(texto).Bold();

        private static void Celda(IContainer celda, string texto) =>
            celda.BorderBottom(1).BorderColor(GrisLinea).PaddingVertical(8).PaddingHorizontal(8).Text(texto ?? "");
    }
}
