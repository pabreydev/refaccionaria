using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;
using RefaccionariaApp.Reportes;

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
        // Notifica los cambios de Cantidad (editable en el grid) para que la
        // BindingList dispare ListChanged, se recalculen los totales y se
        // refresque el Subtotal de la línea.
        private class LineaDetalle : INotifyPropertyChanged
        {
            private int cantidad;

            public event PropertyChangedEventHandler PropertyChanged;

            public int ParteId { get; set; }
            public string Parte { get; set; }
            public decimal PrecioUnitario { get; set; }
            public int Cantidad
            {
                get => cantidad;
                set
                {
                    if (cantidad == value) return;
                    cantidad = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Cantidad)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Subtotal)));
                }
            }
            public decimal DescuentoPorc { get; set; }
            public decimal Subtotal => Math.Round(Cantidad * PrecioUnitario * (1 - DescuentoPorc / 100m), 2);
        }

        private int? folioEditar;
        private bool guardada; // ya se guardó al menos una vez (p. ej. al generar el PDF)

        private readonly TextBox txtCliente = new() { Left = 70, Top = 12, Width = 220 };
        private readonly DateTimePicker dtVigencia = new() { Left = 400, Top = 12, Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(30) };
        private readonly ComboBox cmbParte = new() { Left = 70, Top = 45, Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly NumericUpDown numCantidad = new() { Left = 430, Top = 45, Width = 70, Minimum = 1, Maximum = 100000, Value = 1 };
        private readonly NumericUpDown numDescuento = new() { Left = 510, Top = 45, Width = 70, Minimum = 0, Maximum = 100, DecimalPlaces = 2 };
        private readonly BindingList<LineaDetalle> lineas = new();
        private readonly DataGridView dgv = new() { Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, EditMode = DataGridViewEditMode.EditOnEnter, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
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
            var btnCancelar = new Button { Text = "Cancelar", Left = 288, Top = 11, Width = 90, DialogResult = DialogResult.Cancel, CausesValidation = false };
            // Si ya se guardó (al generar el PDF), se cierra con OK para que el
            // listado de cotizaciones se refresque.
            btnCancelar.Click += (s, e) => { DialogResult = guardada ? DialogResult.OK : DialogResult.Cancel; Close(); };
            var btnPdf = new Button { Text = "Generar PDF", Left = 386, Top = 11, Width = 120 };
            btnPdf.Click += (s, e) => GenerarPdf();
            Tema.BotonSecundario(btnQuitarLinea);
            Tema.BotonPrimario(btnGuardar);
            Tema.BotonSecundario(btnCancelar);
            Tema.BotonSecundario(btnPdf);
            panelInferior.Controls.Add(btnQuitarLinea);
            panelInferior.Controls.Add(btnGuardar);
            panelInferior.Controls.Add(btnCancelar);
            panelInferior.Controls.Add(btnPdf);
            // Cerrar con la X después de generar el PDF también refresca el listado.
            FormClosing += (s, e) => { if (guardada && DialogResult != DialogResult.OK) DialogResult = DialogResult.OK; };

            dgv.DataSource = lineas;
            // Solo la Cantidad es editable; el resto de columnas queda de solo lectura.
            dgv.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewColumn col in dgv.Columns)
                    col.ReadOnly = col.DataPropertyName != nameof(LineaDetalle.Cantidad);
            };
            dgv.CellValidating += ValidarCantidad;
            dgv.DataError += (s, e) => e.Cancel = true; // el mensaje lo da ValidarCantidad

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

        private void ValidarCantidad(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (dgv.Columns[e.ColumnIndex].DataPropertyName != nameof(LineaDetalle.Cantidad)) return;
            if (!int.TryParse(Convert.ToString(e.FormattedValue), out var cantidad) || cantidad < 1 || cantidad > 100000)
            {
                MessageBox.Show("La cantidad debe ser un número entero entre 1 y 100,000.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                e.Cancel = true;
            }
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
            if (!Guardar()) return;
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Guarda la cotización sin cerrar el modal. En un alta, el folio asignado
        /// queda en <see cref="folioEditar"/> para que los siguientes guardados
        /// actualicen la misma cotización en vez de crear otra.
        /// </summary>
        private bool Guardar()
        {
            if (string.IsNullOrWhiteSpace(txtCliente.Text))
            {
                MessageBox.Show("Captura el nombre del cliente.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (lineas.Count == 0)
            {
                MessageBox.Show("Agrega al menos una refacción a la cotización.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
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

                folioEditar = folio;
                guardada = true;
                Text = $"Editar cotización (folio {folio})";
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo guardar la cotización: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Guarda la cotización (para que tenga folio y el PDF coincida con la
        /// base) y genera el PDF en la ruta que elija el usuario.
        /// </summary>
        private void GenerarPdf()
        {
            if (!ValidateChildren()) return; // confirma una cantidad en edición

            if (MessageBox.Show("Para generar el PDF se guardará la cotización. ¿Continuar?", "Generar PDF",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            if (!Guardar()) return;

            int folio = folioEditar.Value;
            using var sfd = new SaveFileDialog
            {
                Filter = "Archivo PDF|*.pdf",
                FileName = $"Cotizacion_{folio}.pdf"
            };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var cabecera = BD.EjecutarConsulta("spCotizacionesMostrar", new SqlParameter("@folio", folio));
                var fecha = cabecera.Rows.Count > 0 && cabecera.Rows[0]["fecha"] != DBNull.Value
                    ? Convert.ToDateTime(cabecera.Rows[0]["fecha"])
                    : DateTime.Today;

                // El detalle de la cotización no trae el número de parte; se toma
                // del catálogo ya cargado en el combo de partes.
                var numPartes = ((System.Data.DataTable)cmbParte.DataSource).Rows
                    .Cast<System.Data.DataRow>()
                    .ToDictionary(r => Convert.ToInt32(r["id_parte"]), r => r["Parte"]?.ToString());

                var subtotal = lineas.Sum(l => l.Subtotal);
                var iva = Math.Round(subtotal * 0.16m, 2);
                var datos = new CotizacionPdf.Datos
                {
                    Folio = folio,
                    Cliente = txtCliente.Text.Trim(),
                    Fecha = fecha,
                    Vigencia = dtVigencia.Value.Date,
                    Subtotal = subtotal,
                    Iva = iva,
                    Total = subtotal + iva,
                    Lineas = lineas.Select(l => new CotizacionPdf.Linea
                    {
                        Codigo = numPartes.TryGetValue(l.ParteId, out var num) ? num : l.ParteId.ToString(),
                        Producto = l.Parte,
                        Cantidad = l.Cantidad,
                        PrecioUnitario = l.PrecioUnitario,
                        DescuentoPorc = l.DescuentoPorc,
                        Subtotal = l.Subtotal
                    }).ToList()
                };

                CotizacionPdf.Generar(datos, sfd.FileName);
                Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo generar el PDF: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
