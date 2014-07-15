partial class SplashForm
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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SplashForm));
        this.InformationLabel = new System.Windows.Forms.Label();
        this.VersionLabel = new System.Windows.Forms.Label();
        this.ProductLinkLabel = new System.Windows.Forms.Label();
        this.ProgressBar = new System.Windows.Forms.ProgressBar();
        this.PrimalogyLinkLabel = new System.Windows.Forms.Label();
        this.LogoPictureBox = new System.Windows.Forms.PictureBox();
        ((System.ComponentModel.ISupportInitialize)(this.LogoPictureBox)).BeginInit();
        this.SuspendLayout();
        // 
        // InformationLabel
        // 
        this.InformationLabel.BackColor = System.Drawing.Color.Transparent;
        this.InformationLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.InformationLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.InformationLabel.ForeColor = System.Drawing.Color.White;
        this.InformationLabel.Location = new System.Drawing.Point(0, 107);
        this.InformationLabel.Name = "InformationLabel";
        this.InformationLabel.Size = new System.Drawing.Size(213, 14);
        this.InformationLabel.TabIndex = 11;
        this.InformationLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
        // 
        // VersionLabel
        // 
        this.VersionLabel.BackColor = System.Drawing.Color.Transparent;
        this.VersionLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.VersionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.VersionLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(145)))), ((int)(((byte)(97)))), ((int)(((byte)(79)))));
        this.VersionLabel.Location = new System.Drawing.Point(0, 87);
        this.VersionLabel.Name = "VersionLabel";
        this.VersionLabel.Size = new System.Drawing.Size(213, 16);
        this.VersionLabel.TabIndex = 18;
        this.VersionLabel.Text = "©2014 Ali Adams";
        this.VersionLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
        // 
        // ProductLinkLabel
        // 
        this.ProductLinkLabel.BackColor = System.Drawing.Color.Transparent;
        this.ProductLinkLabel.Cursor = System.Windows.Forms.Cursors.Hand;
        this.ProductLinkLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.ProductLinkLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.ProductLinkLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(145)))), ((int)(((byte)(97)))), ((int)(((byte)(79)))));
        this.ProductLinkLabel.Location = new System.Drawing.Point(0, 63);
        this.ProductLinkLabel.Name = "ProductLinkLabel";
        this.ProductLinkLabel.Size = new System.Drawing.Size(213, 24);
        this.ProductLinkLabel.TabIndex = 17;
        this.ProductLinkLabel.Tag = "http://qurancode.com/";
        this.ProductLinkLabel.Text = "QuranCode 1433";
        this.ProductLinkLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
        this.ProductLinkLabel.Click += new System.EventHandler(this.LinkLabel_Click);
        // 
        // ProgressBar
        // 
        this.ProgressBar.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.ProgressBar.Location = new System.Drawing.Point(0, 103);
        this.ProgressBar.Name = "ProgressBar";
        this.ProgressBar.Size = new System.Drawing.Size(213, 4);
        this.ProgressBar.TabIndex = 19;
        // 
        // PrimalogyLinkLabel
        // 
        this.PrimalogyLinkLabel.Anchor = System.Windows.Forms.AnchorStyles.None;
        this.PrimalogyLinkLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(195)))), ((int)(((byte)(147)))), ((int)(((byte)(119)))));
        this.PrimalogyLinkLabel.Cursor = System.Windows.Forms.Cursors.Hand;
        this.PrimalogyLinkLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.PrimalogyLinkLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(95)))), ((int)(((byte)(47)))), ((int)(((byte)(19)))));
        this.PrimalogyLinkLabel.Location = new System.Drawing.Point(0, 49);
        this.PrimalogyLinkLabel.Name = "PrimalogyLinkLabel";
        this.PrimalogyLinkLabel.Size = new System.Drawing.Size(213, 14);
        this.PrimalogyLinkLabel.TabIndex = 20;
        this.PrimalogyLinkLabel.Tag = "http://heliwave.com/Primalogy.pdf";
        this.PrimalogyLinkLabel.Text = "Prime numbers are God\'s Signature!";
        this.PrimalogyLinkLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
        this.PrimalogyLinkLabel.Click += new System.EventHandler(this.LinkLabel_Click);
        // 
        // LogoPictureBox
        // 
        this.LogoPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                    | System.Windows.Forms.AnchorStyles.Left)
                    | System.Windows.Forms.AnchorStyles.Right)));
        this.LogoPictureBox.Image = ((System.Drawing.Image)(resources.GetObject("LogoPictureBox.Image")));
        this.LogoPictureBox.Location = new System.Drawing.Point(0, 0);
        this.LogoPictureBox.Name = "LogoPictureBox";
        this.LogoPictureBox.Size = new System.Drawing.Size(213, 49);
        this.LogoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
        this.LogoPictureBox.TabIndex = 21;
        this.LogoPictureBox.TabStop = false;
        // 
        // SplashForm
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(223)))), ((int)(((byte)(210)))), ((int)(((byte)(185)))));
        this.ClientSize = new System.Drawing.Size(213, 121);
        this.Controls.Add(this.PrimalogyLinkLabel);
        this.Controls.Add(this.LogoPictureBox);
        this.Controls.Add(this.ProductLinkLabel);
        this.Controls.Add(this.VersionLabel);
        this.Controls.Add(this.ProgressBar);
        this.Controls.Add(this.InformationLabel);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
        this.Name = "SplashForm";
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "SplashForm";
        ((System.ComponentModel.ISupportInitialize)(this.LogoPictureBox)).EndInit();
        this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Label InformationLabel;
    private System.Windows.Forms.Label VersionLabel;
    private System.Windows.Forms.Label ProductLinkLabel;
    private System.Windows.Forms.ProgressBar ProgressBar;
    private System.Windows.Forms.Label PrimalogyLinkLabel;
    private System.Windows.Forms.PictureBox LogoPictureBox;
}
