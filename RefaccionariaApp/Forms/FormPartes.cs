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
    ///    marca/modelo/versión, los rangos de años de sus aplicaciones y el texto
    ///    completo de las aplicaciones, que solo se ve en el panel de detalle.
    ///    @filtro NULL = todas).
    ///  - Filtros (Marca, Modelo, Versión, Año, Categoría): spPartesFiltro, que
    ///    recibe los nombres seleccionados (NULL = sin filtro); el año debe caer
    ///    en el rango de la aplicación de esa misma versión. Se aplican al
    ///    presionar "Filtrar"; el texto del buscador se combina en memoria sobre
    ///    ese resultado (NumParte/Parte, igual que spPartesMostrar).
    ///  - Borrado: spPartesEliminar (borra en cascada RelPartesVersion,
    ///    RelEquivalencias y la parte, en una transacción).
    /// </summary>
    public class FormPartes : FormListaBase
    {
        private readonly PanelDetalleParte _panelDetalle = new PanelDetalleParte();
        private DataGridView _grid;

        private readonly ComboBox _cmbMarca, _cmbModelo, _cmbVersion, _cmbAnio, _cmbCategoria;
        private bool _cargandoCombos;

        // Filtros aplicados con el botón "Filtrar" (null = sin filtro).
        private string _fMarca, _fModelo, _fVersion, _fAnio, _fCategoria;
        private bool HayFiltros => _fMarca != null || _fModelo != null || _fVersion != null
                                   || _fAnio != null || _fCategoria != null;

        public FormPartes() : base("Partes (refacciones)")
        {
            Width = 1000;
            Height = 620;
            StartPosition = FormStartPosition.CenterScreen;

            // --- Filtros (debajo del buscador) ---
            var panelFiltros = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Padding = new Padding(12, 0, 12, 10),
                BackColor = Tema.Blanco
            };
            _cmbMarca = FiltrosCombo.Agregar(panelFiltros, "Marca");
            _cmbModelo = FiltrosCombo.Agregar(panelFiltros, "Modelo");
            _cmbVersion = FiltrosCombo.Agregar(panelFiltros, "Versión");
            _cmbAnio = FiltrosCombo.Agregar(panelFiltros, "Año");
            _cmbCategoria = FiltrosCombo.Agregar(panelFiltros, "Categoría");

            var btnFiltrar = new Button { Text = "Filtrar", Width = 100, Margin = new Padding(0, 18, 8, 0) };
            var btnLimpiar = new Button { Text = "Limpiar", Width = 90, Margin = new Padding(0, 18, 0, 0) };
            Tema.BotonPrimario(btnFiltrar);
            Tema.BotonSecundario(btnLimpiar);
            btnFiltrar.Click += (s, e) => AplicarFiltros();
            btnLimpiar.Click += (s, e) => LimpiarFiltros();
            panelFiltros.Controls.Add(btnFiltrar);
            panelFiltros.Controls.Add(btnLimpiar);

            _cmbMarca.SelectedIndexChanged += (s, e) => { if (!_cargandoCombos) CargarModelos(); };
            _cmbModelo.SelectedIndexChanged += (s, e) => { if (!_cargandoCombos) CargarVersiones(); };

            // Se acopla después del buscador y antes del grid (Fill).
            Controls.Add(panelFiltros);
            Controls.SetChildIndex(panelFiltros, Controls.GetChildIndex(dgv) + 1);

            _panelDetalle.CerrarClick += (s, e) => _panelDetalle.Ocultar();
            Controls.Add(_panelDetalle);
            _panelDetalle.SendToBack(); // se acopla primero al borde derecho

            Load += (s, e) => CargarCombosFiltro();
        }

        // ---------- Carga de combos ----------

        private void CargarCombosFiltro()
        {
            try
            {
                _cargandoCombos = true;
                FiltrosCombo.Cargar(_cmbMarca, FiltrosCombo.Opciones(BD.EjecutarConsulta("spMarcaMostrar"), "id_marca", "nombre", "(Todas)"));
                FiltrosCombo.Cargar(_cmbAnio, FiltrosCombo.Opciones(BD.EjecutarConsulta("spAnioMostrar"), "id_anio", "nombre", "(Todos)"));
                FiltrosCombo.Cargar(_cmbCategoria, FiltrosCombo.Opciones(BD.EjecutarConsulta("spTipoRefaCatMostrar"), "id_tipoRefaCat", "nombre", "(Todas)"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudieron cargar los filtros: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _cargandoCombos = false;
            }
            CargarModelos();
        }

        /// <summary>Modelos de la marca seleccionada, o todos si no hay marca.</summary>
        private void CargarModelos()
        {
            try
            {
                _cargandoCombos = true;
                var marca = FiltrosCombo.IdDe(_cmbMarca);
                var dt = marca == null
                    ? FiltrosCombo.Opciones(BD.EjecutarConsulta("spModeloMostrar"), "id_modelo", "nombre", "(Todos)")
                    : FiltrosCombo.Opciones(BD.EjecutarConsulta("sp_ObtenerModelos",
                          new SqlParameter("@IdMarca", marca.Value),
                          new SqlParameter("@IdModelo", DBNull.Value)), "id_modelo", "modelo", "(Todos)");
                FiltrosCombo.Cargar(_cmbModelo, dt);
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudieron cargar los modelos: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _cargandoCombos = false;
            }
            CargarVersiones();
        }

        /// <summary>
        /// Versiones del modelo seleccionado. Sin modelo se listan todas, sin
        /// repetir nombres (el filtro del SP es por nombre).
        /// </summary>
        private void CargarVersiones()
        {
            try
            {
                _cargandoCombos = true;
                var modelo = FiltrosCombo.IdDe(_cmbModelo);
                var dt = modelo == null
                    ? FiltrosCombo.Opciones(BD.EjecutarConsulta("spVersionMostrar"), "version_id", "nombre", "(Todas)", nombresUnicos: true)
                    : FiltrosCombo.Opciones(BD.EjecutarConsulta("sp_ObtenerVersiones",
                          new SqlParameter("@IdModelo", modelo.Value)), "version_id", "nombre", "(Todas)");
                FiltrosCombo.Cargar(_cmbVersion, dt);
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudieron cargar las versiones: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _cargandoCombos = false;
            }
        }

        // ---------- Aplicar / limpiar ----------

        private void AplicarFiltros()
        {
            _fMarca = FiltrosCombo.NombreDe(_cmbMarca);
            _fModelo = FiltrosCombo.NombreDe(_cmbModelo);
            _fVersion = FiltrosCombo.NombreDe(_cmbVersion);
            _fAnio = FiltrosCombo.NombreDe(_cmbAnio);
            _fCategoria = FiltrosCombo.NombreDe(_cmbCategoria);
            RecargarSeguro();
        }

        private void LimpiarFiltros()
        {
            _cargandoCombos = true;
            if (_cmbMarca.Items.Count > 0) _cmbMarca.SelectedIndex = 0;
            if (_cmbAnio.Items.Count > 0) _cmbAnio.SelectedIndex = 0;
            if (_cmbCategoria.Items.Count > 0) _cmbCategoria.SelectedIndex = 0;
            _cargandoCombos = false;
            CargarModelos();

            _fMarca = _fModelo = _fVersion = _fAnio = _fCategoria = null;
            RecargarSeguro();
        }

        private void RecargarSeguro()
        {
            try
            {
                RecargarDatos(conservarPagina: false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo filtrar: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        protected override DataTable ObtenerDatos(string filtro) => HayFiltros ? ConsultaFiltrada(filtro) : Consulta(filtro);

        private static DataTable Consulta(string filtro)
        {
            object valorFiltro = string.IsNullOrWhiteSpace(filtro) ? (object)DBNull.Value : filtro;
            return BD.EjecutarConsulta("spPartesMostrar", new SqlParameter("@filtro", valorFiltro));
        }

        private DataTable ConsultaFiltrada(string filtro)
        {
            var dt = BD.EjecutarConsulta("spPartesFiltro",
                new SqlParameter("@Marca", (object)_fMarca ?? DBNull.Value),
                new SqlParameter("@Modelo", (object)_fModelo ?? DBNull.Value),
                new SqlParameter("@Version", (object)_fVersion ?? DBNull.Value),
                new SqlParameter("@Anio", (object)_fAnio ?? DBNull.Value),
                new SqlParameter("@Categoria", (object)_fCategoria ?? DBNull.Value));

            // spPartesFiltro no recibe texto: el buscador se aplica aquí con el
            // mismo criterio que spPartesMostrar (num_parte o nombre contiene).
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                foreach (var fila in dt.Rows.Cast<DataRow>().ToList())
                {
                    bool coincide = Contiene(fila["NumParte"], filtro) || Contiene(fila["Parte"], filtro);
                    if (!coincide) dt.Rows.Remove(fila);
                }
            }
            return dt;
        }

        private static bool Contiene(object valor, string texto) =>
            valor != DBNull.Value && valor.ToString().Contains(texto, StringComparison.OrdinalIgnoreCase);

        protected override string NombreCampoId => "id_parte";

        protected override string[] ColumnasOcultas => new[] { "id_parte", "Path", "Aplicaciones" };

        protected override FormEdicionBase CrearFormEdicion(DataRowView fila) => new FormParteEdicion(fila);

        protected override void EliminarRegistro(int id)
        {
            BD.EjecutarNoQuery("spPartesEliminar", new SqlParameter("@id_parte", id));
        }
    }
}
