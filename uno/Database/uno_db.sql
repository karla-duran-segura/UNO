CREATE DATABASE IF NOT EXISTS uno
	CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE USER IF NOT EXISTS 'uno_user'@'localhost' IDENTIFIED BY 'uno1234';
GRANT ALL PRIVILEGES ON uno.* TO 'uno_user'@'localhost';
FLUSH PRIVILEGES;

USE uno;

CREATE TABLE IF NOT EXISTS Jugadores(
	id INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL UNIQUE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Partidas(
	id INT AUTO_INCREMENT PRIMARY KEY,
    fecha_ini DATETIME NOT NULL,
    fecha_fin DATETIME NULL,
    id_ganador INT NULL,
    FOREIGN KEY (id_ganador) REFERENCES Jugadores(id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS JugadorPartida(
	id_partida INT NOT NULL,
    id_jugador INT NOT NULL,
    orden INT NOT NULL,
    cartar_restantes INT NULL,
    PRIMARY KEY (id_partida, id_jugador),
    FOREIGN KEY (id_partida) REFERENCES Partidas(id),
    FOREIGN KEY (id_jugador) REFERENCES Jugadores(id)
)ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Movimientos(
	id INT AUTO_INCREMENT PRIMARY KEY,
    id_partida INT NOT NULL,
	id_jugador INT NOT NULL,
	numero_turno INT NOT NULL,
    tipo VARCHAR(20) NOT NULL,
    carta VARCHAR(30) NULL,
    color_elegido VARCHAR(20) NULL, 
    fecha DATETIME NOT NULL,
    FOREIGN KEY (id_partida) REFERENCES Partidas(id),
    FOREIGN KEY (id_jugador) REFERENCES Jugadores(id)
) ENGINE=InnoDB;

INSERT IGNORE INTO Jugadores (Nombre) VALUES ('Selyan'), ('Axel'), ('Nahima'), ('Karla');
