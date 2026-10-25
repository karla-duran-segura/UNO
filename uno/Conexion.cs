using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace uno
{
    // Un mensaje de red: un Tipo ("Jugar", "Estado"...) y sus Datos en JSON.
    public class Mensaje
    {
        public string Tipo { get; set; }
        public string Datos { get; set; }

        public T Obtener<T>()
        {
            return JsonConvert.DeserializeObject<T>(Datos);
        }
    }

    public class Conexion
    {
        public const int PuertoPredeterminado = 11000;

        private readonly TcpClient tcp;
        private readonly StreamReader lector;
        private readonly StreamWriter escritor;
        private bool cerrada;

        public event Action<Mensaje> MensajeRecibido;
        public event Action Desconectado;

        public Conexion(TcpClient tcp)
        {
            this.tcp = tcp;
            tcp.NoDelay = true;
            NetworkStream flujo = tcp.GetStream();
            lector = new StreamReader(flujo, new UTF8Encoding(false));
            escritor = new StreamWriter(flujo, new UTF8Encoding(false));
            escritor.AutoFlush = true;
        }

        public void Enviar(string tipo, object datos = null)
        {
            if (cerrada) return;

            Mensaje m = new Mensaje();
            m.Tipo = tipo;
            m.Datos = datos == null ? null : JsonConvert.SerializeObject(datos);

            try
            {
                lock (escritor)
                {
                    escritor.WriteLine(JsonConvert.SerializeObject(m));
                }
            }
            catch (IOException) { }                // si se cayó la red, Iniciar() lo detecta y avisa
            catch (ObjectDisposedException) { }
        }

        // Empieza a escuchar. Termina (y dispara Desconectado) cuando se cierra la conexión.
        public async void Iniciar()
        {
            try
            {
                string linea;
                while (!cerrada && (linea = await lector.ReadLineAsync()) != null)
                {
                    Mensaje m = null;
                    try { m = JsonConvert.DeserializeObject<Mensaje>(linea); }
                    catch (JsonException) { }
                    if (m == null) continue;

                    Action<Mensaje> manejador = MensajeRecibido;
                    if (manejador != null)
                    {
                        try { manejador(m); }
                        catch (Exception ex)
                        {
                            // Un error al procesar un mensaje no debe tumbar la conexión.
                            System.Diagnostics.Debug.WriteLine("Error al procesar '" + m.Tipo + "': " + ex);
                        }
                    }
                }
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }

            Action fin = Desconectado;
            if (fin != null) fin();
        }

        public void Cerrar()
        {
            cerrada = true;
            try { tcp.Close(); }
            catch (Exception) { }
        }
    }
}
