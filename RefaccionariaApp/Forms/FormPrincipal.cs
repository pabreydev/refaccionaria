using System;
using System.Drawing;
using System.Windows.Forms;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Shell principal de la aplicación: barra lateral oscura de navegación y un
    /// área de contenido que embebe la pantalla seleccionada (las pantallas se
    /// muestran dentro del shell, no como ventanas sueltas).
    /// </summary>
    public class FormPrincipal : Form
    {
        private readonly Panel panelHost;
        private readonly Label lblTitulo;
        private readonly FlowLayoutPanel flpNav;

        private Form pantallaActual;
        private Button botonActivo;

        public FormPrincipal()
        {
            Text = "Refaccionaria — Sistema de administración";
            Width = 1150;
            Height = 720;
            MinimumSize = new Size(900, 560);
            StartPosition = FormStartPosition.CenterScreen;
            Font = Tema.FuenteBase;
            BackColor = Tema.Fondo;

            // ---------- Barra lateral ----------
            var sidebar = new Panel { Dock = DockStyle.Left, Width = 236, BackColor = Tema.SidebarFondo };

            var panelLogo = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Tema.SidebarFondo };
            var lblLogo = new Label
            {
                Text = "MANGUERAS Y REFACCIONES",
                Dock = DockStyle.Fill,
                ForeColor = Tema.Blanco,
                Font = new Font("Segoe UI Semibold", 13F),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(20, 0, 0, 0)
            };
            var barraAcento = new Panel { Dock = DockStyle.Left, Width = 5, BackColor = Tema.Acento };
            panelLogo.Controls.Add(lblLogo);
            panelLogo.Controls.Add(barraAcento);

            // ---------- Logo (imagen) ----------
            var picLogo = new PictureBox
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Tema.Blanco,          // el JPEG tiene fondo blanco
                SizeMode = PictureBoxSizeMode.Zoom,
                Padding = new Padding(8)
            };

            try
            {
                string ruta = System.IO.Path.Combine(Application.StartupPath, "Imagenes", "logo.jpeg");
                if (System.IO.File.Exists(ruta))
                {
                    // Se carga desde un stream para no dejar el archivo bloqueado.
                    using (var fs = new System.IO.FileStream(ruta, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                    using (var img = Image.FromStream(fs))
                        picLogo.Image = new Bitmap(img);
                }
            }
            catch { /* si falla la imagen, el shell sigue funcionando con el texto */ }





            flpNav = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Tema.SidebarFondo,
                Padding = new Padding(0, 8, 0, 8)
            };

            AgregarBotonNav("Partes", () => new FormPartes());
            AgregarBotonNav("Cotizaciones", () => new FormCotizaciones());

            AgregarSeccion("CATÁLOGOS");
            AgregarBotonNav("Marcas", () => new FormMarcas());
            AgregarBotonNav("Modelos", () => new FormModelos());
            AgregarBotonNav("Años", () => new FormAnios());
            AgregarBotonNav("Versiones", () => new FormVersiones());
            AgregarBotonNav("Categorías", () => new FormCategorias());
            AgregarBotonNav("Unidades", () => new FormUnidades());
            AgregarBotonNav("Proveedores", () => new FormProveedores());

            AgregarSeccion("REPORTES");
            AgregarBotonNav("Existencias", () => new FormReporteExistencias());

            // ---------- Salir (fijo al fondo de la barra lateral) ----------
            var btnSalir = new Button
            {
                Text = "Salir",
                Dock = DockStyle.Bottom,
                Height = 46,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Tema.SidebarTexto,
                BackColor = Tema.SidebarFondo,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand,
                Padding = new Padding(20, 0, 0, 0)
            };
            btnSalir.FlatAppearance.BorderSize = 0;
            btnSalir.FlatAppearance.MouseOverBackColor = Tema.Acento;
            btnSalir.Click += (s, e) =>
            {
                if (MessageBox.Show("¿Deseas salir de la aplicación?", "Salir",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    Close();
            };
            var separadorSalir = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Tema.SidebarHover };

            sidebar.Controls.Add(flpNav);
            sidebar.Controls.Add(btnSalir);
            sidebar.Controls.Add(separadorSalir);
            sidebar.Controls.Add(panelLogo);
            sidebar.Controls.Add(picLogo);

            // ---------- Contenido ----------
            var contenido = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fondo };

            var header = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Tema.Blanco };
            lblTitulo = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Tema.TextoPrimario,
                Font = Tema.FuenteTitulo,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(24, 0, 0, 0)
            };
            var separador = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Tema.Borde };
            header.Controls.Add(lblTitulo);
            header.Controls.Add(separador);

            panelHost = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fondo, Padding = new Padding(16) };

            contenido.Controls.Add(panelHost);
            contenido.Controls.Add(header);

            Controls.Add(contenido);
            Controls.Add(sidebar);

            // Pantalla inicial
            if (flpNav.Controls.Count > 0 && flpNav.Controls[0] is Button primero)
                primero.PerformClick();
        }

        private void AgregarSeccion(string texto)
        {
            flpNav.Controls.Add(new Label
            {
                Text = texto,
                Width = 210,
                Height = 30,
                Margin = new Padding(20, 12, 0, 2),
                ForeColor = Tema.SidebarTextoTenue,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft
            });
        }

        private void AgregarBotonNav(string texto, Func<Form> crearPantalla)
        {
            var boton = new Button
            {
                Text = texto,
                Width = 214,
                Height = 42,
                Margin = new Padding(0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Tema.SidebarTexto,
                BackColor = Tema.SidebarFondo,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand,
                Padding = new Padding(20, 0, 0, 0)
            };
            boton.FlatAppearance.BorderSize = 0;
            boton.FlatAppearance.MouseOverBackColor = Tema.SidebarHover;
            boton.Click += (s, e) => Navegar(boton, texto, crearPantalla);
            flpNav.Controls.Add(boton);
        }

        private void Navegar(Button boton, string titulo, Func<Form> crearPantalla)
        {
            // Resaltar el botón activo.
            if (botonActivo != null)
            {
                botonActivo.BackColor = Tema.SidebarFondo;
                botonActivo.ForeColor = Tema.SidebarTexto;
                botonActivo.Font = new Font("Segoe UI", 10F);
            }
            botonActivo = boton;
            boton.BackColor = Tema.SidebarActivo;
            boton.ForeColor = Tema.Acento;
            boton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

            // Reemplazar la pantalla embebida.
            if (pantallaActual != null)
            {
                panelHost.Controls.Remove(pantallaActual);
                pantallaActual.Dispose();
                pantallaActual = null;
            }

            lblTitulo.Text = titulo;

            try
            {
                var pantalla = crearPantalla();
                pantalla.TopLevel = false;
                pantalla.FormBorderStyle = FormBorderStyle.None;
                pantalla.Dock = DockStyle.Fill;
                pantallaActual = pantalla;
                panelHost.Controls.Add(pantalla);
                pantalla.Show();
                pantalla.BringToFront();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo abrir la pantalla: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
