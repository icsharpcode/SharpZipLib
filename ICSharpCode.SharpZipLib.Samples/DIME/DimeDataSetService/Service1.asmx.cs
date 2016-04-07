using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Web;
using System.Web.Services;


// using statements necessary for DIME
using Microsoft.Web.Services.Dime;
using Microsoft.Web.Services;
using System.Net;

// using statements necessary for this sample application
using System.IO;
using System.Data.SqlClient;
using ICSharpCode.SharpZipLib.GZip;

namespace DimeDataSetService
{
	/// <summary>
	/// Summary description for Service1.
	/// </summary>
	public class Service1 : System.Web.Services.WebService
	{
		public Service1()
		{
			//CODEGEN: This call is required by the ASP.NET Web Services Designer
			InitializeComponent();
		}

		#region Component Designer generated code
		
		//Required by the Web Services Designer 
		private IContainer components = null;
				
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if(disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);		
		}
		
		#endregion

		[WebMethod]
		public void GetDataSet()
		{
			SoapContext sc = HttpSoapContext.ResponseContext;
			if (null == sc)
			{
				throw new ApplicationException("Only SOAP requests allowed");
			}

			SqlConnection conn = new SqlConnection(@"data source=(local)\NetSDK;" +
				"initial catalog=Northwind;integrated security=SSPI");

			SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM customers", conn);
			DataSet ds = new DataSet("CustomerDS");
			da.Fill(ds, "Customers");
			
			// dispose of all objects that are no longer necessary
			da.Dispose();
			conn.Dispose();

			MemoryStream memoryStream = new MemoryStream(1024);
			GZipOutputStream gzipStream = new GZipOutputStream(memoryStream);
			ds.WriteXml(gzipStream);
			gzipStream.Finish();
			memoryStream.Seek(0, SeekOrigin.Begin);

			DimeAttachment dimeAttachment = new DimeAttachment("application/x-gzip",
				TypeFormatEnum.MediaType, 
				memoryStream);

			sc.Attachments.Add(dimeAttachment);
		}
	}
}
