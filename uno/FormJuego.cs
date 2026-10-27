using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

/*

  QUÉ ES
  - Es la ventana donde se juega. La abre FormInicio con los 4 jugadores
    elegidos (List<JugadorBD>, que trae id y nombre de la base de datos).
  - NO tiene las reglas: las reglas están en Juego.cs (motor). Esta clase
    solo DIBUJA el estado del juego y le MANDA las acciones del usuario.
  - El log en la BD lo hace RegistradorLog (usa BaseDatos.cs, que llama a la
    API en Python, y la API guarda en MySQL). Esta clase solo lo crea.
  - Las imágenes de las cartas las carga ImagenesCartas.

  FLUJO GENERAL
  1. Constructor: tamaño, colores, fuente -> CrearControles() -> IniciarPartida().
  2. IniciarPartida(): separa los nombres, crea el Juego, reparte si hace
     falta (IniciarRonda) y arranca RegistradorLog (crea la partida en la BD).
  3. Cada clic -> evento (ClickCarta, ClickMazo...) -> Intentar(acción del motor)
     -> DespuesDeAccion(). El log no se llama aquí: RegistradorLog escucha
     los eventos de Juego y guarda cada movimiento por su cuenta.
  4. DespuesDeAccion(): resuelve lo pendiente (color inicial si sale comodín,
     desafío del +4), detecta cambio de turno, llama Actualizar() y, si
     terminó la partida, muestra FormFinal (RegistradorLog ya guardó el
     resultado al recibir el evento PartidaFinalizada).
  5. Actualizar(): borra las cartas de la mesa y las vuelve a dibujar todas
     con el estado actual del juego.

  LA MESA (forma de los asientos)
  - ABAJO = jugador en turno (cartas grandes y clicables).
    IZQUIERDA, ARRIBA, DERECHA = los demás, en orden de turno.
  - Fórmula del rival: (indiceActual + k + 1) % cantidadJugadores
    -> por eso la mesa "gira" y el jugador en turno siempre queda abajo.
  - CalcularPaso(): separación entre cartas; si no caben, se enciman más.
  - AgregarCarta(): crea un PictureBox; BringToFront() pone cada carta
    encima de la anterior; se guarda en cartasEnMesa para borrarla después.
  - ImagenRival(): cartas de los lados giradas 90° con RotateFlip.
  - Cartas jugables (juego.PuedeJugar): suben un poco y tienen borde dorado.
  - DibujarRombo(): dibuja en un Bitmap el rombo del color activo.

  IMÁGENES
  - ImagenesCartas.Obtener(carta) y ImagenesCartas.ObtenerDorso(): cargan las
    imágenes, las guardan en caché y, si falta un archivo, dibujan una
    carta de respaldo. Ahí vive la ruta Imagenes_pastel.
  - Aquí solo queda el diccionario "imagenes" para las versiones GIRADAS 90°
    de las cartas de los lados (ver ImagenRival).
  - ObtenerTitulo(): carga el logo decorativo del juego desde Decoracion\uno_titulo.png.

  FUENTE (Fredoka)
  - Datos de jugadores y texto normal: Fredoka Regular.
  - Texto secundario: Fredoka Medium.
  - Títulos y botones: Fredoka SemiBold.
  - Ambas se cargan desde la carpeta Fuentes con PrivateFontCollection.
  - Las etiquetas usan LabelCute para dibujar el texto con AntiAliasGridFit
    y evitar el aspecto pixelado típico de algunos Labels de WinForms.
  - Si no encuentra los archivos, usa Segoe UI como respaldo.

  BASE DE DATOS (log)
  - RegistradorLog (se crea en IniciarPartida, solo en Local y Host):
      Iniciar()                  -> crea la partida en la BD y se suscribe a Juego.
      juego.MovimientoRealizado  -> bd.RegistrarMovimiento en cada acción.
      juego.PartidaFinalizada    -> bd.RegistrarResultado (ganador y cartas).
  - Si algo falla, RegistradorLog guarda el texto en UltimoError y el juego
    sigue. RevisarLog() lo muestra UNA sola vez (variable avisoBD).
  - El cliente en red no tiene Juego ni BD: no registra nada.

  MANEJO DE ERRORES
  - Intentar(Action): ejecuta una acción del motor dentro de try/catch.
    Si la jugada no es válida, el motor lanza una excepción, el mensaje
    se guarda en mensajeError y se muestra en rojo. El juego no se cierra.

  EVENTOS (botones y clics)
  - ClickCarta: si es comodín, primero pide color (ElegirColor); manda si
    se dijo UNO. ClickUno: activa o desactiva "decir UNO" para la jugada.
  - ClickMazo: robar. ClickPasar: pasar después de robar.
  - ClickFaltaUno: castigar a quien no dijo UNO.
  - ClickSiguienteRonda: empieza otra ronda (IniciarRonda).
  - ElegirColor(): ventana modal (ShowDialog). ControlBox = false quita la
    X, así que es obligatorio elegir un color.

  MODO RED (cliente-servidor)
  - Hay tres modos (enum Modo):
      Local   -> una sola computadora, como antes (yo controlo los 4 jugadores).
      Host    -> ESTA computadora tiene el Juego, la BD y las reglas. Tiene una
                 Conexion por cada cliente (1 a 3). Los asientos se reparten por
                 turno: el asiento i es de la computadora (i % computadoras), y la
                 0 es el anfitrión. Con 2 computadoras: yo 0 y 2, el cliente 1 y 3.
                 Con 4: un jugador por computadora.
      Cliente -> NO tiene Juego ni BD. Recibe un EstadoRed (JSON) tras cada acción,
                 lo dibuja y manda sus clics al anfitrión ("Jugar", "Robar"...).
  - Todo se dibuja desde "estado" (un EstadoRed), sea Local, Host o Cliente.
    Actualizar() ya no lee el Juego directamente.
  - El anfitrión arma una foto distinta para cada computadora: cada una solo
    recibe las manos de SUS jugadores (de los demás, cuántas cartas tienen).
  - Los diálogos (color inicial, desafío de +4) los muestra la computadora que
    controla al jugador que debe decidir: ResolverPendientes().
  - Solo el anfitrión valida jugadas y guarda el log en la BD.
  - Conexion.cs manda/recibe; EstadoRed.cs define los datos; FormRed.cs conecta.

 */

