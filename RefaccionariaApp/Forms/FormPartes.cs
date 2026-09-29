using System.Data;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Pantalla de listado de Partes (refacciones). Usa búsqueda en servidor
    /// porque la tabla puede ser grande y los datos deben verse siempre frescos.
    ///
    /// Toda la lógica vive en stored procedures:
    ///  - Listado/búsqueda: spPartesMostrar @filtro (una fila por parte; agrega
    ///    marca/modelo/versión y el año como rango. @filtro NULL = todas).
    ///  - Borrado: spPartesEliminar (borra en cascada RelPartesVersion,
    ///    RelEquivalencias y la parte, en una transacción).
    /// </summary>
    public class FormPartes : FormListaBase
    {
        private readonly PanelDetalleParte _panelDetalle = new PanelDetalleParte();
        private DataGridView _grid;

        public FormPartes() : base("Partes (refacciones)")
        {
            Width = 1000;
            Height = 620;
            StartPosition = FormStartPosition.CenterScreen;

            _panelDetalle.CerrarClick += (s, e) => _panelDetalle.Ocultar();
            Controls.Add(_panelDetalle);
            _panelDetalle.SendToBack(); // se acopla primero al borde derecho
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _grid = BuscarGrid(this);
            if (_grid == null) return;

            _grid.CellClick += (s, ev) =>
            {
                if (ev.RowIndex >= 0) MostrarDetalle();
            };
            _grid.SelectionChanged += (s, ev) =>
            {
                if (_panelDetalle.Visible) MostrarDetalle(); // navegación con teclado / nueva búsqueda
            };
        }

        private void MostrarDetalle()
        {
            if (_grid.CurrentRow?.DataBoundItem is DataRowView fila)
                _panelDetalle.Mostrar(fila);
            else
                _panelDetalle.Ocultar();
        }

        private static DataGridView BuscarGrid(Control padre)
        {
            foreach (Control c in padre.Controls)
            {
                if (c is DataGridView g) return g;
                DataGridView hijo = BuscarGrid(c);
                if (hijo != null) return hijo;
            }
            return null;
        }

        protected override bool BusquedaEnServidor => true;

        protected override DataTable ObtenerDatos() => Consulta(null);

        protected override DataTable ObtenerDatos(string filtro) => Consulta(filtro);

        private static DataTable Consulta(string filtro)
        {
            object valorFiltro = string.IsNullOrWhiteSpace(filtro) ? (object)DBNull.Value : filtro;
            return BD.EjecutarConsulta("spPartesMostrar", new SqlParameter("@filtro", valorFiltro));
        }

        protected override string NombreCampoId => "id_parte";

        protected override string[] ColumnasOcultas => new[] { "id_parte", "Path" };

        protected override FormEdicionBase CrearFormEdicion(DataRowView fila) => new FormParteEdicion(fila);

        protected override void EliminarRegistro(int id)
        {
            BD.EjecutarNoQuery("spPartesEliminar", new SqlParameter("@id_parte", id));
        }
    }
}
