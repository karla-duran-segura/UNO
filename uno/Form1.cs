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
        private readonly BaseDatos baseDatos = new BaseDatos();
        private readonly List<TarjetaJugador> tarjetas = new List<TarjetaJugador>();

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
                // Por ahora solo un aviso; en la etapa 8 se cambia por un panel con reintentar
                MessageBox.Show(
                    "No se pudo conectar con la base de datos.\n\n" +
                    "Revisa que MySQL esté encendido y que ya corriste Database/uno_db.sql.\n\n" +
                    "Detalle: " + ex.Message,
                    "UNO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Quitar tarjetas anteriores sirve también cuando se vuelva a cargar
            foreach (var t in tarjetas)
            {
                panelJugadores.Controls.Remove(t);
                t.Dispose();
            }
            tarjetas.Clear();

            if (jugadores.Count == 0) return;

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

                tarjetas.Add(tarjeta);
                panelJugadores.Controls.Add(tarjeta);
            }
        }

        private static string TextoRecord(EstadisticaJugador est)
        {
            if (est == null || est.partidas_jugadas == 0)
                return "Aún sin partidas";

            string ganadas = est.partidas_ganadas + (est.partidas_ganadas == 1 ? " ganada" : " ganadas");
            string perdidas = est.partidas_perdidas + (est.partidas_perdidas == 1 ? " perdida" : " perdidas");
            return ganadas + "  ·  " + perdidas;
        }
    }
}