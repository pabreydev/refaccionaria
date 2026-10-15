using System.IO;
using Microsoft.Data.SqlClient;

namespace RefaccionariaApp.Data
{
    /// <summary>
    /// Respaldo completo de la base de datos (BACKUP DATABASE): incluye todas las
    /// tablas, datos, procedimientos almacenados, vistas, usuarios y el log.
    /// Se usa SQL directo porque el script no trae un procedimiento de respaldo.
    ///
    /// El archivo .bak lo escribe el servicio de SQL Server (NT Service\MSSQL$...),
    /// que normalmente no tiene permiso sobre las carpetas del usuario, y el
    /// usuario no puede leer la carpeta Backup de SQL Server. Por eso, con el
    /// servidor en esta PC, se respalda primero en Documentos públicos
    /// (accesible para ambos), se verifica y luego se copia al destino elegido.
    /// Con un servidor remoto, el archivo queda en la carpeta Backup del servidor.
    /// </summary>
    public static class RespaldoBD
    {
        public class Resultado
        {
            /// <summary>Ruta final del archivo de respaldo.</summary>
            public string Ruta { get; set; }
            /// <summary>True si la ruta está en el servidor (servidor remoto).</summary>
            public bool EnServidor { get; set; }
            public long Bytes { get; set; }
        }

        private const int TiempoEsperaSeg = 0; // sin límite: una base grande puede tardar

        public static string NombreBaseDatos =>
            new SqlConnectionStringBuilder(ConexionBD.ConnectionString).InitialCatalog;

        /// <summary>Crea un respaldo completo y verificado en <paramref name="carpetaDestino"/>.</summary>
        public static Resultado Respaldar(string carpetaDestino)
        {
            string baseDatos = NombreBaseDatos;
            string archivo = $"{baseDatos}_{DateTime.Now:yyyyMMdd_HHmmss}.bak";

            using var cn = ConexionBD.ObtenerConexion();
            cn.Open();

            if (!ServidorEsLocal(cn))
            {
                string carpetaServidor = Escalar(cn, "SELECT CONVERT(NVARCHAR(4000), SERVERPROPERTY('InstanceDefaultBackupPath'))");
                string rutaServidor = Path.Combine(carpetaServidor, archivo);
                RespaldarYVerificar(cn, baseDatos, rutaServidor);
                return new Resultado { Ruta = rutaServidor, EnServidor = true };
            }

            string carpetaTemporal = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "RespaldosRefaccionaria_tmp");
            Directory.CreateDirectory(carpetaTemporal);
            string rutaTemporal = Path.Combine(carpetaTemporal, archivo);

            try
            {
                RespaldarYVerificar(cn, baseDatos, rutaTemporal);

                Directory.CreateDirectory(carpetaDestino);
                string rutaFinal = Path.Combine(carpetaDestino, archivo);
                File.Copy(rutaTemporal, rutaFinal, overwrite: true);
                return new Resultado { Ruta = rutaFinal, Bytes = new FileInfo(rutaFinal).Length };
            }
            finally
            {
                try { File.Delete(rutaTemporal); } catch { /* se intenta limpiar; no es crítico */ }
            }
        }

        /// <summary>
        /// COPY_ONLY no altera la secuencia de respaldos que pudiera tener el
        /// servidor; CHECKSUM + RESTORE VERIFYONLY confirman que el archivo es
        /// legible y restaurable.
        /// </summary>
        private static void RespaldarYVerificar(SqlConnection cn, string baseDatos, string ruta)
        {
            using (var cmd = new SqlCommand(
                "BACKUP DATABASE @bd TO DISK = @ruta WITH COPY_ONLY, INIT, FORMAT, CHECKSUM, NAME = @nombre;", cn))
            {
                cmd.CommandTimeout = TiempoEsperaSeg;
                cmd.Parameters.AddWithValue("@bd", baseDatos);
                cmd.Parameters.AddWithValue("@ruta", ruta);
                cmd.Parameters.AddWithValue("@nombre", $"{baseDatos} - respaldo completo");
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SqlCommand("RESTORE VERIFYONLY FROM DISK = @ruta WITH CHECKSUM;", cn))
            {
                cmd.CommandTimeout = TiempoEsperaSeg;
                cmd.Parameters.AddWithValue("@ruta", ruta);
                cmd.ExecuteNonQuery();
            }
        }

        private static bool ServidorEsLocal(SqlConnection cn)
        {
            string maquina = Escalar(cn, "SELECT CONVERT(NVARCHAR(256), SERVERPROPERTY('MachineName'))");
            return string.Equals(maquina, Environment.MachineName, StringComparison.OrdinalIgnoreCase);
        }

        private static string Escalar(SqlConnection cn, string sql)
        {
            using var cmd = new SqlCommand(sql, cn);
            return cmd.ExecuteScalar()?.ToString();
        }
    }
}
