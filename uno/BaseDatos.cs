using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uno
{
    public class BaseDatos
    {
        public List<string> ObtenerJugadores()
        {
            List<string> jugadores = new List<string>();
            jugadores.Add("Axel");
            jugadores.Add("Selyan");
            jugadores.Add("Karla");
            jugadores.Add("Nahima");
            return jugadores;
        }
    }
}
