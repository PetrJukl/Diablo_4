namespace Diablo_4
{
    partial class WeekendMotivation
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WeekendMotivation));
            this.WeekLabel1 = new System.Windows.Forms.Label();
            this.StartGameLabel = new System.Windows.Forms.Label();
            this.YesBtn = new System.Windows.Forms.Button();
            this.NoBtn = new System.Windows.Forms.Button();
            this.gamesComboBox = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // WeekLabel1
            // 
            this.WeekLabel1.BackColor = System.Drawing.Color.Transparent;
            this.WeekLabel1.Font = new System.Drawing.Font("Arial Black", 28F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.WeekLabel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.WeekLabel1.Location = new System.Drawing.Point(25, 453);
            this.WeekLabel1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.WeekLabel1.Name = "WeekLabel1";
            this.WeekLabel1.Size = new System.Drawing.Size(698, 78);
            this.WeekLabel1.TabIndex = 0;
            this.WeekLabel1.Text = "Těch 10 hodin přeci zvládneme.";
            // 
            // StartGameLabel
            // 
            this.StartGameLabel.AutoSize = true;
            this.StartGameLabel.BackColor = System.Drawing.Color.Transparent;
            this.StartGameLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.StartGameLabel.ForeColor = System.Drawing.Color.Black;
            this.StartGameLabel.Location = new System.Drawing.Point(18, 541);
            this.StartGameLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.StartGameLabel.Name = "StartGameLabel";
            this.StartGameLabel.Size = new System.Drawing.Size(240, 31);
            this.StartGameLabel.TabIndex = 1;
            this.StartGameLabel.Text = "Chceš si zapařit?";
            // 
            // YesBtn
            // 
            this.YesBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.YesBtn.Location = new System.Drawing.Point(494, 553);
            this.YesBtn.Margin = new System.Windows.Forms.Padding(2);
            this.YesBtn.Name = "YesBtn";
            this.YesBtn.Size = new System.Drawing.Size(70, 26);
            this.YesBtn.TabIndex = 2;
            this.YesBtn.Text = "Ano";
            this.YesBtn.UseVisualStyleBackColor = true;
            this.YesBtn.Click += new System.EventHandler(this.YesBtn_Click);
            // 
            // NoBtn
            // 
            this.NoBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.NoBtn.Location = new System.Drawing.Point(566, 553);
            this.NoBtn.Margin = new System.Windows.Forms.Padding(2);
            this.NoBtn.Name = "NoBtn";
            this.NoBtn.Size = new System.Drawing.Size(70, 26);
            this.NoBtn.TabIndex = 3;
            this.NoBtn.Text = "Ne";
            this.NoBtn.UseVisualStyleBackColor = true;
            this.NoBtn.Click += new System.EventHandler(this.NoBtn_Click);
            // 
            // gamesComboBox
            // 
            this.gamesComboBox.FormattingEnabled = true;
            this.gamesComboBox.Location = new System.Drawing.Point(275, 553);
            this.gamesComboBox.Name = "gamesComboBox";
            this.gamesComboBox.Size = new System.Drawing.Size(190, 21);
            this.gamesComboBox.TabIndex = 4;
            // 
            // WeekendMotivation
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(224)))), ((int)(((byte)(192)))));
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(814, 606);
            this.Controls.Add(this.gamesComboBox);
            this.Controls.Add(this.NoBtn);
            this.Controls.Add(this.YesBtn);
            this.Controls.Add(this.StartGameLabel);
            this.Controls.Add(this.WeekLabel1);
            this.ForeColor = System.Drawing.Color.Black;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "WeekendMotivation";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "WeekendMotivation";
            this.Load += new System.EventHandler(this.WeekendMotivation_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label WeekLabel1;
        private System.Windows.Forms.Label StartGameLabel;
        private System.Windows.Forms.Button YesBtn;
        private System.Windows.Forms.Button NoBtn;
        private System.Windows.Forms.ComboBox gamesComboBox;
    }
}