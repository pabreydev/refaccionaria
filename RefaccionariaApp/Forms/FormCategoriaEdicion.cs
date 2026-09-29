using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>Modal de alta/edición de una Categoría de refacción (TipoRefaCat).</summary>
    public class FormCategoriaEdicion : FormEdicionBase
    {
        private readonly TextBox txtNombre;

        public FormCategoriaEdicion(DataRowView fila) : base("Categorías de refacción", fila)
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
                BD.EjecutarEscalar("spTipoRefaCatInsertar", new SqlParameter("@nombre", txtNombre.Text.Trim()));
            else
                // spCategoriaEditar es, pese al nombre, el procedimiento de edición para TipoRefaCat.
                BD.EjecutarNoQuery("spCategoriaEditar",
                    new SqlParameter("@id_tipoRefaCat", Convert.ToInt32(Fila["id_tipoRefaCat"])),
                    new SqlParameter("@nombre", txtNombre.Text.Trim()));
        }
    }
}
