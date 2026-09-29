using System.Data;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>Pantalla de listado de Versiones con alta/edición en modal.</summary>
    public class FormVersiones : FormListaBase
    {
        public FormVersiones() : base("Versiones") { }

        protected override DataTable ObtenerDatos() => BD.EjecutarConsulta("spVersionMostrarConModelo");

        protected override string NombreCampoId => "version_id";

        // modelo_id solo se usa para preseleccionar el combo al editar; no se muestra.
        protected override string[] ColumnasOcultas => new[] { "modelo_id" };

        protected override FormEdicionBase CrearFormEdicion(DataRowView fila) => new FormVersionEdicion(fila);

        protected override void EliminarRegistro(int id) =>
            BD.EjecutarNoQuery("spVersionEliminar", new SqlParameter("@version", id));
    }
}
