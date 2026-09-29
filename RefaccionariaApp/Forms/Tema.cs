using System.Drawing;
using System.Windows.Forms;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Paleta y helpers de estilo para dar un aspecto moderno y consistente a
    /// toda la aplicación (barra lateral oscura, acento turquesa, grids y
    /// botones planos). Centraliza los colores para poder ajustarlos en un
    /// solo lugar.
    /// </summary>
    public static class Tema
    {
        // --- Paleta ---
        public static readonly Color Acento = Color.FromArgb(0, 173, 181);       // turquesa
        public static readonly Color AcentoHover = Color.FromArgb(0, 148, 156);
        public static readonly Color AcentoSuave = Color.FromArgb(224, 246, 247);

        public static readonly Color SidebarFondo = Color.FromArgb(24, 28, 36);
        public static readonly Color SidebarHover = Color.FromArgb(38, 44, 56);
        public static readonly Color SidebarActivo = Color.FromArgb(31, 37, 48);
        public static readonly Color SidebarTexto = Color.FromArgb(201, 206, 215);
        public static readonly Color SidebarTextoTenue = Color.FromArgb(122, 130, 143);

        public static readonly Color Fondo = Color.FromArgb(245, 246, 248);
        public static readonly Color Blanco = Color.White;
        public static readonly Color Borde = Color.FromArgb(223, 227, 232);
        public static readonly Color TextoPrimario = Color.FromArgb(32, 38, 45);
        public static readonly Color TextoSecundario = Color.FromArgb(110, 118, 128);

        public static readonly Color GridLinea = Color.FromArgb(235, 238, 241);
        public static readonly Color GridFilaAlt = Color.FromArgb(249, 250, 251);
        public static readonly Color GridSeleccion = AcentoSuave;

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
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(244, 245, 247);
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
