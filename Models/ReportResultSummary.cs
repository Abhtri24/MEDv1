namespace MEDv1.Models;

public class ReportResultSummary
{
	public int ReportResultId { get; set; }
	public string ReportCode { get; set; } = "";
	public string ReportName { get; set; } = "";
	public string PackageName { get; set; } = "";
	public DateTime AvailedOn { get; set; }
	public string Status { get; set; } = "";
	public DateTime? UpdatedOn { get; set; }
}