using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;

using Microsoft.Web.Services;
using Microsoft.Web.Services.Dime;
using ICSharpCode.SharpZipLib.GZip;

namespace DimeDataSetServiceConsumer
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class Form1 : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button cmdLoadDataset;
		private System.Windows.Forms.DataGrid dg;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public Form1()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.cmdLoadDataset = new System.Windows.Forms.Button();
			this.dg = new System.Windows.Forms.DataGrid();
			((System.ComponentModel.ISupportInitialize)(this.dg)).BeginInit();
			this.SuspendLayout();
			// 
			// cmdLoadDataset
			// 
			this.cmdLoadDataset.Location = new System.Drawing.Point(8, 16);
			this.cmdLoadDataset.Name = "cmdLoadDataset";
			this.cmdLoadDataset.TabIndex = 0;
			this.cmdLoadDataset.Text = "load dataset";
			this.cmdLoadDataset.Click += new System.EventHandler(this.cmdLoadDataset_Click);
			// 
			// dg
			// 
			this.dg.DataMember = "";
			this.dg.HeaderForeColor = System.Drawing.SystemColors.ControlText;
			this.dg.Location = new System.Drawing.Point(8, 48);
			this.dg.Name = "dg";
			this.dg.Size = new System.Drawing.Size(592, 296);
			this.dg.TabIndex = 1;
			// 
			// Form1
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(608, 349);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.dg,
																		  this.cmdLoadDataset});
			this.Name = "Form1";
			this.Text = "Demo Client";
			((System.ComponentModel.ISupportInitialize)(this.dg)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new Form1());
		}

		private void cmdLoadDataset_Click(object sender, System.EventArgs e)
		{
			// call the service
			localhost.Service1 svc = new localhost.Service1();
			svc.GetDataSet();

			// get the SOAP context, see if there are attachments
			SoapContext sc = svc.ResponseSoapContext;
			if (1 != sc.Attachments.Count)
			{
				MessageBox.Show("Error: # of attachments received is not the # that was expected");
				return;
			}

			GZipInputStream gzipInputStream = new GZipInputStream(sc.Attachments[0].Stream);
			MemoryStream ms = new MemoryStream(1024);
			int nSize = 2048;
			byte[] writeData = new byte[2048];

			while (true) 
			{
				nSize = gzipInputStream.Read(writeData, 0, nSize);
				if (nSize > 0) 
					ms.Write(writeData, 0, nSize);
				else 
					break;
			}
			
			ms.Seek(0, SeekOrigin.Begin);

			DataSet ds = new DataSet();
			ds.ReadXml(ms);
			dg.SetDataBinding(ds, "Customers");
		}
	}
}
