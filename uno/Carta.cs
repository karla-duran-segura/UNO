using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uno
{
    public class Carta
    {
        // Colores: "Rojo", "Amarillo", "Verde", "Azul", "Negro" (Negro = Comodín y +4)
        public string Color { get; }

        // Tipos: "numero", "salta", "reversa", "mas2", "comodin", "mas4"
        public string Tipo { get; }

        public int Numero { get; } // -1 si no es carta de número

        public Carta(string color, string tipo, int numero = -1)
        {
            Color = color;
            Tipo = tipo;
            Numero = numero;
        }

        public bool EsComodin()
        {
            return Tipo == "comodin" || Tipo == "mas4";
        }

        // Nombre del archivo de imagen, ej: "rojo_5.png", "azul_salta.png", "comodin.png"
        public string NombreImagen()
        {
            if (EsComodin())
                return Tipo + ".png";

            string color = Color.ToLower();

            if (Tipo == "numero")
                return color + "_" + Numero + ".png";

            return color + "_" + Tipo + ".png";
        }

        // Texto para mostrar y para el log de la base de datos, ej: "Rojo 5", "Azul Salta"
        public string Texto()
        {
            if (Tipo == "comodin")
                return "Comodín";
            if (Tipo == "mas4")
                return "Comodín +4";
            if (Tipo == "numero")
                return Color + " " + Numero;
            if (Tipo == "salta")
                return Color + " Salta";
            if (Tipo == "reversa")
                return Color + " Reversa";
            if (Tipo == "mas2")
                return Color + " +2";
            return "";
        }
    }
}
