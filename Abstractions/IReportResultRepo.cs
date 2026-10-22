using MEDv1.Models;
namespace MEDv1.Abstractions;

using Microsoft.Data.SqlClient;

public interface IReportResultRepository
{
	Task<List<ReportResultSummary>> ForMemberAsync(int memberId, string? search);
	Task<EntryHeader?> GetEntryHeaderAsync(int id);
	Task<EntryHeader?> GetEntryHeaderByLabReportIdAsync(
	string labReportId);
	Task<Dictionary<string, SavedValues>> GetValuesAsync(int reportResultId);
	Task SaveValuesAsync(int reportResultId, string reportCode,
						 IEnumerable<SavedValues> toSave, IEnumerable<string> toDelete, string updatedBy, SqlConnection connection,
	SqlTransaction transaction);
}