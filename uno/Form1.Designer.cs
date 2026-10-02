namespace uno
{
    partial class FormInicio
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }


        private void InitializeComponent()
        {
            this.panelJugadores = new uno.PanelVidrio();
            this.lblElige = new System.Windows.Forms.Label();
            this.lblIndicacion = new System.Windows.Forms.Label();
            this.panelJugadores.SuspendLayout();
            this.SuspendLayout();
          
            this.panelJugadores.BackColor = System.Drawing.Color.Transparent;
            this.panelJugadores.Controls.Add(this.lblElige);
            this.panelJugadores.Controls.Add(this.lblIndicacion);
            this.panelJugadores.Location = new System.Drawing.Point(540, 20);
            this.panelJugadores.Name = "panelJugadores";
            this.panelJugadores.Size = new System.Drawing.Size(520, 640);
            this.panelJugadores.TabIndex = 4;
           

            this.lblElige.BackColor = System.Drawing.Color.Transparent;
            this.lblElige.Font = new System.Drawing.Font("Segoe UI Semibold", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblElige.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(59)))), ((int)(((byte)(53)))), ((int)(((byte)(82)))));
            this.lblElige.Location = new System.Drawing.Point(44, 44);
            this.lblElige.Name = "lblElige";
            this.lblElige.Size = new System.Drawing.Size(432, 40);
            this.lblElige.TabIndex = 0;
            this.lblElige.Text = "Elige a los jugadores";
            this.lblElige.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
           
            this.lblIndicacion.BackColor = System.Drawing.Color.Transparent;
            this.lblIndicacion.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblIndicacion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(132)))), ((int)(((byte)(163)))));
            this.lblIndicacion.Location = new System.Drawing.Point(44, 86);
            this.lblIndicacion.Name = "lblIndicacion";
            this.lblIndicacion.Size = new System.Drawing.Size(432, 44);
            this.lblIndicacion.TabIndex = 1;
            this.lblIndicacion.Text = "Toca a 4 jugadores. El orden en que los elijas será el orden de los turnos.";
           
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(246)))), ((int)(((byte)(238)))), ((int)(((byte)(255)))));
            this.ClientSize = new System.Drawing.Size(1100, 680);
            this.Controls.Add(this.panelJugadores);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "FormInicio";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "UNO";
            this.panelJugadores.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        private uno.PanelVidrio panelJugadores;
        private System.Windows.Forms.Label lblElige;
        private System.Windows.Forms.Label lblIndicacion;
    }
}