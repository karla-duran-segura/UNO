using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace uno
{
    // ==========================================================
    //  PANTALLA DE JUEGO (forma de mesa)
    //  - Abajo: mano del jugador en turno.
    //  - Izquierda, arriba y derecha: los demás jugadores.
    //  - Centro: mazo, descarte y color activo.
    //  - Guarda cada movimiento en la base de datos (log).
    // ==========================================================
    public class FormJuego : Form
    {
        // Tamaños de cartas
        private const int ANCHO = 105;          // mano del jugador en turno
        private const int ALTO = 158;
        private const int ANCHO_MINI = 60;      // cartas de los rivales
        private const int ALTO_MINI = 90;

        private const string CARPETA_IMAGENES = "Imagenes_pastel";
        private const string ARCHIVO_FUENTE = "Fredoka-Light.ttf";   // en la carpeta Fuentes
        private const bool MOSTRAR_CARTAS_RIVALES = true;   // false = rivales boca abajo

        //Version 1: efeff1
        //Version 2: f0f2f5
        //Version 3: eaf4ed
        //Version 4: f5f5f5

        private static readonly Color COLOR_FONDO = ColorTranslator.FromHtml("#2b2633");

        // ===== COLOR DE LA LETRA (cámbialo aquí) =====
        private static readonly Color COLOR_TEXTO = ColorTranslator.FromHtml("#F5F0FF");
        private static readonly Color COLOR_ERROR = ColorTranslator.FromHtml("#FF8A8A");

        private static readonly Color COLOR_BOTONES = ColorTranslator.FromHtml("#5ED6A8");

        // Asientos de la mesa
        private const int ABAJO = 0;
        private const int IZQUIERDA = 1;
        private const int ARRIBA = 2;
        private const int DERECHA = 3;

        // Fuente Fredoka cargada desde archivo
        private static PrivateFontCollection fuentes = null;
        private static FontFamily familiaFredoka = null;

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
        private List<Control> cartasEnMesa = new List<Control>();   // se borran y se vuelven a dibujar

        private Label lblInfo;
        private Label lblSentido;
        private Label[] lblAsientos = new Label[4];
        private PictureBox picMazo;
        private PictureBox picDescarte;
        private PictureBox picColor;
        private Label lblColor;
        private BotonPastel btnUno;
        private BotonPastel btnPasar;
        private BotonPastel btnFaltaUno;
        private BotonPastel btnSiguienteRonda;
        private Label lblMensaje;

        public FormJuego(List<JugadorBD> jugadores)
        {
            Text = "UNO - Partida";
            ClientSize = new Size(1200, 660);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            DoubleBuffered = true;
            BackColor = COLOR_FONDO;   // fondo liso
            Font = Fuente(10);

            CrearControles();
            IniciarPartida(jugadores);
        }

        // ---------- Fuente Fredoka ----------
        // Busca Fuentes\Fredoka-SemiBold.ttf junto al .exe.
        // Si no está, usa Fredoka instalada en Windows; si tampoco, Segoe UI.
        private Font Fuente(float tamano)
        {
            if (familiaFredoka == null)
            {
                string ruta = Path.Combine(Application.StartupPath, "Fuentes", ARCHIVO_FUENTE);
                if (File.Exists(ruta))
                {
                    fuentes = new PrivateFontCollection();
                    fuentes.AddFontFile(ruta);
                    familiaFredoka = fuentes.Families[0];
                }
            }

            if (familiaFredoka != null)
                return new Font(familiaFredoka, tamano, FontStyle.Regular);

            Font instalada = new Font("Fredoka", tamano, FontStyle.Regular);
            if (instalada.Name == "Fredoka")
                return instalada;

            instalada.Dispose();
            return new Font("Segoe UI Semibold", tamano, FontStyle.Regular);
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
            lblInfo = CrearEtiqueta(new Point(20, 10), new Size(300, 56), 13);
            lblInfo.TextAlign = ContentAlignment.TopLeft;

            lblSentido = CrearEtiqueta(new Point(900, 10), new Size(280, 30), 12);
            lblSentido.TextAlign = ContentAlignment.TopRight;

            // Nombres de cada asiento
            lblAsientos[ABAJO] = CrearEtiqueta(new Point(150, 428), new Size(600, 26), 12);
            lblAsientos[IZQUIERDA] = CrearEtiqueta(new Point(20, 145), new Size(220, 26), 11);
            lblAsientos[ARRIBA] = CrearEtiqueta(new Point(330, 12), new Size(540, 26), 11);
            lblAsientos[ARRIBA].TextAlign = ContentAlignment.MiddleCenter;
            lblAsientos[DERECHA] = CrearEtiqueta(new Point(960, 145), new Size(220, 26), 11);
            lblAsientos[DERECHA].TextAlign = ContentAlignment.MiddleRight;

            // Centro de la mesa
            picMazo = new PictureBox();
            picMazo.Location = new Point(400, 210);
            picMazo.Size = new Size(ANCHO, ALTO);
            picMazo.SizeMode = PictureBoxSizeMode.StretchImage;
            picMazo.BackColor = Color.Transparent;
            picMazo.Cursor = Cursors.Hand;
            picMazo.Click += ClickMazo;
            Controls.Add(picMazo);

            picDescarte = new PictureBox();
            picDescarte.Location = new Point(520, 210);
            picDescarte.Size = new Size(ANCHO, ALTO);
            picDescarte.SizeMode = PictureBoxSizeMode.StretchImage;
            picDescarte.BackColor = Color.Transparent;
            Controls.Add(picDescarte);

            picColor = new PictureBox();
            picColor.Location = new Point(650, 240);
            picColor.Size = new Size(80, 80);
            picColor.BackColor = Color.Transparent;
            Controls.Add(picColor);

            lblColor = CrearEtiqueta(new Point(630, 322), new Size(120, 24), 10);
            lblColor.TextAlign = ContentAlignment.MiddleCenter;

            // Botones
            btnUno = CrearBoton("¡UNO!", new Point(790, 205));
            btnUno.Click += ClickUno;

            btnPasar = CrearBoton("Pasar (después de robar)", new Point(790, 247));
            btnPasar.Click += ClickPasar;

            btnFaltaUno = CrearBoton("¡Señalar falta de UNO!", new Point(790, 289));
            btnFaltaUno.Click += ClickFaltaUno;

            btnSiguienteRonda = CrearBoton("Siguiente ronda", new Point(790, 331));
            btnSiguienteRonda.Click += ClickSiguienteRonda;

            lblMensaje = CrearEtiqueta(new Point(150, 392), new Size(900, 30), 11);
            lblMensaje.TextAlign = ContentAlignment.MiddleCenter;
        }

        private Label CrearEtiqueta(Point posicion, Size tamano, int tamanoLetra)
        {
            Label etiqueta = new Label();
            etiqueta.Location = posicion;
            etiqueta.Size = tamano;
            etiqueta.UseCompatibleTextRendering = true;   // necesario para fuentes cargadas desde archivo
            etiqueta.Font = Fuente(tamanoLetra);
            etiqueta.ForeColor = COLOR_TEXTO;              // <-- color de todos los textos
            etiqueta.BackColor = Color.Transparent;
            etiqueta.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(etiqueta);
            return etiqueta;
        }

        private BotonPastel CrearBoton(string texto, Point posicion)
        {
            BotonPastel boton = new BotonPastel();
            boton.Text = texto;
            boton.Font = Fuente(10);
            boton.Location = posicion;
            boton.Size = new Size(200, 36);
            boton.ColorInicio = COLOR_BOTONES;   // mismo color en los dos lados = color liso
            boton.ColorFin = COLOR_BOTONES;
            boton.ColorLetra = Color.Black;      // letra negra solo en estos botones
            Controls.Add(boton);
            return boton;
        }

        // ---------- Redibujar la pantalla ----------
        private void Actualizar()
        {
            JugadorUno actual = juego.JugadorActual;
            int indiceActual = IndiceDe(actual);

            if (juego.PartidaTerminada)
                lblInfo.Text = "¡" + juego.GanadorPartida.Nombre + " ganó\nla partida!";
            else if (juego.RondaTerminada)
                lblInfo.Text = "Ronda " + juego.NumeroRonda + "\nTerminó la ronda";
            else
                lblInfo.Text = "Ronda " + juego.NumeroRonda + "\nTurno de " + actual.Nombre;

            lblSentido.Text = juego.Direccion >= 0 ? "Sentido: ↻" : "Sentido: ↺";

            // Borrar las cartas dibujadas antes
            for (int i = cartasEnMesa.Count - 1; i >= 0; i--)
                cartasEnMesa[i].Dispose();
            cartasEnMesa.Clear();

            for (int i = 0; i < 4; i++)
                lblAsientos[i].Text = "";

            // Rivales: en orden de turno a partir del jugador actual
            int[] asientos = AsientosRivales(juego.Jugadores.Count);
            for (int k = 0; k < asientos.Length; k++)
            {
                int indice = (indiceActual + k + 1) % juego.Jugadores.Count;
                JugadorUno rival = juego.Jugadores[indice];
                lblAsientos[asientos[k]].Text = TextoJugador(rival);

                if (asientos[k] == ARRIBA)
                    DibujarArriba(rival);
                else
                    DibujarLado(rival, asientos[k] == IZQUIERDA ? 20 : 1090);
            }

            // Jugador en turno (abajo)
            if (!juego.RondaTerminada)
            {
                lblAsientos[ABAJO].Text = TextoJugador(actual) + "  ·  tu turno";
                DibujarMano(actual);
            }

            // Centro
            picMazo.Image = ObtenerReverso();
            picDescarte.Image = ObtenerImagen(juego.CartaSuperior);

            if (picColor.Image != null)
                picColor.Image.Dispose();
            picColor.Image = DibujarRombo(ColorDibujo(juego.ColorActivo));
            lblColor.Text = juego.ColorActivo == null ? "por elegir" : juego.ColorActivo;

            // Mensaje
            if (mensajeError != null)
            {
                lblMensaje.Text = mensajeError;
                lblMensaje.ForeColor = COLOR_ERROR;
                mensajeError = null;
            }
            else
            {
                lblMensaje.ForeColor = COLOR_TEXTO;
                if (juego.RondaTerminada && !juego.PartidaTerminada)
                    lblMensaje.Text = "Terminó la ronda. Da clic en \"Siguiente ronda\" para continuar.";
                else
                    lblMensaje.Text = "";
            }

            btnUno.Text = decirUno ? "¡UNO! ✓" : "¡UNO!";
            btnUno.Enabled = !juego.RondaTerminada;
            btnPasar.Enabled = !juego.RondaTerminada;
            btnFaltaUno.Enabled = juego.FaltaUnoPendiente;
            btnSiguienteRonda.Enabled = juego.RondaTerminada && !juego.PartidaTerminada;
        }

        private string TextoJugador(JugadorUno jugador)
        {
            return jugador.Nombre + "  ·  " + jugador.Mano.Count + " cartas  ·  " + jugador.Puntos + " pts";
        }

        // Qué asientos usan los rivales según cuántos jugadores hay
        private int[] AsientosRivales(int cantidadJugadores)
        {
            if (cantidadJugadores == 2)
                return new int[] { ARRIBA };
            if (cantidadJugadores == 3)
                return new int[] { IZQUIERDA, DERECHA };
            return new int[] { IZQUIERDA, ARRIBA, DERECHA };
        }

        // Separación entre cartas: se enciman más si no caben
        private int CalcularPaso(int cantidad, int espacio, int tamanoCarta, int pasoNormal)
        {
            if (cantidad <= 1)
                return pasoNormal;

            int pasoMaximo = (espacio - tamanoCarta) / (cantidad - 1);
            if (pasoMaximo < pasoNormal)
                return pasoMaximo;
            return pasoNormal;
        }

        // Rival de arriba: fila horizontal centrada
        private void DibujarArriba(JugadorUno jugador)
        {
            int cantidad = jugador.Mano.Count;
            int espacio = 540;
            int paso = CalcularPaso(cantidad, espacio, ANCHO_MINI, 38);
            int anchoTotal = (cantidad - 1) * paso + ANCHO_MINI;
            int x = 330 + (espacio - anchoTotal) / 2;

            for (int c = 0; c < cantidad; c++)
                AgregarCarta(ImagenRival(jugador.Mano[c], false), x + c * paso, 42, ANCHO_MINI, ALTO_MINI);
        }

        // Rivales de los lados: columna de cartas acostadas
        private void DibujarLado(JugadorUno jugador, int x)
        {
            int cantidad = jugador.Mano.Count;
            int espacio = 420;
            int paso = CalcularPaso(cantidad, espacio, ANCHO_MINI, 32);
            int altoTotal = (cantidad - 1) * paso + ANCHO_MINI;
            int y = 178 + (espacio - altoTotal) / 2;

            for (int c = 0; c < cantidad; c++)
                AgregarCarta(ImagenRival(jugador.Mano[c], true), x, y + c * paso, ALTO_MINI, ANCHO_MINI);
        }

        // Jugador en turno: cartas grandes; las que se pueden jugar suben
        private void DibujarMano(JugadorUno jugador)
        {
            int cantidad = jugador.Mano.Count;
            int espacio = 900;
            int paso = CalcularPaso(cantidad, espacio, ANCHO, ANCHO + 6);
            int anchoTotal = (cantidad - 1) * paso + ANCHO;
            int x = 150 + (espacio - anchoTotal) / 2;

            for (int c = 0; c < cantidad; c++)
            {
                Carta carta = jugador.Mano[c];
                bool sePuede = juego.PuedeJugar(carta);
                int y = sePuede ? 458 : 482;

                PictureBox pb = AgregarCarta(ObtenerImagen(carta), x + c * paso, y, ANCHO, ALTO);
                pb.Cursor = Cursors.Hand;
                if (sePuede)
                {
                    pb.Padding = new Padding(3);
                    pb.BackColor = Color.Gold;
                }

                Carta cartaClic = carta;
                pb.Click += (s, e) => ClickCarta(cartaClic);
            }
        }

        private PictureBox AgregarCarta(Image imagen, int x, int y, int ancho, int alto)
        {
            PictureBox pb = new PictureBox();
            pb.Location = new Point(x, y);
            pb.Size = new Size(ancho, alto);
            pb.SizeMode = PictureBoxSizeMode.StretchImage;
            pb.BackColor = Color.Transparent;
            pb.Image = imagen;
            Controls.Add(pb);
            pb.BringToFront();      // la última carta queda encima de la anterior
            cartasEnMesa.Add(pb);
            return pb;
        }

        private Image ImagenRival(Carta carta, bool acostada)
        {
            string clave;
            Image imagen;
            if (MOSTRAR_CARTAS_RIVALES)
            {
                clave = carta.NombreImagen();
                imagen = ObtenerImagen(carta);
            }
            else
            {
                clave = "reverso.png";
                imagen = ObtenerReverso();
            }

            if (!acostada)
                return imagen;

            // Versión girada 90° (se guarda para no girarla cada vez)
            string claveGirada = clave + "|girada";
            if (!imagenes.ContainsKey(claveGirada))
            {
                Bitmap girada = new Bitmap(imagen);
                girada.RotateFlip(RotateFlipType.Rotate90FlipNone);
                imagenes[claveGirada] = girada;
            }
            return imagenes[claveGirada];
        }

        // Rombo del color activo (centro de la mesa)
        private Image DibujarRombo(Color color)
        {
            Bitmap imagen = new Bitmap(80, 80);
            using (Graphics g = Graphics.FromImage(imagen))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Point[] puntos = { new Point(40, 2), new Point(78, 40), new Point(40, 78), new Point(2, 40) };
                using (SolidBrush brocha = new SolidBrush(color))
                    g.FillPolygon(brocha, puntos);
                using (Pen borde = new Pen(Color.White, 3))
                    g.DrawPolygon(borde, puntos);
            }
            return imagen;
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
            ventana.BackColor = COLOR_FONDO;

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
                boton.ForeColor = Tema.Texto;            // letra oscura sobre botón pastel
                boton.UseCompatibleTextRendering = true;
                boton.Font = Fuente(10);
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
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.Clear(Color.White);

                using (SolidBrush brocha = new SolidBrush(fondo))
                    g.FillRectangle(brocha, 5, 5, ANCHO - 10, ALTO - 10);

                using (Font fuente = Fuente(13))
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