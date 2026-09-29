using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>Modal de alta/edición de un Año.</summary>
    public class FormAnioEdicion : FormEdicionBase
    {
        private readonly TextBox txtNombre;

        public FormAnioEdicion(DataRowView fila) : base("Años", fila)
        {
            panelCampos.Controls.Add(new Label { Text = "Año:", Left = 12, Top = 18, Width = 70 });
            txtNombre = new TextBox { Left = 90, Top = 15, Width = 150 };
            panelCampos.Controls.Add(txtNombre);
        }

        protected override void CargarRegistro() => txtNombre.Text = Fila["nombre"].ToString();

        protected override void Guardar()
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
                throw new Exception("El año es obligatorio.");

            if (EsNuevo)
                BD.EjecutarEscalar("spAnioInsertar", new SqlParameter("@nombre", txtNombre.Text.Trim()));
            else
                BD.EjecutarNoQuery("spAnioEditar",
                    new SqlParameter("@id_anio", Convert.ToInt32(Fila["id_anio"])),
                    new SqlParameter("@nombre", txtNombre.Text.Trim()));
        }
    }
}
