using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace uno
{
    public class PanelVidrio : Panel
    {
        private int radio = 28;
        private int margen = 18;

        public PanelVidrio()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
        }

        [DefaultValue(28), Description("Qué tan redondeadas son las esquinas.")]
        public int Radio { get { return radio; } set { radio = value; Invalidate(); } }

        [DefaultValue(18), Description("Espacio reservado alrededor de la tarjeta para la sombra.")]
        public int Margen { get { return margen; } set { margen = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var tarjeta = new RectangleF(margen, margen, Width - 2 * margen, Height - 2 * margen);
            if (tarjeta.Width < 10 || tarjeta.Height < 10) return;

            using (var path = Tema.Redondeado(tarjeta, radio))
            {
              
                var estado = g.Save();
                g.SetClip(path, CombineMode.Exclude);
                int alcance = Math.Max(2, margen - 4);
                for (int i = alcance; i >= 1; i--)
                {
                    var r = RectangleF.Inflate(tarjeta, i, i);
                    r.Offset(0, 4);
                    using (var sp = Tema.Redondeado(r, radio + i))
                    using (var sb = new SolidBrush(Color.FromArgb(3, 110, 90, 170)))
                        g.FillPath(sb, sp);
                }
                g.Restore(estado);

                using (var b = new SolidBrush(Color.FromArgb(205, 255, 255, 255)))
                    g.FillPath(b, path);
                using (var pen = new Pen(Color.FromArgb(245, 255, 255, 255), 1.5f))
                    g.DrawPath(pen, path);
            }
        }
    }
}