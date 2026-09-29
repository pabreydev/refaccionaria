using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Base reutilizable para el modal de alta/edición de un catálogo. Provee el
    /// panel de campos (<see cref="panelCampos"/>) y los botones Guardar/Cancelar.
    ///
    /// Cada catálogo concreto agrega sus controles a <see cref="panelCampos"/> en
    /// su constructor, rellena esos controles en <see cref="CargarRegistro"/>
    /// (solo cuando es edición) y persiste en <see cref="Guardar"/>.
    ///
    /// La misma clase sirve para alta y edición: si <see cref="Fila"/> es null es
    /// un alta; si trae datos, es una edición del registro de esa fila.
    /// </summary>
    public abstract class FormEdicionBase : Form
    {
        protected Panel panelCampos;

        /// <summary>Fila del registro a editar; null cuando es un alta.</summary>
        protected readonly DataRowView Fila;

        /// <summary>true cuando el modal se abrió para dar de alta un registro nuevo.</summary>
        protected bool EsNuevo => Fila == null;

        private readonly Button btnGuardar, btnCancelar;

        protected FormEdicionBase(string titulo, DataRowView fila)
        {
            Fila = fila;
            Text = (fila == null ? "Nuevo — " : "Editar — ") + titulo;
            Width = 460;
            Height = 250;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = Tema.FuenteBase;
            BackColor = Tema.Blanco;

            panelCampos = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16), BackColor = Tema.Blanco };

            var panelBotones = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Tema.Blanco };
            var separador = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Tema.Borde };
            panelBotones.Controls.Add(separador);
            btnGuardar = new Button { Text = "Guardar", Width = 110, Top = 12 };
            btnCancelar = new Button { Text = "Cancelar", Width = 100, Top = 12, DialogResult = DialogResult.Cancel };
            Tema.BotonPrimario(btnGuardar);
            Tema.BotonSecundario(btnCancelar);
            btnGuardar.Click += (s, e) => GuardarSeguro();
            panelBotones.Controls.Add(btnGuardar);
            panelBotones.Controls.Add(btnCancelar);

            // Los botones se anclan a la derecha recalculando su posición cada vez
            // que el panel cambia de ancho (al acoplarse ocupa el ancho real del
            // modal). Con Anchor fijo el desplazamiento se calcula con el ancho por
            // defecto del panel y los botones se salen de la vista.
            void PosicionarBotones()
            {
                btnCancelar.Left = panelBotones.ClientSize.Width - btnCancelar.Width - 12;
                btnGuardar.Left = btnCancelar.Left - btnGuardar.Width - 8;
            }
            panelBotones.SizeChanged += (s, e) => PosicionarBotones();
            PosicionarBotones();

            AcceptButton = btnGuardar;
            CancelButton = btnCancelar;

            Controls.Add(panelCampos);
            Controls.Add(panelBotones);

            // Los controles concretos ya existen (se agregan en el constructor de la
            // subclase); solo cargamos valores cuando estamos editando.
            Load += (s, e) => { if (!EsNuevo) CargarRegistro(); };
        }

        private void GuardarSeguro()
        {
            try
            {
                Guardar();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo guardar: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Rellena los controles con los valores de <see cref="Fila"/> (solo en edición).</summary>
        protected abstract void CargarRegistro();

        /// <summary>Valida y persiste. Lanza <see cref="Exception"/> con el mensaje si algo falla.</summary>
        protected abstract void Guardar();
    }
}
