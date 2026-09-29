using System;
using System.Collections.Generic;
using System.Linq;

namespace uno
{
    public class JugadorUno
    {
        public string Nombre { get; private set; }
        public List<Carta> Mano { get; private set; }
        public int Puntos { get; internal set; }

        public JugadorUno(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre no puede estar vacío.");
            Nombre = nombre;
            Mano = new List<Carta>();
        }
    }

    public class Juego
    {
        private readonly Random azar = new Random();
        private readonly List<Carta> mazo = new List<Carta>();
        private readonly List<Carta> descarte = new List<Carta>();
        private readonly List<string> movimientos = new List<string>();
        private int repartidor;
        private int indiceActual;
        private int direccion = 1;
        private Carta cartaRobadaEnTurno;
        private bool roboEnTurno;
        private int jugadorSinUno = -1;
        private int jugadorMas4 = -1;
        private int jugadorDesafiado = -1;
        private bool mas4Ilegal;
        private bool rondaPendienteMas4;
        private int ronda;

        public List<JugadorUno> Jugadores { get; private set; }
        public IReadOnlyList<string> Movimientos { get { return movimientos.AsReadOnly(); } }
        public Carta CartaSuperior { get { return descarte[descarte.Count - 1]; } }
        public string ColorActivo { get; private set; }
        public JugadorUno JugadorActual { get { return Jugadores[indiceActual]; } }
        public int Direccion { get { return direccion; } }
        public int NumeroRonda { get { return ronda; } }
        public bool RondaTerminada { get; private set; }
        public bool PartidaTerminada { get; private set; }
        public JugadorUno GanadorPartida { get; private set; }
        public bool EsperandoDesafioMas4 { get { return jugadorMas4 >= 0; } }
        public bool FaltaUnoPendiente { get { return jugadorSinUno >= 0; } }

        public Juego(IEnumerable<string> nombres)
        {
            if (nombres == null) throw new ArgumentNullException("nombres");
            var lista = nombres.ToList();
            if (lista.Count != 4 || lista.Any(string.IsNullOrWhiteSpace) ||
                lista.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 4)
                throw new ArgumentException("Se requieren cuatro nombres distintos.");
            Jugadores = lista.Select(n => new JugadorUno(n.Trim())).ToList();
            repartidor = azar.Next(4);
            IniciarRonda();
        }

        public void IniciarRonda()
        {
            if (PartidaTerminada) throw new InvalidOperationException("La partida terminó.");
            if (ronda > 0 && !RondaTerminada)
                throw new InvalidOperationException("La ronda actual no ha terminado.");
            if (ronda > 0) repartidor = IndiceDesde(repartidor, 1);
            ronda++;
            RondaTerminada = false;
            rondaPendienteMas4 = false;
            jugadorSinUno = -1;
            jugadorMas4 = -1;
            jugadorDesafiado = -1;
            roboEnTurno = false;
            cartaRobadaEnTurno = null;
            direccion = 1;
            mazo.Clear();
            descarte.Clear();
            foreach (var jugador in Jugadores) jugador.Mano.Clear();
            CrearMazo();
            Mezclar(mazo);
            for (int n = 0; n < 7; n++)
                foreach (var jugador in Jugadores)
                    jugador.Mano.Add(SacarDelMazo());

            // La carta inicial +4 se devuelve al mazo y se elige otra.
            Carta inicial;
            do
            {
                inicial = SacarDelMazo();
                if (inicial.Tipo == "mas4")
                {
                    mazo.Add(inicial);
                    Mezclar(mazo);
                }
            } while (inicial.Tipo == "mas4");

            descarte.Add(inicial);
            indiceActual = IndiceDesde(repartidor, 1);
            ColorActivo = inicial.Color;
            if (inicial.Tipo == "comodin")
            {
                ColorActivo = null; // El primer jugador debe elegir color.
            }
            else if (inicial.Tipo == "reversa")
            {
                direccion = -1;
                indiceActual = repartidor;
            }
            else if (inicial.Tipo == "salta")
                indiceActual = IndiceDesde(indiceActual, 1);
            else if (inicial.Tipo == "mas2")
            {
                RobarVarias(Jugadores[indiceActual], 2);
                indiceActual = IndiceDesde(indiceActual, 1);
            }
            Registrar("Comienza ronda " + ronda + "; carta inicial: " + inicial.Texto());
        }

        public void ElegirColorInicial(string color)
        {
            ComprobarTurno();
            if (ColorActivo != null || CartaSuperior.Tipo != "comodin")
                throw new InvalidOperationException("No hay color inicial pendiente.");
            ValidarColor(color);
            ColorActivo = color;
            Registrar(JugadorActual.Nombre + " elige " + color + " para la carta inicial.");
        }