namespace uno
{
    public class FormJuego : Form
    {
        // Tamaños de cartas
        private const int ANCHO = 105;
        private const int ALTO = 158;
        private const int ANCHO_MINI = 60;
        private const int ALTO_MINI = 90;

        private const string CARPETA_DECORACION = "Decoracion";
        private const string ARCHIVO_TITULO = "uno_titulo.png";
        private const string FUENTE_REGULAR = "Fredoka-Regular.ttf";
        private const string FUENTE_MEDIUM = "Fredoka-Medium.ttf";
        private const string FUENTE_SEMIBOLD = "Fredoka-SemiBold.ttf";
        private const bool MOSTRAR_CARTAS_RIVALES = true;

        // Paleta lavanda medio oscuro, elegante y más contrastante
        private static readonly Color COLOR_FONDO = ColorTranslator.FromHtml("#7C6884"); //("#846874"); 
        private static readonly Color COLOR_TEXTO = ColorTranslator.FromHtml("#FFF7FC");
        private static readonly Color COLOR_TEXTO_SUAVE = ColorTranslator.FromHtml("#E9DDF3");
        private static readonly Color COLOR_ERROR = ColorTranslator.FromHtml("#FFB0C8");
        private static readonly Color COLOR_BOTON_INICIO = ColorTranslator.FromHtml("#F7C8DE");
        private static readonly Color COLOR_BOTON_FIN = ColorTranslator.FromHtml("#D8CCFF");
        private static readonly Color COLOR_TEXTO_BOTON = ColorTranslator.FromHtml("#4B3B5B");
        private static readonly Color COLOR_JUGABLE = ColorTranslator.FromHtml("#FFF0AF");

        private const int ABAJO = 0;
        private const int IZQUIERDA = 1;
        private const int ARRIBA = 2;
        private const int DERECHA = 3;

        private static PrivateFontCollection fuentesRegular = null;
        private static PrivateFontCollection fuentesMedium = null;
        private static PrivateFontCollection fuentesSemiBold = null;
        private static FontFamily familiaFredokaRegular = null;
        private static FontFamily familiaFredokaMedium = null;
        private static FontFamily familiaFredokaSemiBold = null;
        private enum Modo { Local, Host, Cliente }

        private Modo modo = Modo.Local;
        private Conexion conexion;                           // solo Cliente: su conexión con el anfitrión
        private List<Conexion> conexiones = new List<Conexion>();   // solo Host: una por cliente
        private List<List<int>> asientosDe = new List<List<int>>(); // solo Host: asientosDe[i] = jugadores de la conexión i
        private Juego juego;                                 // solo Local y Host (quien tiene las reglas)
        private BaseDatos bd;                                // solo Local y Host (el cliente no toca la BD)
        private EstadoRed estado;                            // foto de la partida: de aquí se dibuja todo
        private List<int> misIndices = new List<int>();      // jugadores que controla ESTA computadora

        private int perspectiva = 0;                         // jugador que se dibuja abajo
        private int turnoAnterior = -1;
        private string errorCliente = null;                 // jugada rechazada de un cliente; solo ese cliente la recibe
        private int conexionConError = -1;                   // qué conexión recibe errorCliente
        private bool cerrando = false;
        private bool resolviendo = false;
        private RegistradorLog registrador;                  // solo Local y Host: guarda el log en la BD
        private bool finMostrado = false;
        private bool avisoBD = false;
        private bool decirUno = false;
        private string mensajeError = null;

        private Dictionary<string, Image> imagenes = new Dictionary<string, Image>();   // solo las versiones giradas
        private List<Control> cartasEnMesa = new List<Control>();

        private LabelCute lblInfo;
        private Label lblSentido;
        private Label[] lblAsientos = new Label[4];
        private LabelCute lblTitulo;
        private PictureBox picMazo;
        private PictureBox picDescarte;
        private PictureBox picColor;
        private Label lblColor;
        private BotonPastel btnUno;
        private BotonPastel btnPasar;
        private BotonPastel btnFaltaUno;
        private BotonPastel btnSiguienteRonda;
        private Label lblMensaje;

        // Una sola computadora (como antes)
        public FormJuego(List<JugadorBD> jugadores)
        {
            Inicializar();
            modo = Modo.Local;
            IniciarPartida(jugadores);
        }

        // Anfitrión: ya están conectados todos los clientes (1 a 3), en orden de llegada
        public FormJuego(List<JugadorBD> jugadores, List<Conexion> conexiones)
        {
            Inicializar();
            modo = Modo.Host;
            this.conexiones = conexiones;
            Text = "UNO - Partida (anfitrión)";
            IniciarPartida(jugadores);
        }

