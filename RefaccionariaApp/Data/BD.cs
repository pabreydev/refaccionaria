using System.Data;
using Microsoft.Data.SqlClient;

namespace RefaccionariaApp.Data
{
    /// <summary>
    /// Métodos genéricos para llamar a los procedimientos almacenados del
    /// script "Refaccionaria". Las variantes "...SQL" ejecutan una consulta
    /// de texto directa y se usan únicamente en los pocos puntos señalados
    /// en el código/README donde el script no traía el procedimiento
    /// necesario (por ejemplo, editar Proveedores) o donde el procedimiento
    /// existente tiene un error que borraría la tabla completa.
    /// </summary>
    public static class BD
    {
        public static DataTable EjecutarConsulta(string procedimiento, params SqlParameter[] parametros)
        {
            using var cn = ConexionBD.ObtenerConexion();
            using var cmd = new SqlCommand(procedimiento, cn) { CommandType = CommandType.StoredProcedure };
            if (parametros != null) cmd.Parameters.AddRange(parametros);
            var dt = new DataTable();
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        public static object EjecutarEscalar(string procedimiento, params SqlParameter[] parametros)
        {
            using var cn = ConexionBD.ObtenerConexion();
            using var cmd = new SqlCommand(procedimiento, cn) { CommandType = CommandType.StoredProcedure };
            if (parametros != null) cmd.Parameters.AddRange(parametros);
            cn.Open();
            return cmd.ExecuteScalar();
        }

        public static int EjecutarNoQuery(string procedimiento, params SqlParameter[] parametros)
        {
            using var cn = ConexionBD.ObtenerConexion();
            using var cmd = new SqlCommand(procedimiento, cn) { CommandType = CommandType.StoredProcedure };
            if (parametros != null) cmd.Parameters.AddRange(parametros);
            cn.Open();
            return cmd.ExecuteNonQuery();
        }

        // --- Variantes con SQL directo (solo donde el script no alcanzaba) ---

        public static DataTable EjecutarConsultaSQL(string sql, params SqlParameter[] parametros)
        {
            using var cn = ConexionBD.ObtenerConexion();
            using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
            if (parametros != null) cmd.Parameters.AddRange(parametros);
            var dt = new DataTable();
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        public static object EjecutarEscalarSQL(string sql, params SqlParameter[] parametros)
        {
            using var cn = ConexionBD.ObtenerConexion();
            using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
            if (parametros != null) cmd.Parameters.AddRange(parametros);
            cn.Open();
            return cmd.ExecuteScalar();
        }

        public static int EjecutarNoQuerySQL(string sql, params SqlParameter[] parametros)
        {
            using var cn = ConexionBD.ObtenerConexion();
            using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
            if (parametros != null) cmd.Parameters.AddRange(parametros);
            cn.Open();
            return cmd.ExecuteNonQuery();
        }
    }
}
