using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Modal de alta/edición de una Versión. Al editar, el modelo no se puede
    /// reasignar (spVersionEditar solo actualiza el nombre): el combo se muestra
    /// deshabilitado con el modelo actual.
    /// </summary>
    public class FormVersionEdicion : FormEdicionBase
    {
        private readonly ComboBox cmbModelo;
        private readonly TextBox txtNombre;

        public FormVersionEdicion(DataRowView fila) : base("Versiones", fila)
        {
            panelCampos.Controls.Add(new Label { Text = "Modelo:", Left = 12, Top = 18, Width = 70 });
            cmbModelo = new ComboBox { Left = 90, Top = 15, Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            panelCampos.Controls.Add(cmbModelo);

            panelCampos.Controls.Add(new Label { Text = "Nombre:", Left = 12, Top = 52, Width = 70 });
            txtNombre = new TextBox { Left = 90, Top = 49, Width = 320 };
            panelCampos.Controls.Add(txtNombre);

            var dt = BD.EjecutarConsulta("spModeloMostrar");
            cmbModelo.DataSource = dt;
            cmbModelo.DisplayMember = "nombre";
            cmbModelo.ValueMember = "id_modelo";
            cmbModelo.SelectedIndex = dt.Rows.Count > 0 ? 0 : -1;
        }

        protected override void CargarRegistro()
        {
            txtNombre.Text = Fila["nombre"].ToString();
            cmbModelo.SelectedValue = Convert.ToInt32(Fila["modelo_id"]);
            cmbModelo.Enabled = false; // el modelo no se reasigna al editar
        }

        protected override void Guardar()
        {
            if (cmbModelo.SelectedValue == null)
                throw new Exception("Selecciona un modelo.");
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
                throw new Exception("El nombre es obligatorio.");

            if (EsNuevo)
                BD.EjecutarEscalar("spVersionInsertar",
                    new SqlParameter("@modelo_id", (int)cmbModelo.SelectedValue),
                    new SqlParameter("@nombre", txtNombre.Text.Trim()));
            else
                BD.EjecutarNoQuery("spVersionEditar",
                    new SqlParameter("@version_id", Convert.ToInt32(Fila["version_id"])),
                    new SqlParameter("@nombre", txtNombre.Text.Trim()));
        }
    }
}
