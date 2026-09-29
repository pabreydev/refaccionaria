using System.Data;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>Pantalla de listado de Modelos con alta/edición en modal.</summary>
    public class FormModelos : FormListaBase
    {
        public FormModelos() : base("Modelos") { }

        protected override DataTable ObtenerDatos() => BD.EjecutarConsulta("spModeloMostrarConMarca");

        protected override string NombreCampoId => "id_modelo";

        // id_marca solo se usa para preseleccionar el combo al editar; no se muestra.
        protected override string[] ColumnasOcultas => new[] { "id_marca" };

        protected override FormEdicionBase CrearFormEdicion(DataRowView fila) => new FormModeloEdicion(fila);

        protected override void EliminarRegistro(int id) =>
            BD.EjecutarNoQuery("spModeloEliminar", new SqlParameter("@modelo", id));
    }
}
