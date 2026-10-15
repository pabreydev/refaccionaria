using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Listado de cotizaciones con buscador incremental (por cliente o folio) y
    /// filtro por rango de fechas. El detalle se consulta, da de alta y edita en
    /// el modal <see cref="FormCotizacionNueva"/>.
    /// </summary>
    public class FormCotizaciones : Form
    {
        private readonly DataGridView dgvCotizaciones = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        private readonly Paginador pagCotizaciones;
        private readonly TextBox txtBuscar = new();
        private readonly DateTimePicker dtpDesde = new(), dtpHasta = new();

        public FormCotizaciones()
        {
            Text = "Cotizaciones";
            Width = 1000;
            Height = 680;
            StartPosition = FormStartPosition.CenterScreen;
            Font = Tema.FuenteBase;
            BackColor = Tema.Blanco;

            Tema.EstilizarGrid(dgvCotizaciones);
            pagCotizaciones = new Paginador(dgvCotizaciones);

            var panelSuperior = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Padding = new Padding(12, 10, 12, 10),
                BackColor = Tema.Blanco
            };
            Tema.EstilizarBuscador(txtBuscar, "Buscar por cliente o folio...");
            txtBuscar.TextChanged += (s, e) => AplicarFiltro();
            FiltrosCombo.AgregarConEtiqueta(panelSuperior, "Buscar", txtBuscar, ancho: 300);

            // Rango de fechas: la casilla de cada selector activa ese límite.
            foreach (var dtp in new[] { dtpDesde, dtpHasta })
            {
                dtp.Format = DateTimePickerFormat.Short;
                dtp.ShowCheckBox = true;
                dtp.Checked = false;
                dtp.ValueChanged += (s, e) => AplicarFiltro();
            }
            FiltrosCombo.AgregarConEtiqueta(panelSuperior, "Fecha desde", dtpDesde, ancho: 140);
            FiltrosCombo.AgregarConEtiqueta(panelSuperior, "Fecha hasta", dtpHasta, ancho: 140);

            var btnLimpiar = new Button { Text = "Limpiar", Width = 90, Margin = new Padding(0, 18, 0, 0) };
            Tema.BotonSecundario(btnLimpiar);
            btnLimpiar.Click += (s, e) => LimpiarFiltros();
            panelSuperior.Controls.Add(btnLimpiar);

            var panelInferior = new Panel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(12, 0, 12, 0), BackColor = Tema.Blanco };
            var btnNueva = new Button { Text = "+ Nueva cotización", Left = 12, Top = 11, Width = 160 };
            btnNueva.Click += (s, e) => AbrirEdicion(null);
            var btnEditar = new Button { Text = "Editar", Left = 180, Top = 11, Width = 100 };
            btnEditar.Click += (s, e) => EditarSeleccionada();
            var btnEliminar = new Button { Text = "Eliminar", Left = 288, Top = 11, Width = 100 };
            btnEliminar.Click += (s, e) => Eliminar();
            Tema.BotonPrimario(btnNueva);
            Tema.BotonSecundario(btnEditar);
            Tema.BotonSecundario(btnEliminar);
            panelInferior.Controls.Add(btnNueva);
            panelInferior.Controls.Add(btnEditar);
            panelInferior.Controls.Add(btnEliminar);

            Controls.Add(dgvCotizaciones);
            Controls.Add(pagCotizaciones);
            Controls.Add(panelInferior);
            Controls.Add(panelSuperior);

            dgvCotizaciones.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditarSeleccionada(); };

            Load += (s, e) => CargarTodas(conservarPagina: false);
        }

        /// <summary>Recarga las cotizaciones conservando filtro, orden y (por defecto) la página.</summary>
        private void CargarTodas(bool conservarPagina = true)
        {
            pagCotizaciones.Cargar(BD.EjecutarConsulta("spCotizacionesConsulta"), conservarPagina);
        }

        /// <summary>Combina el texto del buscador con el rango de fechas activo.</summary>
        private void AplicarFiltro()
        {
            if (pagCotizaciones.Datos == null) return;

            var condiciones = new List<string>();
            var texto = txtBuscar.Text.Trim().Replace("'", "''");
            if (!string.IsNullOrEmpty(texto))
                condiciones.Add($"(cliente LIKE '%{texto}%' OR CONVERT(folio, 'System.String') LIKE '%{texto}%')");

            // fecha es datetime: "hasta" incluye todo ese día (< día siguiente).
            if (dtpDesde.Checked)
                condiciones.Add($"fecha >= #{FechaFiltro(dtpDesde.Value.Date)}#");
            if (dtpHasta.Checked)
                condiciones.Add($"fecha < #{FechaFiltro(dtpHasta.Value.Date.AddDays(1))}#");

            pagCotizaciones.Filtro = condiciones.Count == 0 ? null : string.Join(" AND ", condiciones);
        }

        /// <summary>Fecha en el formato invariable que espera DataView.RowFilter.</summary>
        private static string FechaFiltro(DateTime fecha) =>
            fecha.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

        private void LimpiarFiltros()
        {
            txtBuscar.Clear();
            dtpDesde.Checked = false;
            dtpHasta.Checked = false;
            AplicarFiltro();
        }

        private int? FolioSeleccionado()
        {
            if (dgvCotizaciones.CurrentRow?.DataBoundItem is not DataRowView drv) return null;
            return Convert.ToInt32(drv["folio"]);
        }

        private void AbrirEdicion(int? folio)
        {
            using var f = new FormCotizacionNueva(folio);
            if (f.ShowDialog(TopLevelControl) == DialogResult.OK)
                CargarTodas();
        }

        private void EditarSeleccionada()
        {
            var folio = FolioSeleccionado();
            if (folio == null)
            {
                MessageBox.Show("Selecciona primero una cotización de la lista.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            AbrirEdicion(folio);
        }

        private void Eliminar()
        {
            var folio = FolioSeleccionado();
            if (folio == null)
            {
                MessageBox.Show("Selecciona primero una cotización de la lista.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show($"¿Eliminar la cotización con folio {folio}?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                // Se borra primero el detalle y luego la cabecera (el script no
                // define llave foránea que lo haga en cascada).
                BD.EjecutarNoQuery("spCotizacionDetalleBaja", new SqlParameter("@folio_cotizacion", folio.Value));
                BD.EjecutarNoQuery("spCotizacionesBaja", new SqlParameter("@folio", folio.Value));
                CargarTodas();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo eliminar: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
