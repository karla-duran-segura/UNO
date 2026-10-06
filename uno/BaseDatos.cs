using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace uno
{
	// Clases propias de la base de datos
	public class JugadorBD
	{
		public int id { get; set; }
		public string nombre { get; set; }
	}

	public class HistorialPartida
	{
		public int id_partida { get; set; }
		public DateTime fecha_ini { get; set; }
		public DateTime? fecha_fin { get; set; }
		public string nombre_ganador { get; set; }
		public int total_movs { get; set; }
	}

	public class EstadisticaJugador
	{
		public string nombre { get; set; }
		public int partidas_jugadas { get; set; }
		public int partidas_ganadas { get; set; }
		public int partidas_perdidas { get; set; }
	}

	public class BaseDatos
	{
		private const string UrlBase = "http://127.0.0.1:8000";
 
		private static readonly HttpClient http = new HttpClient
		{
			BaseAddress = new Uri(UrlBase),
			Timeout = TimeSpan.FromSeconds(10)
		};
 
		// La API pide numero_turno, así que lo llevamos aquí: id_partida -> último turno
		private readonly Dictionary<int, int> turnos = new Dictionary<int, int>();
 
		private static string Leer(HttpResponseMessage r)
		{
			string texto = r.Content.ReadAsStringAsync().GetAwaiter().GetResult();
			if (!r.IsSuccessStatusCode)
				throw new Exception("Error de la API (" + (int)r.StatusCode + "): " + texto);
			return texto;
		}
 
		private static string Get(string ruta)
		{
			return Leer(http.GetAsync(ruta).GetAwaiter().GetResult());
		}
 
		private static string Enviar(HttpMethod metodo, string ruta, object cuerpo)
		{
			var req = new HttpRequestMessage(metodo, ruta)
			{
				Content = new StringContent(JsonConvert.SerializeObject(cuerpo), Encoding.UTF8, "application/json")
			};
			return Leer(http.SendAsync(req).GetAwaiter().GetResult());
		}
 
 
		// Comprueba que la API (y por tanto la base) responde
		public bool ProbarConexion()
		{
			try
			{
				Get("/");
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
 
		public List<JugadorBD> ObtenerJugadores()
		{
			var lista = new List<JugadorBD>();
			foreach (JObject fila in JArray.Parse(Get("/jugadores")))
				lista.Add(new JugadorBD { id = (int)fila["id"], nombre = (string)fila["nombre"] });
			return lista;
		}
 
		// Recibe los ids de los jugadores en su orden de turno. Devuelve el id de la partida
		public int CrearPartida(List<int> idsJugadores)
		{
			string json = Enviar(HttpMethod.Post, "/partidas", new { jugadores = idsJugadores });
			int id_partida = (int)JObject.Parse(json)["id_partida"];
			turnos[id_partida] = 0;
			return id_partida;
		}
 
		// Tipo: "JUGAR", "ROBAR", "PASAR", "UNO", "CASTIGO_UNO", "ACEPTAR_MAS4", "DESAFIAR_MAS4" o "COLOR_INICIAL"
		// (la columna tipo admite hasta 20 caracteres)
		public void RegistrarMovimiento(int id_partida, int id_jugador, string tipo, string carta = null, string color_elegido = null)
		{
			int turno;
			turnos.TryGetValue(id_partida, out turno);
			turno++;
			turnos[id_partida] = turno;
 
			Enviar(HttpMethod.Post, "/movimientos", new
			{
				id_partida = id_partida,
				id_jugador = id_jugador,
				numero_turno = turno,
				tipo = tipo,
				carta = carta,
				color_elegido = color_elegido
			});
		}
 
		// cartasRestantes: id de jugador -> cartas que le quedaron al terminar
		public void RegistrarResultado(int id_partida, int id_ganador, Dictionary<int, int> cartasRestantes)
		{
			var cartas = new List<object>();
			foreach (var par in cartasRestantes)
				cartas.Add(new { id_jugador = par.Key, cartas_restantes = par.Value });
 
			Enviar(HttpMethod.Put, "/partidas/" + id_partida + "/terminar",
				new { id_ganador = id_ganador, cartas = cartas });
		}
 
		public List<HistorialPartida> ObtenerHistorial()
		{
			var lista = new List<HistorialPartida>();
			foreach (JObject fila in JArray.Parse(Get("/partidas")))
			{
				int id = (int)fila["id"];
				// /partidas no trae el total de movimientos, se cuenta con otra consulta
				int totalMovs = JArray.Parse(Get("/partidas/" + id + "/movimientos")).Count;
 
				lista.Add(new HistorialPartida
				{
					id_partida = id,
					fecha_ini = (DateTime)fila["fecha_ini"],
					fecha_fin = fila["fecha_fin"].Type == JTokenType.Null ? (DateTime?)null : (DateTime)fila["fecha_fin"],
					nombre_ganador = fila["ganador"].Type == JTokenType.Null ? null : (string)fila["ganador"],
					total_movs = totalMovs
				});
			}
			return lista;
		}
 
		public List<EstadisticaJugador> ObtenerEstadisticasPorJugador()
		{
			var lista = new List<EstadisticaJugador>();
			foreach (JObject fila in JArray.Parse(Get("/historial")))
			{
				int ganadas = (int)fila["ganadas"];
				int perdidas = (int)fila["perdidas"];
				lista.Add(new EstadisticaJugador
				{
					nombre = (string)fila["nombre"],
					partidas_ganadas = ganadas,
					partidas_perdidas = perdidas,
					partidas_jugadas = ganadas + perdidas
				});
			}
			return lista;
		}
	}
}