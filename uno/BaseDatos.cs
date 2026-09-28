using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

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
		private readonly string _cadenaConexion = "Server=localhost;Port=3306;Database=uno;Uid=uno_user;Pwd=uno1234;";
		
		private MySqlConnection Abrir()
		{
			var conn = new MySqlConnection(_cadenaConexion);
			conn.Open();
			return conn;
		}

		// Comprueba que el servidor y la base existen. Devuelve true si todo esta bien
		public bool ProbarConexion()
		{
			try
			{
				using (var conn = Abrir()) { return true; }
			}
			catch (MySqlException)
			{
				return false;
			}
		}

		public List<JugadorBD> ObtenerJugadores()
		{
			var lista = new List<JugadorBD>();
			using (var conn = Abrir())
			using (var cmd = new MySqlCommand("SELECT id, nombre FROM Jugadores ORDER BY id;", conn))
			using (var lector = cmd.ExecuteReader())
			{
				while (lector.Read())
					lista.Add(new JugadorBD { id = lector.GetInt32(0), nombre = lector.GetString(1) });
			}
			return lista;
		}

		// Recibe los ids de los jugadores en su orden de turno. Devuelve el id de la partida
		public int CrearPartida(List<int> idsJugadores)
		{
			using (var conn = Abrir())
			using (var tx = conn.BeginTransaction())
			{
				int id_partida;
				using (var cmd = new MySqlCommand("INSERT INTO Partidas (fecha_ini) VALUES (@fecha);", conn, tx))
				{
					cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
					cmd.ExecuteNonQuery();
					id_partida = Convert.ToInt32(cmd.LastInsertedId);
				}
				for (int i = 0; i < idsJugadores.Count; i++)
				{
					using (var cmdJ = new MySqlCommand("INSERT INTO JugadorPartida (id_partida, id_jugador, orden) VALUES (@p, @j, @o);", conn, tx))
					{
						cmdJ.Parameters.AddWithValue("@p", id_partida);
						cmdJ.Parameters.AddWithValue("@j", idsJugadores[i]);
						cmdJ.Parameters.AddWithValue("@o", i + 1);
						cmdJ.ExecuteNonQuery();
					}
				}
				tx.Commit(); // confirma los INSERT anteriores; sin esto no se guarda nada
				return id_partida;
			}
		}

		// Tipo: "JUGAR", "ROBAR", "UNO", o "CASTIGO_UNO"
		public void RegistrarMovimiento(int id_partida, int id_jugador, string tipo, string carta=null, string color_elegido = null)
		{
			using (var conn = Abrir())
			{
				int turno;
				using (var cmdT = new MySqlCommand("SELECT COUNT(*) + 1 FROM Movimientos WHERE id_partida = @p;", conn))
				{
					cmdT.Parameters.AddWithValue("@p", id_partida);
					turno = Convert.ToInt32(cmdT.ExecuteScalar());
				}
				using (var cmd = new MySqlCommand(@"INSERT INTO Movimientos (id_partida, id_jugador, numero_turno, tipo, carta, color_elegido, fecha) VALUES (@p, @j, @turno, @tipo, @carta, @color, @fecha);", conn)) 
				{
					cmd.Parameters.AddWithValue("@p", id_partida);
					cmd.Parameters.AddWithValue("@j", id_jugador);
					cmd.Parameters.AddWithValue("@turno", turno);
					cmd.Parameters.AddWithValue("@tipo", tipo);
					cmd.Parameters.AddWithValue("@carta", (object)carta ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@color", (object)color_elegido ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
					cmd.ExecuteNonQuery();
				}
			}
		}

		// Cartas Restantes: id de jugador -> cartas que le quedaron al terminar
		public void RegistrarResultado(int id_partida, int id_ganador, Dictionary<int, int> cartasRestantes)
		{
			using (var conn = Abrir())
			using (var tx = conn.BeginTransaction())
			{
				using (var cmd = new MySqlCommand("UPDATE Partidas SET fecha_fin = @fecha, id_ganador = @g WHERE id = @p;", conn, tx))
				{
					cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
					cmd.Parameters.AddWithValue("@g", id_ganador);
					cmd.Parameters.AddWithValue("@p", id_partida);
					cmd.ExecuteNonQuery();
				}
				foreach (var par in cartasRestantes)
				{
					using (var cmdJ = new MySqlCommand("UPDATE JugadorPartida SET cartas_restantes = @c WHERE id_partida = @p AND id_jugador = @j;", conn, tx))
					{
						cmdJ.Parameters.AddWithValue("@c", par.Value);
						cmdJ.Parameters.AddWithValue("@p", id_partida);
						cmdJ.Parameters.AddWithValue("@j", par.Key);
						cmdJ.ExecuteNonQuery();
					}
				}
				tx.Commit();
			}
		}

		public List<HistorialPartida> ObtenerHistorial()
		{
			var lista = new List<HistorialPartida>();
			using (var conn = Abrir())
			using (var cmd = new MySqlCommand(@"
				SELECT p.id, p.fecha_ini, p.fecha_fin, j.nombre, (SELECT COUNT(*) FROM Movimientos m WHERE m.id_partida = p.id) 
				FROM Partidas p 
				LEFT JOIN Jugadores j ON j.id = p.id_ganador 
				ORDER BY p.id DESC;", conn))
			using (var lector = cmd.ExecuteReader())
			{
				while (lector.Read())
				{
					lista.Add(new HistorialPartida
					{
						id_partida = lector.GetInt32(0),
						fecha_ini = lector.GetDateTime(1),
						fecha_fin = lector.IsDBNull(2) ? (DateTime?)null : lector.GetDateTime(2),
						nombre_ganador = lector.IsDBNull(3) ? null : lector.GetString(3),
						total_movs = Convert.ToInt32(lector.GetValue(4))
					});
				}
			}
			return lista;
		}

		public List<EstadisticaJugador> ObtenerEstadisticasPorJugador()
		{
			var lista = new List<EstadisticaJugador>();
			using (var conn = Abrir())
			using (var cmd = new MySqlCommand(@"
				SELECT
					j.nombre,
					COUNT(jp.id_partida) AS PartidasJugadas,
					SUM(CASE WHEN p.id_ganador = j.id THEN 1 ELSE 0 END) AS PartidasGanadas,
					SUM(CASE WHEN p.id_ganador IS NOT NULL AND p.id_ganador <> j.id THEN 1 ELSE 0 END) AS PartidasPerdidas
				FROM Jugadores j
				LEFT JOIN JugadorPartida jp ON jp.id_jugador = j.id
				LEFT JOIN Partidas p ON p.id = jp.id_partida
				GROUP BY j.id, j.nombre;", conn))
			using (var lector = cmd.ExecuteReader())
			{
				while (lector.Read())
				{
					lista.Add(new EstadisticaJugador
					{
						nombre = lector.GetString(0),
						partidas_jugadas = Convert.ToInt32(lector.GetValue(1)),
						partidas_ganadas = Convert.ToInt32(lector.GetValue(2)),
						partidas_perdidas = Convert.ToInt32(lector.GetValue(3))
					});
				}
			}
			return lista;
		}
	}

}