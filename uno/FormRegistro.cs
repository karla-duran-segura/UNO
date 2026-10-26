using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace uno
{
    /*
      QUÉ ES
      - Pantalla para registrar jugadores nuevos en la base de datos.
      - Pide el nombre, lo manda con BaseDatos.CrearJugador (POST /jugadores en la API) y avisa
        si salió bien o por qué falló (vacío, repetido, API apagada).
      - Se pueden registrar varios jugadores seguidos sin cerrarla. Al cerrarse,
        SeRegistroAlguno le dice a FormInicio si debe recargar la lista de jugadores.

      REGLAS DEL NOMBRE (las mismas de la tabla Jugadores)
      - No puede estar vacío ni pasar de 50 caracteres.
      - No puede repetirse. La base compara sin distinguir mayúsculas ni acentos
        ("karla" y "Karla" son el mismo nombre), y la API responde 409.
    */
    public class FormRegistro : Form
    {
        private const int MAX_NOMBRE = 50;   // igual que VARCHAR(50) en la tabla Jugadores

        private static readonly Color COLOR_ERROR = Color.FromArgb(190, 60, 90);

        private readonly BaseDatos bd;
        private int registrados = 0;

        private Label lblTitulo;
        private Label lblIndicacion;
        private Label lblMensaje;
        private TextBox txtNombre;
        private BotonPastel btnRegistrar;
        private BotonPastel btnCerrar;

        public bool SeRegistroAlguno
        {
            get { return registrados > 0; }
        }

        public FormRegistro(BaseDatos bd)
        {
            this.bd = bd;

            Text = "UNO - Registrar jugador";
            ClientSize = new Size(460, 330);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            DoubleBuffered = true;

            CrearControles();
        }

        private void CrearControles()
        {
            lblTitulo = new Label();
            lblTitulo.AutoSize = false;
            lblTitulo.BackColor = Color.Transparent;
            lblTitulo.ForeColor = Tema.Texto;
            lblTitulo.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Regular, GraphicsUnit.Point);
            lblTitulo.Text = "Registrar jugador";
            lblTitulo.SetBounds(28, 22, 404, 44);
            Controls.Add(lblTitulo);

            lblIndicacion = new Label();
            lblIndicacion.AutoSize = false;
            lblIndicacion.BackColor = Color.Transparent;
            lblIndicacion.ForeColor = Tema.Suave;
            lblIndicacion.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            lblIndicacion.Text = "Escribe el nombre del nuevo jugador (máximo " + MAX_NOMBRE +
                                 " caracteres). No puede repetirse.";
            lblIndicacion.SetBounds(28, 72, 404, 40);
            Controls.Add(lblIndicacion);

            txtNombre = new TextBox();
            txtNombre.Font = new Font("Segoe UI", 14F, FontStyle.Regular, GraphicsUnit.Point);
            txtNombre.MaxLength = MAX_NOMBRE;
            txtNombre.SetBounds(28, 124, 404, 32);
            txtNombre.TabIndex = 0;
            txtNombre.KeyDown += TxtNombre_KeyDown;
            Controls.Add(txtNombre);

            lblMensaje = new Label();
            lblMensaje.AutoSize = false;
            lblMensaje.BackColor = Color.Transparent;
            lblMensaje.ForeColor = Tema.Suave;
            lblMensaje.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            lblMensaje.SetBounds(28, 166, 404, 44);
            Controls.Add(lblMensaje);

            btnRegistrar = new BotonPastel();
            btnRegistrar.Text = "Registrar";
            btnRegistrar.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Regular, GraphicsUnit.Point);
            btnRegistrar.SetBounds(28, 218, 404, 44);
            btnRegistrar.TabIndex = 1;
            btnRegistrar.Click += delegate { Registrar(); };
            Controls.Add(btnRegistrar);

            btnCerrar = new BotonPastel();
            btnCerrar.Text = "Volver";
            btnCerrar.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Regular, GraphicsUnit.Point);
            btnCerrar.SetBounds(28, 270, 404, 40);
            btnCerrar.TabIndex = 2;
            btnCerrar.Click += delegate { Close(); };
            Controls.Add(btnCerrar);
        }

        // Enter registra; Esc cierra
        private void TxtNombre_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                Registrar();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void Registrar()
        {
            string nombre = txtNombre.Text.Trim();
            if (nombre.Length == 0)
            {
                Mostrar("Escribe un nombre.", false);
                txtNombre.Focus();
                return;
            }

            btnRegistrar.Enabled = false;
            UseWaitCursor = true;
            try
            {
                JugadorBD nuevo = bd.CrearJugador(nombre);
                registrados++;
                txtNombre.Clear();

                string texto = "¡" + nuevo.nombre + " quedó registrado!";
                if (registrados > 1)
                    texto += "  (" + registrados + " en esta sesión)";
                Mostrar(texto, true);
            }
            catch (InvalidOperationException ex)
            {
                // La API rechazó el nombre (repetido o inválido): el mensaje ya viene listo
                Mostrar(ex.Message, false);
            }
            catch (Exception ex)
            {
                // API apagada, MySQL caído, sin red...
                Mostrar("No se pudo guardar. Revisa que MySQL y la API estén encendidos.\n" + Recortar(ex.Message, 90), false);
            }
            finally
            {
                UseWaitCursor = false;
                btnRegistrar.Enabled = true;
                txtNombre.Focus();
            }
        }

        private void Mostrar(string texto, bool exito)
        {
            lblMensaje.Text = texto;
            lblMensaje.ForeColor = exito ? Tema.Exito : COLOR_ERROR;
        }

        private static string Recortar(string texto, int max)
        {
            if (string.IsNullOrEmpty(texto)) return "(sin detalle)";
            texto = texto.Replace("\r", " ").Replace("\n", " ");
            return texto.Length <= max ? texto : texto.Substring(0, max) + "…";
        }

        // Mismo fondo pastel que FormInicio
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle rect = ClientRectangle;
            if (rect.Width < 2 || rect.Height < 2) { base.OnPaintBackground(e); return; }

            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (LinearGradientBrush fondo = new LinearGradientBrush(rect, Color.White, Color.White, 55f))
            {
                ColorBlend mezcla = new ColorBlend();
                mezcla.Positions = new float[] { 0f, 0.5f, 1f };
                mezcla.Colors = new Color[]
                {
                    Color.FromArgb(240, 230, 255),   // lavanda
                    Color.FromArgb(255, 232, 242),   // rosa
                    Color.FromArgb(255, 241, 220)    // durazno
                };
                fondo.InterpolationColors = mezcla;
                g.FillRectangle(fondo, rect);
            }

            float w = rect.Width, h = rect.Height;
            using (SolidBrush b = new SolidBrush(Color.FromArgb(90, Tema.Rosa)))
                g.FillEllipse(b, -0.10f * w, -0.25f * h, 0.45f * w, 0.45f * w);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(80, Tema.Azul)))
                g.FillEllipse(b, 0.75f * w, 0.70f * h, 0.40f * w, 0.40f * w);
        }
    }
}
