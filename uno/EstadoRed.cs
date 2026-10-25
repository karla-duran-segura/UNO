using System.Collections.Generic;

namespace uno
{
    /*
      QUÉ ES
      - Datos simples (se convierten a JSON con Newtonsoft) que viajan entre anfitrión y cliente.
      - EstadoRed = "foto" de la partida. El anfitrión la arma desde Juego tras cada acción y
        FormJuego dibuja SIEMPRE desde una foto (también en modo Local y en el anfitrión).
      - AccionRed = lo que el cliente quiere hacer (jugar la carta N, robar, elegir color...).

      PRIVACIDAD
      - JugadorRed.Mano solo viene llena para los jugadores que controla quien recibe la foto.
        De los demás solo se manda NumCartas. Así el cliente no puede ver las cartas del rival.
    */

    public class CartaRed
    {
        public string Color { get; set; }
        public string Tipo { get; set; }
        public int Numero { get; set; }
        public bool Jugable { get; set; }   // lo calcula el anfitrión con juego.PuedeJugar

        public static CartaRed De(Carta carta, bool jugable)
        {
            CartaRed c = new CartaRed();
            c.Color = carta.Color;
            c.Tipo = carta.Tipo;
            c.Numero = carta.Numero;
            c.Jugable = jugable;
            return c;
        }

        // Reconstruye la Carta real (para pedir su imagen). Si tu constructor de Carta es
        // distinto, este es el único lugar que hay que ajustar.
        public Carta ACarta()
        {
            return new Carta(Color, Tipo, Numero);
        }
    }

    public class JugadorRed
    {
        public string Nombre { get; set; }
        public int Puntos { get; set; }
        public int NumCartas { get; set; }
        public List<CartaRed> Mano { get; set; }   // null si esa mano no se puede ver
    }

    public class EstadoRed
    {
        public List<JugadorRed> Jugadores { get; set; }
        public List<int> Tuyos { get; set; }        // índices de jugadores que controla quien recibe esta foto
        public CartaRed CartaSuperior { get; set; }
        public string ColorActivo { get; set; }      // null = falta elegir el color inicial
        public int IndiceActual { get; set; }
        public int Direccion { get; set; }
        public int NumeroRonda { get; set; }
        public bool RondaTerminada { get; set; }
        public bool PartidaTerminada { get; set; }
        public string GanadorPartida { get; set; }
        public bool EsperandoDesafioMas4 { get; set; }
        public bool FaltaUnoPendiente { get; set; }
        public string Error { get; set; }            // jugada rechazada, solo para el cliente

        public EstadoRed()
        {
            Jugadores = new List<JugadorRed>();
            Tuyos = new List<int>();
        }
    }

    // Mensajes cliente -> anfitrión: "Jugar", "Robar", "Pasar", "FaltaUno", "ColorInicial", "Mas4".
    // Solo se llenan los campos que cada acción necesita.
    public class AccionRed
    {
        public int Indice { get; set; }        // Jugar: posición de la carta en SU mano
        public string Color { get; set; }       // Jugar (comodín) y ColorInicial
        public bool DecirUno { get; set; }      // Jugar
        public bool Desafiar { get; set; }      // Mas4
    }
}
