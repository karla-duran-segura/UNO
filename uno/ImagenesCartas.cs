using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace uno
{
    /// <summary>
    /// Centraliza la carga de las imágenes de las cartas del juego.
    /// Evita repetir rutas y conserva las imágenes en memoria para no leer
    /// el mismo archivo del disco cada vez que se actualiza la interfaz.
    /// </summary>
    public static class ImagenesCartas
    {
        private static readonly string CarpetaImagenes =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imagenes_pastel");

        private static readonly Dictionary<string, Image> Cache =
            new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Devuelve la imagen correspondiente a una carta.
        /// El nombre del archivo se obtiene directamente desde Carta.NombreImagen().
        /// </summary>
        public static Image Obtener(Carta carta)
        {
            if (carta == null)
                throw new ArgumentNullException("carta");

            return ObtenerArchivo(carta.NombreImagen(), carta.Texto());
        }

        /// <summary>
        /// Devuelve el dorso de una carta. Si no existe dorso.png,
        /// genera automáticamente un dorso pastel para que la interfaz
        /// nunca quede sin imagen.
        /// </summary>
        public static Image ObtenerDorso()
        {
            const string clave = "__dorso__";

            Image imagen;
            if (Cache.TryGetValue(clave, out imagen))
                return imagen;

            string ruta = Path.Combine(CarpetaImagenes, "dorso.png");

            imagen = File.Exists(ruta)
                ? CargarSinBloquearArchivo(ruta)
                : CrearDorsoPastel();

            Cache[clave] = imagen;
            return imagen;
        }

        /// <summary>
        /// Crea una copia redimensionada manteniendo buena calidad.
        /// Es útil para PictureBox, botones o cartas pequeñas de los rivales.
        /// </summary>
        public static Image Redimensionar(Image original, int ancho, int alto)
        {
            if (original == null)
                throw new ArgumentNullException("original");
            if (ancho <= 0 || alto <= 0)
                throw new ArgumentOutOfRangeException("El tamaño debe ser mayor que cero.");

            Bitmap destino = new Bitmap(ancho, alto);

            using (Graphics g = Graphics.FromImage(destino))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.DrawImage(original, new Rectangle(0, 0, ancho, alto));
            }

            return destino;
        }

        private static Image ObtenerArchivo(string nombreArchivo, string textoCarta)
        {
            Image imagen;
            if (Cache.TryGetValue(nombreArchivo, out imagen))
                return imagen;

            string ruta = Path.Combine(CarpetaImagenes, nombreArchivo);

            imagen = File.Exists(ruta)
                ? CargarSinBloquearArchivo(ruta)
                : CrearPlaceholder(textoCarta);

            Cache[nombreArchivo] = imagen;
            return imagen;
        }

        // Carga la imagen en memoria y cierra el archivo inmediatamente.
        // Así Windows no mantiene bloqueado el PNG durante la ejecución.
        private static Image CargarSinBloquearArchivo(string ruta)
        {
            using (FileStream archivo = new FileStream(ruta, FileMode.Open, FileAccess.Read))
            using (Image temporal = Image.FromStream(archivo))
            {
                return new Bitmap(temporal);
            }
        }

        // Imagen de respaldo por si falta un PNG del mazo.
        private static Image CrearPlaceholder(string texto)
        {
            const int ancho = 410;
            const int alto = 585;

            Bitmap bmp = new Bitmap(ancho, alto);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.FromArgb(250, 244, 252));

                Rectangle borde = new Rectangle(12, 12, ancho - 24, alto - 24);
                using (Pen p = new Pen(Color.FromArgb(185, 162, 255), 8))
                    g.DrawRectangle(p, borde);

                using (Font fuente = new Font("Segoe UI", 24, FontStyle.Bold, GraphicsUnit.Pixel))
                using (Brush pincel = new SolidBrush(Color.FromArgb(59, 53, 82)))
                using (StringFormat formato = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                })
                {
                    g.DrawString(
                        "Imagen no encontrada\n" + texto,
                        fuente,
                        pincel,
                        new RectangleF(30, 30, ancho - 60, alto - 60),
                        formato);
                }
            }

            return bmp;
        }

        // Dorso provisional integrado en código. Si después se agrega dorso.png,
        // este método deja de utilizarse automáticamente.
        private static Image CrearDorsoPastel()
        {
            const int ancho = 410;
            const int alto = 585;

            Bitmap bmp = new Bitmap(ancho, alto);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle rect = new Rectangle(0, 0, ancho, alto);
                using (LinearGradientBrush fondo = new LinearGradientBrush(
                    rect,
                    Color.FromArgb(255, 205, 224),
                    Color.FromArgb(190, 173, 255),
                    45f))
                {
                    g.FillRectangle(fondo, rect);
                }

                Rectangle interior = new Rectangle(18, 18, ancho - 36, alto - 36);
                using (Pen borde = new Pen(Color.White, 9))
                    g.DrawRectangle(borde, interior);

                Rectangle ovalo = new Rectangle(55, 145, ancho - 110, alto - 290);
                using (Brush b = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
                    g.FillEllipse(b, ovalo);

                using (Font fuente = new Font("Segoe UI", 72, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Pixel))
                using (Brush texto = new SolidBrush(Color.FromArgb(132, 98, 218)))
                using (StringFormat formato = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                })
                {
                    g.DrawString("UNO", fuente, texto, ovalo, formato);
                }
            }

            return bmp;
        }
    }
}
