using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>Modal de alta/edición de un Proveedor.</summary>
    public class FormProveedorEdicion : FormEdicionBase
    {
        private readonly TextBox txtNombre;

        public FormProveedorEdicion(DataRowView fila) : base("Proveedores", fila)
        {
            panelCampos.Controls.Add(new Label { Text = "Nombre:", Left = 12, Top = 18, Width = 70 });
            txtNombre = new TextBox { Left = 90, Top = 15, Width = 320 };
            panelCampos.Controls.Add(txtNombre);
        }

        protected override void CargarRegistro() => txtNombre.Text = Fila["nombre"].ToString();

        protected override void Guardar()
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
                throw new Exception("El nombre es obligatorio.");

            if (EsNuevo)
            {
                BD.EjecutarEscalar("spProveedoresInsertar", new SqlParameter("@nombre", txtNombre.Text.Trim()));
            }
            else
            {
                BD.EjecutarNoQuery("spProveedoresEditar",
                    new SqlParameter("@id_proveedor", Convert.ToInt32(Fila["id_proveedor"])),
                    new SqlParameter("@nombre", txtNombre.Text.Trim()));
            }
        }
    }
}
