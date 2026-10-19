using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>Modal de alta/edición de un Proveedor.</summary>
    public class FormProveedorEdicion : FormEdicionBase
    {
        private readonly TextBox txtNombre, txtTelefono;

        public FormProveedorEdicion(DataRowView fila) : base("Proveedores", fila)
        {
            panelCampos.Controls.Add(new Label { Text = "Nombre:", Left = 12, Top = 18, Width = 70 });
            txtNombre = new TextBox { Left = 90, Top = 15, Width = 320 };
            panelCampos.Controls.Add(txtNombre);

            panelCampos.Controls.Add(new Label { Text = "Teléfono:", Left = 12, Top = 52, Width = 70 });
            txtTelefono = new TextBox { Left = 90, Top = 49, Width = 140, MaxLength = 10 };
            // Solo dígitos (se permiten teclas de control como Retroceso y Ctrl+V).
            txtTelefono.KeyPress += (s, e) => e.Handled = !char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar);
            panelCampos.Controls.Add(txtTelefono);
        }

        protected override void CargarRegistro()
        {
            txtNombre.Text = Fila["nombre"].ToString();
            txtTelefono.Text = Fila["telefono"]?.ToString().Trim();
        }

        protected override void Guardar()
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
                throw new Exception("El nombre es obligatorio.");

            string telefono = txtTelefono.Text.Trim();
            if (telefono.Length > 0 && (telefono.Length != 10 || !telefono.All(char.IsDigit)))
                throw new Exception("El teléfono debe tener 10 dígitos.");
            object paramTelefono = telefono.Length > 0 ? telefono : DBNull.Value;

            if (EsNuevo)
            {
                BD.EjecutarEscalar("spProveedoresInsertar",
                    new SqlParameter("@nombre", txtNombre.Text.Trim()),
                    new SqlParameter("@telefono", paramTelefono));
            }
            else
            {
                BD.EjecutarNoQuery("spProveedoresEditar",
                    new SqlParameter("@id_proveedor", Convert.ToInt32(Fila["id_proveedor"])),
                    new SqlParameter("@nombre", txtNombre.Text.Trim()),
                    new SqlParameter("@telefono", paramTelefono));
            }
        }
    }
}
