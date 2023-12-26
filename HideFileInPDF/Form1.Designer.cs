namespace FormMain
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.EmbedButton = new System.Windows.Forms.Button();
            this.DecodeButton = new System.Windows.Forms.Button();
            this.DataToEmbedTextBox = new System.Windows.Forms.TextBox();
            this.labelEmbed = new System.Windows.Forms.Label();
            this.labelDecode = new System.Windows.Forms.Label();
            this.OutputTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // EmbedButton
            // 
            this.EmbedButton.Location = new System.Drawing.Point(502, 82);
            this.EmbedButton.Name = "EmbedButton";
            this.EmbedButton.Size = new System.Drawing.Size(94, 29);
            this.EmbedButton.TabIndex = 0;
            this.EmbedButton.Text = "Embed";
            this.EmbedButton.UseVisualStyleBackColor = true;
            this.EmbedButton.Click += new System.EventHandler(this.EmbedButton_Click);
            // 
            // DecodeButton
            // 
            this.DecodeButton.Location = new System.Drawing.Point(502, 220);
            this.DecodeButton.Name = "DecodeButton";
            this.DecodeButton.Size = new System.Drawing.Size(94, 29);
            this.DecodeButton.TabIndex = 1;
            this.DecodeButton.Text = "Decode";
            this.DecodeButton.UseVisualStyleBackColor = true;
            this.DecodeButton.Click += new System.EventHandler(this.DecodeButton_Click);
            // 
            // DataToEmbedTextBox
            // 
            this.DataToEmbedTextBox.Location = new System.Drawing.Point(233, 82);
            this.DataToEmbedTextBox.Name = "DataToEmbedTextBox";
            this.DataToEmbedTextBox.Size = new System.Drawing.Size(219, 27);
            this.DataToEmbedTextBox.TabIndex = 2;
            // 
            // labelEmbed
            // 
            this.labelEmbed.AutoSize = true;
            this.labelEmbed.Location = new System.Drawing.Point(105, 91);
            this.labelEmbed.Name = "labelEmbed";
            this.labelEmbed.Size = new System.Drawing.Size(90, 20);
            this.labelEmbed.TabIndex = 3;
            this.labelEmbed.Text = "Embed Text:";
            // 
            // labelDecode
            // 
            this.labelDecode.AutoSize = true;
            this.labelDecode.Location = new System.Drawing.Point(105, 229);
            this.labelDecode.Name = "labelDecode";
            this.labelDecode.Size = new System.Drawing.Size(95, 20);
            this.labelDecode.TabIndex = 5;
            this.labelDecode.Text = "Decode Text:";
            // 
            // OutputTextBox
            // 
            this.OutputTextBox.Location = new System.Drawing.Point(233, 220);
            this.OutputTextBox.Name = "OutputTextBox";
            this.OutputTextBox.Size = new System.Drawing.Size(219, 27);
            this.OutputTextBox.TabIndex = 4;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.labelDecode);
            this.Controls.Add(this.OutputTextBox);
            this.Controls.Add(this.labelEmbed);
            this.Controls.Add(this.DataToEmbedTextBox);
            this.Controls.Add(this.DecodeButton);
            this.Controls.Add(this.EmbedButton);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button EmbedButton;
        private Button DecodeButton;
        private TextBox DataToEmbedTextBox;
        private Label labelEmbed;
        private Label labelDecode;
        private TextBox OutputTextBox;
    }
}