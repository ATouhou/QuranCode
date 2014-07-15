namespace InitialLetters
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.StatusStrip = new System.Windows.Forms.StatusStrip();
            this.ProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.StatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.ListView = new System.Windows.Forms.ListView();
            this.ColumnHeader = new System.Windows.Forms.ColumnHeader();
            this.TableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.DisplaySentencesLiveCheckBox = new System.Windows.Forms.CheckBox();
            this.LettersTextBox = new System.Windows.Forms.TextBox();
            this.ElapsedTimeLabel = new System.Windows.Forms.Label();
            this.SaveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.MenuStrip = new System.Windows.Forms.MenuStrip();
            this.FileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.SaveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ExitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.TypeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UniqueLettersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UniqueWordsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.AllWordsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.TypeSeparatorToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            this.RunToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.HelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.AboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.Timer = new System.Windows.Forms.Timer(this.components);
            this.ToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.StatusStrip.SuspendLayout();
            this.TableLayoutPanel.SuspendLayout();
            this.MenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // StatusStrip
            // 
            this.StatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ProgressBar,
            this.StatusLabel});
            this.StatusStrip.Location = new System.Drawing.Point(0, 400);
            this.StatusStrip.Name = "StatusStrip";
            this.StatusStrip.Size = new System.Drawing.Size(266, 22);
            this.StatusStrip.TabIndex = 10;
            this.StatusStrip.Text = "StatusStrip";
            // 
            // ProgressBar
            // 
            this.ProgressBar.ForeColor = System.Drawing.Color.Black;
            this.ProgressBar.Name = "ProgressBar";
            this.ProgressBar.Size = new System.Drawing.Size(120, 16);
            this.ProgressBar.Step = 1;
            this.ProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            // 
            // StatusLabel
            // 
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(67, 17);
            this.StatusLabel.Text = "StatusLabel";
            // 
            // ListView
            // 
            this.ListView.BackColor = System.Drawing.Color.LavenderBlush;
            this.ListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ColumnHeader});
            this.TableLayoutPanel.SetColumnSpan(this.ListView, 2);
            this.ListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ListView.Enabled = false;
            this.ListView.Font = new System.Drawing.Font("Courier New", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ListView.Location = new System.Drawing.Point(3, 3);
            this.ListView.Name = "ListView";
            this.ListView.Size = new System.Drawing.Size(260, 316);
            this.ListView.TabIndex = 1;
            this.ListView.TabStop = false;
            this.ListView.UseCompatibleStateImageBehavior = false;
            this.ListView.View = System.Windows.Forms.View.Details;
            this.ListView.Resize += new System.EventHandler(this.ListView_Resize);
            this.ListView.SelectedIndexChanged += new System.EventHandler(this.ListView_SelectedIndexChanged);
            this.ListView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.ListView_SortColumnClick);
            // 
            // ColumnHeader
            // 
            this.ColumnHeader.Text = "Sentences";
            this.ColumnHeader.Width = 255;
            // 
            // TableLayoutPanel
            // 
            this.TableLayoutPanel.BackColor = System.Drawing.Color.LavenderBlush;
            this.TableLayoutPanel.ColumnCount = 2;
            this.TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 71F));
            this.TableLayoutPanel.Controls.Add(this.DisplaySentencesLiveCheckBox, 0, 1);
            this.TableLayoutPanel.Controls.Add(this.ListView, 0, 0);
            this.TableLayoutPanel.Controls.Add(this.LettersTextBox, 0, 2);
            this.TableLayoutPanel.Controls.Add(this.ElapsedTimeLabel, 1, 1);
            this.TableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TableLayoutPanel.Location = new System.Drawing.Point(0, 24);
            this.TableLayoutPanel.Name = "TableLayoutPanel";
            this.TableLayoutPanel.RowCount = 2;
            this.TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 93.60465F));
            this.TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.395349F));
            this.TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.TableLayoutPanel.Size = new System.Drawing.Size(266, 376);
            this.TableLayoutPanel.TabIndex = 3;
            // 
            // DisplaySentencesLiveCheckBox
            // 
            this.DisplaySentencesLiveCheckBox.AutoSize = true;
            this.DisplaySentencesLiveCheckBox.BackColor = System.Drawing.Color.LavenderBlush;
            this.DisplaySentencesLiveCheckBox.Checked = true;
            this.DisplaySentencesLiveCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.DisplaySentencesLiveCheckBox.Location = new System.Drawing.Point(3, 325);
            this.DisplaySentencesLiveCheckBox.Name = "DisplaySentencesLiveCheckBox";
            this.DisplaySentencesLiveCheckBox.Size = new System.Drawing.Size(137, 16);
            this.DisplaySentencesLiveCheckBox.TabIndex = 2;
            this.DisplaySentencesLiveCheckBox.Text = "Display Sentences Live";
            this.ToolTip.SetToolTip(this.DisplaySentencesLiveCheckBox, "Show sentences as they are found");
            this.DisplaySentencesLiveCheckBox.UseVisualStyleBackColor = false;
            this.DisplaySentencesLiveCheckBox.CheckedChanged += new System.EventHandler(this.DisplaySentencesLiveCheckBox_CheckedChanged);
            // 
            // LettersTextBox
            // 
            this.TableLayoutPanel.SetColumnSpan(this.LettersTextBox, 2);
            this.LettersTextBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.LettersTextBox.Enabled = false;
            this.LettersTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LettersTextBox.Location = new System.Drawing.Point(3, 347);
            this.LettersTextBox.Name = "LettersTextBox";
            this.LettersTextBox.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.LettersTextBox.Size = new System.Drawing.Size(260, 26);
            this.LettersTextBox.TabIndex = 4;
            this.LettersTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.LettersTextBox_KeyPress);
            // 
            // ElapsedTimeLabel
            // 
            this.ElapsedTimeLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.ElapsedTimeLabel.AutoSize = true;
            this.ElapsedTimeLabel.Location = new System.Drawing.Point(214, 326);
            this.ElapsedTimeLabel.Name = "ElapsedTimeLabel";
            this.ElapsedTimeLabel.Size = new System.Drawing.Size(49, 13);
            this.ElapsedTimeLabel.TabIndex = 13;
            this.ElapsedTimeLabel.Text = "00:00:00";
            // 
            // MenuStrip
            // 
            this.MenuStrip.BackColor = System.Drawing.Color.Transparent;
            this.MenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileToolStripMenuItem,
            this.TypeToolStripMenuItem,
            this.RunToolStripMenuItem,
            this.HelpToolStripMenuItem});
            this.MenuStrip.Location = new System.Drawing.Point(0, 0);
            this.MenuStrip.Name = "MenuStrip";
            this.MenuStrip.Size = new System.Drawing.Size(266, 24);
            this.MenuStrip.TabIndex = 0;
            this.MenuStrip.Text = "MenuStrip";
            // 
            // FileToolStripMenuItem
            // 
            this.FileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SaveAsToolStripMenuItem,
            this.ExitToolStripMenuItem});
            this.FileToolStripMenuItem.Enabled = false;
            this.FileToolStripMenuItem.Name = "FileToolStripMenuItem";
            this.FileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.FileToolStripMenuItem.Text = "&File";
            // 
            // SaveAsToolStripMenuItem
            // 
            this.SaveAsToolStripMenuItem.Name = "SaveAsToolStripMenuItem";
            this.SaveAsToolStripMenuItem.Size = new System.Drawing.Size(114, 22);
            this.SaveAsToolStripMenuItem.Text = "Save &As";
            this.SaveAsToolStripMenuItem.Click += new System.EventHandler(this.SaveAsToolStripMenuItem_Click);
            // 
            // ExitToolStripMenuItem
            // 
            this.ExitToolStripMenuItem.Name = "ExitToolStripMenuItem";
            this.ExitToolStripMenuItem.Size = new System.Drawing.Size(114, 22);
            this.ExitToolStripMenuItem.Text = "Exit";
            this.ExitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
            // 
            // TypeToolStripMenuItem
            // 
            this.TypeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.UniqueLettersToolStripMenuItem,
            this.UniqueWordsToolStripMenuItem,
            this.AllWordsToolStripMenuItem,
            this.TypeSeparatorToolStripMenuItem});
            this.TypeToolStripMenuItem.Name = "TypeToolStripMenuItem";
            this.TypeToolStripMenuItem.Size = new System.Drawing.Size(45, 20);
            this.TypeToolStripMenuItem.Text = "Type";
            // 
            // UniqueLettersToolStripMenuItem
            // 
            this.UniqueLettersToolStripMenuItem.Name = "UniqueLettersToolStripMenuItem";
            this.UniqueLettersToolStripMenuItem.Size = new System.Drawing.Size(150, 22);
            this.UniqueLettersToolStripMenuItem.Text = "Unique Letters";
            this.UniqueLettersToolStripMenuItem.Click += new System.EventHandler(this.UniqueLettersToolStripMenuItem_Click);
            // 
            // UniqueWordsToolStripMenuItem
            // 
            this.UniqueWordsToolStripMenuItem.Name = "UniqueWordsToolStripMenuItem";
            this.UniqueWordsToolStripMenuItem.Size = new System.Drawing.Size(150, 22);
            this.UniqueWordsToolStripMenuItem.Text = "Unique Words";
            this.UniqueWordsToolStripMenuItem.Click += new System.EventHandler(this.UniqueWordsToolStripMenuItem_Click);
            // 
            // AllWordsToolStripMenuItem
            // 
            this.AllWordsToolStripMenuItem.Name = "AllWordsToolStripMenuItem";
            this.AllWordsToolStripMenuItem.Size = new System.Drawing.Size(150, 22);
            this.AllWordsToolStripMenuItem.Text = "All Words";
            this.AllWordsToolStripMenuItem.Click += new System.EventHandler(this.AllWordsToolStripMenuItem_Click);
            // 
            // TypeSeparatorToolStripMenuItem
            // 
            this.TypeSeparatorToolStripMenuItem.Name = "TypeSeparatorToolStripMenuItem";
            this.TypeSeparatorToolStripMenuItem.Size = new System.Drawing.Size(147, 6);
            // 
            // RunToolStripMenuItem
            // 
            this.RunToolStripMenuItem.Name = "RunToolStripMenuItem";
            this.RunToolStripMenuItem.Size = new System.Drawing.Size(40, 20);
            this.RunToolStripMenuItem.Text = "Run";
            this.RunToolStripMenuItem.Click += new System.EventHandler(this.RunToolStripMenuItem_Click);
            // 
            // HelpToolStripMenuItem
            // 
            this.HelpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AboutToolStripMenuItem});
            this.HelpToolStripMenuItem.Name = "HelpToolStripMenuItem";
            this.HelpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.HelpToolStripMenuItem.Text = "&Help";
            // 
            // AboutToolStripMenuItem
            // 
            this.AboutToolStripMenuItem.Name = "AboutToolStripMenuItem";
            this.AboutToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.AboutToolStripMenuItem.Text = "&About...";
            this.AboutToolStripMenuItem.Click += new System.EventHandler(this.AboutToolStripMenuItem_Click);
            // 
            // Timer
            // 
            this.Timer.Interval = 1000;
            this.Timer.Tick += new System.EventHandler(this.Timer_Tick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.LavenderBlush;
            this.ClientSize = new System.Drawing.Size(266, 422);
            this.Controls.Add(this.TableLayoutPanel);
            this.Controls.Add(this.StatusStrip);
            this.Controls.Add(this.MenuStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MainMenuStrip = this.MenuStrip;
            this.Name = "MainForm";
            this.Text = "Initial Letters";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
            this.StatusStrip.ResumeLayout(false);
            this.StatusStrip.PerformLayout();
            this.TableLayoutPanel.ResumeLayout(false);
            this.TableLayoutPanel.PerformLayout();
            this.MenuStrip.ResumeLayout(false);
            this.MenuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip StatusStrip;
        private System.Windows.Forms.ToolStripProgressBar ProgressBar;
        private System.Windows.Forms.ToolStripStatusLabel StatusLabel;
        private System.Windows.Forms.ListView ListView;
        private System.Windows.Forms.ColumnHeader ColumnHeader;
        private System.Windows.Forms.TableLayoutPanel TableLayoutPanel;
        private System.Windows.Forms.TextBox LettersTextBox;
        private System.Windows.Forms.SaveFileDialog SaveFileDialog;
        private System.Windows.Forms.MenuStrip MenuStrip;
        private System.Windows.Forms.ToolStripMenuItem FileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem SaveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem HelpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem AboutToolStripMenuItem;
        private System.Windows.Forms.Label ElapsedTimeLabel;
        private System.Windows.Forms.Timer Timer;
        private System.Windows.Forms.ToolStripMenuItem TypeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UniqueLettersToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UniqueWordsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem AllWordsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ExitToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator TypeSeparatorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem RunToolStripMenuItem;
        private System.Windows.Forms.ToolTip ToolTip;
        private System.Windows.Forms.CheckBox DisplaySentencesLiveCheckBox;
    }
}

