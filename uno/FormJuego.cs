using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace uno
{
    // ==========================================================
    //  PANTALLA DE JUEGO
    //  - Mesa con el estilo pastel de la pantalla de inicio.
    //  - Mano del jugador en turno boca arriba; rivales boca abajo.
    //  - Guarda cada movimiento en la base de datos (log).
    // ==========================================================
    public class FormJuego : Form
    {
        private const int ANCHO = 80;
        private const int ALTO = 120;
        private const int ANCHO_MINI = 52;
        private const int ALTO_MINI = 78;
        private const string CARPETA_IMAGENES = "Imagenes_pastel";
        private const bool MOSTRAR_CARTAS_RIVALES = false;   // true = rivales boca arriba

        private Juego juego;
        private BaseDatos bd = new BaseDatos();
        private List<int> idsJugadores = new List<int>();   // id en la BD de cada jugador, en el mismo orden
        private int idPartida = -1;
        private bool resultadoGuardado = false;
        private bool avisoBD = false;
        private bool decirUno = false;
        private string mensajeError = null;
        private JugadorUno jugadorAnterior = null;

        private Dictionary<string, Image> imagenes = new Dictionary<string, Image>();

        private Label lblInfo;
        private FlowLayoutPanel panelRivales;
        private PictureBox picMazo;
        private PictureBox picDescarte;
        private Label lblColor;
        private BotonPastel btnUno;
        private BotonPastel btnPasar;
        private BotonPastel btnFaltaUno;
        private BotonPastel btnSiguienteRonda;
        private Label lblMensaje;
        private Label lblMano;
        private FlowLayoutPanel panelMano;
        private ListBox lstLog;

        public FormJuego(List<JugadorBD> jugadores)
        {
            Text = "UNO - Partida";
            ClientSize = new Size(1200, 780);
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);

            CrearControles();
            IniciarPartida(jugadores);
        }

        // ---------- Fondo pastel (igual que la pantalla de inicio) ----------
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
            DibujarCirculo(g, -0.10f * w, -0.20f * h, 0.42f * w, Color.FromArgb(90, Tema.Rosa));
            DibujarCirculo(g, 0.78f * w, 0.55f * h, 0.40f * w, Color.FromArgb(80, Tema.Azul));
            DibujarCirculo(g, 0.30f * w, 0.78f * h, 0.22f * w, Color.FromArgb(70, Tema.Amarillo));
            DibujarCirculo(g, 0.86f * w, -0.12f * h, 0.20f * w, Color.FromArgb(70, Tema.MoradoPastel));
        }

        private static void DibujarCirculo(Graphics g, float x, float y, float d, Color color)
        {
            using (SolidBrush brocha = new SolidBrush(color))
                g.FillEllipse(brocha, x, y, d, d);
        }

        // ---------- Iniciar partida y registrarla en la BD ----------
        private void IniciarPartida(List<JugadorBD> jugadores)
        {
            List<string> nombres = new List<string>();
            for (int i = 0; i < jugadores.Count; i++)
            {
                nombres.Add(jugadores[i].nombre);
                idsJugadores.Add(jugadores[i].id);
            }

            juego = new Juego(nombres);
            if (juego.Jugadores[0].Mano.Count == 0)
                juego.IniciarRonda();

            try
            {
                idPartida = bd.CrearPartida(idsJugadores);
            }
            catch (Exception ex)
            {
                AvisarErrorBD(ex);
            }

            jugadorAnterior = juego.JugadorActual;
            DespuesDeAccion();
        }

        // ---------- Construcción de la interfaz ----------
        private void CrearControles()
        {
            lblInfo = CrearEtiqueta(new Point(20, 12), new Size(940, 32), 16);

            panelRivales = new FlowLayoutPanel();
            panelRivales.Location = new Point(20, 50);
            panelRivales.Size = new Size(940, 120);
            panelRivales.BackColor = Color.Transparent;
            Controls.Add(panelRivales);

            picMazo = new PictureBox();
            picMazo.Location = new Point(230, 185);
            picMazo.Size = new Size(100, 150);
            picMazo.SizeMode = PictureBoxSizeMode.StretchImage;
            picMazo.BackColor = Color.Transparent;
            picMazo.Cursor = Cursors.Hand;
            picMazo.Click += ClickMazo;
            Controls.Add(picMazo);

            picDescarte = new PictureBox();
            picDescarte.Location = new Point(345, 185);
            picDescarte.Size = new Size(100, 150);
            picDescarte.SizeMode = PictureBoxSizeMode.StretchImage;
            picDescarte.BackColor = Color.Transparent;
            Controls.Add(picDescarte);

            lblColor = CrearEtiqueta(new Point(470, 185), new Size(220, 36), 12);
            lblColor.TextAlign = ContentAlignment.MiddleCenter;

            btnUno = CrearBoton("¡UNO!", new Point(470, 228));
            btnUno.Click += ClickUno;

            btnPasar = CrearBoton("Pasar (después de robar)", new Point(470, 270));
            btnPasar.Click += ClickPasar;

            btnFaltaUno = CrearBoton("¡Señalar falta de UNO!", new Point(470, 312));
            btnFaltaUno.Click += ClickFaltaUno;

            btnSiguienteRonda = CrearBoton("Siguiente ronda", new Point(710, 228));
            btnSiguienteRonda.Click += ClickSiguienteRonda;

            lblMensaje = CrearEtiqueta(new Point(20, 350), new Size(940, 30), 12);

            lblMano = CrearEtiqueta(new Point(20, 385), new Size(940, 26), 12);

            panelMano = new FlowLayoutPanel();
            panelMano.Location = new Point(20, 415);
            panelMano.Size = new Size(940, 350);
            panelMano.AutoScroll = true;
            panelMano.BackColor = Color.Transparent;
            Controls.Add(panelMano);

            Label lblLog = CrearEtiqueta(new Point(980, 12), new Size(200, 32), 13);
            lblLog.Text = "Movimientos";

            lstLog = new ListBox();
            lstLog.Location = new Point(980, 50);
            lstLog.Size = new Size(200, 715);
            lstLog.HorizontalScrollbar = true;
            lstLog.BorderStyle = BorderStyle.None;
            lstLog.ForeColor = Tema.Texto;
            Controls.Add(lstLog);
        }

        private Label CrearEtiqueta(Point posicion, Size tamano, int tamanoLetra)
        {
            Label etiqueta = new Label();
            etiqueta.Location = posicion;
            etiqueta.Size = tamano;
            etiqueta.Font = new Font("Segoe UI Semibold", tamanoLetra, FontStyle.Regular);
            etiqueta.ForeColor = Tema.Texto;
            etiqueta.BackColor = Color.Transparent;
            etiqueta.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(etiqueta);
            return etiqueta;
        }

        private BotonPastel CrearBoton(string texto, Point posicion)
        {
            BotonPastel boton = new BotonPastel();
            boton.Text = texto;
            boton.Font = new Font("Segoe UI Semibold", 10, FontStyle.Regular);
            boton.Location = posicion;
            boton.Size = new Size(220, 36);
            Controls.Add(boton);
            return boton;
        }

        // ---------- Redibujar la pantalla ----------
        private void Actualizar()
        {
            JugadorUno actual = juego.JugadorActual;

            if (juego.PartidaTerminada)
                lblInfo.Text = "¡" + juego.GanadorPartida.Nombre + " ganó la partida!";
            else if (juego.RondaTerminada)
                lblInfo.Text = "Terminó la ronda " + juego.NumeroRonda;
            else
                lblInfo.Text = "Ronda " + juego.NumeroRonda + "  ·  Turno de " + actual.Nombre;

            DibujarRivales(actual);
            DibujarMano(actual);

            picMazo.Image = ObtenerReverso();
            picDescarte.Image = ObtenerImagen(juego.CartaSuperior);

            if (juego.ColorActivo == null)
                lblColor.Text = "Color: por elegir";
            else
                lblColor.Text = "Color: " + juego.ColorActivo;
            lblColor.BackColor = ColorDibujo(juego.ColorActivo);

            if (mensajeError != null)
            {
                lblMensaje.Text = mensajeError;
                lblMensaje.ForeColor = Color.Firebrick;
                mensajeError = null;
            }
            else
            {
                lblMensaje.ForeColor = Tema.Texto;
                if (juego.RondaTerminada && !juego.PartidaTerminada)
                    lblMensaje.Text = "Da clic en \"Siguiente ronda\" para continuar.";
                else
                    lblMensaje.Text = "";
            }

            lstLog.Items.Clear();
            foreach (string movimiento in juego.Movimientos)
                lstLog.Items.Add(movimiento);
            if (lstLog.Items.Count > 0)
                lstLog.TopIndex = lstLog.Items.Count - 1;

            btnUno.Text = decirUno ? "¡UNO! ✓" : "¡UNO!";
            btnUno.Enabled = !juego.RondaTerminada;
            btnPasar.Enabled = !juego.RondaTerminada;
            btnFaltaUno.Enabled = juego.FaltaUnoPendiente;
            btnSiguienteRonda.Enabled = juego.RondaTerminada && !juego.PartidaTerminada;
        }

        // Cartas de los demás jugadores (boca abajo, encimadas)
        private void DibujarRivales(JugadorUno actual)
        {
            for (int i = panelRivales.Controls.Count - 1; i >= 0; i--)
                panelRivales.Controls[i].Dispose();
            panelRivales.Controls.Clear();

            for (int i = 0; i < juego.Jugadores.Count; i++)
            {
                JugadorUno jugador = juego.Jugadores[i];
                if (Object.ReferenceEquals(jugador, actual) && !juego.RondaTerminada)
                    continue;

                Panel bloque = new Panel();
                bloque.Size = new Size(300, 115);
                bloque.Margin = new Padding(0, 0, 10, 0);
                bloque.BackColor = Color.Transparent;

                Label nombre = new Label();
                nombre.Location = new Point(0, 0);
                nombre.Size = new Size(300, 24);
                nombre.Font = new Font("Segoe UI Semibold", 10, FontStyle.Regular);
                nombre.ForeColor = Tema.Texto;
                nombre.BackColor = Color.Transparent;
                nombre.Text = jugador.Nombre + ": " + jugador.Mano.Count + " cartas (" + jugador.Puntos + " pts)";
                bloque.Controls.Add(nombre);

                // Si tiene muchas cartas, se enciman más para que quepan
                int cantidad = jugador.Mano.Count;
                int paso = 30;
                if (cantidad > 1)
                {
                    int pasoMaximo = (300 - ANCHO_MINI) / (cantidad - 1);
                    if (pasoMaximo < paso)
                        paso = pasoMaximo;
                }

                for (int c = 0; c < cantidad; c++)
                {
                    PictureBox mini = new PictureBox();
                    mini.Location = new Point(c * paso, 30);
                    mini.Size = new Size(ANCHO_MINI, ALTO_MINI);
                    mini.SizeMode = PictureBoxSizeMode.StretchImage;
                    if (MOSTRAR_CARTAS_RIVALES)
                        mini.Image = ObtenerImagen(jugador.Mano[c]);
                    else
                        mini.Image = ObtenerReverso();
                    bloque.Controls.Add(mini);
                    mini.BringToFront();
                }

                panelRivales.Controls.Add(bloque);
            }
        }

        // Mano del jugador en turno (boca arriba)
        private void DibujarMano(JugadorUno actual)
        {
            for (int i = panelMano.Controls.Count - 1; i >= 0; i--)
                panelMano.Controls[i].Dispose();
            panelMano.Controls.Clear();

            if (juego.RondaTerminada)
            {
                lblMano.Text = "";
                return;
            }

            lblMano.Text = "Mano de " + actual.Nombre + "  (las cartas resaltadas se pueden jugar)";

            foreach (Carta carta in actual.Mano)
            {
                PictureBox pb = new PictureBox();
                pb.Size = new Size(ANCHO, ALTO);
                pb.SizeMode = PictureBoxSizeMode.StretchImage;
                pb.Margin = new Padding(4);
                pb.Cursor = Cursors.Hand;
                pb.BackColor = Color.Transparent;
                pb.Image = ObtenerImagen(carta);

                if (juego.PuedeJugar(carta))
                {
                    pb.Padding = new Padding(3);
                    pb.BackColor = Color.Gold;
                }

                Carta cartaClic = carta;
                pb.Click += (s, e) => ClickCarta(cartaClic);
                panelMano.Controls.Add(pb);
            }
        }

        // ---------- Después de cada acción ----------
        private void DespuesDeAccion()
        {
            // Carta inicial comodín: el jugador en turno elige el color
            if (!juego.RondaTerminada && juego.ColorActivo == null)
            {
                int indice = IndiceDe(juego.JugadorActual);
                string color = ElegirColor(juego.JugadorActual.Nombre + ", elige el color inicial");
                if (Intentar(() => juego.ElegirColorInicial(color)))
                    Registrar(indice, "color_inicial", null, color);
            }

            // Desafío del +4
            if (juego.EsperandoDesafioMas4)
            {
                int indice = IndiceDe(juego.JugadorActual);
                DialogResult respuesta = MessageBox.Show(
                    "Se jugó un +4. ¿El jugador afectado quiere desafiarlo?",
                    "Desafío de +4", MessageBoxButtons.YesNo);
                bool desafiar = (respuesta == DialogResult.Yes);
                if (Intentar(() => juego.ResolverMas4(desafiar)))
                    Registrar(indice, desafiar ? "desafio_mas4" : "acepta_mas4");
            }

            // Cambio de turno
            if (!juego.RondaTerminada && !Object.ReferenceEquals(juego.JugadorActual, jugadorAnterior))
            {
                jugadorAnterior = juego.JugadorActual;
                decirUno = false;
                CambioDeTurno();
            }

            Actualizar();

            // Fin de la partida: se guarda el resultado una sola vez
            if (juego.PartidaTerminada && !resultadoGuardado)
            {
                resultadoGuardado = true;
                GuardarResultado();
                MessageBox.Show("¡" + juego.GanadorPartida.Nombre + " ganó la partida!", "Fin de la partida");
            }
        }

        // Aquí se puede mostrar la pantalla de "pasa la computadora"
        private void CambioDeTurno()
        {
        }

        // ---------- Eventos ----------
        private void ClickCarta(Carta carta)
        {
            if (juego.RondaTerminada) return;

            int indice = IndiceDe(juego.JugadorActual);

            string colorElegido = null;
            if (carta.EsComodin())
                colorElegido = ElegirColor("Elige un color");

            bool unoDicho = decirUno;
            if (Intentar(() => juego.JugarCarta(carta, colorElegido, unoDicho)))
            {
                Registrar(indice, "jugar", carta.Texto(), colorElegido);
                if (unoDicho)
                    Registrar(indice, "uno");
                decirUno = false;
            }

            DespuesDeAccion();
        }

        private void ClickUno(object sender, EventArgs e)
        {
            decirUno = !decirUno;   // se presiona antes de jugar la penúltima carta
            Actualizar();
        }

        private void ClickMazo(object sender, EventArgs e)
        {
            if (juego.RondaTerminada) return;

            int indice = IndiceDe(juego.JugadorActual);
            Carta robada = null;
            if (Intentar(() => { robada = juego.RobarCarta(); }))
                Registrar(indice, "robar", robada != null ? robada.Texto() : null);

            DespuesDeAccion();
        }

        private void ClickPasar(object sender, EventArgs e)
        {
            if (juego.RondaTerminada) return;

            int indice = IndiceDe(juego.JugadorActual);
            if (Intentar(() => juego.PasarTrasRobar()))
                Registrar(indice, "pasar");

            DespuesDeAccion();
        }

        private void ClickFaltaUno(object sender, EventArgs e)
        {
            int indice = IndiceDe(juego.JugadorActual);
            if (Intentar(() => juego.SenalarFaltaUno()))
                Registrar(indice, "falta_uno");

            DespuesDeAccion();
        }

        private void ClickSiguienteRonda(object sender, EventArgs e)
        {
            Intentar(() => juego.IniciarRonda());
            jugadorAnterior = null;
            DespuesDeAccion();
        }

        // ---------- Base de datos (log) ----------
        private void Registrar(int indiceJugador, string tipo, string carta = null, string color = null)
        {
            if (idPartida < 0 || indiceJugador < 0) return;

            try
            {
                bd.RegistrarMovimiento(idPartida, idsJugadores[indiceJugador], tipo, carta, color);
            }
            catch (Exception ex)
            {
                AvisarErrorBD(ex);
            }
        }

        private void GuardarResultado()
        {
            if (idPartida < 0) return;

            Dictionary<int, int> cartasRestantes = new Dictionary<int, int>();
            for (int i = 0; i < juego.Jugadores.Count; i++)
                cartasRestantes[idsJugadores[i]] = juego.Jugadores[i].Mano.Count;

            int idGanador = idsJugadores[IndiceDe(juego.GanadorPartida)];

            try
            {
                bd.RegistrarResultado(idPartida, idGanador, cartasRestantes);
            }
            catch (Exception ex)
            {
                AvisarErrorBD(ex);
            }
        }

        // El aviso sale solo una vez para no llenar la pantalla de mensajes
        private void AvisarErrorBD(Exception ex)
        {
            if (avisoBD) return;
            avisoBD = true;
            MessageBox.Show("No se pudo guardar en la base de datos. Revisa que la API esté encendida.\n\n" + ex.Message,
                            "Base de datos");
        }

        // ---------- Utilidades ----------
        private bool Intentar(Action accion)
        {
            try
            {
                accion();
                return true;
            }
            catch (Exception ex)
            {
                mensajeError = ex.Message;
                return false;
            }
        }

        private int IndiceDe(JugadorUno jugador)
        {
            for (int i = 0; i < juego.Jugadores.Count; i++)
            {
                if (Object.ReferenceEquals(juego.Jugadores[i], jugador))
                    return i;
            }
            return -1;
        }

        // Ventana para elegir color (obligatoria, sin X)
        private string ElegirColor(string titulo)
        {
            string elegido = "Rosa";

            Form ventana = new Form();
            ventana.Text = titulo;
            ventana.ClientSize = new Size(380, 90);
            ventana.FormBorderStyle = FormBorderStyle.FixedDialog;
            ventana.StartPosition = FormStartPosition.CenterParent;
            ventana.ControlBox = false;
            ventana.BackColor = Color.FromArgb(250, 245, 255);

            string[] colores = { "Rosa", "Morado", "Azul", "Amarillo" };
            for (int i = 0; i < colores.Length; i++)
            {
                string colorBoton = colores[i];
                Button boton = new Button();
                boton.Text = colorBoton;
                boton.Location = new Point(10 + i * 92, 20);
                boton.Size = new Size(85, 50);
                boton.FlatStyle = FlatStyle.Flat;
                boton.FlatAppearance.BorderSize = 0;
                boton.BackColor = ColorDibujo(colorBoton);
                boton.ForeColor = Tema.Texto;
                boton.Font = new Font("Segoe UI Semibold", 10, FontStyle.Regular);
                boton.Click += (s, e) =>
                {
                    elegido = colorBoton;
                    ventana.DialogResult = DialogResult.OK;
                };
                ventana.Controls.Add(boton);
            }

            ventana.ShowDialog(this);
            ventana.Dispose();
            return elegido;
        }

        private Color ColorDibujo(string color)
        {
            if (color == "Rosa") return Tema.Rosa;
            if (color == "Morado") return Tema.MoradoPastel;
            if (color == "Azul") return Tema.Azul;
            if (color == "Amarillo") return Tema.Amarillo;
            return Color.FromArgb(70, 60, 90);   // comodines y "sin color"
        }

        // ---------- Imágenes ----------
        private Image ObtenerImagen(Carta carta)
        {
            return Cargar(carta.NombreImagen(), carta.Texto(), ColorDibujo(carta.Color));
        }

        private Image ObtenerReverso()
        {
            return Cargar("reverso.png", "UNO", Color.FromArgb(70, 60, 90));
        }

        private Image Cargar(string nombreArchivo, string textoProvisional, Color fondo)
        {
            if (imagenes.ContainsKey(nombreArchivo))
                return imagenes[nombreArchivo];

            Image imagen;
            string ruta = RutaImagen(nombreArchivo);

            if (ruta != null)
                imagen = Image.FromFile(ruta);
            else
                imagen = CrearProvisional(textoProvisional, fondo);

            imagenes[nombreArchivo] = imagen;
            return imagen;
        }

        // Busca la imagen; si no existe con Rosa/Morado, prueba con los nombres viejos (rojo/verde)
        private string RutaImagen(string nombreArchivo)
        {
            string ruta = Path.Combine(Application.StartupPath, CARPETA_IMAGENES, nombreArchivo);
            if (File.Exists(ruta))
                return ruta;

            string alterno = nombreArchivo.Replace("rosa_", "rojo_").Replace("morado_", "verde_");
            ruta = Path.Combine(Application.StartupPath, CARPETA_IMAGENES, alterno);
            if (File.Exists(ruta))
                return ruta;

            return null;
        }

        // Carta dibujada cuando no existe la imagen
        private Image CrearProvisional(string texto, Color fondo)
        {
            Bitmap imagen = new Bitmap(ANCHO, ALTO);

            using (Graphics g = Graphics.FromImage(imagen))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);

                using (SolidBrush brocha = new SolidBrush(fondo))
                    g.FillRectangle(brocha, 4, 4, ANCHO - 8, ALTO - 8);

                using (Font fuente = new Font("Segoe UI Semibold", 11, FontStyle.Regular))
                {
                    StringFormat formato = new StringFormat();
                    formato.Alignment = StringAlignment.Center;
                    formato.LineAlignment = StringAlignment.Center;
                    Brush brochaTexto = (fondo.GetBrightness() < 0.4f) ? Brushes.White : new SolidBrush(Tema.Texto);
                    g.DrawString(texto, fuente, brochaTexto, new RectangleF(0, 0, ANCHO, ALTO), formato);
                }
            }

            return imagen;
        }
    }
}