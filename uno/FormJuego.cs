using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace uno
{
    // ==========================================================
    //  PANTALLA DE JUEGO
    //  - Muestra la mesa y la mano del jugador en turno.
    //  - Guarda cada movimiento en la base de datos (log).
    // ==========================================================
    public class FormJuego : Form
    {
        private const int ANCHO = 80;
        private const int ALTO = 120;
        private const string CARPETA_IMAGENES = "Imagenes_pastel";

        private Juego juego;
        private BaseDatos bd = new BaseDatos();
        private List<int> idsJugadores = new List<int>();   // id en la BD de cada jugador, en el mismo orden
        private int idPartida = -1;
        private bool resultadoGuardado = false;
        private bool avisoBD = false;
        private string mensajeError = null;
        private JugadorUno jugadorAnterior = null;

        private Dictionary<string, Image> imagenes = new Dictionary<string, Image>();

        private Label lblInfo;
        private FlowLayoutPanel panelRivales;
        private PictureBox picMazo;
        private PictureBox picDescarte;
        private Label lblColor;
        private CheckBox chkUno;
        private Button btnPasar;
        private Button btnFaltaUno;
        private Button btnSiguienteRonda;
        private Label lblMensaje;
        private Label lblMano;
        private FlowLayoutPanel panelMano;
        private ListBox lstLog;

        public FormJuego(List<JugadorBD> jugadores)
        {
            Text = "UNO - Partida";
            ClientSize = new Size(1200, 760);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.DarkGreen;

            CrearControles();
            IniciarPartida(jugadores);
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
            lblInfo = CrearEtiqueta(new Point(20, 15), new Size(860, 30), 14);

            panelRivales = new FlowLayoutPanel();
            panelRivales.Location = new Point(20, 50);
            panelRivales.Size = new Size(860, 70);
            Controls.Add(panelRivales);

            picMazo = new PictureBox();
            picMazo.Location = new Point(250, 140);
            picMazo.Size = new Size(110, 165);
            picMazo.SizeMode = PictureBoxSizeMode.StretchImage;
            picMazo.Cursor = Cursors.Hand;
            picMazo.Click += ClickMazo;
            Controls.Add(picMazo);

            picDescarte = new PictureBox();
            picDescarte.Location = new Point(380, 140);
            picDescarte.Size = new Size(110, 165);
            picDescarte.SizeMode = PictureBoxSizeMode.StretchImage;
            Controls.Add(picDescarte);

            lblColor = CrearEtiqueta(new Point(520, 140), new Size(200, 35), 11);

            chkUno = new CheckBox();
            chkUno.Text = "¡UNO!";
            chkUno.Appearance = Appearance.Button;          // se ve como botón que se queda presionado
            chkUno.TextAlign = ContentAlignment.MiddleCenter;
            chkUno.Location = new Point(520, 185);
            chkUno.Size = new Size(200, 35);
            chkUno.Font = new Font("Arial", 11, FontStyle.Bold);
            chkUno.BackColor = Color.White;
            chkUno.CheckedChanged += (s, e) => chkUno.BackColor = chkUno.Checked ? Color.Gold : Color.White;
            Controls.Add(chkUno);

            btnPasar = CrearBoton("Pasar (después de robar)", new Point(520, 228));
            btnPasar.Click += ClickPasar;

            btnFaltaUno = CrearBoton("¡Señalar falta de UNO!", new Point(520, 268));
            btnFaltaUno.Click += ClickFaltaUno;

            btnSiguienteRonda = CrearBoton("Siguiente ronda", new Point(740, 185));
            btnSiguienteRonda.Click += ClickSiguienteRonda;

            lblMensaje = CrearEtiqueta(new Point(20, 320), new Size(860, 40), 12);

            lblMano = CrearEtiqueta(new Point(20, 365), new Size(860, 25), 12);

            panelMano = new FlowLayoutPanel();
            panelMano.Location = new Point(20, 395);
            panelMano.Size = new Size(860, 350);
            panelMano.AutoScroll = true;
            Controls.Add(panelMano);

            Label lblLog = CrearEtiqueta(new Point(900, 15), new Size(280, 30), 12);
            lblLog.Text = "Movimientos";

            lstLog = new ListBox();
            lstLog.Location = new Point(900, 50);
            lstLog.Size = new Size(280, 695);
            lstLog.HorizontalScrollbar = true;
            Controls.Add(lstLog);
        }

        private Label CrearEtiqueta(Point posicion, Size tamano, int tamanoLetra)
        {
            Label etiqueta = new Label();
            etiqueta.Location = posicion;
            etiqueta.Size = tamano;
            etiqueta.Font = new Font("Arial", tamanoLetra, FontStyle.Bold);
            etiqueta.ForeColor = Color.White;
            etiqueta.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(etiqueta);
            return etiqueta;
        }

        private Button CrearBoton(string texto, Point posicion)
        {
            Button boton = new Button();
            boton.Text = texto;
            boton.Location = posicion;
            boton.Size = new Size(200, 35);
            boton.BackColor = Color.White;
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
                lblInfo.Text = "Ronda " + juego.NumeroRonda + " - Turno de " + actual.Nombre;

            // Rivales: solo cuántas cartas tienen
            panelRivales.Controls.Clear();
            for (int i = 0; i < juego.Jugadores.Count; i++)
            {
                JugadorUno jugador = juego.Jugadores[i];
                Label etiqueta = new Label();
                etiqueta.AutoSize = true;
                etiqueta.Margin = new Padding(0, 0, 25, 0);
                etiqueta.Font = new Font("Arial", 11, FontStyle.Bold);
                etiqueta.Text = jugador.Nombre + ": " + jugador.Mano.Count + " cartas (" + jugador.Puntos + " pts)";
                etiqueta.ForeColor = Object.ReferenceEquals(jugador, actual) ? Color.Yellow : Color.White;
                panelRivales.Controls.Add(etiqueta);
            }

            // Mano del jugador en turno
            for (int i = panelMano.Controls.Count - 1; i >= 0; i--)
                panelMano.Controls[i].Dispose();
            panelMano.Controls.Clear();

            if (!juego.RondaTerminada)
            {
                lblMano.Text = "Mano de " + actual.Nombre + " (las cartas en amarillo se pueden jugar)";

                foreach (Carta carta in actual.Mano)
                {
                    PictureBox pb = new PictureBox();
                    pb.Size = new Size(ANCHO, ALTO);
                    pb.SizeMode = PictureBoxSizeMode.StretchImage;
                    pb.Margin = new Padding(4);
                    pb.Cursor = Cursors.Hand;
                    pb.Image = ObtenerImagen(carta);

                    if (juego.PuedeJugar(carta))
                    {
                        pb.Padding = new Padding(3);
                        pb.BackColor = Color.Yellow;
                    }

                    Carta cartaClic = carta;
                    pb.Click += (s, e) => ClickCarta(cartaClic);
                    panelMano.Controls.Add(pb);
                }
            }
            else
            {
                lblMano.Text = "";
            }

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
                mensajeError = null;
            }
            else if (juego.RondaTerminada && !juego.PartidaTerminada)
                lblMensaje.Text = "Da clic en \"Siguiente ronda\" para continuar.";
            else
                lblMensaje.Text = "";

            lstLog.Items.Clear();
            foreach (string movimiento in juego.Movimientos)
                lstLog.Items.Add(movimiento);
            if (lstLog.Items.Count > 0)
                lstLog.TopIndex = lstLog.Items.Count - 1;

            chkUno.Enabled = !juego.RondaTerminada;
            btnPasar.Enabled = !juego.RondaTerminada;
            btnFaltaUno.Enabled = juego.FaltaUnoPendiente;
            btnSiguienteRonda.Enabled = juego.RondaTerminada && !juego.PartidaTerminada;
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

            bool decirUno = chkUno.Checked;
            if (Intentar(() => juego.JugarCarta(carta, colorElegido, decirUno)))
            {
                Registrar(indice, "jugar", carta.Texto(), colorElegido);
                if (decirUno)
                    Registrar(indice, "uno");
                chkUno.Checked = false;
            }

            DespuesDeAccion();
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
            string elegido = "Rojo";

            Form ventana = new Form();
            ventana.Text = titulo;
            ventana.ClientSize = new Size(360, 90);
            ventana.FormBorderStyle = FormBorderStyle.FixedDialog;
            ventana.StartPosition = FormStartPosition.CenterParent;
            ventana.ControlBox = false;

            string[] colores = { "Azul", "Verde", "Amarillo", "Rojo" };
            for (int i = 0; i < colores.Length; i++)
            {
                string colorBoton = colores[i];
                Button boton = new Button();
                boton.Text = colorBoton;
                boton.Location = new Point(10 + i * 87, 20);
                boton.Size = new Size(80, 50);
                boton.BackColor = ColorDibujo(colorBoton);
                boton.ForeColor = Color.White;
                boton.Font = new Font("Arial", 9, FontStyle.Bold);
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
            if (color == "Azul") return Color.RoyalBlue;
            if (color == "Verde") return Color.ForestGreen;
            if (color == "Amarillo") return Color.Goldenrod;
            if (color == "Rojo") return Color.Firebrick;
            return Color.Black;
        }

        // ---------- Imágenes ----------
        // Cuando terminen ImagenesCartas.cs, se pueden cambiar estos dos métodos por:
        //   return ImagenesCartas.Obtener(carta);   y   return ImagenesCartas.Reverso();
        private Image ObtenerImagen(Carta carta)
        {
            return Cargar(carta.NombreImagen(), carta.Texto(), ColorDibujo(carta.Color));
        }

        private Image ObtenerReverso()
        {
            return Cargar("reverso.png", "UNO", Color.Black);
        }

        private Image Cargar(string nombreArchivo, string textoProvisional, Color fondo)
        {
            if (imagenes.ContainsKey(nombreArchivo))
                return imagenes[nombreArchivo];

            Image imagen;
            string ruta = Path.Combine(Application.StartupPath, CARPETA_IMAGENES, nombreArchivo);

            if (File.Exists(ruta))
                imagen = Image.FromFile(ruta);
            else
                imagen = CrearProvisional(textoProvisional, fondo);

            imagenes[nombreArchivo] = imagen;
            return imagen;
        }

        private Image CrearProvisional(string texto, Color fondo)
        {
            Bitmap imagen = new Bitmap(ANCHO, ALTO);

            using (Graphics g = Graphics.FromImage(imagen))
            {
                g.Clear(fondo);
                g.DrawRectangle(Pens.White, 4, 4, ANCHO - 9, ALTO - 9);

                using (Font fuente = new Font("Arial", 11, FontStyle.Bold))
                {
                    StringFormat formato = new StringFormat();
                    formato.Alignment = StringAlignment.Center;
                    formato.LineAlignment = StringAlignment.Center;
                    g.DrawString(texto, fuente, Brushes.White, new RectangleF(0, 0, ANCHO, ALTO), formato);
                }
            }

            return imagen;
        }
    }
}