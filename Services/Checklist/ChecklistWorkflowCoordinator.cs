
using ChecklistApp.ViewModels;
using MEDv1.Data;
using Microsoft.Data.SqlClient;

namespace ChecklistApp.Services
{
	public class ChecklistWorkflowCoordinator
	{
		private readonly SqlConnectionFactory _connectionFactory;
		private readonly IChecklistService _checklistService;
		private readonly DocumentService _documentService;

		public ChecklistWorkflowCoordinator(
			SqlConnectionFactory connectionFactory,
			IChecklistService checklistService,
			DocumentService documentService)
		{
			_connectionFactory = connectionFactory;
			_checklistService = checklistService;
			_documentService = documentService;
		}

		public async Task ProcessAtomicSaveAsync(
			ChecklistPageViewModel checklist)
		{
			await using var connection = await _connectionFactory.OpenAsync();

			using var transaction = connection.BeginTransaction();

			try
			{
				await ProcessAtomicSaveAsync(
					checklist,
					connection,
					transaction);

				transaction.Commit();
			}
			catch
			{
				try
				{
					transaction.Rollback();
				}
				catch
				{
				}

				throw;
			}
		}

		public async Task ProcessAtomicSaveAsync(
			ChecklistPageViewModel checklist,
			SqlConnection connection,
			SqlTransaction transaction)
		{
			if (connection.State != System.Data.ConnectionState.Open)
			{
				throw new InvalidOperationException(
					"The supplied SQL connection must be open.");
			}

			if (!ReferenceEquals(transaction.Connection, connection))
			{
				throw new InvalidOperationException(
					"The supplied transaction must belong to the supplied connection.");
			}

			await _checklistService.SaveChecklistAsync(
				checklist,
				connection,
				transaction);

			foreach (var item in checklist.Items)
			{
				if (item.File == null || item.File.Length == 0)
				{
					continue;
				}

				if (!item.SelectedOptionID.HasValue)
				{
					throw new InvalidOperationException(
						$"Select a response before uploading a document for QID {item.QID}.");
				}

				await _documentService.SaveUploadAsync(
					item.File,
					checklist.TID,
					item.QID,
					item.SelectedOptionID.Value,
					checklist.EmployeeID,
					connection,
					transaction);
			}
		}
	}
}