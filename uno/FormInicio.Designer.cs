namespace uno
{
    partial class FormInicio
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.listBoxJugadores = new System.Windows.Forms.ListBox();
            this.btnContinuar = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listBoxJugadores
            // 
            this.listBoxJugadores.FormattingEnabled = true;
            this.listBoxJugadores.ItemHeight = 25;
            this.listBoxJugadores.Location = new System.Drawing.Point(82, 52);
            this.listBoxJugadores.Name = "listBoxJugadores";
            this.listBoxJugadores.Size = new System.Drawing.Size(410, 229);
            this.listBoxJugadores.TabIndex = 0;
            // 
            // btnContinuar
            // 
            this.btnContinuar.Location = new System.Drawing.Point(276, 308);
            this.btnContinuar.Name = "btnContinuar";
            this.btnContinuar.Size = new System.Drawing.Size(241, 74);
            this.btnContinuar.TabIndex = 1;
            this.btnContinuar.Text = "Continuar";
            this.btnContinuar.UseVisualStyleBackColor = true;
            this.btnContinuar.Click += new System.EventHandler(this.btnContinuar_Click);
            // 
            // FormInicio
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btnContinuar);
            this.Controls.Add(this.listBoxJugadores);
            this.Name = "FormInicio";
            this.Text = "UNO - Inicio";
            this.Load += new System.EventHandler(this.FormInicio_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxJugadores;
        private System.Windows.Forms.Button btnContinuar;
    }
}