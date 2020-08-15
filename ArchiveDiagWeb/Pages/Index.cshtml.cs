using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using ArchiveDiagJson;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ArchiveDiag = ICSharpCode.SharpZipLib.ArchiveDiag.Program;

namespace ArchiveDiagWeb
{
    public class DefaultModel : PageModel
    {
	    private IWebHostEnvironment _environment;
	    private BlobServiceClient _blobClient;

	    public DefaultModel(IWebHostEnvironment environment, BlobServiceClient blobClient)
		{
			_environment = environment;
			_blobClient = blobClient;
		}

        [BindProperty]
        public IFormFile Upload { get; set; }

        public async Task<JsonResult> OnPostAsync()
        {

	        var guk = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
		        .TrimEnd('=').Replace('+', '-').Replace('/', '_');


			try
			{
				var jobsContainer = _blobClient.GetBlobContainerClient("jobs");
				var jobClient = jobsContainer.GetBlobClient(guk);
				await using (var ms = new MemoryStream())
				{
					await JsonSerializer.SerializeAsync(ms, new JobStatus()
					{
						FileName = Upload.FileName,
						Status = "uploaded",
					});

					ms.Seek(0, SeekOrigin.Begin);

					await jobClient.UploadAsync(ms, overwrite:true);
				}
			}
			catch (Exception x)
			{

			}
			try
			{
				var uploadContainer = _blobClient.GetBlobContainerClient("uploads");

				await uploadContainer.UploadBlobAsync(guk, Upload.OpenReadStream());

				return new JsonResult(new
				{
					result = "uploaded",
					id = guk,
				});
			}
			catch (Exception x)
			{
				return new JsonResult(new
				{
					result = "error",
					error = $"{x}",
				});
			}

			/*
            var reports = new DirectoryInfo(Path.Combine(_environment.WebRootPath, "reports"));
	        reports.Create();



	        var file = new FileInfo(Path.Combine(reports.FullName, $"{guk}_{Upload.FileName}.html"));
	        await using (var fileStream = file.Create())
	        {
		        ArchiveDiag.Run(Upload.OpenReadStream(), Upload.FileName, fileStream);

	        }

	        var urlFileName = HttpUtility.UrlEncode(file.Name).Replace('+', '_');
	        Response.Redirect(Url.Content($"~/reports/{urlFileName}"));
	        */
        }
	}
}
