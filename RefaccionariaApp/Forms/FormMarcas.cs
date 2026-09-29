using System.Data;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Pantalla de listado de Marcas: grilla con buscador incremental y alta/
    /// edición en el modal <see cref="FormMarcaEdicion"/>.
    /// </summary>
    public class FormMarcas : FormListaBase
    {
        public FormMarcas() : base("Marcas") { }

        protected override DataTable ObtenerDatos() => BD.EjecutarConsulta("spMarcaMostrar");

        protected override string NombreCampoId => "id_marca";

        protected override FormEdicionBase CrearFormEdicion(DataRowView fila) => new FormMarcaEdicion(fila);

        protected override void EliminarRegistro(int id) =>
            BD.EjecutarNoQuery("spMarcaEliminar", new SqlParameter("@marca", id));
    }
}
