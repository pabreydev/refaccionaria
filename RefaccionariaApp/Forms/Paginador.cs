using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Barra de paginación reutilizable para una <see cref="DataGridView"/>
    /// alimentada con un <see cref="DataTable"/>. Se acopla abajo de la grilla y
    /// muestra "Mostrando X–Y de Z", los botones de navegación y el tamaño de
    /// página.
    ///
    /// La paginación es en cliente: los stored procedures devuelven el resultado
    /// completo y aquí se recorta la página visible. El filtro (<see cref="Filtro"/>)
    /// y el orden por columna se aplican sobre el total, no solo sobre la página,
    /// por eso el ordenamiento al hacer clic en el encabezado se maneja aquí en
    /// lugar de dejarlo a la grilla.
    ///
    /// La grilla se enlaza a una tabla de página (copia de las filas visibles);
    /// los <see cref="DataRowView"/> que entrega sirven para leer valores, no
    /// para modificar la tabla original.
    /// </summary>
    public class Paginador : Panel
    {
        private static readonly int[] TamanosPagina = { 25, 50, 100, 200 };

        private readonly DataGridView grid;
        private readonly BindingSource bsPagina = new BindingSource();
        private readonly Label lblResumen, lblPagina;
        private readonly Button btnPrimera, btnAnterior, btnSiguiente, btnUltima;
        private readonly ComboBox cmbTamano;

        private DataView vista;
        private DataTable tablaPagina;
        private int paginaActual; // base 0
        private int tamanoPagina;
        private string columnaOrden;
        private ListSortDirection direccionOrden;

        public Paginador(DataGridView grid, int tamanoPagina = 50)
        {
            this.grid = grid;
            this.tamanoPagina = tamanoPagina;

            Dock = DockStyle.Bottom;
            Height = 46;
            BackColor = Tema.Blanco;

            // Todos los elementos van en un solo grupo centrado bajo la grilla.
            // Las etiquetas tienen ancho fijo para que el grupo no se mueva
            // cuando cambia el texto ("Mostrando 1–50 de 62", "Página 1 de 2").
            var flujo = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                BackColor = Tema.Blanco
            };

            lblResumen = new Label { AutoSize = false, Width = 190, Height = 34, Margin = new Padding(0, 0, 16, 0), TextAlign = ContentAlignment.MiddleRight, ForeColor = Tema.TextoSecundario };
            var lblTamano = new Label { Text = "Filas:", AutoSize = false, Width = 58, Height = 34, Margin = new Padding(16, 0, 0, 0), TextAlign = ContentAlignment.MiddleRight, ForeColor = Tema.TextoSecundario };
            cmbTamano = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 60 };
            cmbTamano.Margin = new Padding(4, Math.Max(0, (34 - cmbTamano.Height) / 2), 0, 0);
            foreach (var t in TamanosPagina.Union(new[] { tamanoPagina }).OrderBy(t => t))
                cmbTamano.Items.Add(t);
            cmbTamano.SelectedItem = tamanoPagina;
            cmbTamano.SelectedIndexChanged += (s, e) =>
            {
                this.tamanoPagina = (int)cmbTamano.SelectedItem;
                paginaActual = 0;
                Mostrar();
            };

            btnPrimera = CrearBoton("«", () => paginaActual = 0);
            btnAnterior = CrearBoton("‹", () => paginaActual--);
            lblPagina = new Label { AutoSize = false, Width = 120, Height = 34, Margin = new Padding(2, 0, 2, 0), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Tema.TextoPrimario };
            btnSiguiente = CrearBoton("›", () => paginaActual++);
            btnUltima = CrearBoton("»", () => paginaActual = TotalPaginas - 1);

            flujo.Controls.AddRange(new Control[] { lblResumen, btnPrimera, btnAnterior, lblPagina, btnSiguiente, btnUltima, lblTamano, cmbTamano });
            Controls.Add(flujo);

            void Centrar()
            {
                flujo.Left = Math.Max(0, (ClientSize.Width - flujo.Width) / 2);
                flujo.Top = Math.Max(0, (ClientSize.Height - flujo.Height) / 2);
            }
            Resize += (s, e) => Centrar();
            flujo.SizeChanged += (s, e) => Centrar();

            grid.DataSource = bsPagina;
            grid.ColumnHeaderMouseClick += OrdenarPorColumna;
            grid.DataBindingComplete += (s, e) => AjustarIndicadoresOrden();

            Mostrar();
        }

        /// <summary>Tabla completa (sin paginar) cargada actualmente, o null.</summary>
        public DataTable Datos => vista?.Table;

        /// <summary>
        /// Filtro con la sintaxis de <see cref="DataView.RowFilter"/>, aplicado
        /// sobre todos los registros. Al cambiarlo se regresa a la primera página.
        /// </summary>
        public string Filtro
        {
            get => vista?.RowFilter;
            set
            {
                if (vista == null) return;
                vista.RowFilter = value;
                paginaActual = 0;
                Mostrar();
            }
        }

        /// <summary>
        /// Carga un nuevo resultado conservando el filtro y el orden actuales.
        /// Con <paramref name="conservarPagina"/> se queda en la misma página
        /// (útil tras editar o eliminar); si no, vuelve a la primera.
        /// </summary>
        public void Cargar(DataTable datos, bool conservarPagina = false)
        {
            var filtro = vista?.RowFilter;
            vista = datos == null ? null : new DataView(datos);

            if (vista != null)
            {
                if (!string.IsNullOrEmpty(filtro)) vista.RowFilter = filtro;
                if (columnaOrden != null && datos.Columns.Contains(columnaOrden))
                    vista.Sort = ExpresionOrden();
                else
                    columnaOrden = null;
            }

            // Solo se re-enlaza la grilla cuando cambia el esquema; así no se
            // regeneran las columnas (ni se pierden las ocultas) en cada página.
            if (datos == null)
            {
                tablaPagina = null;
                bsPagina.DataSource = null;
            }
            else if (tablaPagina == null || !MismoEsquema(tablaPagina, datos))
            {
                tablaPagina = datos.Clone();
                bsPagina.DataSource = tablaPagina;
            }

            if (!conservarPagina) paginaActual = 0;
            Mostrar();
        }

        private int TotalRegistros => vista?.Count ?? 0;

        private int TotalPaginas => Math.Max(1, (TotalRegistros + tamanoPagina - 1) / tamanoPagina);

        private void Mostrar()
        {
            int total = TotalRegistros;
            paginaActual = Math.Clamp(paginaActual, 0, TotalPaginas - 1);
            int inicio = paginaActual * tamanoPagina;
            int fin = Math.Min(inicio + tamanoPagina, total);

            if (tablaPagina != null)
            {
                bsPagina.RaiseListChangedEvents = false;
                tablaPagina.BeginLoadData();
                tablaPagina.Clear();
                for (int i = inicio; i < fin; i++)
                    tablaPagina.ImportRow(vista[i].Row);
                tablaPagina.EndLoadData();
                bsPagina.RaiseListChangedEvents = true;
                bsPagina.ResetBindings(false);
            }

            lblResumen.Text = total == 0 ? "Sin registros" : $"Mostrando {inicio + 1}–{fin} de {total}";
            lblPagina.Text = $"Página {paginaActual + 1} de {TotalPaginas}";
            btnPrimera.Enabled = btnAnterior.Enabled = paginaActual > 0;
            btnSiguiente.Enabled = btnUltima.Enabled = paginaActual < TotalPaginas - 1;
        }

        private void OrdenarPorColumna(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (vista == null || e.ColumnIndex < 0) return;
            var campo = grid.Columns[e.ColumnIndex].DataPropertyName;
            if (string.IsNullOrEmpty(campo) || !vista.Table.Columns.Contains(campo)) return;

            direccionOrden = campo == columnaOrden && direccionOrden == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
            columnaOrden = campo;
            vista.Sort = ExpresionOrden();
            paginaActual = 0;
            Mostrar();
        }

        private void AjustarIndicadoresOrden()
        {
            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (col.SortMode == DataGridViewColumnSortMode.NotSortable) continue;
                col.SortMode = DataGridViewColumnSortMode.Programmatic;
                col.HeaderCell.SortGlyphDirection = col.DataPropertyName != columnaOrden
                    ? SortOrder.None
                    : direccionOrden == ListSortDirection.Ascending ? SortOrder.Ascending : SortOrder.Descending;
            }
        }

        private string ExpresionOrden() =>
            $"[{columnaOrden.Replace("]", "\\]")}] {(direccionOrden == ListSortDirection.Ascending ? "ASC" : "DESC")}";

        private static bool MismoEsquema(DataTable a, DataTable b) =>
            a.Columns.Count == b.Columns.Count &&
            a.Columns.Cast<DataColumn>().Zip(b.Columns.Cast<DataColumn>())
             .All(p => p.First.ColumnName == p.Second.ColumnName && p.First.DataType == p.Second.DataType);

        private Button CrearBoton(string texto, Action mover)
        {
            var b = new Button { Text = texto, Width = 36, Margin = new Padding(2, 0, 2, 0) };
            Tema.BotonSecundario(b);
            b.Click += (s, e) => { mover(); Mostrar(); };
            return b;
        }
    }
}
