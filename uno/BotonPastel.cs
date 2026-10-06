/*using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace uno
{
    public class BotonPastel : Control
    {
        private bool hover;
        private bool presionado;
        private bool principal = true;

        public BotonPastel()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(200, 56);
        }

        [DefaultValue(true), Description("true = botón con degradado; false = botón discreto.")]
        public bool Principal { get { return principal; } set { principal = value; Invalidate(); } }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; presionado = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { presionado = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { presionado = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) { OnClick(EventArgs.Empty); e.Handled = true; }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            if (Width < 12 || Height < 12) return;

            float sombra = principal ? Height * 0.10f : 0f;
            float bajar = presionado ? 1.5f : 0f;
            var r = new RectangleF(1, 1 + bajar, Width - 2, Height - 2 - sombra);
            float radio = r.Height / 2f;
            Color colorTexto;

            if (principal)
            {
                if (Enabled)
                {
                    var rs = new RectangleF(r.X + r.Width * 0.06f, r.Y + sombra * 0.9f, r.Width * 0.88f, r.Height);
                    using (var ps = Tema.Redondeado(rs, radio))
                    using (var bs = new SolidBrush(Color.FromArgb(hover ? 85 : 60, Tema.Morado)))
                        g.FillPath(bs, ps);

                    Color c1 = hover ? Tema.Mezclar(Tema.Morado, Color.White, 0.12f) : Tema.Morado;
                    Color c2 = hover ? Tema.Mezclar(Tema.RosaFuerte, Color.White, 0.12f) : Tema.RosaFuerte;
                    using (var p = Tema.Redondeado(r, radio))
                    using (var b = new LinearGradientBrush(r, c1, c2, 0f))
                        g.FillPath(b, p);
                    colorTexto = Color.White;
                }
                else
                {
                    using (var p = Tema.Redondeado(r, radio))
                    using (var b = new SolidBrush(Color.FromArgb(233, 228, 245)))
                        g.FillPath(b, p);
                    colorTexto = Color.FromArgb(172, 166, 197);
                }
            }
            else
            {
                using (var p = Tema.Redondeado(r, radio))
                {
                    if (hover)
                        using (var b = new SolidBrush(Color.FromArgb(150, 255, 255, 255)))
                            g.FillPath(b, p);
                    using (var pen = new Pen(hover ? Tema.MoradoPastel : Color.FromArgb(214, 207, 236), 1.5f))
                        g.DrawPath(pen, p);
                }
                colorTexto = hover ? Tema.Morado : Tema.Suave;
            }

            using (var sf = Tema.Centrado())
            using (var br = new SolidBrush(colorTexto))
                g.DrawString(Text, Font, br, r, sf);

            if (Focused && ShowFocusCues)
                using (var pen = new Pen(Color.FromArgb(120, Tema.Morado), 1.5f) { DashStyle = DashStyle.Dot })
                using (var pf = Tema.Redondeado(RectangleF.Inflate(r, -3, -3), radio))
                    g.DrawPath(pen, pf);
        }
    }
}*/

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace uno
{
    public class BotonPastel : Control
    {
        private bool hover;
        private bool presionado;
        private bool principal = true;

        // Colores opcionales. Si están vacíos, se usan los del tema.
        private Color colorInicio = Color.Empty;
        private Color colorFin = Color.Empty;
        private Color colorLetra = Color.Empty;

        public BotonPastel()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(200, 56);
        }

        [DefaultValue(true), Description("true = botón con degradado; false = botón discreto.")]
        public bool Principal { get { return principal; } set { principal = value; Invalidate(); } }

        [DefaultValue(typeof(Color), ""), Description("Color izquierdo del degradado. Vacío = morado del tema.")]
        public Color ColorInicio { get { return colorInicio; } set { colorInicio = value; Invalidate(); } }

        [DefaultValue(typeof(Color), ""), Description("Color derecho del degradado. Vacío = rosa del tema.")]
        public Color ColorFin { get { return colorFin; } set { colorFin = value; Invalidate(); } }

        [DefaultValue(typeof(Color), ""), Description("Color de la letra del botón activo. Vacío = blanco.")]
        public Color ColorLetra { get { return colorLetra; } set { colorLetra = value; Invalidate(); } }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; presionado = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { presionado = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { presionado = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) { OnClick(EventArgs.Empty); e.Handled = true; }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            if (Width < 12 || Height < 12) return;

            float sombra = principal ? Height * 0.10f : 0f;
            float bajar = presionado ? 1.5f : 0f;
            var r = new RectangleF(1, 1 + bajar, Width - 2, Height - 2 - sombra);
            float radio = r.Height / 2f;
            Color colorTexto;

            // Colores del degradado: los asignados o, si no hay, los del tema
            Color baseInicio = colorInicio.IsEmpty ? Tema.Morado : colorInicio;
            Color baseFin = colorFin.IsEmpty ? Tema.RosaFuerte : colorFin;

            if (principal)
            {
                if (Enabled)
                {
                    var rs = new RectangleF(r.X + r.Width * 0.06f, r.Y + sombra * 0.9f, r.Width * 0.88f, r.Height);
                    using (var ps = Tema.Redondeado(rs, radio))
                    using (var bs = new SolidBrush(Color.FromArgb(hover ? 85 : 60, baseInicio)))
                        g.FillPath(bs, ps);

                    Color c1 = hover ? Tema.Mezclar(baseInicio, Color.White, 0.12f) : baseInicio;
                    Color c2 = hover ? Tema.Mezclar(baseFin, Color.White, 0.12f) : baseFin;
                    using (var p = Tema.Redondeado(r, radio))
                    using (var b = new LinearGradientBrush(r, c1, c2, 0f))
                        g.FillPath(b, p);
                    colorTexto = colorLetra.IsEmpty ? Color.White : colorLetra;
                }
                else
                {
                    using (var p = Tema.Redondeado(r, radio))
                    using (var b = new SolidBrush(Color.FromArgb(233, 228, 245)))
                        g.FillPath(b, p);
                    colorTexto = Color.FromArgb(172, 166, 197);
                }
            }
            else
            {
                using (var p = Tema.Redondeado(r, radio))
                {
                    if (hover)
                        using (var b = new SolidBrush(Color.FromArgb(150, 255, 255, 255)))
                            g.FillPath(b, p);
                    using (var pen = new Pen(hover ? Tema.MoradoPastel : Color.FromArgb(214, 207, 236), 1.5f))
                        g.DrawPath(pen, p);
                }
                colorTexto = hover ? Tema.Morado : Tema.Suave;
            }

            using (var sf = Tema.Centrado())
            using (var br = new SolidBrush(colorTexto))
                g.DrawString(Text, Font, br, r, sf);

            if (Focused && ShowFocusCues)
                using (var pen = new Pen(Color.FromArgb(120, Tema.Morado), 1.5f) { DashStyle = DashStyle.Dot })
                using (var pf = Tema.Redondeado(RectangleF.Inflate(r, -3, -3), radio))
                    g.DrawPath(pen, pf);
        }
    }
}