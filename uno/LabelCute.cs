using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace uno
{
    // Label personalizado para que Fredoka se vea suave y con mejor calidad.
    public class LabelCute : Label
    {
        public bool UsarSombra { get; set; } = false;
        public Color ColorSombra { get; set; } = Color.FromArgb(90, 30, 20, 40);
        public Point DesplazamientoSombra { get; set; } = new Point(2, 2);

        public bool UsarContorno { get; set; } = false;
        public Color ColorContorno { get; set; } = Color.White;
        public float GrosorContorno { get; set; } = 4f;

        public LabelCute()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor,
                true
            );

            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            using (StringFormat formato = CrearFormato())
            {
                Rectangle areaTexto = ClientRectangle;

                if (UsarContorno && !string.IsNullOrEmpty(Text))
                {
                    using (GraphicsPath ruta = new GraphicsPath())
                    {
                        float emSize = e.Graphics.DpiY * Font.SizeInPoints / 72f;

                        ruta.AddString(
                            Text,
                            Font.FontFamily,
                            (int)Font.Style,
                            emSize,
                            areaTexto,
                            formato
                        );

                        if (UsarSombra)
                        {
                            using (GraphicsPath rutaSombra = (GraphicsPath)ruta.Clone())
                            using (Matrix m = new Matrix())
                            using (Pen penSombra = new Pen(ColorSombra, GrosorContorno + 1.5f) { LineJoin = LineJoin.Round })
                            {
                                m.Translate(DesplazamientoSombra.X, DesplazamientoSombra.Y);
                                rutaSombra.Transform(m);
                                e.Graphics.DrawPath(penSombra, rutaSombra);
                                using (SolidBrush rellenoSombra = new SolidBrush(Color.FromArgb(90, ColorSombra)))
                                {
                                    e.Graphics.FillPath(rellenoSombra, rutaSombra);
                                }
                            }
                        }

                        using (Pen penContorno = new Pen(ColorContorno, GrosorContorno) { LineJoin = LineJoin.Round })
                        using (SolidBrush brocha = new SolidBrush(ForeColor))
                        {
                            e.Graphics.DrawPath(penContorno, ruta);
                            e.Graphics.FillPath(brocha, ruta);
                        }
                    }
                }
                else
                {
                    if (UsarSombra)
                    {
                        Rectangle areaSombra = new Rectangle(
                            areaTexto.X + DesplazamientoSombra.X,
                            areaTexto.Y + DesplazamientoSombra.Y,
                            areaTexto.Width,
                            areaTexto.Height
                        );

                        using (SolidBrush sombra = new SolidBrush(ColorSombra))
                        {
                            e.Graphics.DrawString(
                                Text,
                                Font,
                                sombra,
                                areaSombra,
                                formato
                            );
                        }
                    }

                    using (SolidBrush brocha = new SolidBrush(ForeColor))
                    {
                        e.Graphics.DrawString(
                            Text,
                            Font,
                            brocha,
                            areaTexto,
                            formato
                        );
                    }
                }
            }
        }

        private StringFormat CrearFormato()
        {
            StringFormat formato = new StringFormat();

            if (TextAlign == ContentAlignment.TopCenter ||
                TextAlign == ContentAlignment.MiddleCenter ||
                TextAlign == ContentAlignment.BottomCenter)
            {
                formato.Alignment = StringAlignment.Center;
            }
            else if (TextAlign == ContentAlignment.TopRight ||
                     TextAlign == ContentAlignment.MiddleRight ||
                     TextAlign == ContentAlignment.BottomRight)
            {
                formato.Alignment = StringAlignment.Far;
            }
            else
            {
                formato.Alignment = StringAlignment.Near;
            }

            if (TextAlign == ContentAlignment.MiddleLeft ||
                TextAlign == ContentAlignment.MiddleCenter ||
                TextAlign == ContentAlignment.MiddleRight)
            {
                formato.LineAlignment = StringAlignment.Center;
            }
            else if (TextAlign == ContentAlignment.BottomLeft ||
                     TextAlign == ContentAlignment.BottomCenter ||
                     TextAlign == ContentAlignment.BottomRight)
            {
                formato.LineAlignment = StringAlignment.Far;
            }
            else
            {
                formato.LineAlignment = StringAlignment.Near;
            }

            formato.Trimming = StringTrimming.EllipsisCharacter;
            formato.FormatFlags = StringFormatFlags.NoClip;

            return formato;
        }

        protected override void OnTextChanged(System.EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnFontChanged(System.EventArgs e)
        {
            base.OnFontChanged(e);
            Invalidate();
        }

        protected override void OnForeColorChanged(System.EventArgs e)
        {
            base.OnForeColorChanged(e);
            Invalidate();
        }
    }
}