        // Cliente: solo dibuja lo que manda el anfitrión
        public FormJuego(Conexion conexion)
        {
            Inicializar();
            modo = Modo.Cliente;
            this.conexion = conexion;
            Text = "UNO - Partida (cliente)";
            lblMensaje.Text = "Conectado. Esperando a que empiece la partida...";
            ConectarRedCliente();
        }

        private void Inicializar()
        {
            Text = "UNO - Partida";
            ClientSize = new Size(1200, 660);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = COLOR_FONDO;
            Font = Fuente(10);

            CrearControles();

            // El primer diálogo (color inicial) se muestra cuando la ventana ya está a la vista
            Shown += delegate { ResolverPendientes(); };
            FormClosing += delegate
            {
                cerrando = true;
                if (conexion != null) conexion.Cerrar();
                foreach (Conexion c in conexiones)
                    c.Cerrar();
            };
        }

        private FontFamily CargarFamilia(
            string nombreArchivo,
            ref PrivateFontCollection coleccion,
            ref FontFamily familia)
        {
            if (familia != null)
                return familia;

            string ruta = Path.Combine(
                Application.StartupPath,
                "Fuentes",
                nombreArchivo
            );

            if (!File.Exists(ruta))
                return null;

            coleccion = new PrivateFontCollection();
            coleccion.AddFontFile(ruta);

            if (coleccion.Families.Length > 0)
                familia = coleccion.Families[0];

            return familia;
        }

        // Texto normal y datos de jugadores: Fredoka Regular
        private Font Fuente(float tamano)
        {
            FontFamily familia = CargarFamilia(
                FUENTE_REGULAR,
                ref fuentesRegular,
                ref familiaFredokaRegular
            );

            if (familia != null)
                return new Font(
                    familia,
                    tamano,
                    FontStyle.Regular,
                    GraphicsUnit.Point
                );

            return new Font(
                "Segoe UI",
                tamano,
                FontStyle.Regular,
                GraphicsUnit.Point
            );
        }

        // Texto secundario un poquito más marcado: Fredoka Medium
        private Font FuenteMedia(float tamano)
        {
            FontFamily familia = CargarFamilia(
                FUENTE_MEDIUM,
                ref fuentesMedium,
                ref familiaFredokaMedium
            );

            if (familia != null)
                return new Font(
                    familia,
                    tamano,
                    FontStyle.Regular,
                    GraphicsUnit.Point
                );

            return Fuente(tamano);
        }

        // Títulos y botones: Fredoka SemiBold
        private Font FuenteTitulo(float tamano)
        {
            FontFamily familia = CargarFamilia(
                FUENTE_SEMIBOLD,
                ref fuentesSemiBold,
                ref familiaFredokaSemiBold
            );

            if (familia != null)
                return new Font(
                    familia,
                    tamano,
                    FontStyle.Regular,
                    GraphicsUnit.Point
                );

            return new Font(
                "Segoe UI Semibold",
                tamano,
                FontStyle.Regular,
                GraphicsUnit.Point
            );
        }

        // Fondo liso, suave y pastel.
        // Se eligió un lavanda clarito para que combine con el logo y las cartas.
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(COLOR_FONDO);
        }

        //Inicio de la partida (Local y Host: quien tiene el Juego)
        private void IniciarPartida(List<JugadorBD> jugadores)
        {
            if (modo == Modo.Host && (conexiones.Count < 1 || conexiones.Count >= jugadores.Count))
                throw new ArgumentException("Se necesita al menos 1 cliente y menos clientes que jugadores.");

            List<string> nombres = new List<string>();
            for (int i = 0; i < jugadores.Count; i++)
            {
                nombres.Add(jugadores[i].nombre);
            }

            juego = new Juego(nombres);
            if (juego.Jugadores[0].Mano.Count == 0)
                juego.IniciarRonda();

            // El log se guarda solo: RegistradorLog escucha los eventos de Juego
            // (MovimientoRealizado y PartidaFinalizada). Se crea antes de la primera jugada.
            bd = new BaseDatos();
            registrador = new RegistradorLog(bd, juego);
            if (!registrador.Iniciar())
                AvisarErrorBD(registrador.UltimoError);

            // Reparto de asientos entre las computadoras: el asiento i es de la computadora
            // (i % total). La 0 es el anfitrión; la 1, el primer cliente que se conectó, etc.
            //   2 computadoras: yo 0 y 2; el cliente 1 y 3
            //   4 computadoras: yo 0; un jugador por cada cliente
            int equipos = conexiones.Count + 1;
            for (int c = 0; c < conexiones.Count; c++)
                asientosDe.Add(new List<int>());

            for (int i = 0; i < juego.Jugadores.Count; i++)
            {
                int equipo = (modo == Modo.Host) ? i % equipos : 0;
                if (equipo == 0)
                    misIndices.Add(i);
                else
                    asientosDe[equipo - 1].Add(i);
            }

            if (modo == Modo.Host)
                ConectarRedHost();

            DespuesDeAccion();
        }