        public bool PuedeJugar(Carta carta)
        {
            if (carta == null || ColorActivo == null) return false;
            if (carta.EsComodin()) return true;
            if (carta.Color == ColorActivo) return true;
            return carta.Tipo == CartaSuperior.Tipo &&
                   (carta.Tipo != "numero" || carta.Numero == CartaSuperior.Numero);
        }

        // El +4 se acepta provisionalmente: la legalidad se resuelve si el rival decide desafiar.
        public void JugarCarta(Carta carta, string colorElegido = null, bool decirUno = false)
        {
            ComprobarTurno();
            if (ColorActivo == null) throw new InvalidOperationException("Primero elige el color inicial.");
            if (!JugadorActual.Mano.Contains(carta) || !PuedeJugar(carta))
                throw new InvalidOperationException("Esa carta no se puede jugar.");
            if (roboEnTurno && !Object.ReferenceEquals(carta, cartaRobadaEnTurno))
                throw new InvalidOperationException("Después de robar solo se puede jugar la carta robada.");
            if (carta.EsComodin()) ValidarColor(colorElegido);

            CerrarVentanaUno();
            int autor = indiceActual;
            string colorPrevio = ColorActivo;
            bool teniaColor = carta.Tipo == "mas4" &&
                Jugadores[autor].Mano.Any(c => !Object.ReferenceEquals(c, carta) && c.Color == colorPrevio);
            Jugadores[autor].Mano.Remove(carta);
            descarte.Add(carta);
            ColorActivo = carta.EsComodin() ? colorElegido : carta.Color;
            Registrar(Jugadores[autor].Nombre + " juega " + carta.Texto() +
                     (carta.EsComodin() ? " y elige " + ColorActivo : ""));
            roboEnTurno = false;
            cartaRobadaEnTurno = null;

            if (Jugadores[autor].Mano.Count == 1)
            {
                if (decirUno) Registrar(Jugadores[autor].Nombre + " dice UNO.");
                else jugadorSinUno = autor;
            }

            if (carta.Tipo == "mas4")
            {
                jugadorDesafiado = autor;
                jugadorMas4 = IndiceDesde(autor, direccion);
                mas4Ilegal = teniaColor;
                rondaPendienteMas4 = Jugadores[autor].Mano.Count == 0;
                indiceActual = jugadorMas4;
                Registrar("El jugador afectado debe aceptar o desafiar el +4.");
                return;
            }

            if (carta.Tipo == "reversa") direccion = -direccion;
            int afectado = IndiceDesde(autor, direccion);
            if (carta.Tipo == "mas2")
            {
                RobarVarias(Jugadores[afectado], 2);
                Registrar(Jugadores[afectado].Nombre + " roba 2 y pierde el turno.");
            }

            if (Jugadores[autor].Mano.Count == 0)
            {
                TerminarRonda(autor);
                return;
            }

            indiceActual = (carta.Tipo == "salta" || carta.Tipo == "mas2")
                ? IndiceDesde(afectado, direccion) : afectado;
        }

        public Carta RobarCarta()
        {
            ComprobarTurno();
            if (ColorActivo == null) throw new InvalidOperationException("Primero elige el color inicial.");
            if (roboEnTurno) throw new InvalidOperationException("Ya robaste una carta este turno.");
            CerrarVentanaUno();
            cartaRobadaEnTurno = RobarUna(JugadorActual);
            roboEnTurno = true;
            Registrar(JugadorActual.Nombre + " roba una carta.");
            // Si ya no hay cartas disponibles, se permite pasar.
            return cartaRobadaEnTurno;
        }

        public void PasarTrasRobar()
        {
            ComprobarTurno();
            if (!roboEnTurno) throw new InvalidOperationException("Primero debes robar una carta.");
            roboEnTurno = false;
            cartaRobadaEnTurno = null;
            Registrar(JugadorActual.Nombre + " termina su turno.");
            indiceActual = IndiceDesde(indiceActual, direccion);
        }

        public void SenalarFaltaUno()
        {
            if (jugadorSinUno < 0) throw new InvalidOperationException("No hay una falta de UNO pendiente.");
            var infractor = Jugadores[jugadorSinUno];
            RobarVarias(infractor, 2);
            Registrar(infractor.Nombre + " no dijo UNO y roba 2.");
            jugadorSinUno = -1;
        }

