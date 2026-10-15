using System.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Utilidades para los combos de filtro (Marca, Modelo, Año, etc.) que
    /// comparten Partes y el Reporte de existencias.
    /// </summary>
    public static class FiltrosCombo
    {
        /// <summary>Agrega al panel un combo con su etiqueta encima.</summary>
        public static ComboBox Agregar(FlowLayoutPanel panel, string etiqueta)
        {
            var cmb = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
            AgregarConEtiqueta(panel, etiqueta, cmb);
            return cmb;
        }

        /// <summary>Agrega al panel cualquier control con una etiqueta encima.</summary>
        public static void AgregarConEtiqueta(FlowLayoutPanel panel, string etiqueta, Control control, int ancho = 150)
        {
            var contenedor = new Panel { Width = ancho, Height = 46, Margin = new Padding(0, 0, 10, 0) };
            control.Dock = DockStyle.Top;
            var lbl = new Label
            {
                Text = etiqueta,
                Dock = DockStyle.Top,
                Height = 18,
                ForeColor = Tema.TextoSecundario,
                Font = new Font("Segoe UI", 8.5F)
            };
            contenedor.Controls.Add(control);
            contenedor.Controls.Add(lbl);
            panel.Controls.Add(contenedor);
        }

        /// <summary>
        /// Normaliza el resultado de un SP a columnas (id, nombre) con una primera
        /// opción "(Todas)" de id nulo.
        /// </summary>
        public static DataTable Opciones(DataTable origen, string colId, string colNombre, string textoTodos, bool nombresUnicos = false)
        {
            var dt = new DataTable();
            dt.Columns.Add("id", typeof(int));
            dt.Columns.Add("nombre", typeof(string));
            dt.Rows.Add(DBNull.Value, textoTodos);

            var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in origen.Rows)
            {
                if (r[colNombre] == DBNull.Value) continue;
                var nombre = r[colNombre].ToString();
                if (nombresUnicos && !vistos.Add(nombre)) continue;
                dt.Rows.Add(r[colId], nombre);
            }
            return dt;
        }

        public static void Cargar(ComboBox cmb, DataTable dt)
        {
            cmb.DisplayMember = "nombre";
            cmb.ValueMember = "id";
            cmb.DataSource = dt;
            cmb.SelectedIndex = 0;
        }

        public static int? IdDe(ComboBox cmb) => cmb.SelectedValue is int v ? v : (int?)null;

        /// <summary>Nombre seleccionado, o null si está en "(Todas)".</summary>
        public static string NombreDe(ComboBox cmb) => cmb.SelectedIndex > 0 ? cmb.Text : null;
    }
}