        // Construir la interfaz
        private void CrearControles()
        {
            lblTitulo = CrearEtiqueta(
    new Point(20, 8),
    new Size(390, 90),
    44
);

            lblTitulo.Text = "UNO";

            lblTitulo.Font = FuenteTitulo(44);

            lblTitulo.ForeColor =
                ColorTranslator.FromHtml("#FFD6E8");

            lblTitulo.TextAlign =
                ContentAlignment.MiddleLeft;

            lblTitulo.UsarSombra = true;

            lblTitulo.ColorSombra =
                Color.FromArgb(
                    95,
                    47,
                    34,
                    61
                );

            lblTitulo.DesplazamientoSombra =
                new Point(4, 5);

            lblTitulo.UsarContorno = true;

            lblTitulo.ColorContorno =
                Color.White;

            lblTitulo.GrosorContorno = 7f;

            lblInfo = CrearEtiqueta(new Point(24, 92), new Size(320, 58), 15);
            lblInfo.Font = FuenteTitulo(15);
            lblInfo.ForeColor = COLOR_TEXTO;
            lblInfo.TextAlign = ContentAlignment.TopLeft;
            lblInfo.UsarSombra = false;

            lblSentido = CrearEtiqueta(new Point(955, 16), new Size(220, 26), 10);
            lblSentido.Font = FuenteMedia(10);
            lblSentido.ForeColor = COLOR_TEXTO_SUAVE;
            lblSentido.TextAlign = ContentAlignment.TopRight;

            lblAsientos[ABAJO] = CrearEtiqueta(new Point(150, 428), new Size(620, 30), 11);
            lblAsientos[ABAJO].ForeColor = COLOR_TEXTO;
            lblAsientos[IZQUIERDA] = CrearEtiqueta(new Point(20, 145), new Size(220, 28), 10);
            lblAsientos[IZQUIERDA].ForeColor = COLOR_TEXTO_SUAVE;
            lblAsientos[ARRIBA] = CrearEtiqueta(new Point(330, 12), new Size(540, 28), 10);
            lblAsientos[ARRIBA].ForeColor = COLOR_TEXTO_SUAVE;
            lblAsientos[ARRIBA].TextAlign = ContentAlignment.MiddleCenter;
            lblAsientos[DERECHA] = CrearEtiqueta(new Point(960, 145), new Size(220, 28), 10);
            lblAsientos[DERECHA].ForeColor = COLOR_TEXTO_SUAVE;
            lblAsientos[DERECHA].TextAlign = ContentAlignment.MiddleRight;

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
            lblColor.Font = FuenteMedia(10);
            lblColor.ForeColor = COLOR_TEXTO;
            lblColor.TextAlign = ContentAlignment.MiddleCenter;

            btnUno = CrearBoton("¡UNO!", new Point(790, 204));
            btnUno.Click += ClickUno;

            btnPasar = CrearBoton("Pasar (después de robar)", new Point(790, 250));
            btnPasar.Click += ClickPasar;

            btnFaltaUno = CrearBoton("¡Señalar falta de UNO!", new Point(790, 296));
            btnFaltaUno.Click += ClickFaltaUno;

            btnSiguienteRonda = CrearBoton("Siguiente ronda", new Point(790, 342));
            btnSiguienteRonda.Click += ClickSiguienteRonda;

            lblMensaje = CrearEtiqueta(new Point(150, 392), new Size(900, 30), 10);
            lblMensaje.Font = FuenteMedia(10);
            lblMensaje.TextAlign = ContentAlignment.MiddleCenter;

            lblTitulo.BringToFront();
            lblInfo.BringToFront();
            lblSentido.BringToFront();
        }

        private LabelCute CrearEtiqueta(Point posicion, Size tamano, int tamanoLetra)
        {
            LabelCute etiqueta = new LabelCute();

            etiqueta.Location = posicion;
            etiqueta.Size = tamano;
            etiqueta.Font = Fuente(tamanoLetra);
            etiqueta.ForeColor = COLOR_TEXTO;
            etiqueta.BackColor = Color.Transparent;
            etiqueta.TextAlign = ContentAlignment.MiddleLeft;

            Controls.Add(etiqueta);
            return etiqueta;
        }

        private BotonPastel CrearBoton(string texto, Point posicion)
        {
            BotonPastel boton = new BotonPastel();
            boton.Text = texto;
            boton.Font = FuenteTitulo(10);
            boton.Location = posicion;
            boton.Size = new Size(215, 38);
            boton.ColorInicio = COLOR_BOTON_INICIO;
            boton.ColorFin = COLOR_BOTON_FIN;
            boton.ColorLetra = COLOR_TEXTO_BOTON;
            Controls.Add(boton);
            return boton;
        }

        private Image ObtenerTitulo()
        {
            return CrearTituloProvisional();
        }

        private Image CrearTituloProvisional()
        {
            Bitmap imagen = new Bitmap(800, 180);

            using (Graphics g = Graphics.FromImage(imagen))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.Transparent);

                FontFamily familia = familiaFredokaSemiBold;
                if (familia == null)
                    familia = FontFamily.GenericSansSerif;

