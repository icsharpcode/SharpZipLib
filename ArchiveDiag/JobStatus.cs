namespace ArchiveDiagJson
{
	public class JobStatus
	{
		public string FileName { get; set; }

		public string Status { get; set; }

		public JobStatus WithStatus(string status) => new JobStatus() {FileName = FileName, Status = status};
	}
}
