using System.Data;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>Administra UnidadesCat con alta/edición en modal (vía stored procedures).</summary>
    public class FormUnidades : FormListaBase
    {
        public FormUnidades() : base("Unidades") { }

        protected override DataTable ObtenerDatos() => BD.EjecutarConsulta("spUnidadesCatMostrar");

        protected override string NombreCampoId => "id_unidadesCat";

        protected override FormEdicionBase CrearFormEdicion(DataRowView fila) => new FormUnidadEdicion(fila);

        protected override void EliminarRegistro(int id) =>
            BD.EjecutarNoQuery("spUnidadesCatEliminar", new SqlParameter("@id_unidadesCat", id));
    }
}
