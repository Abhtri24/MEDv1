using MEDv1.Models;
namespace MEDv1.ViewModels;

public class MemberReportsViewModel
{
	public int MemberId { get; set; }
	public string MemberName { get; set; } = "";
	public string? Query { get; set; }

	public string? DiagnosticLab { get; set; }
	public string? LabRemarks { get; set; }
	public List<ReportResultSummary> Reports { get; set; } = new();
}