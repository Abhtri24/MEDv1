
using ChecklistApp.ViewModels;
using MEDv1.Abstractions;
using MEDv1.Data;
using MEDv1.Models;
using Microsoft.Data.SqlClient;

namespace ChecklistApp.Services
{
	public class ChecklistWorkflowCoordinator
	{
		private readonly SqlConnectionFactory _connectionFactory;
		private readonly IChecklistService _checklistService;
		private readonly DocumentService _documentService;
		private readonly IEntryFormService _entryFormService;

		public ChecklistWorkflowCoordinator(
			SqlConnectionFactory connectionFactory,
			IChecklistService checklistService,
			DocumentService documentService,
			IEntryFormService entryFormService)
		{
			_connectionFactory = connectionFactory;
			_checklistService = checklistService;
			_documentService = documentService;
			_entryFormService = entryFormService;
		}

		// Existing checklist-only workflow.
		public async Task ProcessAtomicSaveAsync(
			ChecklistPageViewModel checklist)
		{
			await using var connection = await _connectionFactory.OpenAsync();
			await using var transaction =
				(SqlTransaction)await connection.BeginTransactionAsync();

			try
			{
				await ProcessAtomicSaveAsync(
					checklist, connection, transaction);

				await transaction.CommitAsync();
			}
			catch
			{
				try { await transaction.RollbackAsync(); }
				catch { /* Preserve original exception. */ }

				throw;
			}
		}

		// Existing checklist-only shared-transaction method.
		public async Task ProcessAtomicSaveAsync(
			ChecklistPageViewModel checklist,
			SqlConnection connection,
			SqlTransaction transaction)
		{
			ValidateTransaction(connection, transaction);

			await _checklistService.SaveChecklistAsync(
				checklist, connection, transaction);

			await SaveUploadsAsync(checklist, connection, transaction);
		}

		// New combined medical-form + checklist + upload workflow.
		public async Task<SaveResult?> ProcessMedicalAndChecklistSaveAsync(
			int reportResultId,
			IReadOnlyDictionary<string, string?> inputs,
			string updatedBy,
			ChecklistPageViewModel checklist)
		{
			await using var connection = await _connectionFactory.OpenAsync();
			await using var transaction =
				(SqlTransaction)await connection.BeginTransactionAsync();

			try
			{
				ValidateTransaction(connection, transaction);

				var result = await _entryFormService.SaveAsync(
					reportResultId,
					inputs,
					updatedBy,
					connection,
					transaction);

				if (result == null)
					throw new InvalidOperationException(
						"The medical report could not be found.");

				if (!result.Saved)
				{
					await transaction.RollbackAsync();
					return result;
				}

				await _checklistService.SaveChecklistAsync(
					checklist, connection, transaction);

				await SaveUploadsAsync(
					checklist, connection, transaction);

				await transaction.CommitAsync();
				return result;
			}
			catch
			{
				try { await transaction.RollbackAsync(); }
				catch { /* Preserve original exception. */ }

				throw;
			}
		}

		private async Task SaveUploadsAsync(
			ChecklistPageViewModel checklist,
			SqlConnection connection,
			SqlTransaction transaction)
		{
			foreach (var item in checklist.Items)
			{
				if (item.File == null || item.File.Length == 0)
					continue;

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

		private static void ValidateTransaction(
			SqlConnection connection,
			SqlTransaction transaction)
		{
			if (connection.State != System.Data.ConnectionState.Open)
				throw new InvalidOperationException(
					"The supplied SQL connection must be open.");

			if (!ReferenceEquals(transaction.Connection, connection))
				throw new InvalidOperationException(
					"The transaction must belong to the supplied connection.");
		}
	}
}
