using System.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Panel lateral de consulta: muestra imagen + todos los campos de la fila.
    /// </summary>
    public class PanelDetalleParte : Panel
    {
        private const int AnchoPanel = 340;
        private static readonly string[] ColumnasIgnoradas = { "id_parte", "Path" };

        private readonly Panel _encabezado = new Panel();
        private readonly Label _titulo = new Label();
        private readonly Button _btnCerrar = new Button();
        private readonly PictureBox _imagen = new PictureBox();
        private readonly Label _sinImagen = new Label();
        private readonly Panel _contenedor = new Panel();
        private readonly TableLayoutPanel _tabla = new TableLayoutPanel();
        private readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer();
        private bool _abriendo;

        public event EventHandler CerrarClick;

        public PanelDetalleParte()
        {
            Dock = DockStyle.Right;
            Width = AnchoPanel;
            BackColor = Tema.Blanco;
            Visible = false;

            // Contenedor con scroll para los campos
            _contenedor.Dock = DockStyle.Fill;
            _contenedor.AutoScroll = true;
            _contenedor.Padding = new Padding(12);

            _tabla.AutoSize = true;
            _tabla.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _tabla.Dock = DockStyle.Top;
            _tabla.ColumnCount = 2;
            _tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            _tabla.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _contenedor.Controls.Add(_tabla);

            // Imagen
            _imagen.Dock = DockStyle.Top;
            _imagen.Height = 220;
            _imagen.SizeMode = PictureBoxSizeMode.Zoom;
            _imagen.BackColor = Tema.FondoImagen;

            _sinImagen.Dock = DockStyle.Fill;
            _sinImagen.Text = "Sin imagen";
            _sinImagen.TextAlign = ContentAlignment.MiddleCenter;
            _sinImagen.ForeColor = Tema.TextoSecundario;
            _imagen.Controls.Add(_sinImagen);

            // Encabezado
            _encabezado.Dock = DockStyle.Top;
            _encabezado.Height = 40;
            _encabezado.BackColor = Tema.SidebarFondo;

            _titulo.Text = "Detalle de la parte";
            _titulo.ForeColor = Tema.Blanco;
            _titulo.Font = new Font(Font, FontStyle.Bold);
            _titulo.Dock = DockStyle.Fill;
            _titulo.TextAlign = ContentAlignment.MiddleLeft;
            _titulo.Padding = new Padding(12, 0, 0, 0);

            _btnCerrar.Text = "✕";
            _btnCerrar.Dock = DockStyle.Right;
            _btnCerrar.Width = 40;
            _btnCerrar.FlatStyle = FlatStyle.Flat;
            _btnCerrar.FlatAppearance.BorderSize = 0;
            _btnCerrar.ForeColor = Tema.Blanco;
            _btnCerrar.FlatAppearance.MouseOverBackColor = Tema.Acento;
            _btnCerrar.Cursor = Cursors.Hand;
            _btnCerrar.Click += (s, e) => CerrarClick?.Invoke(this, EventArgs.Empty);

            _encabezado.Controls.Add(_titulo);
            _encabezado.Controls.Add(_btnCerrar);

            // Orden: Fill primero, el encabezado al final para que quede arriba
            Controls.Add(_contenedor);
            Controls.Add(_imagen);
            Controls.Add(_encabezado);

            // Animación de despliegue
            _timer.Interval = 15;
            _timer.Tick += Timer_Tick;
        }

        public void Mostrar(DataRowView fila)
        {
            CargarCampos(fila);
            CargarImagen(fila.Row.Table.Columns.Contains("Path") ? fila["Path"] as string : null);
            Desplegar();
        }

        public void Ocultar()
        {
            _timer.Stop();
            Visible = false;
        }

        private void Desplegar()
        {
            if (Visible && Width >= AnchoPanel) return;
            if (!Visible) Width = 0;
            Visible = true;
            _abriendo = true;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!_abriendo) { _timer.Stop(); return; }
            Width = Math.Min(AnchoPanel, Width + 60);
            if (Width >= AnchoPanel) { _abriendo = false; _timer.Stop(); }
        }

        private void CargarCampos(DataRowView fila)
        {
            _tabla.SuspendLayout();
            while (_tabla.Controls.Count > 0)
            {
                Control c = _tabla.Controls[0];
                _tabla.Controls.Remove(c);
                c.Dispose();
            }
            _tabla.RowStyles.Clear();
            _tabla.RowCount = 0;

            foreach (DataColumn col in fila.Row.Table.Columns)
            {
                if (ColumnasIgnoradas.Contains(col.ColumnName, StringComparer.OrdinalIgnoreCase))
                    continue;
                AgregarFila(col.ColumnName.Replace('_', ' '), Formatear(col.ColumnName, fila[col.ColumnName]));
            }
            _tabla.ResumeLayout();
        }

        private void AgregarFila(string campo, string valor)
        {
            int fila = _tabla.RowCount++;
            _tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblCampo = new Label
            {
                Text = campo,
                AutoSize = true,
                ForeColor = Tema.TextoSecundario,
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(0, 6, 6, 6),
                MaximumSize = new Size(104, 0)
            };
            var lblValor = new Label
            {
                Text = valor,
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 6),
                MaximumSize = new Size(AnchoPanel - 110 - 50, 0)
            };
            _tabla.Controls.Add(lblCampo, 0, fila);
            _tabla.Controls.Add(lblValor, 1, fila);
        }

        private static string Formatear(string columna, object valor)
        {
            if (valor == null || valor == DBNull.Value) return "—";
            if (valor is bool b) return b ? "Sí" : "No";
            if (valor is DateTime dt) return dt.ToShortDateString();
            if ((valor is decimal || valor is double || valor is float) &&
                (columna.Contains("precio", StringComparison.OrdinalIgnoreCase) ||
                 columna.Contains("costo", StringComparison.OrdinalIgnoreCase)))
                return Convert.ToDecimal(valor).ToString("C2");
            return valor.ToString();
        }

        private void CargarImagen(string ruta)
        {
            Image anterior = _imagen.Image;
            _imagen.Image = null;
            anterior?.Dispose();
            _sinImagen.Visible = true;

            if (string.IsNullOrWhiteSpace(ruta)) return;

            string completa = System.IO.Path.IsPathRooted(ruta)
                ? ruta
                : System.IO.Path.Combine(AppContext.BaseDirectory, ruta);
            if (!File.Exists(completa)) return;

            try
            {
                // Se copia a memoria para no dejar el archivo bloqueado
                using var fs = new FileStream(completa, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var img = Image.FromStream(fs);
                _imagen.Image = new Bitmap(img);
                _sinImagen.Visible = false;
            }
            catch
            {
                // archivo corrupto o formato no soportado: se queda "Sin imagen"
            }
        }
    }
}