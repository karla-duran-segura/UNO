using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace uno
{
    // Sala de espera: el servidor no juega; los cuatro asientos pertenecen a clientes.
    public class FormRed : Form
    {
        private static readonly Color COLOR_FONDO = ColorTranslator.FromHtml("#7C6884");
        private static readonly Color COLOR_TEXTO = ColorTranslator.FromHtml("#FFF7FC");
        private static readonly Color COLOR_BOTON_INICIO = ColorTranslator.FromHtml("#F7C8DE");
        private static readonly Color COLOR_BOTON_FIN = ColorTranslator.FromHtml("#D8CCFF");
        private static readonly Color COLOR_TEXTO_BOTON = ColorTranslator.FromHtml("#4B3B5B");
        private const int JUGADORES_REQUERIDOS = 4;

        private readonly bool esServidor;
        private readonly JugadorBD jugadorCliente;
        private readonly List<Conexion> conexiones = new List<Conexion>();
        private readonly Dictionary<Conexion, JugadorBD> selecciones = new Dictionary<Conexion, JugadorBD>();
        private readonly Dictionary<Conexion, Action<Mensaje>> manejadoresSala = new Dictionary<Conexion, Action<Mensaje>>();
        private readonly BaseDatos bd = new BaseDatos();
        private TcpListener listener;
        private bool cancelado;
        private bool partidaIniciada;
        private Conexion conexionCliente;
        private Action<Mensaje> manejadorCliente;

        private Label lblTitulo;
        private Label lblEstado;
        private TextBox txtIp;
        private BotonPastel btnConectar;
        private BotonPastel btnEsperar;
        private BotonPastel btnCancelar;

        // Servidor: no recibe jugadores preseleccionados.
        public FormRed(bool servidor)
        {
            esServidor = servidor;
            jugadorCliente = null;
            Construir();
            lblTitulo.Text = "Crear partida en red";
            lblEstado.Text = "El servidor no ocupará un asiento.\nPulsa para esperar a cuatro clientes.";
            btnEsperar.Visible = true;
        }

        // Cliente: primero eligió un jugador guardado en la BD.
        public FormRed(JugadorBD jugador)
        {
            if (jugador == null) throw new ArgumentNullException("jugador");
            esServidor = false;
            jugadorCliente = jugador;
            Construir();
            lblTitulo.Text = "Unirme a una partida";
            lblEstado.Text = "Jugador elegido: " + jugador.nombre + "\nEscribe la IP del servidor:";
            txtIp.Visible = true;
            btnConectar.Visible = true;
        }

        private void Construir()
        {
            Text = "UNO - Partida en red";
            ClientSize = new Size(460, 420);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = COLOR_FONDO;
            Font = new Font("Segoe UI", 10);

            lblTitulo = new Label
            {
                Location = new Point(20, 16),
                Size = new Size(420, 36),
                Font = new Font("Segoe UI Semibold", 16),
                ForeColor = COLOR_TEXTO
            };
            Controls.Add(lblTitulo);

            lblEstado = new Label
            {
                Location = new Point(20, 64),
                Size = new Size(420, 200),
                ForeColor = COLOR_TEXTO
            };
            Controls.Add(lblEstado);

            txtIp = new TextBox { Location = new Point(20, 270), Size = new Size(420, 27), Visible = false };
            Controls.Add(txtIp);

            btnConectar = CrearBoton("Conectar", new Point(20, 310));
            btnConectar.Visible = false;
            btnConectar.Click += ClickConectar;

            btnEsperar = CrearBoton("Iniciar servidor y esperar", new Point(20, 310));
            btnEsperar.Visible = false;
            btnEsperar.Click += ClickEsperar;

            btnCancelar = CrearBoton("Cancelar", new Point(20, 362));
            btnCancelar.Click += delegate { Close(); };

            FormClosing += delegate
            {
                cancelado = true;
                DetenerListener();
                if (conexionCliente != null) conexionCliente.Cerrar();
                foreach (Conexion c in conexiones) c.Cerrar();
            };
        }

        private BotonPastel CrearBoton(string texto, Point posicion)
        {
            BotonPastel boton = new BotonPastel();
            boton.Text = texto;
            boton.Font = new Font("Segoe UI Semibold", 10);
            boton.Location = posicion;
            boton.Size = new Size(420, 38);
            boton.ColorInicio = COLOR_BOTON_INICIO;
            boton.ColorFin = COLOR_BOTON_FIN;
            boton.ColorLetra = COLOR_TEXTO_BOTON;
            Controls.Add(boton);
            return boton;
        }

        // ---------------- SERVIDOR ----------------
        private async void ClickEsperar(object sender, EventArgs e)
        {
            btnEsperar.Enabled = false;
            try
            {
                listener = new TcpListener(IPAddress.Any, Conexion.PuertoPredeterminado);
                listener.Start();
                lblEstado.Text = "Servidor iniciado. Comparte una de estas IP:\n" + MisIps() +
                    "\n\nPuerto: " + Conexion.PuertoPredeterminado +
                    "\n\nEsperando conexiones: 0 de 4";

                while (!cancelado && conexiones.Count < JUGADORES_REQUERIDOS)
                {
                    TcpClient tcp = await listener.AcceptTcpClientAsync();
                    if (cancelado) { tcp.Close(); break; }

                    Conexion c = new Conexion(tcp);
                    conexiones.Add(c);
                    Action<Mensaje> handler = m => ProcesarSeleccion(c, m);
                    manejadoresSala[c] = handler;
                    c.MensajeRecibido += handler;
                    c.Desconectado += () =>
                    {
                        if (!IsDisposed && IsHandleCreated)
                            BeginInvoke((Action)(() =>
                            {
                                if (!partidaIniciada && !cancelado)
                                    lblEstado.Text = "Un cliente se desconectó.\nCancela y vuelve a iniciar la sala si no puede reconectarse.";
                            }));
                    };
                    c.Iniciar();
                    ActualizarEstadoServidor();
                }

                DetenerListener();
                if (cancelado) return;
                lblEstado.Text = "Ya se conectaron cuatro clientes.\nEsperando que cada uno elija un jugador...\n\n" +
                    "Seleccionados: " + selecciones.Count + " de 4";
            }
            catch (Exception ex)
            {
                if (!cancelado)
                    lblEstado.Text = "No se pudo abrir el puerto " + Conexion.PuertoPredeterminado + ":\n" + ex.Message;
                DetenerListener();
            }
        }

        private void ProcesarSeleccion(Conexion conexion, Mensaje mensaje)
        {
            if (cancelado || partidaIniciada || mensaje.Tipo != "SeleccionJugador") return;
            if (selecciones.ContainsKey(conexion)) return;

            JugadorBD solicitado;
            try { solicitado = mensaje.Obtener<JugadorBD>(); }
            catch { conexion.Enviar("ErrorSala", "No se pudo leer el jugador seleccionado."); return; }

            try
            {
                // No confiamos solo en el nombre que envía el cliente: verificamos el ID con la API.
                List<JugadorBD> registrados = bd.ObtenerJugadores();
                JugadorBD confirmado = registrados.FirstOrDefault(j => j.id == solicitado.id);
                if (confirmado == null || !string.Equals(confirmado.nombre, solicitado.nombre, StringComparison.Ordinal))
                {
                    conexion.Enviar("ErrorSala", "Ese jugador ya no existe en la base de datos. Reinicia la conexión y elige otro.");
                    return;
                }
                if (selecciones.Values.Any(j => j.id == confirmado.id))
                {
                    conexion.Enviar("ErrorSala", "Ese jugador ya fue elegido por otra persona. Cancela y vuelve a conectarte con otro jugador.");
                    return;
                }

                selecciones.Add(conexion, confirmado);
                conexion.Enviar("SeleccionAceptada", confirmado.nombre);
                ActualizarEstadoServidor();

                // Diferido: no se debe abrir la partida dentro del manejador, porque eso
                // congela el ciclo de lectura de esta conexión.
                if (selecciones.Count == JUGADORES_REQUERIDOS)
                    BeginInvoke((Action)ConfirmarEIniciarPartida);
            }
            catch (Exception ex)
            {
                conexion.Enviar("ErrorSala", "El servidor no pudo validar al jugador con la API: " + ex.Message);
            }
        }

        private void ActualizarEstadoServidor()
        {
            if (IsDisposed) return;
            string texto = "Comparte una de estas IP:\n" + MisIps() +
                "\n\nPuerto: " + Conexion.PuertoPredeterminado +
                "\n\nConectados: " + conexiones.Count + " de 4" +
                "\nJugadores seleccionados: " + selecciones.Count + " de 4";
            foreach (var par in selecciones)
                texto += "\n• " + par.Value.nombre;
            lblEstado.Text = texto;
        }

        private void ConfirmarEIniciarPartida()
        {
            if (partidaIniciada || selecciones.Count != JUGADORES_REQUERIDOS) return;

            List<JugadorBD> jugadores = conexiones.Select(c => selecciones.ContainsKey(c) ? selecciones[c] : null).ToList();
            if (jugadores.Any(j => j == null) || jugadores.Select(j => j.id).Distinct().Count() != 4)
            {
                lblEstado.Text = "La selección no es válida. Cancela y vuelve a crear la sala.";
                return;
            }

            string resumen = "Los cuatro jugadores seleccionados son:\n\n" +
                string.Join("\n", jugadores.Select((j, i) => (i + 1) + ". " + j.nombre).ToArray()) +
                "\n\n¿Confirmas que inicie la partida?";
            DialogResult confirmar = MessageBox.Show(this, resumen, "Confirmar jugadores",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirmar != DialogResult.Yes)
            {
                MessageBox.Show(this, "La sala se cancelará. Vuelve a crearla para seleccionar otros jugadores.",
                    "Sala cancelada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
                return;
            }

            partidaIniciada = true;
            // Retiramos los manejadores de la sala antes de transferir las conexiones a FormJuego.
            foreach (var par in manejadoresSala)
                par.Key.MensajeRecibido -= par.Value;
            manejadoresSala.Clear();

            foreach (Conexion c in conexiones)
                c.Enviar("IniciarPartida");

            try
            {
                using (FormJuego partida = new FormJuego(jugadores, conexiones))
                {
                    Hide();
                    partida.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "No se pudo iniciar la partida:\n" + ex.Message, "Partida en red");
            }
            Close();
        }

        // ---------------- CLIENTE ----------------
        private async void ClickConectar(object sender, EventArgs e)
        {
            string ip = txtIp.Text.Trim();
            if (ip.Length == 0)
            {
                lblEstado.Text = "Escribe la IP del servidor.";
                return;
            }

            btnConectar.Enabled = false;
            txtIp.Enabled = false;
            lblEstado.Text = "Conectando con el servidor...";
            TcpClient tcp = new TcpClient();
            try
            {
                Task conectar = tcp.ConnectAsync(ip, Conexion.PuertoPredeterminado);
                Task primero = await Task.WhenAny(conectar, Task.Delay(5000));
                if (primero != conectar)
                    throw new TimeoutException("Se agotó el tiempo. Revisa la IP y el firewall del servidor.");
                await conectar;
            }
            catch (Exception ex)
            {
                tcp.Close();
                lblEstado.Text = "No se pudo conectar:\n" + ex.Message;
                btnConectar.Enabled = true;
                txtIp.Enabled = true;
                return;
            }

            conexionCliente = new Conexion(tcp);
            manejadorCliente = ProcesarMensajeCliente;
            conexionCliente.MensajeRecibido += manejadorCliente;
            conexionCliente.Desconectado += ClienteDesconectado;
            conexionCliente.Iniciar();
            conexionCliente.Enviar("SeleccionJugador", jugadorCliente);
            lblEstado.Text = "Conectado como " + jugadorCliente.nombre +
                ". Esperando la validación y a los otros jugadores...";
        }

        private void ProcesarMensajeCliente(Mensaje mensaje)
        {
            if (cancelado || partidaIniciada) return;
            if (mensaje.Tipo == "SeleccionAceptada")
            {
                BeginInvoke((Action)(() => lblEstado.Text = "Jugador confirmado: " + mensaje.Obtener<string>() +
                    "\nEsperando a que el servidor confirme a los cuatro jugadores..."));
            }
            else if (mensaje.Tipo == "ErrorSala")
            {
                string detalle = mensaje.Obtener<string>();
                BeginInvoke((Action)(() =>
                {
                    lblEstado.Text = detalle;
                    MessageBox.Show(this, detalle, "No se pudo elegir el jugador", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }));
            }
            else if (mensaje.Tipo == "IniciarPartida")
            {
                partidaIniciada = true;
                conexionCliente.MensajeRecibido -= manejadorCliente;
                // Siempre diferido (BeginInvoke): si se abre FormJuego aquí mismo, ShowDialog bloquea
                // el ciclo de lectura de Conexion y el cliente nunca recibe el primer Estado.
                // Los mensajes que lleguen mientras tanto los guarda Conexion hasta que
                // FormJuego se suscriba.
                BeginInvoke((Action)AbrirPartidaCliente);
            }
        }

        private void AbrirPartidaCliente()
        {
            try
            {
                using (FormJuego partida = new FormJuego(conexionCliente))
                {
                    Hide();
                    partida.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "No se pudo iniciar la partida:\n" + ex.Message, "Partida en red");
            }
            Close();
        }

        private void ClienteDesconectado()
        {
            if (cancelado || partidaIniciada || IsDisposed || !IsHandleCreated) return;
            BeginInvoke((Action)(() =>
            {
                if (!partidaIniciada)
                    lblEstado.Text = "Se perdió la conexión con el servidor. Cierra esta ventana e inténtalo de nuevo.";
            }));
        }

        private void DetenerListener()
        {
            if (listener == null) return;
            try { listener.Stop(); } catch { }
            listener = null;
        }

        private static string MisIps()
        {
            List<string> ips = new List<string>();
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;
                foreach (UnicastIPAddressInformation info in ni.GetIPProperties().UnicastAddresses)
                    if (info.Address.AddressFamily == AddressFamily.InterNetwork)
                        ips.Add(info.Address + "   (" + ni.Name + ")");
            }
            return ips.Count == 0 ? "(no se encontró ninguna)" : string.Join("\n", ips.ToArray());
        }
    }
}