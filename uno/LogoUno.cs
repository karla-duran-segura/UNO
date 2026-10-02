using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace uno
{
    public class LogoUno : Control
    {
        public LogoUno()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
            Size = new Size(470, 400);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            if (Width < 60 || Height < 60) return;

            DibujarConfeti(g);

            float cw = Math.Min(Width / 2.9f, Height * 0.80f / 1.5f);
            float ch = cw * 1.5f;
            float cx = Width / 2f;
            float cy = Height / 2f + ch * 0.42f;

            float[] angulos = { -40f, 40f, -20f, 20f, 0f };
            Color[] colores = { Tema.Azul, Tema.Amarillo, Tema.Menta, Tema.Rosa, Tema.MoradoPastel };
            string[] textos = { "7", "2", "9", "5", "UNO" };

            for (int i = 0; i < angulos.Length; i++)
            {
                var estado = g.Save();
                g.TranslateTransform(cx, cy);
                g.RotateTransform(angulos[i]);
                DibujarNaipe(g, new RectangleF(-cw / 2f, -ch * 0.92f, cw, ch), colores[i], textos[i]);
                g.Restore(estado);
            }
        }

        private void DibujarNaipe(Graphics g, RectangleF r, Color c, string texto)
        {
            float rad = r.Width * 0.14f;
            Color oscuro = Tema.Mezclar(c, Tema.Texto, 0.45f);

            using (var sp = Tema.Redondeado(new RectangleF(r.X + 3, r.Y + 7, r.Width, r.Height), rad))
            using (var sb = new SolidBrush(Color.FromArgb(40, 80, 60, 130)))
                g.FillPath(sb, sp);

            using (var p = Tema.Redondeado(r, rad))
                g.FillPath(Brushes.White, p);

            var interior = RectangleF.Inflate(r, -r.Width * 0.06f, -r.Width * 0.06f);
            using (var p = Tema.Redondeado(interior, rad * 0.75f))
            using (var b = new SolidBrush(c))
                g.FillPath(b, p);

            var estado = g.Save();
            g.TranslateTransform(r.X + r.Width / 2f, r.Y + r.Height / 2f);
            g.RotateTransform(28f);
            g.FillEllipse(Brushes.White, -r.Width * 0.33f, -r.Height * 0.40f, r.Width * 0.66f, r.Height * 0.80f);
            g.Restore(estado);

            estado = g.Save();
            g.TranslateTransform(r.X + r.Width / 2f, r.Y + r.Height / 2f);
            bool frente = texto == "UNO";
            if (frente) g.RotateTransform(-8f);
            float px = frente ? r.Width * 0.34f : r.Height * 0.32f;
            using (var f = Tema.Fuente("Segoe UI Black", px, frente ? FontStyle.Italic : FontStyle.Regular))
            using (var sf = Tema.Centrado())
            using (var b = new SolidBrush(frente ? Tema.Morado : oscuro))
                g.DrawString(texto, f, b, new RectangleF(-r.Width, -r.Height / 2f, r.Width * 2f, r.Height), sf);
            g.Restore(estado);
        }

        private void DibujarConfeti(Graphics g)
        {
            object[][] puntos =
            {
                new object[] { 0.10f, 0.14f, 0.030f, Tema.Rosa, 0 },
                new object[] { 0.86f, 0.10f, 0.022f, Tema.Amarillo, 1 },
                new object[] { 0.20f, 0.30f, 0.016f, Tema.Azul, 1 },
                new object[] { 0.92f, 0.38f, 0.026f, Tema.MoradoPastel, 0 },
                new object[] { 0.06f, 0.58f, 0.022f, Tema.Menta, 1 },
                new object[] { 0.80f, 0.74f, 0.018f, Tema.Rosa, 0 },
                new object[] { 0.14f, 0.84f, 0.026f, Tema.Amarillo, 0 },
                new object[] { 0.70f, 0.20f, 0.014f, Tema.Menta, 0 },
            };
            foreach (var p in puntos)
            {
                float s = Width * (float)p[2];
                float x = Width * (float)p[0];
                float y = Height * (float)p[1];
                using (var b = new SolidBrush(Color.FromArgb(200, (Color)p[3])))
                {
                    if ((int)p[4] == 0)
                        g.FillEllipse(b, x - s, y - s, s * 2, s * 2);
                    else
                        g.FillPolygon(b, new[]
                        {
                            new PointF(x, y - s * 1.3f), new PointF(x + s, y),
                            new PointF(x, y + s * 1.3f), new PointF(x - s, y)
                        });
                }
            }
        }
    }
}