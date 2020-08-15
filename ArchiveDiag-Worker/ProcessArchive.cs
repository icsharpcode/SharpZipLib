using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ArchiveDiagJson;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using ArchiveDiagApp = ICSharpCode.SharpZipLib.ArchiveDiag.Program;

namespace SharpZipLib.ArchiveDiag.Worker
{
    public static class ArchiveDiagWorker
    {
        [FunctionName("ProcessArchive")]
        public static async Task Run([BlobTrigger("uploads/{name}", Connection = "AzureWebJobsStorage")]Stream blobStream, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {blobStream.Length} Bytes");


			//

			log.LogInformation("Creating reports storage client");
			var reports = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "reports");

			var jobs = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "jobs");
			
			var jobClient = jobs.GetBlobClient(name);

			var job = await jobClient.ExistsAsync()
				? await JsonSerializer.DeserializeAsync<JobStatus>((await jobClient.DownloadAsync()).Value.Content)
				: new JobStatus() {FileName = "Unknown", Status = ""};
				

			await using (var ms = new MemoryStream())
			{
				await JsonSerializer.SerializeAsync(ms, job.WithStatus("processing"));
				ms.Seek(0, SeekOrigin.Begin);
				await jobClient.UploadAsync(ms, overwrite:true);
			}

			var reportTempFile = new FileInfo(Path.GetTempFileName());

			await using (var reportStream = reportTempFile.Create())
			{
				log.LogInformation("Analyzing file...");
				ArchiveDiagApp.Run(blobStream, job.FileName, reportStream);
			}

			await using (var reportStream = reportTempFile.OpenRead())
			{
				log.LogInformation("Uploading results...");
				await reports.UploadBlobAsync($"{name}.html", reportStream);
			}


			blobStream.Close();

			log.LogInformation("Deleting upload blob...");
			var uploads = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "uploads");
			var deleteResult = await uploads.DeleteBlobAsync(name);
			log.LogInformation("Upload deletion result: {Status}, {Reason}", deleteResult.Status, deleteResult.ReasonPhrase);

			await using (var ms = new MemoryStream())
			{
				await JsonSerializer.SerializeAsync(ms, job.WithStatus("done"));
				ms.Seek(0, SeekOrigin.Begin);
				await jobClient.UploadAsync(ms, overwrite: true);
			}

		}
	}
}
