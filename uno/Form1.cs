using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace uno
{
    public partial class FormInicio : Form
    {
        private const int Requeridos = 4;

        private readonly BaseDatos baseDatos = new BaseDatos();
        private readonly List<TarjetaJugador> tarjetas = new List<TarjetaJugador>();
        private readonly List<TarjetaJugador> elegidas = new List<TarjetaJugador>();

        // Aviso de error de conexión
        private Label lblErrorTitulo;
        private Label lblErrorDetalle;
        private BotonPastel btnReintentar;

        // Botones creados por código
        private BotonPastel btnRegistrar;
        private BotonPastel btnCrearRed;
        private BotonPastel btnUnirse;

        public FormInicio()
        {
            InitializeComponent();
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
            Load += FormInicio_Load;
            btnComenzar.Click += btnComenzar_Click;
        }

        private void FormInicio_Load(object sender, EventArgs e)
        {
            CrearBotonesExtra();
            CargarJugadores();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = ClientRectangle;

            if (rect.Width < 2 || rect.Height < 2)
            {
                base.OnPaintBackground(e);
                return;
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var fondo = new LinearGradientBrush(
                rect, Color.White, Color.White, 55f))
            {
                fondo.InterpolationColors = new ColorBlend
                {
                    Positions = new[] { 0f, 0.5f, 1f },
                    Colors = new[]
                    {
                        Color.FromArgb(240, 230, 255),
                        Color.FromArgb(255, 232, 242),
                        Color.FromArgb(255, 241, 220)
                    }
                };

                g.FillRectangle(fondo, rect);
            }

            float w = rect.Width;
            float h = rect.Height;

            DibujarCirculo(
                g, -0.10f * w, -0.20f * h, 0.42f * w,
                Color.FromArgb(90, Tema.Rosa));

            DibujarCirculo(
                g, 0.78f * w, 0.55f * h, 0.40f * w,
                Color.FromArgb(80, Tema.Azul));

            DibujarCirculo(
                g, 0.30f * w, 0.78f * h, 0.22f * w,
                Color.FromArgb(70, Tema.Amarillo));

            DibujarCirculo(
                g, 0.86f * w, -0.12f * h, 0.20f * w,
                Color.FromArgb(70, Tema.MoradoPastel));
        }

        private static void DibujarCirculo(
            Graphics g, float x, float y, float d, Color color)
        {
            using (var b = new SolidBrush(color))
                g.FillEllipse(b, x, y, d, d);
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            Close();
        }

        // ---------- Botones adicionales ----------
        private void CrearBotonesExtra()
        {
            if (btnRegistrar != null)
                return;

            ClientSize = new Size(1100, 760);
            panelJugadores.SetBounds(540, 20, 520, 710);

            btnRegistrar = CrearBotonAccion(
                "+ Registrar jugador",
                new Rectangle(64, 620, 210, 42),
                this,
                30);

            btnRegistrar.Click += btnRegistrar_Click;

            btnUnirse = CrearBotonAccion(
                "Unirme a una partida",
                new Rectangle(290, 620, 230, 42),
                this,
                31);

            btnUnirse.Click += btnUnirse_Click;

            btnCrearRed = CrearBotonAccion(
                "Crear partida en red",
                new Rectangle(44, 610, 432, 46),
                panelJugadores,
                32);

            btnCrearRed.Click += btnCrearRed_Click;

            // El servidor no necesita seleccionar jugadores.
            btnCrearRed.Enabled = true;

            btnSalir.Location = new Point(64, 690);
        }

        private BotonPastel CrearBotonAccion(
            string texto,
            Rectangle posicion,
            Control contenedor,
            int tabIndex)
        {
            BotonPastel boton = new BotonPastel
            {
                Text = texto,
                Font = new Font(
                    "Segoe UI Semibold",
                    10F,
                    FontStyle.Regular,
                    GraphicsUnit.Point),
                Location = posicion.Location,
                Size = posicion.Size,
                TabIndex = tabIndex,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };

            contenedor.Controls.Add(boton);
            boton.BringToFront();

            return boton;
        }

        // ---------- Registrar jugador ----------
        private void btnRegistrar_Click(object sender, EventArgs e)
        {
            using (FormRegistro registro = new FormRegistro(baseDatos))
            {
                registro.ShowDialog(this);

                if (registro.SeRegistroAlguno)
                    RecargarConservandoSeleccion();
            }
        }

        private void RecargarConservandoSeleccion()
        {
            List<int> ids = elegidas
                .Select(t => t.Jugador.id)
                .ToList();

            CargarJugadores();

            foreach (int id in ids)
            {
                TarjetaJugador tarjeta =
                    tarjetas.FirstOrDefault(t => t.Jugador.id == id);

                if (tarjeta != null)
                    elegidas.Add(tarjeta);
            }

            ActualizarSeleccion();
        }

        // ---------- Crear partida en red ----------
        // Esta computadora será solamente el servidor.
        // Los cuatro jugadores serán clientes conectados.
        private void btnCrearRed_Click(object sender, EventArgs e)
        {
            using (FormRed red = new FormRed(true))
            {
                Hide();
                red.ShowDialog(this);
            }

            Show();
            CargarJugadores();
        }

        // ---------- Unirse a una partida ----------
        // El cliente selecciona un jugador guardado en la base de datos.
        private void btnUnirse_Click(object sender, EventArgs e)
        {
            if (elegidas.Count != 1)
            {
                MessageBox.Show(
                    "Para unirte a una partida, selecciona exactamente un jugador.",
                    "UNO - Unirse a una partida",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            JugadorBD jugador = elegidas[0].Jugador;

            using (FormRed red = new FormRed(jugador))
            {
                Hide();
                red.ShowDialog(this);
            }

            Show();
            CargarJugadores();
        }

        // ---------- Partida local ----------
        private void btnComenzar_Click(object sender, EventArgs e)
        {
            ComenzarPartida();
        }

        protected override bool ProcessCmdKey(
            ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter && btnComenzar.Enabled)
            {
                ComenzarPartida();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ComenzarPartida()
        {
            if (elegidas.Count != Requeridos)
                return;

            List<JugadorBD> jugadores = new List<JugadorBD>();

            for (int i = 0; i < elegidas.Count; i++)
                jugadores.Add(elegidas[i].Jugador);

            FormJuego pantalla;

            try
            {
                pantalla = new FormJuego(jugadores);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo crear la partida.\n\n" + ex.Message,
                    "UNO",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            pantalla.FormClosed += (s, a) =>
            {
                Show();
                CargarJugadores();
            };

            pantalla.Show();
            Hide();
        }

        // ---------- Cargar jugadores ----------
        private void CargarJugadores()
        {
            List<JugadorBD> jugadores;
            List<EstadisticaJugador> estadisticas;

            try
            {
                jugadores = baseDatos.ObtenerJugadores();
                estadisticas = baseDatos.ObtenerEstadisticasPorJugador();
            }
            catch (Exception ex)
            {
                LimpiarTarjetas();
                ActualizarSeleccion();
                MostrarError(ex.Message);
                return;
            }

            OcultarError();
            LimpiarTarjetas();

            if (jugadores.Count == 0)
            {
                ActualizarSeleccion();
                return;
            }

            int x = lblElige.Left;
            int ancho = lblElige.Width;

            int yInicio =
                lblIndicacion.Bottom + (int)(lblElige.Height * 0.25);

            int separacion = (int)(lblElige.Height * 0.30);

            int disponible =
                lblContador.Top - yInicio -
                (int)(lblElige.Height * 0.20);

            int altoMaximo = (int)(lblElige.Height * 1.85);

            int alto = Math.Min(
                altoMaximo,
                (disponible - separacion * (jugadores.Count - 1))
                    / jugadores.Count);

            for (int i = 0; i < jugadores.Count; i++)
            {
                var jug = jugadores[i];

                var est = estadisticas.FirstOrDefault(
                    s => s.nombre == jug.nombre);

                var tarjeta = new TarjetaJugador
                {
                    Jugador = jug,
                    Nombre = jug.nombre,
                    Record = TextoRecord(est),
                    Acento = Tema.Acentos[i % Tema.Acentos.Length],
                    Location = new Point(
                        x, yInicio + i * (alto + separacion)),
                    Size = new Size(ancho, alto),
                    TabIndex = 10 + i
                };

                tarjeta.Click += Tarjeta_Click;

                tarjetas.Add(tarjeta);
                panelJugadores.Controls.Add(tarjeta);
            }

            ActualizarSeleccion();
        }

        private void LimpiarTarjetas()
        {
            foreach (var t in tarjetas)
            {
                panelJugadores.Controls.Remove(t);
                t.Dispose();
            }

            tarjetas.Clear();
            elegidas.Clear();
        }

        private static string TextoRecord(EstadisticaJugador est)
        {
            if (est == null || est.partidas_jugadas == 0)
                return "Aún sin partidas";

            string ganadas =
                est.partidas_ganadas +
                (est.partidas_ganadas == 1 ? " ganada" : " ganadas");

            string perdidas =
                est.partidas_perdidas +
                (est.partidas_perdidas == 1 ? " perdida" : " perdidas");

            return ganadas + "  ·  " + perdidas;
        }

        // ---------- Mostrar errores de conexión ----------
        private void CrearControlesError()
        {
            if (lblErrorTitulo != null)
                return;

            lblErrorTitulo = new Label
            {
                AutoSize = false,
                BackColor = Color.Transparent,
                Font = new Font(
                    "Segoe UI Semibold",
                    16F,
                    FontStyle.Regular,
                    GraphicsUnit.Point),
                ForeColor = Tema.Texto,
                Text = "No hay conexión con el juego",
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblErrorDetalle = new Label
            {
                AutoSize = false,
                BackColor = Color.Transparent,
                Font = new Font(
                    "Segoe UI",
                    10F,
                    FontStyle.Regular,
                    GraphicsUnit.Point),
                ForeColor = Tema.Suave,
                TextAlign = ContentAlignment.TopLeft
            };

            btnReintentar = new BotonPastel
            {
                Text = "Reintentar",
                Font = new Font(
                    "Segoe UI Semibold",
                    12F,
                    FontStyle.Regular,
                    GraphicsUnit.Point),
                TabIndex = 20
            };

            btnReintentar.Click += btnReintentar_Click;

            panelJugadores.Controls.Add(lblErrorTitulo);
            panelJugadores.Controls.Add(lblErrorDetalle);
            panelJugadores.Controls.Add(btnReintentar);
        }

        private void MostrarError(string detalle)
        {
            CrearControlesError();

            int unidad = lblElige.Height;
            int x = lblElige.Left;
            int ancho = lblElige.Width;
            int y = lblIndicacion.Bottom + unidad;

            lblErrorTitulo.SetBounds(
                x, y, ancho, (int)(unidad * 1.2));

            y = lblErrorTitulo.Bottom + (int)(unidad * 0.3);

            lblErrorDetalle.Text =
                "No se pudo cargar la lista de jugadores.\r\n\r\n" +
                "Revisa que MySQL esté encendido y que la API esté corriendo.\r\n\r\n" +
                "Detalle: " + Recortar(detalle, 140);

            lblErrorDetalle.SetBounds(
                x, y, ancho, (int)(unidad * 5.5));

            btnReintentar.SetBounds(
                x,
                lblErrorDetalle.Bottom + (int)(unidad * 0.4),
                ancho,
                (int)(unidad * 1.3));

            lblErrorTitulo.Visible = true;
            lblErrorDetalle.Visible = true;
            btnReintentar.Visible = true;
            lblContador.Visible = false;
        }

        private void OcultarError()
        {
            if (lblErrorTitulo != null)
            {
                lblErrorTitulo.Visible = false;
                lblErrorDetalle.Visible = false;
                btnReintentar.Visible = false;
            }

            lblContador.Visible = true;
        }

        private void btnReintentar_Click(object sender, EventArgs e)
        {
            UseWaitCursor = true;

            try
            {
                CargarJugadores();
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private static string Recortar(string texto, int max)
        {
            if (string.IsNullOrEmpty(texto))
                return "(sin detalle)";

            texto = texto.Replace("\r", " ").Replace("\n", " ");

            return texto.Length <= max
                ? texto
                : texto.Substring(0, max) + "…";
        }

        // ---------- Selección de jugadores ----------
        private void Tarjeta_Click(object sender, EventArgs e)
        {
            var tarjeta = (TarjetaJugador)sender;

            if (elegidas.Contains(tarjeta))
            {
                elegidas.Remove(tarjeta);
            }
            else if (elegidas.Count < Requeridos)
            {
                elegidas.Add(tarjeta);
            }

            ActualizarSeleccion();
        }

        private void ActualizarSeleccion()
        {
            bool completo = elegidas.Count == Requeridos;

            foreach (var t in tarjetas)
            {
                t.Turno = elegidas.IndexOf(t) + 1;
                t.Enabled = !completo || t.Elegida;
            }

            // La partida local necesita cuatro jugadores.
            btnComenzar.Enabled = completo;

            // El servidor no necesita seleccionar jugadores.
            if (btnCrearRed != null)
                btnCrearRed.Enabled = true;

            if (completo)
            {
                lblContador.Text = "¡Listo! Ya están los 4 jugadores";
                lblContador.ForeColor = Tema.Exito;
            }
            else if (elegidas.Count == 0)
            {
                lblContador.Text = "Elige a 4 jugadores";
                lblContador.ForeColor = Tema.Suave;
            }
            else
            {
                int faltan = Requeridos - elegidas.Count;

                lblContador.Text =
                    elegidas.Count + " de " + Requeridos + "  ·  " +
                    (faltan == 1 ? "falta 1" : "faltan " + faltan);

                lblContador.ForeColor = Tema.Suave;
            }
        }
    }
}