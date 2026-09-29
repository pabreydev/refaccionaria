using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Modal de alta/edición de un Modelo. Al editar, la marca no se puede
    /// reasignar (spModeloEditar solo actualiza el nombre): el combo se muestra
    /// deshabilitado con la marca actual.
    /// </summary>
    public class FormModeloEdicion : FormEdicionBase
    {
        private readonly ComboBox cmbMarca;
        private readonly TextBox txtNombre;

        public FormModeloEdicion(DataRowView fila) : base("Modelos", fila)
        {
            panelCampos.Controls.Add(new Label { Text = "Marca:", Left = 12, Top = 18, Width = 70 });
            cmbMarca = new ComboBox { Left = 90, Top = 15, Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            panelCampos.Controls.Add(cmbMarca);

            panelCampos.Controls.Add(new Label { Text = "Nombre:", Left = 12, Top = 52, Width = 70 });
            txtNombre = new TextBox { Left = 90, Top = 49, Width = 320 };
            panelCampos.Controls.Add(txtNombre);

            var dt = BD.EjecutarConsulta("spMarcaMostrar");
            cmbMarca.DataSource = dt;
            cmbMarca.DisplayMember = "nombre";
            cmbMarca.ValueMember = "id_marca";
            cmbMarca.SelectedIndex = dt.Rows.Count > 0 ? 0 : -1;
        }

        protected override void CargarRegistro()
        {
            txtNombre.Text = Fila["nombre"].ToString();
            cmbMarca.SelectedValue = Convert.ToInt32(Fila["id_marca"]);
            cmbMarca.Enabled = false; // la marca no se reasigna al editar
        }

        protected override void Guardar()
        {
            if (cmbMarca.SelectedValue == null)
                throw new Exception("Selecciona una marca.");
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
                throw new Exception("El nombre es obligatorio.");

            if (EsNuevo)
                BD.EjecutarEscalar("spModeloInsertar",
                    new SqlParameter("@nombre", txtNombre.Text.Trim()),
                    new SqlParameter("@marca", (int)cmbMarca.SelectedValue));
            else
                BD.EjecutarNoQuery("spModeloEditar",
                    new SqlParameter("@id_modelo", Convert.ToInt32(Fila["id_modelo"])),
                    new SqlParameter("@nombre", txtNombre.Text.Trim()));
        }
    }
}
