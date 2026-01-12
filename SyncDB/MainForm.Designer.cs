namespace SyncDB
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
            this.txtBackupPath = new System.Windows.Forms.TextBox();
            this.txtRemotePath = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.chkIgnoreExisting = new System.Windows.Forms.CheckBox();
            this.btStart = new System.Windows.Forms.Button();
            this.btStop = new System.Windows.Forms.Button();
            this.btTest = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtBackupPath
            // 
            this.txtBackupPath.Location = new System.Drawing.Point(16, 34);
            this.txtBackupPath.Name = "txtBackupPath";
            this.txtBackupPath.Size = new System.Drawing.Size(320, 20);
            this.txtBackupPath.TabIndex = 0;
            // 
            // txtRemotePath
            // 
            this.txtRemotePath.Location = new System.Drawing.Point(16, 73);
            this.txtRemotePath.Name = "txtRemotePath";
            this.txtRemotePath.Size = new System.Drawing.Size(320, 20);
            this.txtRemotePath.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(154, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Đường dẫn tới thư mục backup";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 57);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(61, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Tên remote";
            // 
            // chkIgnoreExisting
            // 
            this.chkIgnoreExisting.AutoSize = true;
            this.chkIgnoreExisting.Location = new System.Drawing.Point(16, 99);
            this.chkIgnoreExisting.Name = "chkIgnoreExisting";
            this.chkIgnoreExisting.Size = new System.Drawing.Size(87, 17);
            this.chkIgnoreExisting.TabIndex = 2;
            this.chkIgnoreExisting.Text = "Bỏ qua trùng";
            this.chkIgnoreExisting.UseVisualStyleBackColor = true;
            // 
            // btStart
            // 
            this.btStart.Location = new System.Drawing.Point(225, 99);
            this.btStart.Name = "btStart";
            this.btStart.Size = new System.Drawing.Size(111, 23);
            this.btStart.TabIndex = 3;
            this.btStart.Text = "Start";
            this.btStart.UseVisualStyleBackColor = true;
            this.btStart.Click += new System.EventHandler(this.btStart_Click);
            // 
            // btStop
            // 
            this.btStop.Location = new System.Drawing.Point(225, 128);
            this.btStop.Name = "btStop";
            this.btStop.Size = new System.Drawing.Size(111, 23);
            this.btStop.TabIndex = 3;
            this.btStop.Text = "Stop";
            this.btStop.UseVisualStyleBackColor = true;
            this.btStop.Click += new System.EventHandler(this.btStop_Click);
            // 
            // btTest
            // 
            this.btTest.Location = new System.Drawing.Point(144, 99);
            this.btTest.Name = "btTest";
            this.btTest.Size = new System.Drawing.Size(75, 23);
            this.btTest.TabIndex = 4;
            this.btTest.Text = "Test";
            this.btTest.UseVisualStyleBackColor = true;
            this.btTest.Click += new System.EventHandler(this.btTest_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(348, 181);
            this.Controls.Add(this.btTest);
            this.Controls.Add(this.btStop);
            this.Controls.Add(this.btStart);
            this.Controls.Add(this.chkIgnoreExisting);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtRemotePath);
            this.Controls.Add(this.txtBackupPath);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "Backup db";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtBackupPath;
        private System.Windows.Forms.TextBox txtRemotePath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox chkIgnoreExisting;
        private System.Windows.Forms.Button btStart;
        private System.Windows.Forms.Button btStop;
        private System.Windows.Forms.Button btTest;
    }
}

