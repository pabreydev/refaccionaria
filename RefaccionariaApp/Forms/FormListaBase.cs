using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RefaccionariaApp.Forms
{
    /// <summary>
    /// Base reutilizable para las pantallas de listado (Marca, Año, Proveedores,
    /// Categorías, Unidades, Modelo, Versión). Muestra un buscador incremental
    /// arriba, la grilla al centro y los botones Nuevo/Editar/Eliminar abajo.
    /// El alta y la edición se hacen en un modal (<see cref="FormEdicionBase"/>)
    /// que cada catálogo concreto construye en <see cref="CrearFormEdicion"/>.
    ///
    /// Por defecto la búsqueda es incremental en cliente: filtra en memoria lo
    /// ya cargado con <see cref="BindingSource.Filter"/>, sin volver a consultar
    /// la base. Una pantalla con tablas grandes puede activar la búsqueda en
    /// servidor (ver <see cref="BusquedaEnServidor"/>): en ese caso cada cambio
    /// del texto re-consulta la base (con un pequeño retraso para no golpearla
    /// en cada tecla) llamando a <see cref="ObtenerDatos(string)"/>.
    /// </summary>
    public abstract class FormListaBase : Form
    {
        protected DataGridView dgv;
        private readonly TextBox txtBuscar;
        private readonly BindingSource bs = new BindingSource();
        private readonly Button btnNuevo, btnEditar, btnEliminar;
        private readonly System.Windows.Forms.Timer debounceBusqueda;

        protected FormListaBase(string titulo)
        {
            Text = titulo;
            Width = 720;
            Height = 520;
            StartPosition = FormStartPosition.CenterParent;
            Font = Tema.FuenteBase;
            BackColor = Tema.Blanco;

            // --- Buscador incremental (arriba) ---
            var panelBusqueda = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(12), BackColor = Tema.Blanco };
            txtBuscar = new TextBox { Left = 12, Top = 14, Width = 360 };
            Tema.EstilizarBuscador(txtBuscar);
            txtBuscar.TextChanged += (s, e) => OnBusquedaCambio();
            panelBusqueda.Controls.Add(txtBuscar);

            // Debounce para la búsqueda en servidor: espera a que el usuario deje
            // de teclear antes de re-consultar la base.
            debounceBusqueda = new System.Windows.Forms.Timer { Interval = 250 };
            debounceBusqueda.Tick += (s, e) => { debounceBusqueda.Stop(); RecargarDatos(); };

            // --- Grilla (centro) ---
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            Tema.EstilizarGrid(dgv);
            dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditarSeleccionado(); };

            // --- Botones (abajo) ---
            var panelBotones = new Panel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(12, 0, 12, 0), BackColor = Tema.Blanco };
            btnNuevo = new Button { Text = "+ Nuevo", Left = 12, Top = 11, Width = 110 };
            btnEditar = new Button { Text = "Editar", Left = 130, Top = 11, Width = 100 };
            btnEliminar = new Button { Text = "Eliminar", Left = 238, Top = 11, Width = 100 };
            Tema.BotonPrimario(btnNuevo);
            Tema.BotonSecundario(btnEditar);
            Tema.BotonSecundario(btnEliminar);
            btnNuevo.Click += (s, e) => AbrirEdicion(null);
            btnEditar.Click += (s, e) => EditarSeleccionado();
            btnEliminar.Click += (s, e) => EliminarSeguro();
            panelBotones.Controls.Add(btnNuevo);
            panelBotones.Controls.Add(btnEditar);
            panelBotones.Controls.Add(btnEliminar);

            // Orden de agregado: primero Fill, luego Bottom/Top para que el docking se respete.
            Controls.Add(dgv);
            Controls.Add(panelBotones);
            Controls.Add(panelBusqueda);

            Load += (s, e) => RecargarDatos();
        }

        /// <summary>Recarga la grilla desde la fuente de datos y reaplica el filtro actual.</summary>
        protected void RecargarDatos()
        {
            bs.Filter = null;
            bs.DataSource = BusquedaEnServidor
                ? ObtenerDatos(txtBuscar.Text.Trim())
                : ObtenerDatos();
            dgv.DataSource = bs;
            OcultarColumnas();
            if (!BusquedaEnServidor) AplicarFiltroCliente();
        }

        private void OnBusquedaCambio()
        {
            if (BusquedaEnServidor)
            {
                debounceBusqueda.Stop();
                debounceBusqueda.Start();
            }
            else
            {
                AplicarFiltroCliente();
            }
        }

        private void OcultarColumnas()
        {
            if (ColumnasOcultas == null) return;
            foreach (var nombre in ColumnasOcultas)
                if (dgv.Columns.Contains(nombre))
                    dgv.Columns[nombre].Visible = false;
        }

        private void AplicarFiltroCliente()
        {
            if (bs.DataSource is not DataTable dt) return;

            var texto = txtBuscar.Text.Trim();
            if (texto.Length == 0) { bs.Filter = null; return; }

            var columnas = ColumnasBusqueda
                ?? dt.Columns.Cast<DataColumn>()
                     .Where(c => c.DataType == typeof(string))
                     .Select(c => c.ColumnName)
                     .ToArray();

            if (columnas.Length == 0) { bs.Filter = null; return; }

            var t = EscaparLike(texto);
            bs.Filter = string.Join(" OR ",
                columnas.Select(c => $"CONVERT([{c}], 'System.String') LIKE '%{t}%'"));
        }

        private void EditarSeleccionado()
        {
            var fila = FilaSeleccionada();
            if (fila == null)
            {
                MessageBox.Show("Selecciona primero un registro de la lista.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            AbrirEdicion(fila);
        }

        private void AbrirEdicion(DataRowView fila)
        {
            using var frm = CrearFormEdicion(fila);
            if (frm.ShowDialog(TopLevelControl) == DialogResult.OK)
                RecargarDatos();
        }

        private void EliminarSeguro()
        {
            var fila = FilaSeleccionada();
            if (fila == null)
            {
                MessageBox.Show("Selecciona primero un registro de la lista.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("¿Eliminar el registro seleccionado?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                EliminarRegistro(Convert.ToInt32(fila[NombreCampoId]));
                RecargarDatos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo eliminar: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataRowView FilaSeleccionada() => dgv.CurrentRow?.DataBoundItem as DataRowView;

        private static string EscaparLike(string s)
        {
            var sb = new StringBuilder();
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case '\'': sb.Append("''"); break;
                    case '%':
                    case '*':
                    case '[': sb.Append('[').Append(ch).Append(']'); break;
                    default: sb.Append(ch); break;
                }
            }
            return sb.ToString();
        }

        /// <summary>Devuelve los datos a listar (normalmente un SP de "Mostrar").</summary>
        protected abstract DataTable ObtenerDatos();

        /// <summary>
        /// Devuelve los datos filtrados por <paramref name="filtro"/>. Solo se usa
        /// cuando <see cref="BusquedaEnServidor"/> es true; por defecto ignora el
        /// filtro y delega en <see cref="ObtenerDatos()"/>.
        /// </summary>
        protected virtual DataTable ObtenerDatos(string filtro) => ObtenerDatos();

        /// <summary>
        /// Cuando es true, la búsqueda re-consulta la base en cada cambio del texto
        /// (vía <see cref="ObtenerDatos(string)"/>) en lugar de filtrar en memoria.
        /// Úsalo en tablas grandes o donde los datos deban verse siempre frescos.
        /// </summary>
        protected virtual bool BusquedaEnServidor => false;

        /// <summary>Nombre de la columna que contiene el id del registro (para eliminar).</summary>
        protected abstract string NombreCampoId { get; }

        /// <summary>Crea el modal de alta/edición. <paramref name="fila"/> es null en alta.</summary>
        protected abstract FormEdicionBase CrearFormEdicion(DataRowView fila);

        /// <summary>Elimina el registro con el id indicado.</summary>
        protected abstract void EliminarRegistro(int id);

        /// <summary>Columnas sobre las que busca el filtro. null = todas las de texto.</summary>
        protected virtual string[] ColumnasBusqueda => null;

        /// <summary>Columnas técnicas (p. ej. FK) a ocultar en la grilla. null = ninguna.</summary>
        protected virtual string[] ColumnasOcultas => null;
    }
}
