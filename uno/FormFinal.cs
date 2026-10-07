using System;
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
            this.ClientSize = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = ColorTranslator.FromHtml("#2b2633");
        }
    }
}