namespace ZipSFX
{
    partial class SfxForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SfxForm));
            this.lExtractPath = new System.Windows.Forms.Label();
            this.tbExtractPath = new System.Windows.Forms.TextBox();
            this.bExtract = new System.Windows.Forms.Button();
            this.bBrowse = new System.Windows.Forms.Button();
            this.fbdExtractPath = new System.Windows.Forms.FolderBrowserDialog();
            this.extractionWorker = new System.ComponentModel.BackgroundWorker();
            this.pbExtractProgress = new System.Windows.Forms.ProgressBar();
            this.lbStatusLeft = new System.Windows.Forms.Label();
            this.bCancel = new System.Windows.Forms.Button();
            this.lbStatusRight = new System.Windows.Forms.Label();
            this.cbOpenAfterExtract = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // lExtractPath
            // 
            this.lExtractPath.AutoSize = true;
            this.lExtractPath.Location = new System.Drawing.Point(12, 9);
            this.lExtractPath.Name = "lExtractPath";
            this.lExtractPath.Size = new System.Drawing.Size(67, 13);
            this.lExtractPath.TabIndex = 0;
            this.lExtractPath.Text = "Extract path:";
            // 
            // tbExtractPath
            // 
            this.tbExtractPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbExtractPath.Location = new System.Drawing.Point(15, 25);
            this.tbExtractPath.Name = "tbExtractPath";
            this.tbExtractPath.Size = new System.Drawing.Size(329, 20);
            this.tbExtractPath.TabIndex = 1;
            // 
            // bExtract
            // 
            this.bExtract.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bExtract.Location = new System.Drawing.Point(310, 113);
            this.bExtract.Name = "bExtract";
            this.bExtract.Size = new System.Drawing.Size(115, 36);
            this.bExtract.TabIndex = 2;
            this.bExtract.Text = "Extract";
            this.bExtract.UseVisualStyleBackColor = true;
            this.bExtract.Click += new System.EventHandler(this.bExtract_Click);
            // 
            // bBrowse
            // 
            this.bBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bBrowse.Location = new System.Drawing.Point(350, 23);
            this.bBrowse.Name = "bBrowse";
            this.bBrowse.Size = new System.Drawing.Size(75, 23);
            this.bBrowse.TabIndex = 3;
            this.bBrowse.Text = "Browse...";
            this.bBrowse.UseVisualStyleBackColor = true;
            this.bBrowse.Click += new System.EventHandler(this.bBrowse_Click);
            // 
            // extractionWorker
            // 
            this.extractionWorker.WorkerReportsProgress = true;
            this.extractionWorker.WorkerSupportsCancellation = true;
            this.extractionWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.extractionWorker_DoWork);
            this.extractionWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.extractionWorker_ProgressChanged);
            this.extractionWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.extractionWorker_RunWorkerCompleted);
            // 
            // pbExtractProgress
            // 
            this.pbExtractProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbExtractProgress.Location = new System.Drawing.Point(12, 113);
            this.pbExtractProgress.MarqueeAnimationSpeed = 50;
            this.pbExtractProgress.Name = "pbExtractProgress";
            this.pbExtractProgress.Size = new System.Drawing.Size(292, 36);
            this.pbExtractProgress.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.pbExtractProgress.TabIndex = 4;
            this.pbExtractProgress.Visible = false;
            // 
            // lbStatusLeft
            // 
            this.lbStatusLeft.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbStatusLeft.Location = new System.Drawing.Point(12, 97);
            this.lbStatusLeft.Name = "lbStatusLeft";
            this.lbStatusLeft.Size = new System.Drawing.Size(222, 13);
            this.lbStatusLeft.TabIndex = 5;
            this.lbStatusLeft.Text = "Extracting: {File}";
            this.lbStatusLeft.Visible = false;
            // 
            // bCancel
            // 
            this.bCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bCancel.Location = new System.Drawing.Point(310, 113);
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new System.Drawing.Size(115, 36);
            this.bCancel.TabIndex = 6;
            this.bCancel.Text = "Cancel";
            this.bCancel.UseVisualStyleBackColor = true;
            this.bCancel.Visible = false;
            this.bCancel.Click += new System.EventHandler(this.bCancel_Click);
            // 
            // lbStatusRight
            // 
            this.lbStatusRight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lbStatusRight.Location = new System.Drawing.Point(240, 97);
            this.lbStatusRight.Name = "lbStatusRight";
            this.lbStatusRight.Size = new System.Drawing.Size(64, 13);
            this.lbStatusRight.TabIndex = 7;
            this.lbStatusRight.Text = "{Status}";
            this.lbStatusRight.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.lbStatusRight.Visible = false;
            // 
            // cbOpenAfterExtract
            // 
            this.cbOpenAfterExtract.AutoSize = true;
            this.cbOpenAfterExtract.Checked = true;
            this.cbOpenAfterExtract.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbOpenAfterExtract.Location = new System.Drawing.Point(15, 52);
            this.cbOpenAfterExtract.Name = "cbOpenAfterExtract";
            this.cbOpenAfterExtract.Size = new System.Drawing.Size(179, 17);
            this.cbOpenAfterExtract.TabIndex = 8;
            this.cbOpenAfterExtract.Text = "Open target path after extraction";
            this.cbOpenAfterExtract.UseVisualStyleBackColor = true;
            // 
            // SfxForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(437, 161);
            this.Controls.Add(this.cbOpenAfterExtract);
            this.Controls.Add(this.lbStatusRight);
            this.Controls.Add(this.bCancel);
            this.Controls.Add(this.lbStatusLeft);
            this.Controls.Add(this.pbExtractProgress);
            this.Controls.Add(this.bBrowse);
            this.Controls.Add(this.bExtract);
            this.Controls.Add(this.tbExtractPath);
            this.Controls.Add(this.lExtractPath);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(99999, 200);
            this.MinimumSize = new System.Drawing.Size(0, 200);
            this.Name = "SfxForm";
            this.Text = "Self-extractor";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

		#endregion

		private System.Windows.Forms.Label lExtractPath;
		private System.Windows.Forms.TextBox tbExtractPath;
		private System.Windows.Forms.Button bExtract;
		private System.Windows.Forms.Button bBrowse;
		private System.Windows.Forms.FolderBrowserDialog fbdExtractPath;
		private System.ComponentModel.BackgroundWorker extractionWorker;
		private System.Windows.Forms.ProgressBar pbExtractProgress;
		private System.Windows.Forms.Label lbStatusLeft;
		private System.Windows.Forms.Button bCancel;
		private System.Windows.Forms.Label lbStatusRight;
		private System.Windows.Forms.CheckBox cbOpenAfterExtract;
	}
}