        // false: acepta el +4; true: desafía al jugador que lo lanzó.
        public void ResolverMas4(bool desafiar)
        {
            if (jugadorMas4 < 0) throw new InvalidOperationException("No hay +4 pendiente.");
            CerrarVentanaUno();
            int afectado = jugadorMas4;
            int autor = jugadorDesafiado;
            if (desafiar && mas4Ilegal)
            {
                RobarVarias(Jugadores[autor], 4);
                Registrar("Desafío exitoso: " + Jugadores[autor].Nombre + " roba 4.");
                // El jugador afectado conserva su turno.
                indiceActual = afectado;
                rondaPendienteMas4 = false;
            }
            else
            {
                int cantidad = desafiar ? 6 : 4;
                RobarVarias(Jugadores[afectado], cantidad);
                Registrar(Jugadores[afectado].Nombre + " roba " + cantidad +
                         " por el +4" + (desafiar ? " (desafío fallido)." : "."));
                indiceActual = IndiceDesde(afectado, direccion);
            }
            jugadorMas4 = -1;
            jugadorDesafiado = -1;
            if (rondaPendienteMas4) TerminarRonda(autor);
        }

        private void ComprobarTurno()
        {
            if (PartidaTerminada || RondaTerminada)
                throw new InvalidOperationException("No hay una ronda en curso.");
            if (jugadorMas4 >= 0)
                throw new InvalidOperationException("Primero resuelve el +4 pendiente.");
        }

        private void CerrarVentanaUno() { jugadorSinUno = -1; }

        private int IndiceDesde(int origen, int paso)
        {
            return (origen + paso + Jugadores.Count) % Jugadores.Count;
        }

        private static void ValidarColor(string color)
        {
            if (color != "Rosa" && color != "Morado" && color != "Azul" && color != "Amarillo")
                throw new ArgumentException("Elige Rosa, Morado, Azul o Amarillo.");
        }

        private void CrearMazo()
        {
            string[] colores = { "Rosa", "Morado", "Azul", "Amarillo" };
            foreach (string color in colores)
            {
                mazo.Add(new Carta(color, "numero", 0));
                for (int n = 1; n <= 9; n++)
                    for (int copia = 0; copia < 2; copia++)
                        mazo.Add(new Carta(color, "numero", n));
                for (int copia = 0; copia < 2; copia++)
                {
                    mazo.Add(new Carta(color, "salta"));
                    mazo.Add(new Carta(color, "reversa"));
                    mazo.Add(new Carta(color, "mas2"));
                }
            }
            for (int n = 0; n < 4; n++)
            {
                mazo.Add(new Carta("Negro", "comodin"));
                mazo.Add(new Carta("Negro", "mas4"));
            }
        }

        private void Mezclar(List<Carta> cartas)
        {
            for (int i = cartas.Count - 1; i > 0; i--)
            {
                int j = azar.Next(i + 1);
                Carta temporal = cartas[i];
                cartas[i] = cartas[j];
                cartas[j] = temporal;
            }
        }

        private Carta SacarDelMazo()
        {
            if (mazo.Count == 0 && descarte.Count > 1)
            {
                Carta superior = CartaSuperior;
                mazo.AddRange(descarte.Take(descarte.Count - 1));
                descarte.Clear();
                descarte.Add(superior);
                Mezclar(mazo);
                Registrar("Se mezclan los descartes para formar un nuevo mazo.");
            }
            if (mazo.Count == 0) return null;
            int ultimo = mazo.Count - 1;
            Carta carta = mazo[ultimo];
            mazo.RemoveAt(ultimo);
            return carta;
        }

        private Carta RobarUna(JugadorUno jugador)
        {
            Carta carta = SacarDelMazo();
            if (carta != null) jugador.Mano.Add(carta);
            return carta;
        }

        private void RobarVarias(JugadorUno jugador, int cantidad)
        {
            for (int i = 0; i < cantidad; i++)
                if (RobarUna(jugador) == null) break;
        }

        private void TerminarRonda(int ganador)
        {
            RondaTerminada = true;
            int puntos = Jugadores.Where((j, i) => i != ganador)
                .SelectMany(j => j.Mano).Sum(ValorCarta);
            Jugadores[ganador].Puntos += puntos;
            Registrar(Jugadores[ganador].Nombre + " gana la ronda y obtiene " + puntos + " puntos.");
            if (Jugadores[ganador].Puntos >= 500)
            {
                PartidaTerminada = true;
                GanadorPartida = Jugadores[ganador];
                Registrar(GanadorPartida.Nombre + " gana la partida.");
            }
        }

        private static int ValorCarta(Carta carta)
        {
            if (carta.Tipo == "numero") return carta.Numero;
            if (carta.Tipo == "comodin" || carta.Tipo == "mas4") return 50;
            return 20;
        }

        private void Registrar(string texto)
        {
            movimientos.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | " + texto);
        }
    }
}
