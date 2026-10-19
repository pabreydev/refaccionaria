using System.IO;
using Microsoft.Data.SqlClient;

namespace RefaccionariaApp.Data
{
    /// <summary>
    /// Punto único donde se define la cadena de conexión a la base de datos
    /// "Refaccionaria".
    ///
    /// En una PC instalada, la cadena viene del archivo "conexion.txt" junto al
    /// ejecutable (lo escribe el instalador con el servidor elegido). Si no
    /// existe, como al desarrollar, se usa <see cref="CadenaPorDefecto"/>: ajusta
    /// ahí el Data Source y el modo de autenticación según tu SQL Server.
    /// </summary>
    public static class ConexionBD
    {
        public const string ArchivoConfiguracion = "conexion.txt";

        // Ejemplos comunes:
        //  - Instancia local por defecto:      "Data Source=.;Initial Catalog=Refaccionaria;Integrated Security=True;TrustServerCertificate=True"
        //  - Instancia SQLEXPRESS (la que generó el script original):
        private const string CadenaPorDefecto =
            @"Data Source=localhost\SQLEXPRESS;Initial Catalog=Refaccionaria;Integrated Security=True;Pooling=False;Connect Timeout=30;TrustServerCertificate=True";

        public static string ConnectionString { get; } = LeerCadena();

        public static SqlConnection ObtenerConexion() => new SqlConnection(ConnectionString);

        private static string LeerCadena()
        {
            try
            {
                string ruta = Path.Combine(AppContext.BaseDirectory, ArchivoConfiguracion);
                if (File.Exists(ruta))
                {
                    string cadena = File.ReadAllText(ruta).Trim();
                    if (cadena.Length > 0) return cadena;
                }
            }
            catch { /* archivo ilegible: se usa la cadena por defecto */ }
            return CadenaPorDefecto;
        }
    }
}