                using (Font fuente = new Font(familia, 60, FontStyle.Regular, GraphicsUnit.Point))
                using (SolidBrush brocha = new SolidBrush(ColorTranslator.FromHtml("#FFE7F2")))
                {
                    g.DrawString("UNO", fuente, brocha, new PointF(20, 20));
                }
            }

            return imagen;
        }

        //Volver a dibujar la pantalla después de cada acción (siempre desde "estado")
        private void Actualizar()
        {
            if (estado == null) return;

            EstadoRed e = estado;
            int total = e.Jugadores.Count;
            int actual = e.IndiceActual;

            // Mi asiento (abajo): el jugador en turno si es mío; si no, el último mío que estuvo abajo
            if (EsMio(actual))
                perspectiva = actual;
            else if (!EsMio(perspectiva))
                perspectiva = misIndices.Count > 0 ? misIndices[0] : 0;

            if (e.PartidaTerminada)
                lblInfo.Text = "¡" + e.GanadorPartida + " ganó\nla partida!";
            else if (e.RondaTerminada)
                lblInfo.Text = "Ronda " + e.NumeroRonda + "\nTerminó la ronda";
            else
                lblInfo.Text = "Ronda " + e.NumeroRonda + "\nTurno de " + e.Jugadores[actual].Nombre;

            lblSentido.Text = e.Direccion >= 0 ? "Sentido: ↻" : "Sentido: ↺";

            // Borrar las cartas dibujadas antes
            for (int i = cartasEnMesa.Count - 1; i >= 0; i--)
                cartasEnMesa[i].Dispose();
            cartasEnMesa.Clear();

            for (int i = 0; i < 4; i++)
                lblAsientos[i].Text = "";

            // Rivales (en orden de turno a partir de mi asiento)
            int[] asientos = AsientosRivales(total);
            for (int k = 0; k < asientos.Length; k++)
            {
                int indice = (perspectiva + k + 1) % total;
                JugadorRed rival = e.Jugadores[indice];
                lblAsientos[asientos[k]].Text = TextoJugador(rival);

                if (asientos[k] == ARRIBA)
                    DibujarArriba(rival);
                else
                    DibujarLado(rival, asientos[k] == IZQUIERDA ? 20 : 1090);
            }

            // Mi jugador (abajo)
            if (!e.RondaTerminada)
            {
                JugadorRed yo = e.Jugadores[perspectiva];
                lblAsientos[ABAJO].Text = TextoJugador(yo) + (perspectiva == actual ? "  ·  tu turno" : "");
                DibujarMano(yo);
            }

            // Centro
            picMazo.Image = ImagenesCartas.ObtenerDorso();
            picDescarte.Image = ImagenesCartas.Obtener(e.CartaSuperior.ACarta());

            if (picColor.Image != null)
                picColor.Image.Dispose();
            picColor.Image = DibujarRombo(ColorDibujo(e.ColorActivo));
            lblColor.Text = e.ColorActivo == null ? "por elegir" : e.ColorActivo;

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
                if (e.RondaTerminada && !e.PartidaTerminada)
                    lblMensaje.Text = modo == Modo.Cliente
                        ? "Terminó la ronda. Esperando a que el anfitrión inicie la siguiente."
                        : "Terminó la ronda. Da clic en \"Siguiente ronda\" para continuar.";
                else if (!e.RondaTerminada && !EsMio(actual))
                    lblMensaje.Text = "Esperando a " + e.Jugadores[actual].Nombre + "...";
                else
                    lblMensaje.Text = "";
            }

            bool miTurno = EsMio(actual);
            btnUno.Text = decirUno ? "¡UNO! ✓" : "¡UNO!";
            btnUno.Enabled = !e.RondaTerminada && miTurno;
            btnPasar.Enabled = !e.RondaTerminada && miTurno;
            btnFaltaUno.Enabled = e.FaltaUnoPendiente;
            btnSiguienteRonda.Enabled = e.RondaTerminada && !e.PartidaTerminada && modo != Modo.Cliente;
        }

        private string TextoJugador(JugadorRed jugador)
        {
            return jugador.Nombre + "  ·  " + jugador.NumCartas + " cartas  ·  " + jugador.Puntos + " pts";
        }

        private int[] AsientosRivales(int cantidadJugadores)
        {
            if (cantidadJugadores == 2)
                return new int[] { ARRIBA };
            if (cantidadJugadores == 3)
                return new int[] { IZQUIERDA, DERECHA };
            return new int[] { IZQUIERDA, ARRIBA, DERECHA };
        }

        private int CalcularPaso(int cantidad, int espacio, int tamanoCarta, int pasoNormal)
        {
            if (cantidad <= 1)
                return pasoNormal;

            int pasoMaximo = (espacio - tamanoCarta) / (cantidad - 1);
            if (pasoMaximo < pasoNormal)
                return pasoMaximo;
            return pasoNormal;
        }

        private void DibujarArriba(JugadorRed jugador)
        {
            int cantidad = jugador.NumCartas;
            int espacio = 540;
            int paso = CalcularPaso(cantidad, espacio, ANCHO_MINI, 38);
            int anchoTotal = (cantidad - 1) * paso + ANCHO_MINI;
            int x = 330 + (espacio - anchoTotal) / 2;

            for (int c = 0; c < cantidad; c++)
                AgregarCarta(ImagenRival(CartaVisible(jugador, c), false), x + c * paso, 42, ANCHO_MINI, ALTO_MINI);
        }

        private void DibujarLado(JugadorRed jugador, int x)
        {
            int cantidad = jugador.NumCartas;
            int espacio = 420;
            int paso = CalcularPaso(cantidad, espacio, ANCHO_MINI, 32);
            int altoTotal = (cantidad - 1) * paso + ANCHO_MINI;
            int y = 178 + (espacio - altoTotal) / 2;

            for (int c = 0; c < cantidad; c++)
                AgregarCarta(ImagenRival(CartaVisible(jugador, c), true), x, y + c * paso, ALTO_MINI, ANCHO_MINI);
        }

        // La carta si esa mano se puede ver; null si no (entonces se dibuja el dorso)
        private Carta CartaVisible(JugadorRed jugador, int posicion)
        {
            if (!MOSTRAR_CARTAS_RIVALES || jugador.Mano == null)
                return null;
            return jugador.Mano[posicion].ACarta();
        }

        private void DibujarMano(JugadorRed jugador)
        {
            if (jugador.Mano == null) return;

            int cantidad = jugador.Mano.Count;
            int espacio = 900;
            int paso = CalcularPaso(cantidad, espacio, ANCHO, ANCHO + 6);
            int anchoTotal = (cantidad - 1) * paso + ANCHO;
            int x = 150 + (espacio - anchoTotal) / 2;

            for (int c = 0; c < cantidad; c++)
            {
                CartaRed carta = jugador.Mano[c];
                bool sePuede = carta.Jugable;
                int y = sePuede ? 458 : 482;

                PictureBox pb = AgregarCarta(ImagenesCartas.Obtener(carta.ACarta()), x + c * paso, y, ANCHO, ALTO);
                pb.Cursor = Cursors.Hand;
                if (sePuede)
                {
                    pb.Padding = new Padding(3);
                    pb.BackColor = COLOR_JUGABLE;
                }

                int posicion = c;   // copia para el clic (la carta se identifica por su posición en la mano)
                pb.Click += (s, ev) => ClickCarta(posicion);
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

        // carta == null -> dorso (mano que no se puede ver)
        private Image ImagenRival(Carta carta, bool acostada)
        {
            string clave;
            Image imagen;
            if (carta != null)
            {
                clave = carta.NombreImagen();
                imagen = ImagenesCartas.Obtener(carta);
            }
            else
            {
                clave = "reverso.png";
                imagen = ImagenesCartas.ObtenerDorso();
            }

            if (!acostada)
                return imagen;

            string claveGirada = clave + "|girada";
            if (!imagenes.ContainsKey(claveGirada))
            {
                Bitmap girada = new Bitmap(imagen);
                girada.RotateFlip(RotateFlipType.Rotate90FlipNone);
                imagenes[claveGirada] = girada;
            }
            return imagenes[claveGirada];
        }

        private Image DibujarRombo(Color color)
        {
            Bitmap imagen = new Bitmap(80, 80);
            using (Graphics g = Graphics.FromImage(imagen))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Point[] puntos = { new Point(40, 2), new Point(78, 40), new Point(40, 78), new Point(2, 40) };
                using (SolidBrush brocha = new SolidBrush(color))
                    g.FillPolygon(brocha, puntos);
                using (Pen borde = new Pen(COLOR_TEXTO, 3))
                    g.DrawPolygon(borde, puntos);
            }
            return imagen;
        }

        // ---------- Después de cada acción (Local y Host: quien tiene el Juego) ----------
        private void DespuesDeAccion()
        {
            // 1. Foto nueva de la partida y redibujar
            estado = ConstruirEstado(misIndices);
            AplicarEstado();

            // 2. El anfitrión le manda su foto al cliente
            if (modo == Modo.Host)
                EnviarEstadosAClientes();

            // 3. ¿Falló el log? (aviso una sola vez)
            RevisarLog();

            // 4. Fin de la partida
            if (juego.PartidaTerminada && !finMostrado)
            {
                finMostrado = true;
                using (FormFinal pantallaFinal = new FormFinal(juego))
                    pantallaFinal.ShowDialog(this);

                Close();
                return;
            }

            // 5. Color inicial / desafío de +4, si me toca decidir a mí
            ProgramarPendientes();
        }

        // Dibuja y reinicia lo que depende del turno (antes: CambioDeTurno)
        private void AplicarEstado()
        {
            if (estado.IndiceActual != turnoAnterior)
            {
                turnoAnterior = estado.IndiceActual;
                decirUno = false;
            }
            Actualizar();
        }

        // Foto del juego para una computadora: solo ve las manos de "visibles"
        // (al terminar la ronda se enseñan todas para ver los puntos)
        private EstadoRed ConstruirEstado(List<int> visibles)
        {
            bool verTodo = juego.RondaTerminada;
            EstadoRed e = new EstadoRed();
            e.Tuyos = new List<int>(visibles);

            for (int i = 0; i < juego.Jugadores.Count; i++)
            {
                JugadorUno j = juego.Jugadores[i];
                JugadorRed jr = new JugadorRed();
                jr.Nombre = j.Nombre;
                jr.Puntos = j.Puntos;
                jr.NumCartas = j.Mano.Count;
                if (verTodo || visibles.Contains(i))
                {
                    jr.Mano = new List<CartaRed>();
                    foreach (Carta c in j.Mano)
                        jr.Mano.Add(CartaRed.De(c, juego.PuedeJugar(c)));
                }
                e.Jugadores.Add(jr);
            }

            e.IndiceActual = IndiceDe(juego.JugadorActual);
            e.CartaSuperior = CartaRed.De(juego.CartaSuperior, false);
            e.ColorActivo = juego.ColorActivo;
            e.Direccion = juego.Direccion;
            e.NumeroRonda = juego.NumeroRonda;
            e.RondaTerminada = juego.RondaTerminada;
            e.PartidaTerminada = juego.PartidaTerminada;
            e.GanadorPartida = juego.GanadorPartida == null ? null : juego.GanadorPartida.Nombre;
            e.EsperandoDesafioMas4 = juego.EsperandoDesafioMas4;
            e.FaltaUnoPendiente = juego.FaltaUnoPendiente;
            return e;
        }

        // Cada cliente recibe SU foto: solo ve las manos de sus propios asientos.
        // Si la jugada de un cliente fue rechazada, el error es solo para él.
        private void EnviarEstadosAClientes()
        {
            for (int c = 0; c < conexiones.Count; c++)
            {
                EstadoRed e = ConstruirEstado(asientosDe[c]);
                e.Error = (c == conexionConError) ? errorCliente : null;
                conexiones[c].Enviar("Estado", e);
            }
            errorCliente = null;
            conexionConError = -1;
        }

        private bool EsMio(int indiceJugador)
        {
            return misIndices.Contains(indiceJugador);
        }

        // ---------- Acciones del motor (Local y Host) ----------
        // Las ejecuta quien tiene el Juego: un clic propio o un mensaje del cliente.
        // Ya no guardan el log: RegistradorLog lo hace solo al recibir los eventos de Juego.
        private void EjecutarJugar(int posicionMano, string colorElegido, bool unoDicho)
        {
            List<Carta> mano = juego.JugadorActual.Mano;
            if (posicionMano < 0 || posicionMano >= mano.Count)
            {
                mensajeError = "Esa carta no existe.";
                return;
            }

            Carta carta = mano[posicionMano];
            Intentar(() => juego.JugarCarta(carta, colorElegido, unoDicho));
        }

        private void EjecutarRobar()
        {
            Intentar(() => { juego.RobarCarta(); });
        }

        private void EjecutarPasar()
        {
            Intentar(() => juego.PasarTrasRobar());
        }

        private void EjecutarFaltaUno()
        {
            Intentar(() => juego.SenalarFaltaUno());
        }

        private void EjecutarColorInicial(string color)
        {
            Intentar(() => juego.ElegirColorInicial(color));
        }

        private void EjecutarMas4(bool desafiar)
        {
            Intentar(() => juego.ResolverMas4(desafiar));
        }

        // ---------- Eventos (clics) ----------
        // Local/Host: ejecutan en el motor. Cliente: mandan la petición al anfitrión.
        private void ClickCarta(int posicionMano)
        {
            if (estado == null || estado.RondaTerminada) return;
            if (!EsMio(estado.IndiceActual)) { AvisarNoEsTurno(); return; }

            JugadorRed yo = estado.Jugadores[estado.IndiceActual];
            if (yo.Mano == null || posicionMano >= yo.Mano.Count) return;

            string colorElegido = null;
            if (yo.Mano[posicionMano].ACarta().EsComodin())
                colorElegido = ElegirColor("Elige un color");

            bool unoDicho = decirUno;   // se apaga solo al cambiar el turno (AplicarEstado)

            if (modo == Modo.Cliente)
            {
                EnviarAccion("Jugar", posicionMano, colorElegido, unoDicho, false);
                return;
            }

            EjecutarJugar(posicionMano, colorElegido, unoDicho);
            DespuesDeAccion();
        }

        private void ClickUno(object sender, EventArgs e)
        {
            if (estado == null) return;
            decirUno = !decirUno;   // se presiona antes de jugar la penúltima carta
            Actualizar();
        }

        private void ClickMazo(object sender, EventArgs e)
        {
            if (estado == null || estado.RondaTerminada) return;
            if (!EsMio(estado.IndiceActual)) { AvisarNoEsTurno(); return; }

            if (modo == Modo.Cliente)
            {
                EnviarAccion("Robar", 0, null, false, false);
                return;
            }

            EjecutarRobar();
            DespuesDeAccion();
        }

        private void ClickPasar(object sender, EventArgs e)
        {
            if (estado == null || estado.RondaTerminada) return;
            if (!EsMio(estado.IndiceActual)) { AvisarNoEsTurno(); return; }

            if (modo == Modo.Cliente)
            {
                EnviarAccion("Pasar", 0, null, false, false);
                return;
            }

            EjecutarPasar();
            DespuesDeAccion();
        }

        private void ClickFaltaUno(object sender, EventArgs e)
        {
            if (estado == null) return;

            if (modo == Modo.Cliente)
            {
                EnviarAccion("FaltaUno", 0, null, false, false);
                return;
            }

            EjecutarFaltaUno();
            DespuesDeAccion();
        }

        private void ClickSiguienteRonda(object sender, EventArgs e)
        {
            if (modo == Modo.Cliente) return;   // solo el anfitrión (o el modo Local) la inicia

            Intentar(() => juego.IniciarRonda());
            decirUno = false;
            turnoAnterior = -1;
            DespuesDeAccion();
        }

        private void AvisarNoEsTurno()
        {
            mensajeError = "No es tu turno.";
            Actualizar();
        }

        // ---------- Decisiones pendientes: color inicial y desafío de +4 ----------
        // Las toma quien controla al jugador en turno (en el anfitrión, el cliente o Local).
        private void ProgramarPendientes()
        {
            if (IsHandleCreated && !cerrando)
                BeginInvoke((Action)ResolverPendientes);
        }

        private void ResolverPendientes()
        {
            if (resolviendo || cerrando || IsDisposed || estado == null) return;
            if (estado.RondaTerminada || estado.PartidaTerminada) return;
            if (!EsMio(estado.IndiceActual)) return;

            resolviendo = true;
            try
            {
                string nombre = estado.Jugadores[estado.IndiceActual].Nombre;

                if (estado.ColorActivo == null)
                {
                    string color = ElegirColor(nombre + ", elige el color inicial");
                    if (modo == Modo.Cliente)
                        EnviarAccion("ColorInicial", 0, color, false, false);
                    else
                    {
                        EjecutarColorInicial(color);
                        DespuesDeAccion();
                    }
                }
                else if (estado.EsperandoDesafioMas4)
                {
                    DialogResult respuesta = MessageBox.Show(
                        nombre + ": se jugó un +4. ¿Quieres desafiarlo?",
                        "Desafío de +4", MessageBoxButtons.YesNo);
                    bool desafiar = (respuesta == DialogResult.Yes);
                    if (modo == Modo.Cliente)
                        EnviarAccion("Mas4", 0, null, false, desafiar);
                    else
                    {
                        EjecutarMas4(desafiar);
                        DespuesDeAccion();
                    }
                }
            }
            finally
            {
                resolviendo = false;
            }
        }

        // ---------- Red ----------
        // Se suscribe primero y se llama a Iniciar() después, para no perder mensajes
        private void ConectarRedCliente()
        {
            conexion.MensajeRecibido += AlRecibirMensajeCliente;
            conexion.Desconectado += AlDesconectar;
            conexion.Iniciar();
        }

        // Una suscripción por conexión, para saber de qué cliente llega cada mensaje
        private void ConectarRedHost()
        {
            for (int i = 0; i < conexiones.Count; i++)
            {
                int c = i;   // copia para el lambda
                conexiones[c].MensajeRecibido += m =>
                {
                    if (!cerrando) RecibirComoHost(m, c);
                };
                conexiones[c].Desconectado += AlDesconectar;
                conexiones[c].Iniciar();
            }
        }

        private void EnviarAccion(string tipo, int posicion, string color, bool uno, bool desafiar)
        {
            AccionRed a = new AccionRed();
            a.Indice = posicion;
            a.Color = color;
            a.DecirUno = uno;
            a.Desafiar = desafiar;
            conexion.Enviar(tipo, a);
        }

        // Llega en el hilo de la interfaz (Conexion.Iniciar se llamó desde aquí)
        private void AlRecibirMensajeCliente(Mensaje m)
        {
            if (cerrando) return;
            RecibirComoCliente(m);
        }

        private void RecibirComoCliente(Mensaje m)
        {
            if (m.Tipo != "Estado") return;

            estado = m.Obtener<EstadoRed>();
            misIndices = estado.Tuyos;
            if (!string.IsNullOrEmpty(estado.Error))
                mensajeError = estado.Error;

            AplicarEstado();

            if (estado.PartidaTerminada && !finMostrado)
            {
                finMostrado = true;
                MostrarFinCliente();
                return;
            }
            ProgramarPendientes();
        }

        private void MostrarFinCliente()
        {
            List<JugadorRed> orden = new List<JugadorRed>(estado.Jugadores);
            orden.Sort((a, b) => b.Puntos.CompareTo(a.Puntos));

            string texto = "¡" + estado.GanadorPartida + " ganó la partida!\n\n";
            foreach (JugadorRed j in orden)
                texto += j.Nombre + ": " + j.Puntos + " pts\n";

            MessageBox.Show(this, texto, "Fin de la partida");
            Close();
        }

        // El anfitrión valida y ejecuta lo que pide el cliente c; el motor decide si es legal
        private void RecibirComoHost(Mensaje m, int c)
        {
            AccionRed a = m.Datos == null ? new AccionRed() : m.Obtener<AccionRed>();
            mensajeError = null;
            errorCliente = null;
            conexionConError = -1;

            bool exigeTurno = m.Tipo == "Jugar" || m.Tipo == "Robar" || m.Tipo == "Pasar"
                           || m.Tipo == "ColorInicial" || m.Tipo == "Mas4";

            // Solo puede actuar la conexión que controla al jugador en turno
            if (exigeTurno && !asientosDe[c].Contains(IndiceDe(juego.JugadorActual)))
            {
                errorCliente = "No es tu turno.";
            }
            else
            {
                switch (m.Tipo)
                {
                    case "Jugar": EjecutarJugar(a.Indice, a.Color, a.DecirUno); break;
                    case "Robar": EjecutarRobar(); break;
                    case "Pasar": EjecutarPasar(); break;
                    case "FaltaUno": EjecutarFaltaUno(); break;
                    case "ColorInicial": EjecutarColorInicial(a.Color); break;
                    case "Mas4": EjecutarMas4(a.Desafiar); break;
                    default: return;
                }
            }

            // Si el motor rechazó la jugada, el aviso es para ese cliente, no para mí
            if (mensajeError != null)
            {
                errorCliente = mensajeError;
                mensajeError = null;
            }
            if (errorCliente != null)
                conexionConError = c;

            DespuesDeAccion();
        }

        private void AlDesconectar()
        {
            if (cerrando) return;
            if (estado != null && estado.PartidaTerminada) return;

            cerrando = true;
            MessageBox.Show(this, "Se perdió la conexión con la otra computadora.", "Conexión");
            Close();
        }

        // ---------- Base de datos (log) ----------
        // El guardado lo hace RegistradorLog. Aquí solo se avisa si algo falló.
        private void RevisarLog()
        {
            if (registrador != null && !avisoBD && registrador.UltimoError != null)
                AvisarErrorBD(registrador.UltimoError);
        }

        // El aviso sale solo una vez para no llenar la pantalla de mensajes
        private void AvisarErrorBD(string detalle)
        {
            if (avisoBD) return;
            avisoBD = true;
            MessageBox.Show("No se pudo guardar en la base de datos. Revisa que la API esté encendida.\n\n" + detalle,
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

        // Ventana para elegir color
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
                boton.ForeColor = COLOR_TEXTO_BOTON;
                boton.UseCompatibleTextRendering = true;
                boton.Font = FuenteTitulo(10);
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
            return Color.FromArgb(70, 60, 90);
        }
    }
}