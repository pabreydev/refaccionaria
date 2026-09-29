using System.Data;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>Pantalla de listado de Años con alta/edición en modal.</summary>
    public class FormAnios : FormListaBase
    {
        public FormAnios() : base("Años") { }

        protected override DataTable ObtenerDatos() => BD.EjecutarConsulta("spAnioMostrar");

        protected override string NombreCampoId => "id_anio";

        protected override FormEdicionBase CrearFormEdicion(DataRowView fila) => new FormAnioEdicion(fila);

        protected override void EliminarRegistro(int id) =>
            BD.EjecutarNoQuery("spAnioEliminar", new SqlParameter("@anio", id));
    }
}
