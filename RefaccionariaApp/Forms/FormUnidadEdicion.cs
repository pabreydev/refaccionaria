using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>Modal de alta/edición de una Unidad (UnidadesCat).</summary>
    public class FormUnidadEdicion : FormEdicionBase
    {
        private readonly TextBox txtNombre, txtDescripcion;

        public FormUnidadEdicion(DataRowView fila) : base("Unidades", fila)
        {
            panelCampos.Controls.Add(new Label { Text = "Nombre:", Left = 12, Top = 18, Width = 80 });
            txtNombre = new TextBox { Left = 100, Top = 15, Width = 200 };
            panelCampos.Controls.Add(txtNombre);

            panelCampos.Controls.Add(new Label { Text = "Descripción:", Left = 12, Top = 52, Width = 80 });
            txtDescripcion = new TextBox { Left = 100, Top = 49, Width = 320 };
            panelCampos.Controls.Add(txtDescripcion);
        }

        protected override void CargarRegistro()
        {
            txtNombre.Text = Fila["nombre"].ToString();
            txtDescripcion.Text = Fila["descripcion"]?.ToString();
        }

        protected override void Guardar()
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
                throw new Exception("El nombre es obligatorio.");

            if (EsNuevo)
            {
                BD.EjecutarEscalar("spUnidadesCatInsertar",
                    new SqlParameter("@nombre", txtNombre.Text.Trim()),
                    new SqlParameter("@descripcion", (object)txtDescripcion.Text.Trim() ?? DBNull.Value));
            }
            else
            {
                BD.EjecutarNoQuery("spUnidadesCatEditar",
                    new SqlParameter("@id_unidadesCat", Convert.ToInt32(Fila["id_unidadesCat"])),
                    new SqlParameter("@nombre", txtNombre.Text.Trim()),
                    new SqlParameter("@descripcion", (object)txtDescripcion.Text.Trim() ?? DBNull.Value));
            }
        }
    }
}
