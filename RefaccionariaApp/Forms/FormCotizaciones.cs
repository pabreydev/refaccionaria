using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Listado maestro-detalle de cotizaciones con buscador incremental (por
    /// cliente o folio). El alta y la edición se hacen en el modal
    /// <see cref="FormCotizacionNueva"/>.
    /// </summary>
    public class FormCotizaciones : Form
    {
        private readonly DataGridView dgvCotizaciones = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        private readonly DataGridView dgvDetalle = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        private readonly Paginador pagCotizaciones;
        private readonly Paginador pagDetalle;
        private readonly TextBox txtBuscar = new() { Left = 70, Top = 11, Width = 300 };

        public FormCotizaciones()
        {
            Text = "Cotizaciones";
            Width = 1000;
            Height = 680;
            StartPosition = FormStartPosition.CenterScreen;
            Font = Tema.FuenteBase;
            BackColor = Tema.Blanco;

            Tema.EstilizarGrid(dgvCotizaciones);
            Tema.EstilizarGrid(dgvDetalle);
            pagCotizaciones = new Paginador(dgvCotizaciones);
            pagDetalle = new Paginador(dgvDetalle, 25);

            var panelSuperior = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(12), BackColor = Tema.Blanco };
            txtBuscar.Left = 12; txtBuscar.Top = 14; txtBuscar.Width = 360;
            Tema.EstilizarBuscador(txtBuscar, "Buscar por cliente o folio...");
            txtBuscar.TextChanged += (s, e) => AplicarFiltro();
            panelSuperior.Controls.Add(txtBuscar);

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

            var splitPrincipal = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 320 };
            splitPrincipal.Panel1.Controls.Add(dgvCotizaciones);
            splitPrincipal.Panel1.Controls.Add(pagCotizaciones);
            var lblDetalle = new Label { Text = "Detalle de la cotización seleccionada:", Dock = DockStyle.Top, Height = 26, Padding = new Padding(5, 6, 0, 0), ForeColor = Tema.TextoSecundario, BackColor = Tema.Blanco };
            splitPrincipal.Panel2.Controls.Add(dgvDetalle);
            splitPrincipal.Panel2.Controls.Add(pagDetalle);
            splitPrincipal.Panel2.Controls.Add(lblDetalle);

            Controls.Add(splitPrincipal);
            Controls.Add(panelInferior);
            Controls.Add(panelSuperior);

            dgvCotizaciones.SelectionChanged += (s, e) => CargarDetalle();
            dgvCotizaciones.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditarSeleccionada(); };

            Load += (s, e) => CargarTodas(conservarPagina: false);
        }

        /// <summary>Recarga las cotizaciones conservando filtro, orden y (por defecto) la página.</summary>
        private void CargarTodas(bool conservarPagina = true)
        {
            pagCotizaciones.Cargar(BD.EjecutarConsulta("spCotizacionesConsulta"), conservarPagina);
        }

        private void AplicarFiltro()
        {
            if (pagCotizaciones.Datos == null) return;
            var texto = txtBuscar.Text.Trim().Replace("'", "''");
            pagCotizaciones.Filtro = string.IsNullOrEmpty(texto)
                ? null
                : $"cliente LIKE '%{texto}%' OR CONVERT(folio, 'System.String') LIKE '%{texto}%'";
        }

        private void CargarDetalle()
        {
            if (dgvCotizaciones.CurrentRow?.DataBoundItem is not DataRowView drv) { pagDetalle.Cargar(null); return; }
            var folio = Convert.ToInt32(drv["folio"]);
            pagDetalle.Cargar(BD.EjecutarConsulta("spCotizacionDetalleMostrar", new SqlParameter("@folio_cotizacion", folio)));
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
