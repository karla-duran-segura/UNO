using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace uno
{
    /*
      QUÉ ES
      - Pantalla previa a la partida en red. Tiene dos modos según el constructor:
          FormRed(jugadores)  -> ANFITRIÓN: eliges cuántas computadoras van a jugar (2 a 4),
                                 muestra tu IP, abre el puerto y espera a los clientes.
          FormRed()           -> CLIENTE: pide la IP del anfitrión y se conecta.
      - Cuando están todos conectados abre FormJuego (modal) y al terminar se cierra sola.
      - El anfitrión es quien elige los 4 jugadores (FormInicio); los clientes no necesitan
        elegir nada ni tener la API/BD: los nombres les llegan en el primer EstadoRed.

      CUÁNTAS COMPUTADORAS
      - 2 computadoras = 1 cliente  -> el anfitrión controla 2 jugadores y el cliente 2.
      - 4 computadoras = 3 clientes -> cada computadora controla 1 jugador.
      - El reparto de asientos lo hace FormJuego.IniciarPartida (asiento i -> computadora
        i % cantidadDeComputadoras); el primero en conectarse es el cliente 1.
    */
    public class FormRed : Form
    {
        private static readonly Color COLOR_FONDO = ColorTranslator.FromHtml("#7C6884");
        private static readonly Color COLOR_TEXTO = ColorTranslator.FromHtml("#FFF7FC");
        private static readonly Color COLOR_BOTON_INICIO = ColorTranslator.FromHtml("#F7C8DE");
        private static readonly Color COLOR_BOTON_FIN = ColorTranslator.FromHtml("#D8CCFF");
        private static readonly Color COLOR_TEXTO_BOTON = ColorTranslator.FromHtml("#4B3B5B");

        private readonly bool esAnfitrion;
        private readonly List<JugadorBD> jugadores;
        private readonly List<Conexion> conexiones = new List<Conexion>();
        private TcpListener listener;
        private bool cancelado;
        private int clientesEsperados;

        private Label lblTitulo;
        private Label lblEstado;
        private TextBox txtIp;
        private ComboBox cmbEquipos;
        private BotonPastel btnConectar;
        private BotonPastel btnEsperar;
        private BotonPastel btnCancelar;

        // Anfitrión
        public FormRed(List<JugadorBD> jugadores)
        {
            esAnfitrion = true;
            this.jugadores = jugadores;
            Construir();

            lblTitulo.Text = "Crear partida en red";
            lblEstado.Text = "¿Cuántas computadoras van a jugar?\n(contándote a ti)";

            // De 2 computadoras hasta una por jugador
            for (int n = 2; n <= jugadores.Count; n++)
                cmbEquipos.Items.Add(n + " computadoras");
            cmbEquipos.SelectedIndex = cmbEquipos.Items.Count - 1;

            cmbEquipos.Visible = true;
            btnEsperar.Visible = true;
        }

        // Cliente
        public FormRed()
        {
            esAnfitrion = false;
            Construir();

            lblTitulo.Text = "Unirse a una partida";
            lblEstado.Text = "Escribe la IP del anfitrión:";
            txtIp.Visible = true;
            btnConectar.Visible = true;
        }

        private void Construir()
        {
            Text = "UNO - Partida en red";
            ClientSize = new Size(440, 400);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = COLOR_FONDO;
            Font = new Font("Segoe UI", 10);

            lblTitulo = new Label();
            lblTitulo.Location = new Point(20, 16);
            lblTitulo.Size = new Size(400, 36);
            lblTitulo.Font = new Font("Segoe UI Semibold", 16);
            lblTitulo.ForeColor = COLOR_TEXTO;
            Controls.Add(lblTitulo);

            lblEstado = new Label();
            lblEstado.Location = new Point(20, 64);
            lblEstado.Size = new Size(400, 170);
            lblEstado.ForeColor = COLOR_TEXTO;
            Controls.Add(lblEstado);

            txtIp = new TextBox();
            txtIp.Location = new Point(20, 244);
            txtIp.Size = new Size(400, 27);
            txtIp.Visible = false;
            Controls.Add(txtIp);

            cmbEquipos = new ComboBox();
            cmbEquipos.Location = new Point(20, 244);
            cmbEquipos.Size = new Size(400, 27);
            cmbEquipos.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbEquipos.Visible = false;
            Controls.Add(cmbEquipos);

            btnConectar = CrearBoton("Conectar", new Point(20, 284));
            btnConectar.Visible = false;
            btnConectar.Click += ClickConectar;

            btnEsperar = CrearBoton("Esperar jugadores", new Point(20, 284));
            btnEsperar.Visible = false;
            btnEsperar.Click += ClickEsperar;

            btnCancelar = CrearBoton("Cancelar", new Point(20, 336));
            btnCancelar.Click += delegate { Close(); };

            FormClosing += delegate
            {
                cancelado = true;
                DetenerListener();
                CerrarConexiones();   // si la partida ya empezó, FormJuego ya las cerró (no pasa nada)
            };
        }

        private BotonPastel CrearBoton(string texto, Point posicion)
        {
            BotonPastel boton = new BotonPastel();
            boton.Text = texto;
            boton.Font = new Font("Segoe UI Semibold", 10);
            boton.Location = posicion;
            boton.Size = new Size(400, 38);
            boton.ColorInicio = COLOR_BOTON_INICIO;
            boton.ColorFin = COLOR_BOTON_FIN;
            boton.ColorLetra = COLOR_TEXTO_BOTON;
            Controls.Add(boton);
            return boton;
        }

        // ---------- Anfitrión ----------
        private void ClickEsperar(object sender, EventArgs ev)
        {
            clientesEsperados = cmbEquipos.SelectedIndex + 1;   // "2 computadoras" = 1 cliente
            cmbEquipos.Visible = false;
            btnEsperar.Visible = false;

            MostrarEstadoAnfitrion();
            EsperarClientes();
        }

        private void MostrarEstadoAnfitrion()
        {
            lblEstado.Text = "Dile a tus compañeros una de estas IP:\n" + MisIps() +
                             "\n\nPuerto: " + Conexion.PuertoPredeterminado +
                             "\n\nConectados: " + (conexiones.Count + 1) + " de " + (clientesEsperados + 1) +
                             " (contándote a ti)";
        }

        private async void EsperarClientes()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, Conexion.PuertoPredeterminado);
                listener.Start();

                // Cada conexión que llega es el siguiente cliente (1, 2, 3...)
                while (conexiones.Count < clientesEsperados)
                {
                    TcpClient tcp = await listener.AcceptTcpClientAsync();
                    if (cancelado)
                    {
                        tcp.Close();
                        break;
                    }
                    conexiones.Add(new Conexion(tcp));
                    MostrarEstadoAnfitrion();
                }
            }
            catch (Exception ex)
            {
                if (!cancelado)
                    lblEstado.Text = "No se pudo abrir el puerto " + Conexion.PuertoPredeterminado +
                                     ":\n" + ex.Message;
                CerrarConexiones();
                return;
            }
            finally
            {
                DetenerListener();
            }

            if (cancelado)
            {
                CerrarConexiones();
                return;
            }

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
                MessageBox.Show("No se pudo iniciar la partida:\n" + ex.Message, "Partida en red");
            }
            Close();
        }

        private void DetenerListener()
        {
            if (listener == null) return;
            try { listener.Stop(); }
            catch (Exception) { }
        }

        private void CerrarConexiones()
        {
            foreach (Conexion c in conexiones)
                c.Cerrar();
            conexiones.Clear();
        }

        // ---------- Cliente ----------
        private async void ClickConectar(object sender, EventArgs ev)
        {
            string ip = txtIp.Text.Trim();
            if (ip.Length == 0)
            {
                lblEstado.Text = "Escribe la IP del anfitrión:";
                return;
            }

            btnConectar.Enabled = false;
            lblEstado.Text = "Conectando...";
            TcpClient tcp = new TcpClient();

            try
            {
                Task conectar = tcp.ConnectAsync(ip, Conexion.PuertoPredeterminado);
                Task primero = await Task.WhenAny(conectar, Task.Delay(5000));
                if (primero != conectar)
                    throw new TimeoutException("Se agotó el tiempo. Revisa la IP y el firewall del anfitrión.");
                await conectar;   // si falló, aquí sale la excepción real
            }
            catch (Exception ex)
            {
                tcp.Close();
                lblEstado.Text = "No se pudo conectar:\n" + ex.Message;
                btnConectar.Enabled = true;
                return;
            }

            lblEstado.Text = "Conectado. Esperando a que se conecten los demás jugadores...";

            try
            {
                using (FormJuego partida = new FormJuego(new Conexion(tcp)))
                {
                    Hide();
                    partida.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo iniciar la partida:\n" + ex.Message, "Partida en red");
            }
            Close();
        }

        // Muestra las IPv4 de esta computadora para no tener que abrir ipconfig
        private static string MisIps()
        {
            List<string> ips = new List<string>();
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                foreach (UnicastIPAddressInformation info in ni.GetIPProperties().UnicastAddresses)
                {
                    if (info.Address.AddressFamily == AddressFamily.InterNetwork)
                        ips.Add(info.Address + "   (" + ni.Name + ")");
                }
            }
            return ips.Count == 0 ? "(no se encontró ninguna)" : string.Join("\n", ips.ToArray());
        }
    }
}