using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Modal de alta/edición de una cotización (maestro-detalle). Si se construye
    /// con un folio, abre esa cotización para editar; sin folio, es un alta nueva.
    ///
    /// El script no define procedimientos para actualizar una cotización, así que
    /// al editar la cabecera se actualiza por SQL directo y el detalle se
    /// reemplaza (se borra con spCotizacionDetalleBaja y se reinserta).
    /// </summary>
    public class FormCotizacionNueva : Form
    {
        private class LineaDetalle
        {
            public int ParteId { get; set; }
            public string Parte { get; set; }
            public decimal PrecioUnitario { get; set; }
            public int Cantidad { get; set; }
            public decimal DescuentoPorc { get; set; }
            public decimal Subtotal => Math.Round(Cantidad * PrecioUnitario * (1 - DescuentoPorc / 100m), 2);
        }

        private readonly int? folioEditar;

        private readonly TextBox txtCliente = new() { Left = 70, Top = 12, Width = 220 };
        private readonly DateTimePicker dtVigencia = new() { Left = 400, Top = 12, Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(30) };
        private readonly ComboBox cmbParte = new() { Left = 70, Top = 45, Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly NumericUpDown numCantidad = new() { Left = 430, Top = 45, Width = 70, Minimum = 1, Maximum = 100000, Value = 1 };
        private readonly NumericUpDown numDescuento = new() { Left = 510, Top = 45, Width = 70, Minimum = 0, Maximum = 100, DecimalPlaces = 2 };
        private readonly BindingList<LineaDetalle> lineas = new();
        private readonly DataGridView dgv = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        // Importes del área de totales (solo el monto; el concepto va en otra etiqueta).
        private readonly Label lblSubtotal = NuevaEtiquetaImporte(false);
        private readonly Label lblIva = NuevaEtiquetaImporte(false);
        private readonly Label lblTotal = NuevaEtiquetaImporte(true);

        public FormCotizacionNueva(int? folio = null)
        {
            folioEditar = folio;
            Text = folio == null ? "Nueva cotización" : $"Editar cotización (folio {folio})";
            Width = 850;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;
            Font = Tema.FuenteBase;
            BackColor = Tema.Blanco;
            Tema.EstilizarGrid(dgv);

            var panelSuperior = new Panel { Dock = DockStyle.Top, Height = 80, Padding = new Padding(10) };
            panelSuperior.Controls.Add(new Label { Text = "Cliente:", Left = 10, Top = 15, Width = 55 });
            panelSuperior.Controls.Add(txtCliente);
            panelSuperior.Controls.Add(new Label { Text = "Vigencia:", Left = 300, Top = 15, Width = 60 });
            panelSuperior.Controls.Add(dtVigencia);

            panelSuperior.Controls.Add(new Label { Text = "Parte:", Left = 10, Top = 48, Width = 55 });
            panelSuperior.Controls.Add(cmbParte);
            panelSuperior.Controls.Add(new Label { Text = "Cant.", Left = 405, Top = 48, Width = 30 });
            panelSuperior.Controls.Add(numCantidad);
            panelSuperior.Controls.Add(new Label { Text = "% Desc.", Left = 485, Top = 48, Width = 50 });
            panelSuperior.Controls.Add(numDescuento);
            var btnAgregar = new Button { Text = "Agregar línea", Left = 590, Top = 43, Width = 110 };
            Tema.BotonSecundario(btnAgregar);
            btnAgregar.Click += (s, e) => AgregarLinea();
            panelSuperior.Controls.Add(btnAgregar);

            // --- Totales (abajo del grid, alineados a la derecha) ---
            var panelTotales = new Panel { Dock = DockStyle.Bottom, Height = 104, Padding = new Padding(10, 8, 20, 8), BackColor = Tema.Fondo };
            var tablaTotales = new TableLayoutPanel { Dock = DockStyle.Right, Width = 300, ColumnCount = 2, RowCount = 3 };
            tablaTotales.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            tablaTotales.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            tablaTotales.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            tablaTotales.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            tablaTotales.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            tablaTotales.Controls.Add(NuevaEtiquetaConcepto("Subtotal:", false), 0, 0);
            tablaTotales.Controls.Add(lblSubtotal, 1, 0);
            tablaTotales.Controls.Add(NuevaEtiquetaConcepto("IVA (16%):", false), 0, 1);
            tablaTotales.Controls.Add(lblIva, 1, 1);
            tablaTotales.Controls.Add(NuevaEtiquetaConcepto("Total:", true), 0, 2);
            tablaTotales.Controls.Add(lblTotal, 1, 2);
            panelTotales.Controls.Add(tablaTotales);
            panelTotales.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Tema.Borde });

            var panelInferior = new Panel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(10, 0, 10, 0), BackColor = Tema.Blanco };
            var btnQuitarLinea = new Button { Text = "Quitar línea", Left = 10, Top = 11, Width = 110 };
            btnQuitarLinea.Click += (s, e) => QuitarLinea();
            var btnGuardar = new Button { Text = "Guardar cotización", Left = 130, Top = 11, Width = 150 };
            btnGuardar.Click += (s, e) => GuardarCotizacion();
            var btnCancelar = new Button { Text = "Cancelar", Left = 288, Top = 11, Width = 90, DialogResult = DialogResult.Cancel };
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Tema.BotonSecundario(btnQuitarLinea);
            Tema.BotonPrimario(btnGuardar);
            Tema.BotonSecundario(btnCancelar);
            panelInferior.Controls.Add(btnQuitarLinea);
            panelInferior.Controls.Add(btnGuardar);
            panelInferior.Controls.Add(btnCancelar);

            dgv.DataSource = lineas;

            // Orden: Fill primero; panelInferior se agrega después de panelTotales
            // para quedar hasta abajo, con los totales justo arriba de los botones.
            Controls.Add(dgv);
            Controls.Add(panelTotales);
            Controls.Add(panelInferior);
            Controls.Add(panelSuperior);

            Load += (s, e) => { CargarPartes(); if (folioEditar != null) CargarCotizacion(folioEditar.Value); };
            lineas.ListChanged += (s, e) => ActualizarTotales();
            ActualizarTotales();
        }

        private void CargarPartes()
        {
            var dt = BD.EjecutarConsulta("sp_NumPartesMostrar");
            cmbParte.DataSource = dt;
            cmbParte.DisplayMember = "idNombre";
            cmbParte.ValueMember = "id_parte";
        }

        private void CargarCotizacion(int folio)
        {
            var cab = BD.EjecutarConsulta("spCotizacionesMostrar", new SqlParameter("@folio", folio));
            if (cab.Rows.Count > 0)
            {
                txtCliente.Text = cab.Rows[0]["cliente"]?.ToString();
                if (cab.Rows[0]["vigencia"] != DBNull.Value)
                    dtVigencia.Value = Convert.ToDateTime(cab.Rows[0]["vigencia"]);
            }

            var det = BD.EjecutarConsulta("spCotizacionDetalleMostrar", new SqlParameter("@folio_cotizacion", folio));
            foreach (System.Data.DataRow r in det.Rows)
            {
                lineas.Add(new LineaDetalle
                {
                    ParteId = Convert.ToInt32(r["parte_id"]),
                    Parte = r["refaccion"]?.ToString(),
                    PrecioUnitario = Convert.ToDecimal(r["precio_unitario"]),
                    Cantidad = Convert.ToInt32(r["cantidad"]),
                    DescuentoPorc = Convert.ToDecimal(r["descuento"])
                });
            }
        }

        private void AgregarLinea()
        {
            if (cmbParte.SelectedValue == null) return;
            var fila = ((System.Data.DataRowView)cmbParte.SelectedItem).Row;
            lineas.Add(new LineaDetalle
            {
                ParteId = Convert.ToInt32(fila["id_parte"]),
                Parte = fila["Nombre"].ToString(),
                PrecioUnitario = Convert.ToDecimal(fila["precio"]),
                Cantidad = (int)numCantidad.Value,
                DescuentoPorc = numDescuento.Value
            });
        }

        private void QuitarLinea()
        {
            if (dgv.CurrentRow?.DataBoundItem is LineaDetalle linea)
                lineas.Remove(linea);
        }

        private void ActualizarTotales()
        {
            var subtotal = lineas.Sum(l => l.Subtotal);
            var iva = Math.Round(subtotal * 0.16m, 2);
            var total = subtotal + iva;
            lblSubtotal.Text = subtotal.ToString("C2");
            lblIva.Text = iva.ToString("C2");
            lblTotal.Text = total.ToString("C2");
        }

        private static Label NuevaEtiquetaConcepto(string texto, bool esTotal) => new()
        {
            Text = texto,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = esTotal ? Tema.TextoPrimario : Tema.TextoSecundario,
            Font = esTotal ? new Font("Segoe UI", 12F, FontStyle.Bold) : new Font("Segoe UI", 10F)
        };

        private static Label NuevaEtiquetaImporte(bool esTotal) => new()
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = esTotal ? Tema.Acento : Tema.TextoPrimario,
            Font = esTotal ? new Font("Segoe UI", 12F, FontStyle.Bold) : new Font("Segoe UI", 10F)
        };

        private void GuardarCotizacion()
        {
            if (string.IsNullOrWhiteSpace(txtCliente.Text))
            {
                MessageBox.Show("Captura el nombre del cliente.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (lineas.Count == 0)
            {
                MessageBox.Show("Agrega al menos una refacción a la cotización.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var subtotal = lineas.Sum(l => l.Subtotal);
                var iva = Math.Round(subtotal * 0.16m, 2);
                var total = subtotal + iva;

                int folio;
                if (folioEditar == null)
                {
                    folio = Convert.ToInt32(BD.EjecutarEscalar("spCotizacionesAlta",
                        new SqlParameter("@cliente", txtCliente.Text.Trim()),
                        new SqlParameter("@subtotal", subtotal),
                        new SqlParameter("@iva", iva),
                        new SqlParameter("@total", total),
                        new SqlParameter("@vigencia_dias", dtVigencia.Value.Date)));
                }
                else
                {
                    folio = folioEditar.Value;
                    // Se actualiza la cabecera con su SP y se reemplaza el detalle completo.
                    BD.EjecutarNoQuery("spCotizacionesActualizarCabecera",
                        new SqlParameter("@folio", folio),
                        new SqlParameter("@cliente", txtCliente.Text.Trim()),
                        new SqlParameter("@subtotal", subtotal),
                        new SqlParameter("@IVA", iva),
                        new SqlParameter("@total", total),
                        new SqlParameter("@vigencia", dtVigencia.Value.Date));

                    BD.EjecutarNoQuery("spCotizacionDetalleBaja", new SqlParameter("@folio_cotizacion", folio));
                }

                foreach (var l in lineas)
                {
                    BD.EjecutarEscalar("spCotizacionDetalleAlta",
                        new SqlParameter("@folio_cotizacion", folio),
                        new SqlParameter("@parte_id", l.ParteId),
                        new SqlParameter("@cantidad", l.Cantidad),
                        new SqlParameter("@precio_unitario", l.PrecioUnitario),
                        new SqlParameter("@descuento", l.DescuentoPorc),
                        new SqlParameter("@subtotal", l.Subtotal));
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo guardar la cotización: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
