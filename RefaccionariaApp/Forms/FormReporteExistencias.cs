using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;
using RefaccionariaApp.Reportes;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Reporte de existencias con filtros por Parte, Marca, Modelo, Año y Categoría.
    ///
    /// spReporteExistencias solo recibe @existenciasMinimas y no devuelve la
    /// compatibilidad ni la categoría, así que se usa spPartesFiltro (filtra por
    /// Marca/Modelo/Año/Categoría por nombre, NULL = sin filtro). El texto de
    /// Parte (número o nombre) y el tope de existencias se aplican en memoria
    /// sobre ese resultado, con el mismo criterio que spReporteExistencias.
    /// </summary>
    public class FormReporteExistencias : Form
    {
        private readonly NumericUpDown numMinimo = new() { Width = 90, Maximum = 1000000, Margin = new Padding(0, 18, 10, 0) };
        private readonly CheckBox chkFiltrar = new() { Text = "Existencias menores o iguales a:", AutoSize = true, Margin = new Padding(0, 20, 4, 0) };
        private readonly TextBox txtParte = new();
        private readonly ComboBox cmbMarca, cmbModelo, cmbAnio, cmbCategoria;
        private readonly DataGridView dgv = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        private readonly Paginador paginador;
        private bool cargandoCombos;
        private string filtrosAplicados = ""; // de la última consulta, para el Excel

        public FormReporteExistencias()
        {
            Text = "Reporte de existencias";
            Width = 1050;
            Height = 600;
            StartPosition = FormStartPosition.CenterScreen;
            Font = Tema.FuenteBase;
            BackColor = Tema.Blanco;

            Tema.EstilizarGrid(dgv);
            paginador = new Paginador(dgv);

            var panelFiltros = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Padding = new Padding(12, 10, 12, 10),
                BackColor = Tema.Blanco
            };

            Tema.EstilizarBuscador(txtParte, "Número o nombre...");
            FiltrosCombo.AgregarConEtiqueta(panelFiltros, "Parte", txtParte, ancho: 180);
            cmbMarca = FiltrosCombo.Agregar(panelFiltros, "Marca");
            cmbModelo = FiltrosCombo.Agregar(panelFiltros, "Modelo");
            cmbAnio = FiltrosCombo.Agregar(panelFiltros, "Año");
            cmbCategoria = FiltrosCombo.Agregar(panelFiltros, "Categoría");

            panelFiltros.Controls.Add(chkFiltrar);
            panelFiltros.Controls.Add(numMinimo);

            var btnConsultar = new Button { Text = "Consultar", Width = 110, Margin = new Padding(0, 18, 8, 0) };
            var btnLimpiar = new Button { Text = "Limpiar", Width = 90, Margin = new Padding(0, 18, 8, 0) };
            var btnExcel = new Button { Text = "Exportar a Excel", Width = 140, Margin = new Padding(0, 18, 0, 0) };
            Tema.BotonPrimario(btnConsultar);
            Tema.BotonSecundario(btnLimpiar);
            Tema.BotonSecundario(btnExcel);
            btnConsultar.Click += (s, e) => Consultar();
            btnLimpiar.Click += (s, e) => LimpiarFiltros();
            btnExcel.Click += (s, e) => ExportarExcel();
            panelFiltros.Controls.Add(btnConsultar);
            panelFiltros.Controls.Add(btnLimpiar);
            panelFiltros.Controls.Add(btnExcel);

            txtParte.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                Consultar();
            };
            cmbMarca.SelectedIndexChanged += (s, e) => { if (!cargandoCombos) CargarModelos(); };

            Controls.Add(dgv);
            Controls.Add(paginador);
            Controls.Add(panelFiltros);

            Load += (s, e) =>
            {
                CargarCombosFiltro();
                Consultar();
            };
        }

        // ---------- Combos ----------

        private void CargarCombosFiltro()
        {
            try
            {
                cargandoCombos = true;
                FiltrosCombo.Cargar(cmbMarca, FiltrosCombo.Opciones(BD.EjecutarConsulta("spMarcaMostrar"), "id_marca", "nombre", "(Todas)"));
                FiltrosCombo.Cargar(cmbAnio, FiltrosCombo.Opciones(BD.EjecutarConsulta("spAnioMostrar"), "id_anio", "nombre", "(Todos)"));
                FiltrosCombo.Cargar(cmbCategoria, FiltrosCombo.Opciones(BD.EjecutarConsulta("spTipoRefaCatMostrar"), "id_tipoRefaCat", "nombre", "(Todas)"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudieron cargar los filtros: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                cargandoCombos = false;
            }
            CargarModelos();
        }

        /// <summary>Modelos de la marca seleccionada, o todos si no hay marca.</summary>
        private void CargarModelos()
        {
            try
            {
                cargandoCombos = true;
                var marca = FiltrosCombo.IdDe(cmbMarca);
                var dt = marca == null
                    ? FiltrosCombo.Opciones(BD.EjecutarConsulta("spModeloMostrar"), "id_modelo", "nombre", "(Todos)", nombresUnicos: true)
                    : FiltrosCombo.Opciones(BD.EjecutarConsulta("sp_ObtenerModelos",
                          new SqlParameter("@IdMarca", marca.Value),
                          new SqlParameter("@IdModelo", DBNull.Value)), "id_modelo", "modelo", "(Todos)");
                FiltrosCombo.Cargar(cmbModelo, dt);
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudieron cargar los modelos: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                cargandoCombos = false;
            }
        }

        private void LimpiarFiltros()
        {
            txtParte.Clear();
            chkFiltrar.Checked = false;
            numMinimo.Value = 0;
            cargandoCombos = true;
            if (cmbMarca.Items.Count > 0) cmbMarca.SelectedIndex = 0;
            if (cmbAnio.Items.Count > 0) cmbAnio.SelectedIndex = 0;
            if (cmbCategoria.Items.Count > 0) cmbCategoria.SelectedIndex = 0;
            cargandoCombos = false;
            CargarModelos();
            Consultar();
        }

        // ---------- Consulta ----------

        private void Consultar()
        {
            try
            {
                var origen = BD.EjecutarConsulta("spPartesFiltro",
                    new SqlParameter("@Marca", (object)FiltrosCombo.NombreDe(cmbMarca) ?? DBNull.Value),
                    new SqlParameter("@Modelo", (object)FiltrosCombo.NombreDe(cmbModelo) ?? DBNull.Value),
                    new SqlParameter("@Version", DBNull.Value),
                    new SqlParameter("@Anio", (object)FiltrosCombo.NombreDe(cmbAnio) ?? DBNull.Value),
                    new SqlParameter("@Categoria", (object)FiltrosCombo.NombreDe(cmbCategoria) ?? DBNull.Value));

                paginador.Cargar(ArmarReporte(origen));
                filtrosAplicados = DescribirFiltros();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo consultar el reporte: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Aplica el filtro de Parte y de existencias, y deja las columnas del
        /// reporte ordenadas por existencias y nombre (como spReporteExistencias).
        /// </summary>
        private DataTable ArmarReporte(DataTable origen)
        {
            var dt = new DataTable();
            dt.Columns.Add("Número de Parte", typeof(string));
            dt.Columns.Add("Nombre", typeof(string));
            dt.Columns.Add("Descripción", typeof(string));
            dt.Columns.Add("Categoría", typeof(string));
            dt.Columns.Add("Marca", typeof(string));
            dt.Columns.Add("Modelo", typeof(string));
            dt.Columns.Add("Año", typeof(string));
            dt.Columns.Add("Existencias", typeof(double));

            string parte = txtParte.Text.Trim();
            double? maximo = chkFiltrar.Checked ? (double)numMinimo.Value : null;

            foreach (DataRow r in origen.Rows)
            {
                if (parte.Length > 0 && !Contiene(r["NumParte"], parte) && !Contiene(r["Parte"], parte)) continue;

                object existencias = r["Existencias"];
                if (maximo != null && (existencias == DBNull.Value || Convert.ToDouble(existencias) > maximo)) continue;

                dt.Rows.Add(r["NumParte"], r["Parte"], r["Descripción"], r["Categoria"],
                            r["Marca"], r["Modelo"], r["Año"], existencias);
            }

            dt.DefaultView.Sort = "Existencias ASC, Nombre ASC";
            return dt.DefaultView.ToTable();
        }

        private static bool Contiene(object valor, string texto) =>
            valor != DBNull.Value && valor.ToString().Contains(texto, StringComparison.OrdinalIgnoreCase);

        // ---------- Exportar ----------

        /// <summary>Texto con los filtros actuales, p. ej. "Marca: Nissan; Año: 2015".</summary>
        private string DescribirFiltros()
        {
            var partes = new List<string>();
            if (txtParte.Text.Trim().Length > 0) partes.Add("Parte: " + txtParte.Text.Trim());
            if (FiltrosCombo.NombreDe(cmbMarca) is string marca) partes.Add("Marca: " + marca);
            if (FiltrosCombo.NombreDe(cmbModelo) is string modelo) partes.Add("Modelo: " + modelo);
            if (FiltrosCombo.NombreDe(cmbAnio) is string anio) partes.Add("Año: " + anio);
            if (FiltrosCombo.NombreDe(cmbCategoria) is string categoria) partes.Add("Categoría: " + categoria);
            if (chkFiltrar.Checked) partes.Add("Existencias <= " + numMinimo.Value);
            return string.Join("; ", partes);
        }

        /// <summary>
        /// Exporta todos los registros de la última consulta (no solo la página
        /// visible), con el orden actual del grid.
        /// </summary>
        private void ExportarExcel()
        {
            var datos = paginador.DatosVisibles;
            if (datos == null || datos.Rows.Count == 0)
            {
                MessageBox.Show("No hay registros para exportar.", "Exportar a Excel", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "Libro de Excel|*.xlsx",
                FileName = $"ReporteExistencias_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                ExistenciasExcel.Generar(datos, filtrosAplicados, sfd.FileName);
                Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo exportar a Excel: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
