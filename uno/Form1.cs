using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace uno
{
    public partial class FormInicio : Form
    {
        public FormInicio()
        {
            InitializeComponent();
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = ClientRectangle;
            if (rect.Width < 2 || rect.Height < 2) { base.OnPaintBackground(e); return; }

            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var fondo = new LinearGradientBrush(rect, Color.White, Color.White, 55f))
            {
                fondo.InterpolationColors = new ColorBlend
                {
                    Positions = new[] { 0f, 0.5f, 1f },
                    Colors = new[]
                    {
                        Color.FromArgb(240, 230, 255),   
                        Color.FromArgb(255, 232, 242),   
                        Color.FromArgb(255, 241, 220)   
                    }
                };
                g.FillRectangle(fondo, rect);
            }

            float w = rect.Width, h = rect.Height;
            DibujarCirculo(g, -0.10f * w, -0.20f * h, 0.42f * w, Color.FromArgb(90, Tema.Rosa));
            DibujarCirculo(g, 0.78f * w, 0.55f * h, 0.40f * w, Color.FromArgb(80, Tema.Azul));
            DibujarCirculo(g, 0.30f * w, 0.78f * h, 0.22f * w, Color.FromArgb(70, Tema.Amarillo));
            DibujarCirculo(g, 0.86f * w, -0.12f * h, 0.20f * w, Color.FromArgb(70, Tema.MoradoPastel));
        }

        private static void DibujarCirculo(Graphics g, float x, float y, float d, Color color)
        {
            using (var b = new SolidBrush(color))
                g.FillEllipse(b, x, y, d, d);
        }
    }
}