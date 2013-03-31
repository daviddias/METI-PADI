namespace Puppet_Master
{
    partial class puppetMasterGUI
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
            this.openScriptFile = new System.Windows.Forms.Button();
            this.scriptTextBox = new System.Windows.Forms.TextBox();
            this.runScript = new System.Windows.Forms.Button();
            this.runNextStep = new System.Windows.Forms.Button();
            this.currentStep = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // openScriptFile
            // 
            this.openScriptFile.Location = new System.Drawing.Point(12, 12);
            this.openScriptFile.Name = "openScriptFile";
            this.openScriptFile.Size = new System.Drawing.Size(106, 23);
            this.openScriptFile.TabIndex = 0;
            this.openScriptFile.Text = "Open Script File";
            this.openScriptFile.UseVisualStyleBackColor = true;
            this.openScriptFile.Click += new System.EventHandler(this.openScriptFile_Click);
            // 
            // scriptTextBox
            // 
            this.scriptTextBox.Location = new System.Drawing.Point(124, 12);
            this.scriptTextBox.Multiline = true;
            this.scriptTextBox.Name = "scriptTextBox";
            this.scriptTextBox.Size = new System.Drawing.Size(410, 288);
            this.scriptTextBox.TabIndex = 1;
            // 
            // runScript
            // 
            this.runScript.Location = new System.Drawing.Point(12, 41);
            this.runScript.Name = "runScript";
            this.runScript.Size = new System.Drawing.Size(106, 23);
            this.runScript.TabIndex = 2;
            this.runScript.Text = "Run Script";
            this.runScript.UseVisualStyleBackColor = true;
            // 
            // runNextStep
            // 
            this.runNextStep.Location = new System.Drawing.Point(13, 71);
            this.runNextStep.Name = "runNextStep";
            this.runNextStep.Size = new System.Drawing.Size(105, 23);
            this.runNextStep.TabIndex = 3;
            this.runNextStep.Text = "Run Next Step";
            this.runNextStep.UseVisualStyleBackColor = true;
            this.runNextStep.Click += new System.EventHandler(this.runNextStep_Click);
            // 
            // currentStep
            // 
            this.currentStep.Location = new System.Drawing.Point(543, 12);
            this.currentStep.Name = "currentStep";
            this.currentStep.Size = new System.Drawing.Size(410, 20);
            this.currentStep.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(121, 303);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Script";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(540, 35);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(66, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Current Step";
            // 
            // textBox4
            // 
            this.textBox4.Location = new System.Drawing.Point(543, 51);
            this.textBox4.Multiline = true;
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new System.Drawing.Size(410, 248);
            this.textBox4.TabIndex = 8;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(540, 303);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(39, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Output";
            // 
            // puppetMasterGUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1072, 420);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBox4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.currentStep);
            this.Controls.Add(this.runNextStep);
            this.Controls.Add(this.runScript);
            this.Controls.Add(this.scriptTextBox);
            this.Controls.Add(this.openScriptFile);
            this.Name = "puppetMasterGUI";
            this.Text = "padiFS - Puppet Master";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button openScriptFile;
        private System.Windows.Forms.TextBox scriptTextBox;
        private System.Windows.Forms.Button runScript;
        private System.Windows.Forms.Button runNextStep;
        private System.Windows.Forms.TextBox currentStep;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox4;
        private System.Windows.Forms.Label label3;
    }
}

