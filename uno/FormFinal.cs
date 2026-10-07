using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace uno
{
    public class FormFinal : Form
    {
        private Juego juego;

        public FormFinal(Juego juego)
        {
            this.juego = juego;

            this.Text = "UNO - Fin de la partida";
            this.ClientSize = new Size(700, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = ColorTranslator.FromHtml("#2b2633");

            ConstruirInterfaz();
        }

        private void ConstruirInterfaz()
        {
            // Titulo
            Label titulo = new Label();
            titulo.Text = "Fin de la partida";
            titulo.Font = new Font("Segoe UI", 28, FontStyle.Bold);
            titulo.ForeColor = Color.White;
            titulo.TextAlign = ContentAlignment.MiddleCenter;
            titulo.AutoSize = false;
            titulo.Size = new Size(700, 70);
            titulo.Location = new Point(0, 30);
            this.Controls.Add(titulo);

            // Ganador
            Label ganador = new Label();
            if (juego.GanadorPartida != null)
            {
                ganador.Text = juego.GanadorPartida.Nombre + " ganó la partida";
            }
            else
            {
                ganador.Text = "La partida terminó sin ganador";
            }
            ganador.Font = new Font("Segoe UI", 20, FontStyle.Regular);
            ganador.ForeColor = Tema.Amarillo;
            ganador.TextAlign = ContentAlignment.MiddleCenter;
            ganador.AutoSize = false;
            ganador.Size = new Size(700, 50);
            ganador.Location = new Point(0, 110);
            this.Controls.Add(ganador);

            // Ranking: copia de la lista, ordenada de mayor a menor puntaje
            List<JugadorUno> orden = new List<JugadorUno>(juego.Jugadores);
            orden.Sort(delegate (JugadorUno a, JugadorUno b)
            {
                return b.Puntos.CompareTo(a.Puntos);
            });

            for (int i = 0; i < orden.Count; i++)
            {
                Label fila = new Label();
                fila.Text = (i + 1) + ". " + orden[i].Nombre + "   " + orden[i].Puntos + " pts";
                fila.Font = new Font("Segoe UI", 16, FontStyle.Regular);
                fila.ForeColor = Tema.Linea;
                fila.TextAlign = ContentAlignment.MiddleCenter;
                fila.AutoSize = false;
                fila.Size = new Size(700, 40);
                fila.Location = new Point(0, 220 + i * 45);
                this.Controls.Add(fila);
            }

            AgregarEstadisticas();
        }

        private void AgregarEstadisticas()
        {
            Label subtitulo = new Label();
            subtitulo.Text = "Historial de partidas";
            subtitulo.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            subtitulo.ForeColor = Color.White;
            subtitulo.TextAlign = ContentAlignment.MiddleCenter;
            subtitulo.AutoSize = false;
            subtitulo.Size = new Size(700, 50);
            subtitulo.Location = new Point(0, 430);
            this.Controls.Add(subtitulo);

            List<EstadisticaJugador> estadisticas = null;
            try
            {
                BaseDatos baseDatos = new BaseDatos();
                estadisticas = baseDatos.ObtenerEstadisticasPorJugador();
            }
            catch (Exception)
            {
                estadisticas = null;
            }

            if (estadisticas == null)
            {
                Label aviso = new Label();
                aviso.Text = "No se pudo cargar el historial";
                aviso.Font = new Font("Segoe UI", 14, FontStyle.Regular);
                aviso.ForeColor = Tema.Linea;
                aviso.TextAlign = ContentAlignment.MiddleCenter;
                aviso.AutoSize = false;
                aviso.Size = new Size(700, 40);
                aviso.Location = new Point(0, 485);
                this.Controls.Add(aviso);
                return;
            }

            for (int i = 0; i < estadisticas.Count; i++)
            {
                EstadisticaJugador est = estadisticas[i];
                Label fila = new Label();
                fila.Text = est.nombre + ":  " + est.partidas_ganadas + " ganadas  ·  " + est.partidas_perdidas + " perdidas";
                fila.Font = new Font("Segoe UI", 14, FontStyle.Regular);
                fila.ForeColor = Tema.Linea;
                fila.TextAlign = ContentAlignment.MiddleCenter;
                fila.AutoSize = false;
                fila.Size = new Size(700, 35);
                fila.Location = new Point(0, 485 + i * 40);
                this.Controls.Add(fila);
            }
        }
    }
}