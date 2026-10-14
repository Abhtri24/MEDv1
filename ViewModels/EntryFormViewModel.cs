namespace MEDv1.ViewModels;

    public class EntryFormViewModel
    {
		public int ReportResultId { get; set; }
		public int MemberId { get; set; }
		public string MemberName { get; set; } = "";
		public string PackageName { get; set; } = "";
		public string ReportName { get; set; } = "";
		public DateTime? UpdatedOn { get; set; }
		public List<FieldViewModel> Fields { get; set; } = new();
	}

	public class FieldViewModel
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
	}

