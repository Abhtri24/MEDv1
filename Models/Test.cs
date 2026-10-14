namespace MEDv1.Models;

public class Test
{
	public string TestCode { get; set; } = "";
	public string ReportCode { get; set; } = "";
	public string TestName { get; set; } = "";
	public string DataType { get; set; } = "";
	public string? Unit { get; set; }
	public decimal? MinValue { get; set; }
	public decimal? MaxValue { get; set; }
	public bool IsRequired { get; set; }
	public int DisplayOrder { get; set; }
}