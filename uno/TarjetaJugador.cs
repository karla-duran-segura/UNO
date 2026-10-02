using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace uno
{
    public class TarjetaJugador : Control
    {
        private string nombre = "";
        private string record = "";
        private Color acento = Tema.Rosa;
        private int turno;         
        private bool hover;

        public TarjetaJugador()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(432, 74);
        }

        // Datos del jugador en la base de datos 
        public JugadorBD Jugador { get; set; }

        public string Nombre { get { return nombre; } set { nombre = value ?? ""; Invalidate(); } }
        public string Record { get { return record; } set { record = value ?? ""; Invalidate(); } }
        public Color Acento { get { return acento; } set { acento = value; Invalidate(); } }

      
        public int Turno { get { return turno; } set { turno = value; Invalidate(); } }
        public bool Elegida { get { return turno > 0; } }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        // Cuando la tarjeta está deshabilitada se ve más clara
        private Color Atenuar(Color c)
        {
            return Enabled ? c : Tema.Mezclar(c, Color.White, 0.55f);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            if (Width < 20 || Height < 20) return;

            float h = Height;
            var r = new RectangleF(1.5f, 1.5f, Width - 3f, Height - 3f);
            float radio = h * 0.32f;

            // Fondo y borde
            float mezclaFondo = Elegida ? 0.70f : (hover && Enabled ? 0.74f : 0.80f);
            using (var p = Tema.Redondeado(r, radio))
            {
                using (var b = new SolidBrush(Atenuar(Tema.Mezclar(acento, Color.White, mezclaFondo))))
                    g.FillPath(b, p);
                using (var pen = new Pen(Atenuar(Tema.Mezclar(acento, Color.White, Elegida ? 0f : 0.25f)), Elegida ? 2.5f : 2f))
                    g.DrawPath(pen, p);
            }

            // Avatar con la inicial
            float dAvatar = h * 0.58f;
            var rAvatar = new RectangleF(h * 0.20f, (h - dAvatar) / 2f, dAvatar, dAvatar);
            using (var b = new SolidBrush(Atenuar(acento)))
                g.FillEllipse(b, rAvatar);

            string inicial = nombre.Length > 0 ? nombre.Substring(0, 1).ToUpper() : "?";
            using (var f = Tema.Fuente("Segoe UI Semibold", h * 0.34f, FontStyle.Regular))
            using (var sf = Tema.Centrado())
            using (var br = new SolidBrush(Atenuar(Tema.Texto)))
                g.DrawString(inicial, f, br, rAvatar, sf);

            // Círculo de turno 
            float dTurno = h * 0.38f;
            var rTurno = new RectangleF(Width - h * 0.25f - dTurno, (h - dTurno) / 2f, dTurno, dTurno);
            if (Elegida)
            {
                using (var b = new SolidBrush(Atenuar(Tema.Morado)))
                    g.FillEllipse(b, rTurno);
                using (var f = Tema.Fuente("Segoe UI Semibold", h * 0.22f, FontStyle.Bold))
                using (var sf = Tema.Centrado())
                    g.DrawString(turno.ToString(), f, Brushes.White, rTurno, sf);
            }
            else
            {
                using (var pen = new Pen(Atenuar(Color.FromArgb(200, 255, 255, 255)), 2f))
                    g.DrawEllipse(pen, rTurno);
            }

            // Nombre y récord
            float xTexto = rAvatar.Right + h * 0.20f;
            float anchoTexto = rTurno.Left - xTexto - h * 0.10f;
            using (var sf = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            {
                using (var f = Tema.Fuente("Segoe UI Semibold", h * 0.27f, FontStyle.Regular))
                using (var br = new SolidBrush(Atenuar(Tema.Texto)))
                    g.DrawString(nombre, f, br, new RectangleF(xTexto, h * 0.14f, anchoTexto, h * 0.40f), sf);

                using (var f = Tema.Fuente("Segoe UI", h * 0.18f, FontStyle.Regular))
                using (var br = new SolidBrush(Atenuar(Tema.Suave)))
                    g.DrawString(record, f, br, new RectangleF(xTexto, h * 0.52f, anchoTexto, h * 0.30f), sf);
            }
        }
    }
}