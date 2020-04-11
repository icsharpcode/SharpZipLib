using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipSFX
{
	public partial class SfxForm : Form
	{
		public SfxForm()
		{
			InitializeComponent();

			tbExtractPath.Text = Path.Combine(Path.GetTempPath(),
				$"{Path.GetFileNameWithoutExtension(Application.ExecutablePath)}-{Path.GetRandomFileName()}");
		}

		private void bExtract_Click(object sender, EventArgs e)
		{
			pbExtractProgress.Visible = true;
			lbStatusLeft.Visible = true;
			lbStatusRight.Visible = true;
			bExtract.Visible = false;
			bCancel.Visible = true;
			pbExtractProgress.Style = ProgressBarStyle.Blocks;
			extractionWorker.RunWorkerAsync(tbExtractPath.Text);

			tbExtractPath.Enabled = false;
		}

		private void bBrowse_Click(object sender, EventArgs e)
		{
			fbdExtractPath.SelectedPath = tbExtractPath.Text;
			if (fbdExtractPath.ShowDialog(this) == DialogResult.OK)
			{
				tbExtractPath.Text = fbdExtractPath.SelectedPath;
			}
		}

		private void extractionWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			if (!(e.Argument is string outputRoot)) return;

			Directory.CreateDirectory(outputRoot);

			using (var exeStream = File.OpenRead(Application.ExecutablePath))
			using (var zip = new ZipFile(exeStream))
			{
				var fileCount = zip.Count;

				foreach (ZipEntry entry in zip)
				{
					if (extractionWorker.CancellationPending) return;
					var entryIndex = entry.ZipFileIndex;
					var percentage = (int)((entry.ZipFileIndex / (float)fileCount) * 100);

					var status = new ExtractStatus(entry.Name, entry.ZipFileIndex, fileCount);

					extractionWorker.ReportProgress(percentage, status);

					var outputFile = Path.GetFullPath(Path.Combine(outputRoot, entry.Name));
					if (outputFile.StartsWith(outputRoot, StringComparison.InvariantCultureIgnoreCase))
					{
						using (var outputStream = File.Open(outputFile, FileMode.Create))
						using (var inputStream = zip.GetInputStream(entryIndex))
						{
							inputStream.CopyTo(outputStream);
						}
					}
				}

				extractionWorker.ReportProgress(100, new ExtractStatus("Completed!", fileCount, fileCount));
			}
		}

		private void extractionWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			pbExtractProgress.Value = e.ProgressPercentage;
			if(e.UserState is ExtractStatus es)
			{
				lbStatusLeft.Text = $"File: {es.name}";
				lbStatusRight.Text = $"{es.zipFileIndex} / {es.fileCount})";

			}
		}

		private void extractionWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if(e.Cancelled)
			{
				lbStatusLeft.Text = "Cancelled";
			} 
			else if (e.Error != null)
			{
				MessageBox.Show(this, e.ToString());
			}
			else
			{
				if(cbOpenAfterExtract.Checked)
				{
					Process.Start("explorer", tbExtractPath.Text);
				}
			}

			bExtract.Visible = true;
			tbExtractPath.Enabled = true;
			bCancel.Visible = false;
		}

		private void bCancel_Click(object sender, EventArgs e) => extractionWorker.CancelAsync();
	}

	internal struct ExtractStatus
	{
		public string name;
		public long zipFileIndex;
		public long fileCount;

		public ExtractStatus(string name, long zipFileIndex, long fileCount)
		{
			this.name = name;
			this.zipFileIndex = zipFileIndex;
			this.fileCount = fileCount;
		}
	}
}
