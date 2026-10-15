namespace MEDv1.Models;

public class EntryForm
{
	public int ReportResultId { get; set; }
	public int MemberId { get; set; }
	public string MemberName { get; set; } = "";
	public string PackageName { get; set; } = "";
	public string ReportName { get; set; } = "";
	public DateTime? UpdatedOn { get; set; }
	public List<EntryField> Fields { get; set; } = new();
}

public class EntryField
{
	public string TestCode { get; set; } = "";
	public string TestName { get; set; } = "";
	public string DataType { get; set; } = "";
	public string? Unit { get; set; }
	public decimal? MinValue { get; set; }
	public decimal? MaxValue { get; set; }
	public bool IsRequired { get; set; }
	public string? Value { get; set; }
	public string? Error { get; set; }

	public string? InputStyle { get; set; }
	public List<TestOption> Options { get; set; } = new();
}