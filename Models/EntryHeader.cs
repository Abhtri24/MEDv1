namespace MEDv1.Models  

    // holds context shown above the form
{
    public class EntryHeader
    {
        public int ReportResultId {get; set;}
        public int MemberId {get; set;}

        public string MemberName { get; set; } = "";

        public string PackageName { get; set; } = "";

        public string ReportCode { get; set; } = "";

        public string ReportName { get; set; } = "";

		public DateTime? UpdatedOn { get; set; }



	}
}
