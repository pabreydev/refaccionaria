using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using RefaccionariaApp.Data;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Modal de alta/edición de una Parte (refacción). Reúne todos los campos,
    /// los combos en cascada Marca → Modelo → Versión y la selección de imagen.
    ///
    /// Notas sobre el script original (idénticas a la versión anterior):
    ///  - spPartesInsertar no recibe unidad_id (solo se agregó a spPartesModificar),
    ///    así que en el alta se completa con una actualización aparte.
    ///  - Las equivalencias solo se administran al editar (requieren un id ya guardado).
    /// </summary>
    public class FormParteEdicion : FormEdicionBase
    {
        private readonly TextBox txtNumParte, txtNombre, txtDescripcion, txtCosto, txtPrecio, txtExistencias, txtImgPath;
        private readonly ComboBox cmbCategoria, cmbProveedor, cmbUnidad, cmbMarca, cmbModelo, cmbVersion;
        private readonly PictureBox picImagen;
        private readonly Button btnEquivalencias;
        private bool cargandoParaEditar = false;

        private int IdParte => EsNuevo ? 0 : Convert.ToInt32(Fila["id_parte"]);

        public FormParteEdicion(DataRowView fila) : base("Partes (refacciones)", fila)
        {
            Width = 920;
            Height = 520;

            panelCampos.Controls.Add(new Label { Text = "Núm. de parte:", Left = 10, Top = 12, Width = 100 });
            txtNumParte = new TextBox { Left = 115, Top = 9, Width = 150 };
            panelCampos.Controls.Add(txtNumParte);

            panelCampos.Controls.Add(new Label { Text = "Nombre:", Left = 280, Top = 12, Width = 60 });
            txtNombre = new TextBox { Left = 345, Top = 9, Width = 250 };
            panelCampos.Controls.Add(txtNombre);

            panelCampos.Controls.Add(new Label { Text = "Categoría:", Left = 10, Top = 42, Width = 100 });
            cmbCategoria = new ComboBox { Left = 115, Top = 39, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            panelCampos.Controls.Add(cmbCategoria);

            panelCampos.Controls.Add(new Label { Text = "Proveedor:", Left = 280, Top = 42, Width = 60 });
            cmbProveedor = new ComboBox { Left = 345, Top = 39, Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
            panelCampos.Controls.Add(cmbProveedor);

            panelCampos.Controls.Add(new Label { Text = "Unidad:", Left = 10, Top = 72, Width = 100 });
            cmbUnidad = new ComboBox { Left = 115, Top = 69, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            panelCampos.Controls.Add(cmbUnidad);

            panelCampos.Controls.Add(new Label { Text = "Costo:", Left = 280, Top = 72, Width = 60 });
            txtCosto = new TextBox { Left = 345, Top = 69, Width = 90 };
            panelCampos.Controls.Add(txtCosto);

            panelCampos.Controls.Add(new Label { Text = "Precio:", Left = 445, Top = 72, Width = 50 });
            txtPrecio = new TextBox { Left = 495, Top = 69, Width = 90 };
            panelCampos.Controls.Add(txtPrecio);

            panelCampos.Controls.Add(new Label { Text = "Existencias:", Left = 595, Top = 72, Width = 70 });
            txtExistencias = new TextBox { Left = 665, Top = 69, Width = 80 };
            panelCampos.Controls.Add(txtExistencias);

            panelCampos.Controls.Add(new Label { Text = "Marca:", Left = 10, Top = 102, Width = 100 });
            cmbMarca = new ComboBox { Left = 115, Top = 99, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            panelCampos.Controls.Add(cmbMarca);

            panelCampos.Controls.Add(new Label { Text = "Modelo:", Left = 280, Top = 102, Width = 60 });
            cmbModelo = new ComboBox { Left = 345, Top = 99, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            panelCampos.Controls.Add(cmbModelo);

            panelCampos.Controls.Add(new Label { Text = "Versión:", Left = 505, Top = 102, Width = 60 });
            cmbVersion = new ComboBox { Left = 570, Top = 99, Width = 175, DropDownStyle = ComboBoxStyle.DropDownList };
            panelCampos.Controls.Add(cmbVersion);

            panelCampos.Controls.Add(new Label { Text = "Imagen:", Left = 10, Top = 132, Width = 100 });
            txtImgPath = new TextBox { Left = 115, Top = 129, Width = 380, ReadOnly = true };
            panelCampos.Controls.Add(txtImgPath);
            var btnExaminar = new Button { Text = "Examinar...", Left = 500, Top = 127, Width = 90 };
            Tema.BotonSecundario(btnExaminar);
            btnExaminar.Click += (s, e) => ExaminarImagen();
            panelCampos.Controls.Add(btnExaminar);

            panelCampos.Controls.Add(new Label { Text = "Descripción:", Left = 10, Top = 162, Width = 100 });
            txtDescripcion = new TextBox { Left = 115, Top = 159, Width = 770, Height = 50, Multiline = true };
            panelCampos.Controls.Add(txtDescripcion);

            panelCampos.Controls.Add(new Label { Text = "Vista previa:", Left = 10, Top = 222, Width = 100 });
            picImagen = new PictureBox
            {
                Left = 115,
                Top = 222,
                Width = 300,
                Height = 170,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = SystemColors.ControlLightLight
            };
            panelCampos.Controls.Add(picImagen);

            // Las equivalencias requieren una parte ya guardada; solo al editar.
            btnEquivalencias = new Button { Text = "Equivalencias...", Left = 440, Top = 222, Width = 140, Visible = !EsNuevo };
            Tema.BotonSecundario(btnEquivalencias);
            btnEquivalencias.Click += (s, e) => AbrirEquivalencias();
            panelCampos.Controls.Add(btnEquivalencias);

            cmbMarca.SelectedIndexChanged += (s, e) => { if (!cargandoParaEditar) CargarModelosPorMarca(); };
            cmbModelo.SelectedIndexChanged += (s, e) => { if (!cargandoParaEditar) CargarVersionesPorModelo(); };

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

        private static void CargarCombo(ComboBox cmb, DataTable dt, string display, string value)
        {
            cmb.DataSource = dt;
            cmb.DisplayMember = display;
            cmb.ValueMember = value;
        }

        // Durante el enlace del combo (al asignar DataSource antes de ValueMember)
        // SelectedValue puede devolver el DataRowView completo en vez del id; por eso
        // solo se toma cuando ya es un int.
        private static int? IdDeCombo(ComboBox cmb) => cmb.SelectedValue is int v ? v : (int?)null;

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
            var dt = BD.EjecutarConsulta("spPartePorId", new SqlParameter("@id_parte", IdParte));
            if (dt.Rows.Count == 0) return;
            var fila = dt.Rows[0];

            var version = BD.EjecutarConsulta("spParteVersionActual", new SqlParameter("@parte_id", IdParte));

            txtNumParte.Text = fila["num_parte"]?.ToString();
            txtNombre.Text = fila["nombre"]?.ToString();
            txtDescripcion.Text = fila["descripcion"]?.ToString();
            txtCosto.Text = fila["costo"]?.ToString();
            txtPrecio.Text = fila["precio"]?.ToString();
            txtExistencias.Text = fila["existencias"]?.ToString();
            txtImgPath.Text = fila["img_path"]?.ToString();
            MostrarImagen(txtImgPath.Text);

            cargandoParaEditar = true;
            try
            {
                SeleccionarEnCombo(cmbCategoria, fila["tipoRefaCat_id"]);
                SeleccionarEnCombo(cmbProveedor, fila["proveedor_id"]);
                SeleccionarEnCombo(cmbUnidad, fila["unidad_id"]);

                if (version.Rows.Count > 0)
                {
                    SeleccionarEnCombo(cmbMarca, version.Rows[0]["id_marca"]);
                    CargarModelosPorMarca();
                    SeleccionarEnCombo(cmbModelo, version.Rows[0]["modelo_id"]);
                    CargarVersionesPorModelo();
                    SeleccionarEnCombo(cmbVersion, version.Rows[0]["version_id"]);
                }
            }
            finally
            {
                cargandoParaEditar = false;
            }
        }

        protected override void Guardar()
        {
            if (string.IsNullOrWhiteSpace(txtNumParte.Text) || string.IsNullOrWhiteSpace(txtNombre.Text))
                throw new Exception("Número de parte y nombre son obligatorios.");

            double costo = ParseDouble(txtCosto.Text, "Costo");
            double precio = ParseDouble(txtPrecio.Text, "Precio");
            double existencias = ParseDouble(txtExistencias.Text, "Existencias");

            int? categoriaId = IdDeCombo(cmbCategoria);
            int? proveedorId = IdDeCombo(cmbProveedor);
            int? unidadId = IdDeCombo(cmbUnidad);
            int? versionId = IdDeCombo(cmbVersion);

            if (EsNuevo)
            {
                var nuevoId = Convert.ToInt32(BD.EjecutarEscalar("spPartesInsertar",
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

                if (versionId != null)
                    BD.EjecutarEscalar("spRelPartesVersionInsertar",
                        new SqlParameter("@version_id", versionId.Value),
                        new SqlParameter("@parte_id", nuevoId));
            }
            else
            {
                BD.EjecutarNoQuery("spPartesModificar",
                    new SqlParameter("@id_parte", IdParte),
                    new SqlParameter("@tipoRefaCat_id", (object)categoriaId ?? DBNull.Value),
                    new SqlParameter("@proveedor_id", (object)proveedorId ?? DBNull.Value),
                    new SqlParameter("@unidad_id", (object)unidadId ?? DBNull.Value),
                    new SqlParameter("@nombre", txtNombre.Text.Trim()),
                    new SqlParameter("@descripcion", txtDescripcion.Text.Trim()),
                    new SqlParameter("@num_parte", txtNumParte.Text.Trim()),
                    new SqlParameter("@img_path", (object)txtImgPath.Text ?? DBNull.Value),
                    new SqlParameter("@costo", (decimal)costo),
                    new SqlParameter("@precio", (decimal)precio),
                    new SqlParameter("@existencias", (int)existencias),
                    new SqlParameter("@version_id", (object)versionId ?? DBNull.Value));
            }
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
            new FormEquivalencias(IdParte, txtNombre.Text).ShowDialog(this);
        }
    }
}
