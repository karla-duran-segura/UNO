using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace uno
{
    public class FormFinal : Form
    {
        private class Confeti
        {
            public float X, Y, Vx, Vy;
            public int Ancho, Alto;
            public Color Color;
        }

        private const int ANCHO_FORM = 640;
        private const int ANCHO_CAJA = 168;
        private const int SEPARACION = 9;
        private const int BASE_PODIO = 395;
        private const int ALTO_BOTON = 52;

        private static readonly Color COLOR_FONDO = ColorTranslator.FromHtml("#2B2633");
        private static readonly Color COLOR_TEXTO_CLARO = Color.FromArgb(240, 236, 248);

        private static PrivateFontCollection coleccionSemiBold;
        private static PrivateFontCollection coleccionRegular;
        private static FontFamily familiaSemiBold;
        private static FontFamily familiaRegular;

        private readonly Juego juego;
        private readonly List<JugadorUno> orden = new List<JugadorUno>();
        private readonly List<Confeti> confeti = new List<Confeti>();
        private readonly Random azar = new Random();
        private readonly Timer reloj = new Timer();
        private readonly StringFormat centro = new StringFormat();

        private readonly Font fTitulo, fGanador, fNombre, fNumero, fPuntos, fResto, fSeccion, fFila, fBoton;

        private BotonPastel btnVolver;
        private int yResto, yHistorial, yFilas;

        private List<string> filasHistorial = null;
        private bool errorHistorial = false;

        public FormFinal(Juego juego)
        {
            if (juego == null) throw new ArgumentNullException("juego");
            this.juego = juego;

            Text = "UNO - Fin de la partida";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = COLOR_FONDO;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);

            centro.Alignment = StringAlignment.Center;
            centro.LineAlignment = StringAlignment.Center;
            centro.FormatFlags = StringFormatFlags.NoWrap;
            centro.Trimming = StringTrimming.EllipsisCharacter;

            fTitulo = CrearFuente(true, 44);
            fGanador = CrearFuente(true, 30);
            fNombre = CrearFuente(true, 24);
            fNumero = CrearFuente(true, 54);
            fPuntos = CrearFuente(false, 22);
            fResto = CrearFuente(false, 24);
            fSeccion = CrearFuente(true, 30);
            fFila = CrearFuente(false, 23);
            fBoton = CrearFuente(true, 19);

            CalcularOrden();
            CalcularLayout();
            CrearBoton();
            CrearConfeti();

            reloj.Interval = 30;
            reloj.Tick += delegate { MoverConfeti(); Invalidate(); };
            reloj.Start();
        }

        private void CalcularOrden()
        {
            JugadorUno ganador = juego.GanadorPartida;
            if (ganador != null)
                orden.Add(ganador);

            orden.AddRange(juego.Jugadores
                .Where(j => !Object.ReferenceEquals(j, ganador))
                .OrderByDescending(j => j.Puntos));
        }

        private void CalcularLayout()
        {
            int n = orden.Count;
            int extras = Math.Max(0, n - 3);

            yResto = 412;
            yHistorial = yResto + extras * 30 + 24;
            yFilas = yHistorial + 48;

            int yBoton = yFilas + n * 34 + 18;
            ClientSize = new Size(ANCHO_FORM, yBoton + ALTO_BOTON + 30);
        }

        private void CrearBoton()
        {
            int n = orden.Count;
            int yBoton = yFilas + n * 34 + 18;

            btnVolver = new BotonPastel();
            btnVolver.Text = "Volver al inicio";
            btnVolver.Font = fBoton;
            btnVolver.Size = new Size(270, ALTO_BOTON);
            btnVolver.Location = new Point((ANCHO_FORM - 270) / 2, yBoton);
            btnVolver.Click += delegate
            {
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(btnVolver);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            Task.Run(() =>
            {
                List<EstadisticaJugador> datos = null;
                try
                {
                    datos = new BaseDatos().ObtenerEstadisticasPorJugador();
                }
                catch (Exception)
                {
                    datos = null;
                }

                try
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    BeginInvoke(new Action(() => MostrarHistorial(datos)));
                }
                catch (InvalidOperationException) { } 
            });
        }

        private void MostrarHistorial(List<EstadisticaJugador> datos)
        {
            if (IsDisposed) return;

            if (datos == null)
            {
                errorHistorial = true;
                filasHistorial = new List<string>();
            }
            else
            {
                // Solo los jugadores de esta partida
                filasHistorial = datos
                    .Where(d => juego.Jugadores.Any(j =>
                        string.Equals(j.Nombre, d.nombre, StringComparison.OrdinalIgnoreCase)))
                    .OrderByDescending(d => d.partidas_ganadas)
                    .ThenBy(d => d.nombre)
                    .Select(d => d.nombre + ":  " + d.partidas_ganadas + " ganadas  ·  " +
                                 d.partidas_perdidas + " perdidas")
                    .ToList();
            }
            Invalidate();
        }


        private void CrearConfeti()
        {
            Color[] colores = { Tema.Amarillo, Tema.Rosa, Tema.Azul, Tema.Menta, Tema.MoradoPastel };

            for (int i = 0; i < 90; i++)
            {
                Confeti c = new Confeti();
                c.X = azar.Next(0, ClientSize.Width);
                c.Y = azar.Next(-ClientSize.Height, ClientSize.Height);
                c.Vx = (float)(azar.NextDouble() * 1.2 - 0.6);
                c.Vy = (float)(azar.NextDouble() * 2.4 + 1.2);
                c.Ancho = azar.Next(8, 13);
                c.Alto = azar.Next(5, 8);
                c.Color = colores[azar.Next(colores.Length)];
                confeti.Add(c);
            }
        }

        private void MoverConfeti()
        {
            foreach (Confeti c in confeti)
            {
                c.X += c.Vx;
                c.Y += c.Vy;

                if (c.Y > ClientSize.Height + 10)
                {
                    c.Y = -10;
                    c.X = azar.Next(0, ClientSize.Width);
                }
                if (c.X < -15) c.X = ClientSize.Width;
                if (c.X > ClientSize.Width + 15) c.X = -10;
            }
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            // Títulos
            Texto(g, "Fin de la partida", fTitulo, Color.White, new RectangleF(0, 14, ANCHO_FORM, 68));

            string textoGanador = juego.GanadorPartida != null
                ? juego.GanadorPartida.Nombre + " ganó la partida"
                : "La partida terminó sin ganador";
            Texto(g, textoGanador, fGanador, Tema.Amarillo, new RectangleF(0, 84, ANCHO_FORM, 44));

            DibujarPodio(g);
            DibujarResto(g);
            DibujarHistorial(g);
            DibujarConfeti(g);
        }

        private void DibujarPodio(Graphics g)
        {
            // De izquierda a derecha
            int[] indice = { 1, 0, 2 };
            int[] alto = { 124, 168, 88 };
            Color[] colores = { Tema.Azul, Tema.Amarillo, Tema.Rosa };

            int ancho = 3 * ANCHO_CAJA + 2 * SEPARACION;
            int xInicial = (ANCHO_FORM - ancho) / 2;

            for (int k = 0; k < 3; k++)
            {
                if (indice[k] >= orden.Count) continue;

                JugadorUno jugador = orden[indice[k]];
                int x = xInicial + k * (ANCHO_CAJA + SEPARACION);
                int arriba = BASE_PODIO - alto[k];
                RectangleF caja = new RectangleF(x, arriba, ANCHO_CAJA, alto[k]);

                // Nombre arriba de la caja
                Texto(g, jugador.Nombre, fNombre, Color.White,
                      new RectangleF(x, arriba - 36, ANCHO_CAJA, 32));

                using (GraphicsPath ruta = Tema.Redondeado(caja, 16))
                using (SolidBrush brocha = new SolidBrush(colores[k]))
                    g.FillPath(brocha, ruta);

                // Posición en grande y puntos abajo
                float centroNumero = arriba + (alto[k] - 30) / 2f;
                Texto(g, (indice[k] + 1).ToString(), fNumero, Tema.Texto,
                      new RectangleF(x, centroNumero - 30, ANCHO_CAJA, 60));
                Texto(g, jugador.Puntos + " pts", fPuntos, Tema.Texto,
                      new RectangleF(x, arriba + alto[k] - 36, ANCHO_CAJA, 28));
            }
        }

        private void DibujarResto(Graphics g)
        {
            for (int i = 3; i < orden.Count; i++)
            {
                string linea = (i + 1) + ". " + orden[i].Nombre + "   " + orden[i].Puntos + " pts";
                Texto(g, linea, fResto, COLOR_TEXTO_CLARO,
                      new RectangleF(0, yResto + (i - 3) * 30, ANCHO_FORM, 30));
            }
        }

        private void DibujarHistorial(Graphics g)
        {
            Texto(g, "Historial de partidas", fSeccion, Color.White,
                  new RectangleF(0, yHistorial, ANCHO_FORM, 40));

            if (filasHistorial == null)
            {
                Texto(g, "Cargando historial...", fFila, Tema.Suave,
                      new RectangleF(0, yFilas, ANCHO_FORM, 34));
                return;
            }

            if (errorHistorial)
            {
                Texto(g, "No se pudo cargar el historial", fFila, Tema.Suave,
                      new RectangleF(0, yFilas, ANCHO_FORM, 34));
                return;
            }

            for (int i = 0; i < filasHistorial.Count; i++)
                Texto(g, filasHistorial[i], fFila, COLOR_TEXTO_CLARO,
                      new RectangleF(0, yFilas + i * 34, ANCHO_FORM, 34));
        }

        private void DibujarConfeti(Graphics g)
        {
            SmoothingMode anterior = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.None;

            using (SolidBrush brocha = new SolidBrush(Color.White))
            {
                foreach (Confeti c in confeti)
                {
                    brocha.Color = c.Color;
                    g.FillRectangle(brocha, c.X, c.Y, c.Ancho, c.Alto);
                }
            }

            g.SmoothingMode = anterior;
        }

        private void Texto(Graphics g, string texto, Font fuente, Color color, RectangleF area)
        {
            using (SolidBrush brocha = new SolidBrush(color))
                g.DrawString(texto, fuente, brocha, area, centro);
        }


        private static FontFamily CargarFamilia(string archivo, ref PrivateFontCollection coleccion, ref FontFamily familia)
        {
            if (familia != null) return familia;

            try
            {
                string ruta = Path.Combine(Application.StartupPath, "Fuentes", archivo);
                if (!File.Exists(ruta)) return null;

                coleccion = new PrivateFontCollection();
                coleccion.AddFontFile(ruta);
                if (coleccion.Families.Length > 0)
                    familia = coleccion.Families[0];
            }
            catch (Exception)
            {
                familia = null;
            }
            return familia;
        }

        private static Font CrearFuente(bool semiBold, float px)
        {
            FontFamily familia = semiBold
                ? CargarFamilia("Fredoka-SemiBold.ttf", ref coleccionSemiBold, ref familiaSemiBold)
                : CargarFamilia("Fredoka-Regular.ttf", ref coleccionRegular, ref familiaRegular);

            if (familia != null)
                return new Font(familia, px, FontStyle.Regular, GraphicsUnit.Pixel);

            return new Font("Segoe UI", px, semiBold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            reloj.Stop();
            base.OnFormClosed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                reloj.Dispose();
                centro.Dispose();
                fTitulo.Dispose();
                fGanador.Dispose();
                fNombre.Dispose();
                fNumero.Dispose();
                fPuntos.Dispose();
                fResto.Dispose();
                fSeccion.Dispose();
                fFila.Dispose();
                fBoton.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}