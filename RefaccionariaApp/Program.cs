using System;
using System.IO;
using System.Windows.Forms;
using RefaccionariaApp.Data;
using RefaccionariaApp.Forms;

namespace RefaccionariaApp
{
    internal static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            // Modo usado por el instalador para crear/actualizar la base de datos.
            if (args.Length > 0 && args[0] == "--instalar-bd")
                return InstalarBaseDatos(args);

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormPrincipal());
            return 0;
        }

        /// <summary>
        /// --instalar-bd "servidor" "carpetaSql" "archivoLog". Devuelve 0 si todo
        /// salió bien; el detalle (y cualquier error) queda en el log.
        /// </summary>
        private static int InstalarBaseDatos(string[] args)
        {
            if (args.Length < 4) return 2;
            using var log = new StreamWriter(args[3], append: true) { AutoFlush = true };
            try
            {
                log.WriteLine($"[{DateTime.Now}] Instalando base de datos en {args[1]}");
                InstaladorBD.Instalar(args[1], args[2], log);
                return 0;
            }
            catch (Exception ex)
            {
                log.WriteLine("ERROR: " + ex.Message);
                return 1;
            }
        }
    }
}
