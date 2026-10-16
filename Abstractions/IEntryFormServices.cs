using MEDv1.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace MEDv1.Abstractions;

public interface IEntryFormService
{
	Task<EntryForm?> BuildAsync(int reportResultId);
	Task<EntryForm?> BuildByLabReportIdAsync(string labReportId);
	Task<SaveResult?> SaveAsync(int reportResultId, IReadOnlyDictionary<string, string?> inputs, string updatedBy);
}