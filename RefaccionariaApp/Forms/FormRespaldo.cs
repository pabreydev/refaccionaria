using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Respaldo completo de la base de datos (ver <see cref="RespaldoBD"/>).
    /// Muestra los respaldos (.bak) que ya existen en la carpeta de destino.
    /// </summary>
    public class FormRespaldo : Form
    {
        private readonly TextBox txtCarpeta = new() { Width = 420, Margin = new Padding(0, 4, 8, 0) };
        private readonly Button btnRespaldar = new() { Text = "Crear respaldo", Width = 150, Margin = new Padding(0, 0, 8, 0) };
        private readonly Label lblEstado = new() { AutoSize = true, ForeColor = Tema.TextoSecundario, Margin = new Padding(0, 8, 0, 0) };
        private readonly DataGridView dgv = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        private readonly Paginador paginador;

        public FormRespaldo()
        {
            Text = "Respaldo de la base de datos";
            Width = 900;
            Height = 550;
            StartPosition = FormStartPosition.CenterScreen;
            Font = Tema.FuenteBase;
            BackColor = Tema.Blanco;

            Tema.EstilizarGrid(dgv);
            paginador = new Paginador(dgv);

            var panelSuperior = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(16, 14, 16, 12),
                BackColor = Tema.Blanco
            };

            panelSuperior.Controls.Add(new Label
            {
                Text = $"Genera un respaldo completo de la base de datos \"{RespaldoBD.NombreBaseDatos}\" " +
                       "(tablas, datos, procedimientos y demás objetos) en un archivo .bak verificado.",
                AutoSize = true,
                MaximumSize = new Size(820, 0),
                Margin = new Padding(0, 0, 0, 12)
            });
            panelSuperior.Controls.Add(new Label
            {
                Text = "Carpeta de destino",
                AutoSize = true,
                ForeColor = Tema.TextoSecundario,
                Font = new Font("Segoe UI", 8.5F)
            });

            var filaCarpeta = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 2, 0, 10) };
            txtCarpeta.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Respaldos Refaccionaria");
            Tema.EstilizarBuscador(txtCarpeta, "Carpeta donde se guardará el respaldo");
            var btnExaminar = new Button { Text = "Examinar...", Width = 110 };
            Tema.BotonSecundario(btnExaminar);
            btnExaminar.Click += (s, e) => ElegirCarpeta();
            filaCarpeta.Controls.Add(txtCarpeta);
            filaCarpeta.Controls.Add(btnExaminar);
            panelSuperior.Controls.Add(filaCarpeta);

            var filaAcciones = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
            var btnAbrir = new Button { Text = "Abrir carpeta", Width = 130 };
            Tema.BotonPrimario(btnRespaldar);
            Tema.BotonSecundario(btnAbrir);
            btnRespaldar.Click += async (s, e) => await Respaldar();
            btnAbrir.Click += (s, e) => AbrirCarpeta();
            filaAcciones.Controls.Add(btnRespaldar);
            filaAcciones.Controls.Add(btnAbrir);
            panelSuperior.Controls.Add(filaAcciones);
            panelSuperior.Controls.Add(lblEstado);

            var lblLista = new Label
            {
                Text = "Respaldos en la carpeta",
                Dock = DockStyle.Top,
                Height = 28,
                Padding = new Padding(16, 6, 0, 0),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Tema.TextoPrimario
            };

            Controls.Add(dgv);
            Controls.Add(paginador);
            Controls.Add(lblLista);
            Controls.Add(panelSuperior);

            txtCarpeta.Leave += (s, e) => CargarRespaldos();
            Load += (s, e) => CargarRespaldos();
        }

        private void ElegirCarpeta()
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Carpeta donde se guardarán los respaldos",
                SelectedPath = Directory.Exists(txtCarpeta.Text) ? txtCarpeta.Text : ""
            };
            if (fbd.ShowDialog(this) != DialogResult.OK) return;
            txtCarpeta.Text = fbd.SelectedPath;
            CargarRespaldos();
        }

        private async Task Respaldar()
        {
            string carpeta = txtCarpeta.Text.Trim();
            if (carpeta.Length == 0)
            {
                MessageBox.Show("Indica la carpeta de destino.", "Respaldo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnRespaldar.Enabled = false;
            UseWaitCursor = true;
            lblEstado.ForeColor = Tema.TextoSecundario;
            lblEstado.Text = "Generando y verificando el respaldo...";
            try
            {
                // En segundo plano para no congelar la ventana con bases grandes.
                var r = await Task.Run(() => RespaldoBD.Respaldar(carpeta));

                lblEstado.ForeColor = Tema.TextoPrimario;
                if (r.EnServidor)
                {
                    lblEstado.Text = "Respaldo creado en el servidor: " + r.Ruta;
                    MessageBox.Show("El servidor de base de datos no está en esta PC, así que el respaldo quedó " +
                                    "en la carpeta de respaldos del servidor:\n\n" + r.Ruta,
                                    "Respaldo creado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    lblEstado.Text = $"Último respaldo: {Path.GetFileName(r.Ruta)} ({FormatoTamano(r.Bytes)}), verificado correctamente.";
                    MessageBox.Show("Respaldo creado y verificado correctamente:\n\n" + r.Ruta,
                                    "Respaldo creado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                CargarRespaldos();
            }
            catch (Exception ex)
            {
                lblEstado.ForeColor = Tema.Acento;
                lblEstado.Text = "No se pudo crear el respaldo.";
                MessageBox.Show("No se pudo crear el respaldo: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                btnRespaldar.Enabled = true;
            }
        }

        private void CargarRespaldos()
        {
            var dt = new DataTable();
            dt.Columns.Add("Archivo", typeof(string));
            dt.Columns.Add("Fecha", typeof(DateTime));
            dt.Columns.Add("Tamaño", typeof(string));

            try
            {
                var carpeta = new DirectoryInfo(txtCarpeta.Text.Trim());
                if (carpeta.Exists)
                    foreach (var f in carpeta.GetFiles("*.bak").OrderByDescending(f => f.LastWriteTime))
                        dt.Rows.Add(f.Name, f.LastWriteTime, FormatoTamano(f.Length));
            }
            catch { /* ruta inválida o sin permiso: se muestra la lista vacía */ }

            paginador.Cargar(dt);
        }

        private void AbrirCarpeta()
        {
            string carpeta = txtCarpeta.Text.Trim();
            if (!Directory.Exists(carpeta))
            {
                MessageBox.Show("La carpeta todavía no existe; se crea al hacer el primer respaldo.", "Respaldo",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Process.Start(new ProcessStartInfo(carpeta) { UseShellExecute = true });
        }

        private static string FormatoTamano(long bytes) =>
            bytes >= 1024 * 1024 ? $"{bytes / 1024d / 1024d:0.0} MB" : $"{bytes / 1024d:0} KB";
    }
}
