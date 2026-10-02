from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import List, Optional
import pymysql

app = FastAPI(title="API del UNO")

# 1. Conectar Python a la base de datos
def conectar():
    return pymysql.connect(
        host="localhost",
        user="uno_user",
        password="uno1234",
        database="uno",
        charset="utf8mb4"
    )


# 2. Convertir el resultado de una consulta SQL a JSON
def consulta_a_json(sql, parametros=()):
    conexion = conectar()
    try:
        cursor = conexion.cursor()
        cursor.execute(sql, parametros)

        columnas = []
        for columna in cursor.description:
            columnas.append(columna[0])

        resultado = []
        for fila in cursor.fetchall():
            registro = {}
            for i in range(len(columnas)):
                registro[columnas[i]] = fila[i]
            resultado.append(registro)

        return resultado
    finally:
        conexion.close()


def ejecutar(sql, parametros=()):
    conexion = conectar()
    try:
        cursor = conexion.cursor()
        cursor.execute(sql, parametros)
        conexion.commit()
        return cursor.lastrowid
    finally:
        conexion.close()


# Datos que el juego manda a la API (en JSON)
class NuevaPartida(BaseModel):
    jugadores: List[int] # ids de los jugadores, en el orden en que juegan


class NuevoMovimiento(BaseModel):
    id_partida: int
    id_jugador: int
    numero_turno: int
    tipo: str  # "jugar", "robar", "pasar", "uno", "castigo"...
    carta: Optional[str] = None  # ej: "Rojo 5", "Comodín +4"
    color_elegido: Optional[str] = None


class CartasJugador(BaseModel):
    id_jugador: int
    cartas_restantes: int


class FinPartida(BaseModel):
    id_ganador: int
    cartas: List[CartasJugador] = []


# Rutas para CONSULTAR (GET)
@app.get("/")
def inicio():
    return {"mensaje": "API del UNO funcionando"}


@app.get("/jugadores")
def obtener_jugadores():
    return consulta_a_json("SELECT id, nombre FROM Jugadores ORDER BY id")


@app.get("/historial")
def obtener_historial():
    sql = """
        SELECT j.id, j.nombre,
               COUNT(CASE WHEN p.id_ganador = j.id THEN 1 END) AS ganadas,
               COUNT(CASE WHEN p.id_ganador IS NOT NULL AND p.id_ganador <> j.id THEN 1 END) AS perdidas
        FROM Jugadores j
        LEFT JOIN JugadorPartida jp ON jp.id_jugador = j.id
        LEFT JOIN Partidas p ON p.id = jp.id_partida
        GROUP BY j.id, j.nombre
        ORDER BY ganadas DESC
    """
    return consulta_a_json(sql)


@app.get("/partidas")
def obtener_partidas():
    sql = """
        SELECT p.id, p.fecha_ini, p.fecha_fin, j.nombre AS ganador
        FROM Partidas p
        LEFT JOIN Jugadores j ON j.id = p.id_ganador
        ORDER BY p.id DESC
    """
    return consulta_a_json(sql)


@app.get("/partidas/{id_partida}/movimientos")
def obtener_movimientos(id_partida: int):
    sql = """
        SELECT m.id, m.numero_turno, j.nombre AS jugador, m.tipo,
               m.carta, m.color_elegido, m.fecha
        FROM Movimientos m
        JOIN Jugadores j ON j.id = m.id_jugador
        WHERE m.id_partida = %s
        ORDER BY m.id
    """
    return consulta_a_json(sql, (id_partida,))


# Rutas para GUARDAR (POST / PUT)
@app.post("/partidas")
def crear_partida(datos: NuevaPartida):
    if len(datos.jugadores) < 2:
        raise HTTPException(status_code=400, detail="Se necesitan al menos 2 jugadores")

    conexion = conectar()
    try:
        cursor = conexion.cursor()
        cursor.execute("INSERT INTO Partidas (fecha_ini) VALUES (NOW())")
        id_partida = cursor.lastrowid

        for i in range(len(datos.jugadores)):
            cursor.execute(
                "INSERT INTO JugadorPartida (id_partida, id_jugador, orden) VALUES (%s, %s, %s)",
                (id_partida, datos.jugadores[i], i + 1)
            )

        conexion.commit()
        return {"id_partida": id_partida}
    except Exception:
        conexion.rollback()
        raise
    finally:
        conexion.close()


@app.post("/movimientos")
def registrar_movimiento(datos: NuevoMovimiento):
    id_movimiento = ejecutar(
        """INSERT INTO Movimientos
           (id_partida, id_jugador, numero_turno, tipo, carta, color_elegido, fecha)
           VALUES (%s, %s, %s, %s, %s, %s, NOW())""",
        (datos.id_partida, datos.id_jugador, datos.numero_turno,
         datos.tipo, datos.carta, datos.color_elegido)
    )
    return {"id_movimiento": id_movimiento}


@app.put("/partidas/{id_partida}/terminar")
def terminar_partida(id_partida: int, datos: FinPartida):
    conexion = conectar()
    try:
        cursor = conexion.cursor()
        cursor.execute(
            "UPDATE Partidas SET fecha_fin = NOW(), id_ganador = %s WHERE id = %s",
            (datos.id_ganador, id_partida)
        )

        for c in datos.cartas:
            cursor.execute(
                "UPDATE JugadorPartida SET cartas_restantes = %s WHERE id_partida = %s AND id_jugador = %s",
                (c.cartas_restantes, id_partida, c.id_jugador)
            )

        conexion.commit()
        return {"mensaje": "Partida terminada"}
    except Exception:
        conexion.rollback()
        raise
    finally:
        conexion.close()