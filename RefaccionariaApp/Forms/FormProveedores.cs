using System.Data;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>Pantalla de listado de Proveedores con alta/edición en modal.</summary>
    public class FormProveedores : FormListaBase
    {
        public FormProveedores() : base("Proveedores") { }

        protected override DataTable ObtenerDatos() => BD.EjecutarConsulta("spProveedoresMostrar");

        protected override string NombreCampoId => "id_proveedor";

        protected override FormEdicionBase CrearFormEdicion(DataRowView fila) => new FormProveedorEdicion(fila);

        protected override void EliminarRegistro(int id) =>
            BD.EjecutarNoQuery("spProveedoresEliminar", new SqlParameter("@proveedor", id));
    }
}
