using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace uno
{
    public partial class FormInicio : Form
    {
        public FormInicio()
        {
            InitializeComponent();
        }

        private void btnContinuar_Click(object sender, EventArgs e)
        {
            if (listBoxJugadores.SelectedItems.Count == 0)
            {
                MessageBox.Show("Selecciona al menos un jugador para continuar.");
            }
            else
            {
                string nombresSeleccionados = "";
                foreach (string jugador in listBoxJugadores.SelectedItems)
                {
                    nombresSeleccionados = nombresSeleccionados + jugador + "\n";
                }
                MessageBox.Show("Jugadores seleccionados:\n" + nombresSeleccionados);
            }
        }

        private void FormInicio_Load(object sender, EventArgs e)
        {
            BaseDatos baseDatos = new BaseDatos();
            List<string> jugadores = baseDatos.ObtenerJugadores();

            foreach (string jugador in jugadores)
            {
                listBoxJugadores.Items.Add(jugador);
            }
        }
    }
}
