using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace RefaccionariaApp.Data
{
    /// <summary>
    /// Crea o actualiza la base de datos ejecutando los scripts de la carpeta
    /// "sql" que copia el instalador. Lo invoca el instalador con
    /// <c>RefaccionariaApp.exe --instalar-bd "servidor" "carpetaSql" "log"</c>.
    ///
    /// Todos los scripts se pueden correr varias veces sin duplicar ni borrar
    /// datos (crean solo lo que falta), así que reinstalar o actualizar la app
    /// conserva la información existente.
    /// </summary>
    public static class InstaladorBD
    {
        /// <summary>Scripts en orden de ejecución.</summary>
        public static readonly string[] Scripts =
        {
            "script.sql",                    // estructura + procedimientos
            "correcciones_recomendadas.sql", // SPs de borrado corregidos
            "seed_catalogo.sql",             // años, marcas, modelos, versiones, categorías
            "permisos_usuarios.sql"          // acceso para los usuarios de Windows de la PC
        };

        // Separador de lotes de SSMS/sqlcmd: una línea que solo dice GO.
        private static readonly Regex SeparadorGo = new(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

        public static string CadenaConexion(string servidor, string baseDatos) =>
            new SqlConnectionStringBuilder
            {
                DataSource = servidor,
                InitialCatalog = baseDatos,
                IntegratedSecurity = true,
                TrustServerCertificate = true,
                Pooling = false,
                ConnectTimeout = 30
            }.ConnectionString;

        /// <summary>
        /// Ejecuta los scripts contra <paramref name="servidor"/> en una sola
        /// conexión (el seed usa tablas temporales entre lotes).
        /// </summary>
        public static void Instalar(string servidor, string carpetaSql, TextWriter log)
        {
            using var cn = new SqlConnection(CadenaConexion(servidor, "master"));
            AbrirConReintentos(cn, log);

            foreach (var nombre in Scripts)
            {
                string ruta = Path.Combine(carpetaSql, nombre);
                log.WriteLine($"Ejecutando {nombre}...");
                // ReadAllText detecta la codificación por el BOM (script.sql es UTF-16).
                string texto = File.ReadAllText(ruta, Encoding.UTF8);

                int numLote = 0;
                foreach (var lote in SeparadorGo.Split(texto))
                {
                    numLote++;
                    if (string.IsNullOrWhiteSpace(lote)) continue;
                    try
                    {
                        using var cmd = new SqlCommand(lote, cn) { CommandTimeout = 0 };
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        throw new InvalidOperationException($"Error en {nombre}, lote {numLote}: {ex.Message}", ex);
                    }
                }
            }
            log.WriteLine("Base de datos lista.");
        }

        /// <summary>
        /// Recién instalado SQL Server, el servicio puede tardar en aceptar
        /// conexiones; se reintenta durante un par de minutos.
        /// </summary>
        private static void AbrirConReintentos(SqlConnection cn, TextWriter log)
        {
            for (int intento = 1; ; intento++)
            {
                try
                {
                    cn.Open();
                    return;
                }
                catch (SqlException ex) when (intento < 12)
                {
                    log.WriteLine($"Sin conexión aún (intento {intento}): {ex.Message}");
                    Thread.Sleep(10000);
                }
            }
        }
    }
}
