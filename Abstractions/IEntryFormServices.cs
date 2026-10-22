using MEDv1.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
namespace MEDv1.Abstractions;

public interface IEntryFormService
{
	Task<EntryForm?> BuildAsync(int reportResultId);
	Task<EntryForm?> BuildByLabReportIdAsync(string labReportId);
	Task<SaveResult?> SaveAsync(
	int reportResultId,
	IReadOnlyDictionary<string, string?> inputs,
	string updatedBy);

	Task<SaveResult?> SaveAsync(
		int reportResultId,
		IReadOnlyDictionary<string, string?> inputs,
		string updatedBy,
		SqlConnection connection,
		SqlTransaction transaction);
}