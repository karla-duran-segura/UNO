using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace uno
{
    internal static class Tema
    {
        public static readonly Color Texto = Color.FromArgb(59, 53, 82);      // ciruela oscuro
        public static readonly Color Suave = Color.FromArgb(138, 132, 163);   // gris lavanda
        public static readonly Color Linea = Color.FromArgb(230, 224, 245);   // bordes claros
        public static readonly Color Morado = Color.FromArgb(155, 123, 245);  // color principal
        public static readonly Color RosaFuerte = Color.FromArgb(240, 139, 192);
        public static readonly Color Exito = Color.FromArgb(63, 170, 120);

        // Colores de las cartas en versión pastel
        public static readonly Color Rosa = Color.FromArgb(255, 158, 181);
        public static readonly Color MoradoPastel = Color.FromArgb(185, 162, 255);
        public static readonly Color Azul = Color.FromArgb(142, 197, 255);
        public static readonly Color Amarillo = Color.FromArgb(255, 217, 122);
        public static readonly Color Menta = Color.FromArgb(143, 221, 181);

        // Colores de avatar 
        public static readonly Color[] Acentos = { Rosa, MoradoPastel, Azul, Amarillo, Menta };

        public static Color Mezclar(Color a, Color b, float t)
        {
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        public static GraphicsPath Redondeado(RectangleF r, float radio)
        {
            float d = Math.Max(1f, radio * 2f);
            d = Math.Min(d, Math.Min(r.Width, r.Height));
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static Font Fuente(string familia, float px, FontStyle estilo)
        {
            return new Font(familia, Math.Max(1f, px), estilo, GraphicsUnit.Pixel);
        }

        public static StringFormat Centrado()
        {
            return new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoWrap
            };
        }
    }
}