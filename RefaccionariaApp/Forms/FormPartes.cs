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
    ///  - Filtros (Marca, Modelo, Versión, Año, Categoría): spPartesFiltro, que
    ///    recibe los nombres seleccionados (NULL = sin filtro). Se aplican al
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
            _cmbMarca = AgregarFiltro(panelFiltros, "Marca");
            _cmbModelo = AgregarFiltro(panelFiltros, "Modelo");
            _cmbVersion = AgregarFiltro(panelFiltros, "Versión");
            _cmbAnio = AgregarFiltro(panelFiltros, "Año");
            _cmbCategoria = AgregarFiltro(panelFiltros, "Categoría");

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

        private static ComboBox AgregarFiltro(FlowLayoutPanel panel, string etiqueta)
        {
            var contenedor = new Panel { Width = 150, Height = 46, Margin = new Padding(0, 0, 10, 0) };
            var cmb = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
            var lbl = new Label
            {
                Text = etiqueta,
                Dock = DockStyle.Top,
                Height = 18,
                ForeColor = Tema.TextoSecundario,
                Font = new Font("Segoe UI", 8.5F)
            };
            contenedor.Controls.Add(cmb);
            contenedor.Controls.Add(lbl);
            panel.Controls.Add(contenedor);
            return cmb;
        }

        // ---------- Carga de combos ----------

        private void CargarCombosFiltro()
        {
            try
            {
                _cargandoCombos = true;
                CargarCombo(_cmbMarca, Opciones(BD.EjecutarConsulta("spMarcaMostrar"), "id_marca", "nombre", "(Todas)"));
                CargarCombo(_cmbAnio, Opciones(BD.EjecutarConsulta("spAnioMostrar"), "id_anio", "nombre", "(Todos)"));
                CargarCombo(_cmbCategoria, Opciones(BD.EjecutarConsulta("spTipoRefaCatMostrar"), "id_tipoRefaCat", "nombre", "(Todas)"));
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
                var marca = IdDeCombo(_cmbMarca);
                var dt = marca == null
                    ? Opciones(BD.EjecutarConsulta("spModeloMostrar"), "id_modelo", "nombre", "(Todos)")
                    : Opciones(BD.EjecutarConsulta("sp_ObtenerModelos",
                          new SqlParameter("@IdMarca", marca.Value),
                          new SqlParameter("@IdModelo", DBNull.Value)), "id_modelo", "modelo", "(Todos)");
                CargarCombo(_cmbModelo, dt);
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
                var modelo = IdDeCombo(_cmbModelo);
                var dt = modelo == null
                    ? Opciones(BD.EjecutarConsulta("spVersionMostrar"), "version_id", "nombre", "(Todas)", nombresUnicos: true)
                    : Opciones(BD.EjecutarConsulta("sp_ObtenerVersiones",
                          new SqlParameter("@IdModelo", modelo.Value)), "version_id", "nombre", "(Todas)");
                CargarCombo(_cmbVersion, dt);
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

        /// <summary>
        /// Normaliza el resultado de un SP a columnas (id, nombre) con una primera
        /// opción "(Todas)" de id nulo.
        /// </summary>
        private static DataTable Opciones(DataTable origen, string colId, string colNombre, string textoTodos, bool nombresUnicos = false)
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

        private static void CargarCombo(ComboBox cmb, DataTable dt)
        {
            cmb.DisplayMember = "nombre";
            cmb.ValueMember = "id";
            cmb.DataSource = dt;
            cmb.SelectedIndex = 0;
        }

        private static int? IdDeCombo(ComboBox cmb) => cmb.SelectedValue is int v ? v : (int?)null;

        /// <summary>Nombre seleccionado, o null si está en "(Todas)".</summary>
        private static string NombreDeCombo(ComboBox cmb) => cmb.SelectedIndex > 0 ? cmb.Text : null;

        // ---------- Aplicar / limpiar ----------

        private void AplicarFiltros()
        {
            _fMarca = NombreDeCombo(_cmbMarca);
            _fModelo = NombreDeCombo(_cmbModelo);
            _fVersion = NombreDeCombo(_cmbVersion);
            _fAnio = NombreDeCombo(_cmbAnio);
            _fCategoria = NombreDeCombo(_cmbCategoria);
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

        protected override string[] ColumnasOcultas => new[] { "id_parte", "Path" };

        protected override FormEdicionBase CrearFormEdicion(DataRowView fila) => new FormParteEdicion(fila);

        protected override void EliminarRegistro(int id)
        {
            BD.EjecutarNoQuery("spPartesEliminar", new SqlParameter("@id_parte", id));
        }
    }
}
