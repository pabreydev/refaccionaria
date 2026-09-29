using System.Data;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>Administra la tabla TipoRefaCat (categorías de refacción) con alta/edición en modal.</summary>
    public class FormCategorias : FormListaBase
    {
        public FormCategorias() : base("Categorías de refacción") { }

        protected override DataTable ObtenerDatos() => BD.EjecutarConsulta("spTipoRefaCatMostrar");

        protected override string NombreCampoId => "id_tipoRefaCat";

        protected override FormEdicionBase CrearFormEdicion(DataRowView fila) => new FormCategoriaEdicion(fila);

        protected override void EliminarRegistro(int id) =>
            BD.EjecutarNoQuery("spTipoRefaCatEliminar", new SqlParameter("@id_tipoRefaCat", id));
    }
}
