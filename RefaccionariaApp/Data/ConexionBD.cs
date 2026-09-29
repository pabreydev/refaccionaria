using Microsoft.Data.SqlClient;

namespace RefaccionariaApp.Data
{
    /// <summary>
    /// Punto único donde se define la cadena de conexión a la base de datos
    /// "Refaccionaria". Ajusta el Data Source (nombre del servidor/instancia)
    /// y el modo de autenticación según tu instalación de SQL Server.
    /// </summary>
    public static class ConexionBD
    {
        // Ejemplos comunes:
        //  - Instancia local por defecto:      "Data Source=.;Initial Catalog=Refaccionaria;Integrated Security=True;TrustServerCertificate=True"
        //  - Instancia SQLEXPRESS (la que generó el script original):
        public static string ConnectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Refaccionaria;Integrated Security=True;Pooling=False;Connect Timeout=30;TrustServerCertificate=True";

        public static SqlConnection ObtenerConexion() => new SqlConnection(ConnectionString);
    }
}


