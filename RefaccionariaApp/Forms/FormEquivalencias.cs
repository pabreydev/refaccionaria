using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Administra RelEquivalencias para una parte. spRelEquivalenciasEliminar
    /// tiene el mismo error de comparación "@param = @param" que otros
    /// procedimientos del script (borraría toda la tabla), así que aquí se
    /// quita la relación con SQL directo, filtrando por las dos partes
    /// involucradas.
    /// </summary>
    public class FormEquivalencias : Form
    {
        private readonly int idParte;
        private readonly DataGridView dgv = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        private readonly Paginador paginador;
        private readonly ComboBox cmbParte = new() { DropDownStyle = ComboBoxStyle.DropDownList, Left = 10, Top = 12, Width = 400 };

        public FormEquivalencias(int idParte, string nombreParte)
        {
            this.idParte = idParte;
            Text = $"Equivalencias de: {nombreParte}";
            Width = 600;
            Height = 450;
            StartPosition = FormStartPosition.CenterParent;
            Font = Tema.FuenteBase;
            BackColor = Tema.Blanco;

            Tema.EstilizarGrid(dgv);
            paginador = new Paginador(dgv, 25);

            var panelSuperior = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(12), BackColor = Tema.Blanco };
            cmbParte.Top = 14;
            panelSuperior.Controls.Add(cmbParte);
            var btnAgregar = new Button { Text = "Agregar", Left = 420, Top = 11, Width = 100 };
            Tema.BotonPrimario(btnAgregar);
            btnAgregar.Click += (s, e) => Agregar();
            panelSuperior.Controls.Add(btnAgregar);

            var panelInferior = new Panel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(12, 0, 12, 0), BackColor = Tema.Blanco };
            var btnQuitar = new Button { Text = "Quitar seleccionada", Left = 12, Top = 11, Width = 160 };
            btnQuitar.Click += (s, e) => Quitar();
            var btnCerrar = new Button { Text = "Cerrar", Left = 180, Top = 11, Width = 90 };
            btnCerrar.Click += (s, e) => Close();
            Tema.BotonSecundario(btnQuitar);
            Tema.BotonSecundario(btnCerrar);
            panelInferior.Controls.Add(btnQuitar);
            panelInferior.Controls.Add(btnCerrar);

            Controls.Add(dgv);
            Controls.Add(paginador);
            Controls.Add(panelInferior);
            Controls.Add(panelSuperior);

            Load += (s, e) => { CargarComboPartes(); CargarEquivalencias(conservarPagina: false); };
        }

        private void CargarComboPartes()
        {
            var dt = BD.EjecutarConsulta("sp_NumPartesMostrar");
            dt.DefaultView.RowFilter = $"id_parte <> {idParte}";
            cmbParte.DataSource = dt.DefaultView;
            cmbParte.DisplayMember = "idNombre";
            cmbParte.ValueMember = "id_parte";
        }

        private void CargarEquivalencias(bool conservarPagina = true)
        {
            paginador.Cargar(BD.EjecutarConsulta("spEquivalentesMostrar", new SqlParameter("@idparte", idParte)), conservarPagina);
        }

        private void Agregar()
        {
            if (cmbParte.SelectedValue == null) return;
            try
            {
                BD.EjecutarEscalar("spRelEquivalenciasInsertar",
                    new SqlParameter("@parte_id", idParte),
                    new SqlParameter("@equivalente_id", (int)cmbParte.SelectedValue));
                CargarEquivalencias();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo agregar: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Quitar()
        {
            if (dgv.CurrentRow?.DataBoundItem is not DataRowView drv) return;
            var otraParte = Convert.ToInt32(drv["id_parte"]);
            try
            {
                BD.EjecutarNoQuery("spRelEquivalenciasEliminarPorPar",
                    new SqlParameter("@parte_id", idParte),
                    new SqlParameter("@equivalente_id", otraParte));
                CargarEquivalencias();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo quitar: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
