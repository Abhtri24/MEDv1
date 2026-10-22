using ChecklistApp.ViewModels;
using Microsoft.Data.SqlClient;

namespace ChecklistApp.Services
{
	public interface IChecklistService
	{
		Task SaveChecklistAsync(ChecklistPageViewModel checklist);

		Task SaveChecklistAsync(
			ChecklistPageViewModel checklist,
			SqlConnection connection,
			SqlTransaction transaction);
	}
}