using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Modal de alta/edición de una Parte (refacción). Reúne todos los campos,
    /// la selección de imagen y las aplicaciones de la parte.
    ///
    /// Una aplicación es "esta parte sirve en esta versión en estos años"
    /// (RelPartesVersion con anio_desde/anio_hasta). Una parte puede tener varias:
    /// p. ej. una barra cardan para Grand Cherokee Laredo 1999-2002 y Limited
    /// 1999-2002, mientras que la Laredo 2003-2004 lleva otra parte distinta.
    /// Se capturan con los combos Marca → Modelo → Versión y Desde/Hasta (años
    /// del modelo) y se guardan todas juntas con spParteAplicacionesGuardar.
    ///
    /// Las equivalencias solo se administran al editar (requieren un id ya guardado).
    /// </summary>
    public class FormParteEdicion : FormEdicionBase
    {
        /// <summary>Renglón de la lista de aplicaciones de la parte.</summary>
        private class Aplicacion
        {
            [Browsable(false)] public int VersionId { get; set; }
            public string Marca { get; set; }
            public string Modelo { get; set; }
            [DisplayName("Versión")] public string Version { get; set; }
            [DisplayName("Desde")] public int Desde { get; set; }
            [DisplayName("Hasta")] public int Hasta { get; set; }
        }

        private readonly TextBox txtNumParte, txtNombre, txtDescripcion, txtCosto, txtPrecio, txtExistencias, txtImgPath;
        private readonly ComboBox cmbCategoria, cmbProveedor, cmbUnidad, cmbMarca, cmbModelo, cmbVersion, cmbDesde, cmbHasta;
        private readonly PictureBox picImagen;
        private readonly Button btnEquivalencias;
        private readonly BindingList<Aplicacion> aplicaciones = new();
        private readonly DataGridView dgvAplicaciones = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        // Id de la parte ya guardada en este modal. En un alta se llena tras el
        // INSERT: si después falla el guardado de las aplicaciones y el usuario
        // reintenta, se actualiza esa parte en vez de duplicarla.
        private int? idGuardado;

        public FormParteEdicion(DataRowView fila) : base("Partes (refacciones)", fila)
        {
            Width = 920;
            Height = 800;
            if (!EsNuevo) idGuardado = Convert.ToInt32(Fila["id_parte"]);

            // Rejilla de 3 pares etiqueta/campo: las etiquetas toman el ancho de su
            // texto con la fuente del tema y los campos se reparten el resto, así
            // nada se corta ni se empalma (con posiciones fijas sí pasaba).
            var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 7 };
            for (int i = 0; i < 3; i++)
            {
                tabla.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            }
            for (int i = 0; i < 6; i++)
                tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tabla.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panelCampos.Controls.Add(tabla);

            void Agregar(string etiqueta, Control campo, int col, int fila, int columnasCampo = 1)
            {
                // En campos altos (descripción, aplicaciones, vista previa) la etiqueta va arriba.
                bool alto = campo is PictureBox || campo is Panel || (campo is TextBox t && t.Multiline);
                tabla.Controls.Add(new Label
                {
                    Text = etiqueta,
                    AutoSize = true,
                    Anchor = alto ? AnchorStyles.Top | AnchorStyles.Left : AnchorStyles.Left,
                    Margin = new Padding(col == 0 ? 0 : 12, alto ? 9 : 6, 6, 6)
                }, col, fila);
                if (campo.Anchor == (AnchorStyles.Top | AnchorStyles.Left))
                    campo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                campo.Margin = new Padding(0, 6, 0, 6);
                tabla.Controls.Add(campo, col + 1, fila);
                tabla.SetColumnSpan(campo, columnasCampo);
            }

            ComboBox NuevoCombo() => new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };

            txtNumParte = new TextBox();
            txtNombre = new TextBox();
            Agregar("Núm. de parte:", txtNumParte, 0, 0);
            Agregar("Nombre:", txtNombre, 2, 0, 3);

            cmbCategoria = NuevoCombo();
            cmbProveedor = NuevoCombo();
            cmbUnidad = NuevoCombo();
            Agregar("Categoría:", cmbCategoria, 0, 1);
            Agregar("Proveedor:", cmbProveedor, 2, 1);
            Agregar("Unidad:", cmbUnidad, 4, 1);

            txtCosto = new TextBox();
            txtPrecio = new TextBox();
            txtExistencias = new TextBox();
            Agregar("Costo:", txtCosto, 0, 2);
            Agregar("Precio:", txtPrecio, 2, 2);
            Agregar("Existencias:", txtExistencias, 4, 2);

            txtImgPath = new TextBox { ReadOnly = true };
            Agregar("Imagen:", txtImgPath, 0, 3, 4);
            var btnExaminar = new Button { Text = "Examinar...", Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(8, 5, 0, 5) };
            Tema.BotonSecundario(btnExaminar);
            btnExaminar.Click += (s, e) => ExaminarImagen();
            tabla.Controls.Add(btnExaminar, 5, 3);

            txtDescripcion = new TextBox { Multiline = true, Height = 60, ScrollBars = ScrollBars.Vertical };
            Agregar("Descripción:", txtDescripcion, 0, 4, 5);

            // --- Aplicaciones: combos de captura arriba y la lista abajo ---
            cmbMarca = NuevoCombo();
            cmbModelo = NuevoCombo();
            cmbVersion = NuevoCombo();
            cmbDesde = NuevoCombo();
            cmbHasta = NuevoCombo();
            var panelCaptura = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, Padding = new Padding(0, 0, 0, 6) };
            FiltrosCombo.AgregarConEtiqueta(panelCaptura, "Marca", cmbMarca, 120);
            FiltrosCombo.AgregarConEtiqueta(panelCaptura, "Modelo", cmbModelo, 130);
            FiltrosCombo.AgregarConEtiqueta(panelCaptura, "Versión", cmbVersion, 120);
            FiltrosCombo.AgregarConEtiqueta(panelCaptura, "Desde", cmbDesde, 72);
            FiltrosCombo.AgregarConEtiqueta(panelCaptura, "Hasta", cmbHasta, 72);
            var btnAgregarApl = new Button { Text = "Agregar", Width = 80, Margin = new Padding(0, 17, 6, 0) };
            var btnQuitarApl = new Button { Text = "Quitar", Width = 70, Margin = new Padding(0, 17, 0, 0) };
            Tema.BotonPrimario(btnAgregarApl);
            Tema.BotonSecundario(btnQuitarApl);
            btnAgregarApl.Click += (s, e) => AgregarAplicacion();
            btnQuitarApl.Click += (s, e) => QuitarAplicacion();
            panelCaptura.Controls.Add(btnAgregarApl);
            panelCaptura.Controls.Add(btnQuitarApl);

            Tema.EstilizarGrid(dgvAplicaciones);
            dgvAplicaciones.DataSource = aplicaciones;
            // Los años ocupan poco; el resto del ancho es para marca/modelo/versión.
            dgvAplicaciones.DataBindingComplete += (s, e) =>
            {
                dgvAplicaciones.Columns[nameof(Aplicacion.Modelo)].FillWeight = 140;
                dgvAplicaciones.Columns[nameof(Aplicacion.Desde)].FillWeight = 70;
                dgvAplicaciones.Columns[nameof(Aplicacion.Hasta)].FillWeight = 70;
            };

            var panelAplicaciones = new Panel { Height = 230 };
            panelAplicaciones.Controls.Add(dgvAplicaciones); // Fill primero
            panelAplicaciones.Controls.Add(panelCaptura);
            Agregar("Aplicaciones:", panelAplicaciones, 0, 5, 5);

            picImagen = new PictureBox
            {
                Width = 300,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = SystemColors.ControlLightLight
            };
            Agregar("Vista previa:", picImagen, 0, 6, 3);

            // Las equivalencias requieren una parte ya guardada; solo al editar.
            btnEquivalencias = new Button { Text = "Equivalencias...", Width = 140, Anchor = AnchorStyles.Top | AnchorStyles.Left, Margin = new Padding(12, 6, 0, 6), Visible = !EsNuevo };
            Tema.BotonSecundario(btnEquivalencias);
            btnEquivalencias.Click += (s, e) => AbrirEquivalencias();
            tabla.Controls.Add(btnEquivalencias, 4, 6);
            tabla.SetColumnSpan(btnEquivalencias, 2);

            cmbMarca.SelectedIndexChanged += (s, e) => CargarModelosPorMarca();
            cmbModelo.SelectedIndexChanged += (s, e) => { CargarVersionesPorModelo(); CargarAniosPorModelo(); };
            cmbDesde.SelectedIndexChanged += (s, e) =>
            {
                // El rango no puede quedar al revés: Hasta se recorre a Desde.
                if (AnioDe(cmbDesde) is int d && AnioDe(cmbHasta) is int h && h < d)
                    cmbHasta.SelectedIndex = cmbDesde.SelectedIndex;
            };

            CargarCombosIndependientes();
        }

        private void CargarCombosIndependientes()
        {
            CargarCombo(cmbCategoria, BD.EjecutarConsulta("spTipoRefaCatMostrar"), "nombre", "id_tipoRefaCat");
            CargarCombo(cmbProveedor, BD.EjecutarConsulta("spProveedoresMostrar"), "nombre", "id_proveedor");
            CargarCombo(cmbUnidad, BD.EjecutarConsulta("spUnidadesCatMostrar"), "nombre", "id_unidadesCat");
            CargarCombo(cmbMarca, BD.EjecutarConsulta("spMarcaMostrar"), "nombre", "id_marca");
            CargarModelosPorMarca();
        }

        // Los miembros van antes que el DataSource: así, cuando el enlace dispara
        // SelectedIndexChanged, SelectedValue ya es el id y la cascada
        // (Modelo → Versión y Años) se carga desde la primera selección.
        private static void CargarCombo(ComboBox cmb, DataTable dt, string display, string value)
        {
            cmb.DisplayMember = display;
            cmb.ValueMember = value;
            cmb.DataSource = dt;
        }

        // Mientras el combo se enlaza SelectedValue puede devolver el DataRowView
        // completo en vez del id; por eso solo se toma cuando ya es un int.
        private static int? IdDeCombo(ComboBox cmb) => cmb.SelectedValue is int v ? v : (int?)null;

        // Los años del catálogo son texto ("1999"); en las aplicaciones se guardan como número.
        private static int? AnioDe(ComboBox cmb) => int.TryParse(cmb.Text, out var anio) ? anio : null;

        // Asignar SelectedValue = null a un combo enlazado lanza ArgumentNullException,
        // así que cuando el valor es nulo/DBNull se deselecciona el combo.
        private static void SeleccionarEnCombo(ComboBox cmb, object valor)
        {
            if (valor == null || valor == DBNull.Value)
                cmb.SelectedIndex = -1;
            else
                cmb.SelectedValue = valor;
        }

        private void CargarModelosPorMarca()
        {
            var marca = IdDeCombo(cmbMarca);
            if (marca == null) { cmbModelo.DataSource = null; return; }
            var dt = BD.EjecutarConsulta("sp_ObtenerModelos",
                new SqlParameter("@IdMarca", marca.Value),
                new SqlParameter("@IdModelo", DBNull.Value));
            CargarCombo(cmbModelo, dt, "modelo", "id_modelo");
        }

        private void CargarVersionesPorModelo()
        {
            var modelo = IdDeCombo(cmbModelo);
            if (modelo == null) { cmbVersion.DataSource = null; return; }
            var dt = BD.EjecutarConsulta("sp_ObtenerVersiones", new SqlParameter("@IdModelo", modelo.Value));
            CargarCombo(cmbVersion, dt, "nombre", "version_id");
        }

        /// <summary>
        /// Años en que existe el modelo (RelAnioModelo). Si el modelo no tiene
        /// años registrados se ofrecen todos los del catálogo. Por defecto queda
        /// seleccionado el rango completo del modelo.
        /// </summary>
        private void CargarAniosPorModelo()
        {
            var modelo = IdDeCombo(cmbModelo);
            if (modelo == null) { cmbDesde.DataSource = null; cmbHasta.DataSource = null; return; }

            var dt = BD.EjecutarConsulta("sp_ObtenerAnios", new SqlParameter("@IdModelo", modelo.Value));
            if (dt.Rows.Count == 0)
                dt = BD.EjecutarConsulta("sp_ObtenerAnios", new SqlParameter("@IdModelo", DBNull.Value));

            // Cada combo necesita su propia copia: con el mismo DataTable comparten
            // la selección (mismo CurrencyManager).
            CargarCombo(cmbDesde, dt, "nombre", "id_anio");
            CargarCombo(cmbHasta, dt.Copy(), "nombre", "id_anio");
            if (cmbHasta.Items.Count > 0) cmbHasta.SelectedIndex = cmbHasta.Items.Count - 1;
        }

        private void AgregarAplicacion()
        {
            if (IdDeCombo(cmbVersion) is not int versionId)
            {
                MessageBox.Show("Selecciona la marca, el modelo y la versión.", "Aplicaciones", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (AnioDe(cmbDesde) is not int desde || AnioDe(cmbHasta) is not int hasta)
            {
                MessageBox.Show("Selecciona el año desde y el año hasta.", "Aplicaciones", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (desde > hasta)
            {
                MessageBox.Show("El año desde no puede ser mayor que el año hasta.", "Aplicaciones", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Para una misma versión los rangos no se empalman (serían ambiguos).
            var empalmada = aplicaciones.FirstOrDefault(a => a.VersionId == versionId && desde <= a.Hasta && a.Desde <= hasta);
            if (empalmada != null)
            {
                MessageBox.Show($"La versión {empalmada.Version} ya tiene el rango {empalmada.Desde}-{empalmada.Hasta}, que se empalma con {desde}-{hasta}.",
                    "Aplicaciones", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            aplicaciones.Add(new Aplicacion
            {
                VersionId = versionId,
                Marca = cmbMarca.Text,
                Modelo = cmbModelo.Text,
                Version = cmbVersion.Text,
                Desde = desde,
                Hasta = hasta
            });
        }

        private void QuitarAplicacion()
        {
            if (dgvAplicaciones.CurrentRow?.DataBoundItem is Aplicacion apl)
                aplicaciones.Remove(apl);
        }

        private void ExaminarImagen()
        {
            using var ofd = new OpenFileDialog { Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp|Todos los archivos|*.*" };
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                txtImgPath.Text = ofd.FileName;
                MostrarImagen(ofd.FileName);
            }
        }

        // Carga la imagen en memoria (para no bloquear el archivo) y tolera rutas
        // vacías o inexistentes dejando la vista previa en blanco.
        private void MostrarImagen(string ruta)
        {
            picImagen.Image?.Dispose();
            picImagen.Image = null;

            if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta)) return;

            try
            {
                var bytes = File.ReadAllBytes(ruta);
                using var ms = new MemoryStream(bytes);
                picImagen.Image = Image.FromStream(ms);
            }
            catch
            {
                picImagen.Image = null;
            }
        }

        protected override void CargarRegistro()
        {
            // spPartePorId expone los ids de llaves foráneas (spPartesMostrar solo
            // devuelve los nombres), para poder precargar los combos de edición.
            var dt = BD.EjecutarConsulta("spPartePorId", new SqlParameter("@id_parte", idGuardado.Value));
            if (dt.Rows.Count == 0) return;
            var fila = dt.Rows[0];

            txtNumParte.Text = fila["num_parte"]?.ToString();
            txtNombre.Text = fila["nombre"]?.ToString();
            txtDescripcion.Text = fila["descripcion"]?.ToString();
            txtCosto.Text = fila["costo"]?.ToString();
            txtPrecio.Text = fila["precio"]?.ToString();
            txtExistencias.Text = fila["existencias"]?.ToString();
            txtImgPath.Text = fila["img_path"]?.ToString();
            MostrarImagen(txtImgPath.Text);

            SeleccionarEnCombo(cmbCategoria, fila["tipoRefaCat_id"]);
            SeleccionarEnCombo(cmbProveedor, fila["proveedor_id"]);
            SeleccionarEnCombo(cmbUnidad, fila["unidad_id"]);

            var apls = BD.EjecutarConsulta("spParteAplicacionesMostrar", new SqlParameter("@parte_id", idGuardado.Value));
            foreach (DataRow a in apls.Rows)
            {
                // Relaciones anteriores al rango de años (sin anio_desde) se
                // muestran con 0 para que se corrijan al editar.
                aplicaciones.Add(new Aplicacion
                {
                    VersionId = Convert.ToInt32(a["version_id"]),
                    Marca = a["marca"].ToString(),
                    Modelo = a["modelo"].ToString(),
                    Version = a["version"].ToString(),
                    Desde = a["anio_desde"] == DBNull.Value ? 0 : Convert.ToInt32(a["anio_desde"]),
                    Hasta = a["anio_hasta"] == DBNull.Value ? 0 : Convert.ToInt32(a["anio_hasta"])
                });
            }
        }

        protected override void Guardar()
        {
            if (string.IsNullOrWhiteSpace(txtNumParte.Text) || string.IsNullOrWhiteSpace(txtNombre.Text))
                throw new Exception("Número de parte y nombre son obligatorios.");
            if (aplicaciones.Any(a => a.Desde == 0 || a.Hasta == 0))
                throw new Exception("Hay aplicaciones sin años. Quítalas y vuelve a agregarlas con su rango de años.");

            double costo = ParseDouble(txtCosto.Text, "Costo");
            double precio = ParseDouble(txtPrecio.Text, "Precio");
            double existencias = ParseDouble(txtExistencias.Text, "Existencias");

            int? categoriaId = IdDeCombo(cmbCategoria);
            int? proveedorId = IdDeCombo(cmbProveedor);
            int? unidadId = IdDeCombo(cmbUnidad);

            if (idGuardado == null)
            {
                idGuardado = Convert.ToInt32(BD.EjecutarEscalar("spPartesInsertar",
                    new SqlParameter("@tipoRefaCat_id", (object)categoriaId ?? DBNull.Value),
                    new SqlParameter("@proveedor_id", (object)proveedorId ?? DBNull.Value),
                    new SqlParameter("@unidad_id", (object)unidadId ?? DBNull.Value),
                    new SqlParameter("@nombre", txtNombre.Text.Trim()),
                    new SqlParameter("@descripcion", (object)txtDescripcion.Text.Trim() ?? DBNull.Value),
                    new SqlParameter("@num_parte", txtNumParte.Text.Trim()),
                    new SqlParameter("@img_path", (object)txtImgPath.Text ?? DBNull.Value),
                    new SqlParameter("@costo", costo),
                    new SqlParameter("@precio", precio),
                    new SqlParameter("@existencias", existencias)));
            }
            else
            {
                BD.EjecutarNoQuery("spPartesModificar",
                    new SqlParameter("@id_parte", idGuardado.Value),
                    new SqlParameter("@tipoRefaCat_id", (object)categoriaId ?? DBNull.Value),
                    new SqlParameter("@proveedor_id", (object)proveedorId ?? DBNull.Value),
                    new SqlParameter("@unidad_id", (object)unidadId ?? DBNull.Value),
                    new SqlParameter("@nombre", txtNombre.Text.Trim()),
                    new SqlParameter("@descripcion", txtDescripcion.Text.Trim()),
                    new SqlParameter("@num_parte", txtNumParte.Text.Trim()),
                    new SqlParameter("@img_path", (object)txtImgPath.Text ?? DBNull.Value),
                    new SqlParameter("@costo", (decimal)costo),
                    new SqlParameter("@precio", (decimal)precio),
                    new SqlParameter("@existencias", (int)existencias));
            }

            // Todas las aplicaciones se reemplazan juntas (en una transacción en el SP).
            // Se mandan como parámetro tipo tabla (dbo.UDT_ParteAplicacion), no JSON:
            // OPENJSON no existe en SQL Server 2014.
            var tabla = new DataTable();
            tabla.Columns.Add("version_id", typeof(int));
            tabla.Columns.Add("anio_desde", typeof(int));
            tabla.Columns.Add("anio_hasta", typeof(int));
            foreach (var a in aplicaciones)
                tabla.Rows.Add(a.VersionId, a.Desde, a.Hasta);
            BD.EjecutarNoQuery("spParteAplicacionesGuardar",
                new SqlParameter("@parte_id", idGuardado.Value),
                new SqlParameter("@aplicaciones", SqlDbType.Structured)
                {
                    TypeName = "dbo.UDT_ParteAplicacion",
                    Value = tabla
                });
        }

        private static double ParseDouble(string texto, string campo)
        {
            if (string.IsNullOrWhiteSpace(texto)) return 0;
            if (!double.TryParse(texto, out var valor))
                throw new Exception($"El campo '{campo}' debe ser numérico.");
            return valor;
        }

        private void AbrirEquivalencias()
        {
            new FormEquivalencias(idGuardado.Value, txtNombre.Text).ShowDialog(this);
        }
    }
}
