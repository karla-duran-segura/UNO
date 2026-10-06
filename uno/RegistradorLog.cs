using System;
using System.Collections.Generic;
using System.Linq;

namespace uno
{
	// Escucha los eventos de Juego y los guarda en la base de datos (vía API).
	// Juego no sabe nada de la BD; la pantalla solo crea un RegistradorLog.
	public class RegistradorLog
	{
		private readonly BaseDatos bd;
		private readonly Juego juego;
		private readonly Dictionary<string, int> idPorNombre =
			new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		private int idPartida;

		public bool Activo { get; private set; }
		public string UltimoError { get; private set; }
		public int IdPartida { get { return idPartida; } }

		public RegistradorLog(BaseDatos bd, Juego juego)
		{
			this.bd = bd;
			this.juego = juego;
		}

		// Crea la partida en la BD. Devuelve false si algo falla (el juego puede seguir sin log).
		public bool Iniciar()
		{
			try
			{
				var enBD = bd.ObtenerJugadores();
				var ids = new List<int>();
				foreach (var j in juego.Jugadores)
				{
					var fila = enBD.FirstOrDefault(x =>
						string.Equals(x.nombre, j.Nombre, StringComparison.OrdinalIgnoreCase));
					if (fila == null)
						throw new Exception("El jugador '" + j.Nombre + "' no existe en la base de datos.");
					idPorNombre[j.Nombre] = fila.id;
					ids.Add(fila.id);   // mismo orden que juego.Jugadores
				}

				idPartida = bd.CrearPartida(ids);
				juego.MovimientoRealizado += AlMover;
				juego.PartidaFinalizada += AlTerminarPartida;
				Activo = true;
				return true;
			}
			catch (Exception ex)
			{
				UltimoError = ex.Message;
				Activo = false;
				return false;
			}
		}

		private void AlMover(MovimientoJuego mov)
		{
			try
			{
				bd.RegistrarMovimiento(
					idPartida,
					idPorNombre[mov.Jugador.Nombre],
					mov.Tipo,
					mov.Carta == null ? null : mov.Carta.Texto(),
					mov.ColorElegido);
			}
			catch (Exception ex)
			{
				UltimoError = ex.Message; // un fallo del log no debe tumbar la partida
			}
		}

		private void AlTerminarPartida()
		{
			try
			{
				var cartas = new Dictionary<int, int>();
				foreach (var j in juego.Jugadores)
					cartas[idPorNombre[j.Nombre]] = j.Mano.Count;

				bd.RegistrarResultado(idPartida, idPorNombre[juego.GanadorPartida.Nombre], cartas);
			}
			catch (Exception ex)
			{
				UltimoError = ex.Message;
			}
		}
	}
}