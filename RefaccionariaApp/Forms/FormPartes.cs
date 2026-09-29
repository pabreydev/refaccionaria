using System.Data;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Pantalla de listado de Partes (refacciones). Usa búsqueda en servidor
    /// porque la tabla puede ser grande y los datos deben verse siempre frescos.
    ///
    /// Toda la lógica vive en stored procedures:
    ///  - Listado/búsqueda: spPartesMostrar @filtro (una fila por parte; agrega
    ///    marca/modelo/versión y el año como rango. @filtro NULL = todas).
    ///  - Borrado: spPartesEliminar (borra en cascada RelPartesVersion,
    ///    RelEquivalencias y la parte, en una transacción).
    /// </summary>
    public class FormPartes : FormListaBase
    {
        public FormPartes() : base("Partes (refacciones)")
        {
            Width = 1000;
            Height = 620;
            StartPosition = FormStartPosition.CenterScreen;
        }

        protected override bool BusquedaEnServidor => true;

        protected override DataTable ObtenerDatos() => Consulta(null);

        protected override DataTable ObtenerDatos(string filtro) => Consulta(filtro);

        private static DataTable Consulta(string filtro)
        {
            object valorFiltro = string.IsNullOrWhiteSpace(filtro) ? (object)DBNull.Value : filtro;
            return BD.EjecutarConsulta("spPartesMostrar", new SqlParameter("@filtro", valorFiltro));
        }

        protected override string NombreCampoId => "id_parte";

        protected override string[] ColumnasOcultas => new[] { "id_parte", "Path" };

        protected override FormEdicionBase CrearFormEdicion(DataRowView fila) => new FormParteEdicion(fila);

        protected override void EliminarRegistro(int id)
        {
            BD.EjecutarNoQuery("spPartesEliminar", new SqlParameter("@id_parte", id));
        }
    }
}
