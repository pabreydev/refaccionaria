using System.Drawing;
using System.Windows.Forms;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Paleta y helpers de estilo para dar un aspecto moderno y consistente a
    /// toda la aplicación (barra lateral oscura, acento rojo, grids y botones
    /// planos). Los colores siguen el logotipo: rojo de las letras, negro de la
    /// manguera y gris cromado de las conexiones. Centraliza los colores para
    /// poder ajustarlos en un solo lugar.
    /// </summary>
    public static class Tema
    {
        // --- Paleta ---
        public static readonly Color Acento = Color.FromArgb(220, 30, 38);       // rojo del logo
        public static readonly Color AcentoHover = Color.FromArgb(180, 20, 28);
        public static readonly Color AcentoSuave = Color.FromArgb(252, 228, 229);

        public static readonly Color SidebarFondo = Color.FromArgb(22, 22, 24);  // negro de la manguera
        public static readonly Color SidebarHover = Color.FromArgb(44, 44, 48);
        public static readonly Color SidebarActivo = Color.FromArgb(34, 34, 37);
        public static readonly Color SidebarTexto = Color.FromArgb(214, 216, 219); // gris cromado
        public static readonly Color SidebarTextoTenue = Color.FromArgb(138, 141, 146);

        public static readonly Color Fondo = Color.FromArgb(244, 244, 245);
        public static readonly Color Blanco = Color.White;
        public static readonly Color Borde = Color.FromArgb(221, 222, 225);
        public static readonly Color TextoPrimario = Color.FromArgb(28, 28, 30);
        public static readonly Color TextoSecundario = Color.FromArgb(108, 110, 115);

        public static readonly Color GridLinea = Color.FromArgb(234, 235, 237);
        public static readonly Color GridFilaAlt = Color.FromArgb(249, 249, 250);
        public static readonly Color GridSeleccion = AcentoSuave;
        public static readonly Color BotonSecundarioHover = Color.FromArgb(242, 242, 243);
        public static readonly Color FondoImagen = Color.FromArgb(238, 238, 240);

        // --- Fuentes ---
        public static Font FuenteBase => new Font("Segoe UI", 9.75F);
        public static Font FuenteTitulo => new Font("Segoe UI Semibold", 15F);
        public static Font FuenteEncabezadoGrid => new Font("Segoe UI", 9F, FontStyle.Bold);

        public static void EstilizarGrid(DataGridView dgv)
        {
            dgv.BorderStyle = BorderStyle.None;
            dgv.BackgroundColor = Blanco;
            dgv.Font = FuenteBase;
            dgv.EnableHeadersVisualStyles = false;
            dgv.RowHeadersVisible = false;
            dgv.AllowUserToResizeRows = false;
            dgv.GridColor = GridLinea;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = 42;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Blanco;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextoSecundario;
            dgv.ColumnHeadersDefaultCellStyle.Font = FuenteEncabezadoGrid;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Blanco;
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextoSecundario;
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 10, 0);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            dgv.RowTemplate.Height = 36;
            dgv.DefaultCellStyle.BackColor = Blanco;
            dgv.DefaultCellStyle.ForeColor = TextoPrimario;
            dgv.DefaultCellStyle.SelectionBackColor = GridSeleccion;
            dgv.DefaultCellStyle.SelectionForeColor = TextoPrimario;
            dgv.DefaultCellStyle.Padding = new Padding(10, 0, 10, 0);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = GridFilaAlt;
            dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = GridSeleccion;
            dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = TextoPrimario;
        }

        public static void BotonPrimario(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = AcentoHover;
            b.BackColor = Acento;
            b.ForeColor = Blanco;
            b.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
            b.Height = 34;
        }

        public static void BotonSecundario(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Borde;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.MouseOverBackColor = BotonSecundarioHover;
            b.BackColor = Blanco;
            b.ForeColor = TextoPrimario;
            b.Font = new Font("Segoe UI", 9.5F);
            b.Cursor = Cursors.Hand;
            b.Height = 34;
        }

        public static void EstilizarBuscador(TextBox t, string placeholder = "Buscar...")
        {
            t.BorderStyle = BorderStyle.FixedSingle;
            t.Font = FuenteBase;
            t.PlaceholderText = placeholder;
        }
    }
}
