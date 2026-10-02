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
        private readonly List<TarjetaJugador> elegidas = new List<TarjetaJugador>();   // en orden de turno

        public FormInicio()
        {
            InitializeComponent();
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
            Load += FormInicio_Load;
        }

        private void FormInicio_Load(object sender, EventArgs e)
        {
            CargarJugadores();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = ClientRectangle;
            if (rect.Width < 2 || rect.Height < 2) { base.OnPaintBackground(e); return; }

            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var fondo = new LinearGradientBrush(rect, Color.White, Color.White, 55f))
            {
                fondo.InterpolationColors = new ColorBlend
                {
                    Positions = new[] { 0f, 0.5f, 1f },
                    Colors = new[]
                    {
                        Color.FromArgb(240, 230, 255),   // lavanda
                        Color.FromArgb(255, 232, 242),   // rosa
                        Color.FromArgb(255, 241, 220)    // durazno
                    }
                };
                g.FillRectangle(fondo, rect);
            }

            float w = rect.Width, h = rect.Height;
            DibujarCirculo(g, -0.10f * w, -0.20f * h, 0.42f * w, Color.FromArgb(90, Tema.Rosa));
            DibujarCirculo(g, 0.78f * w, 0.55f * h, 0.40f * w, Color.FromArgb(80, Tema.Azul));
            DibujarCirculo(g, 0.30f * w, 0.78f * h, 0.22f * w, Color.FromArgb(70, Tema.Amarillo));
            DibujarCirculo(g, 0.86f * w, -0.12f * h, 0.20f * w, Color.FromArgb(70, Tema.MoradoPastel));
        }

        private static void DibujarCirculo(Graphics g, float x, float y, float d, Color color)
        {
            using (var b = new SolidBrush(color))
                g.FillEllipse(b, x, y, d, d);
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            Close();
        }

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
                // Por ahora solo un aviso ya que en la etapa 8 se cambia por un panel con reintentar
                MessageBox.Show(
                    "No se pudo conectar con la base de datos.\n\n" +
                    "Revisa que MySQL esté encendido y que ya corriste Database/uno_db.sql.\n\n" +
                    "Detalle: " + ex.Message,
                    "UNO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Quitar tarjetas anteriores 
            foreach (var t in tarjetas)
            {
                panelJugadores.Controls.Remove(t);
                t.Dispose();
            }
            tarjetas.Clear();
            elegidas.Clear();

            if (jugadores.Count == 0) { ActualizarSeleccion(); return; }

            // Medidas relativas al diseño del panel 
            int x = lblElige.Left;
            int ancho = lblElige.Width;
            int yInicio = lblIndicacion.Bottom + (int)(lblElige.Height * 0.25);
            int separacion = (int)(lblElige.Height * 0.30);
            int disponible = lblContador.Top - yInicio - (int)(lblElige.Height * 0.20);
            int altoMaximo = (int)(lblElige.Height * 1.85);
            int alto = Math.Min(altoMaximo, (disponible - separacion * (jugadores.Count - 1)) / jugadores.Count);

            for (int i = 0; i < jugadores.Count; i++)
            {
                var jug = jugadores[i];
                var est = estadisticas.FirstOrDefault(s => s.nombre == jug.nombre);

                var tarjeta = new TarjetaJugador
                {
                    Jugador = jug,
                    Nombre = jug.nombre,
                    Record = TextoRecord(est),
                    Acento = Tema.Acentos[i % Tema.Acentos.Length],
                    Location = new Point(x, yInicio + i * (alto + separacion)),
                    Size = new Size(ancho, alto),
                    TabIndex = 10 + i
                };
                tarjeta.Click += Tarjeta_Click;

                tarjetas.Add(tarjeta);
                panelJugadores.Controls.Add(tarjeta);
            }

            ActualizarSeleccion();
        }

        private static string TextoRecord(EstadisticaJugador est)
        {
            if (est == null || est.partidas_jugadas == 0)
                return "Aún sin partidas";

            string ganadas = est.partidas_ganadas + (est.partidas_ganadas == 1 ? " ganada" : " ganadas");
            string perdidas = est.partidas_perdidas + (est.partidas_perdidas == 1 ? " perdida" : " perdidas");
            return ganadas + "  ·  " + perdidas;
        }

        private void Tarjeta_Click(object sender, EventArgs e)
        {
            var tarjeta = (TarjetaJugador)sender;

            if (elegidas.Contains(tarjeta))
                elegidas.Remove(tarjeta);              // la quita y las demás se renumeran
            else if (elegidas.Count < Requeridos)
                elegidas.Add(tarjeta);                 

            ActualizarSeleccion();
        }

        private void ActualizarSeleccion()
        {
            bool completo = elegidas.Count == Requeridos;

            foreach (var t in tarjetas)
            {
                t.Turno = elegidas.IndexOf(t) + 1;  
                t.Enabled = !completo || t.Elegida;    // con 4 elegidos, las demás se bloquean
            }

            btnComenzar.Enabled = completo;

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
                lblContador.Text = elegidas.Count + " de " + Requeridos + "  ·  " +
                                   (faltan == 1 ? "falta 1" : "faltan " + faltan);
                lblContador.ForeColor = Tema.Suave;
            }
        }
    }
}