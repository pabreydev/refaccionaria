using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    public class FormReporteExistencias : Form
    {
        private readonly NumericUpDown numMinimo = new() { Left = 220, Top = 12, Width = 100, Maximum = 1000000 };
        private readonly CheckBox chkFiltrar = new() { Text = "Filtrar por existencias menores o iguales a:", Left = 10, Top = 14, Width = 210 };
        private readonly DataGridView dgv = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };

        public FormReporteExistencias()
        {
            Text = "Reporte de existencias";
            Width = 750;
            Height = 550;
            StartPosition = FormStartPosition.CenterScreen;
            Font = Tema.FuenteBase;
            BackColor = Tema.Blanco;

            Tema.EstilizarGrid(dgv);

            var panelSuperior = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(12), BackColor = Tema.Blanco };
            chkFiltrar.Top = 16;
            numMinimo.Top = 14;
            panelSuperior.Controls.Add(chkFiltrar);
            panelSuperior.Controls.Add(numMinimo);
            var btnConsultar = new Button { Text = "Consultar", Left = 340, Top = 11, Width = 110 };
            Tema.BotonPrimario(btnConsultar);
            btnConsultar.Click += (s, e) => Consultar();
            panelSuperior.Controls.Add(btnConsultar);

            Controls.Add(dgv);
            Controls.Add(panelSuperior);

            Load += (s, e) => Consultar();
        }

        private void Consultar()
        {
            dgv.DataSource = BD.EjecutarConsulta("spReporteExistencias",
                new SqlParameter("@existenciasMinimas", chkFiltrar.Checked ? (object)(int)numMinimo.Value : DBNull.Value));
        }
    }
}
