
using System.Globalization;
using MEDv1.Abstractions;
using MEDv1.Models;
using Microsoft.Data.SqlClient;
using MEDv1.Data;

namespace MEDv1.Services;

public class EntryFormService : IEntryFormService
{
	private readonly IReportResultRepository _results;
	private readonly ITestRepository _tests;
	private readonly SqlConnectionFactory _connectionFactory;

	public EntryFormService(
		IReportResultRepository results,
		ITestRepository tests,
		SqlConnectionFactory connectionFactory)
	{
		_results = results;
		_tests = tests;
		_connectionFactory = connectionFactory;
	}

	public async Task<EntryForm?> BuildAsync(int reportResultId)
	{
		var header = await _results.GetEntryHeaderAsync(reportResultId);

		if (header == null)
			return null;

		return await BuildFromHeaderAsync(header);
	}

	public async Task<EntryForm?> BuildByLabReportIdAsync(string labReportId)
	{
		if (string.IsNullOrWhiteSpace(labReportId))
			return null;

		var header = await _results.GetEntryHeaderByLabReportIdAsync(
			labReportId.Trim());

		if (header == null)
			return null;

		return await BuildFromHeaderAsync(header);
	}

	private async Task<EntryForm> BuildFromHeaderAsync(EntryHeader header)
	{
		var tests = await _tests.ActiveForReportAsync(header.ReportCode);
		var saved = await _results.GetValuesAsync(header.ReportResultId);
		var form = NewForm(header, tests);

		foreach (var field in form.Fields)
		{
			if (saved.TryGetValue(field.TestCode, out var value))
				field.Value = Display(field.DataType, value);
		}

		return form;
	}

	// Existing controller-compatible overload.
	public async Task<SaveResult?> SaveAsync(
		int reportResultId,
		IReadOnlyDictionary<string, string?> inputs,
		string updatedBy)
	{
		await using var connection = await _connectionFactory.OpenAsync();
		await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

		try
		{
			var result = await SaveAsync(
				reportResultId,
				inputs,
				updatedBy,
				connection,
				transaction);

			if (result == null || !result.Saved)
			{
				await transaction.RollbackAsync();
				return result;
			}

			await transaction.CommitAsync();
			return result;
		}
		catch
		{
			try
			{
				await transaction.RollbackAsync();
			}
			catch
			{
				// Preserve the original exception.
			}

			throw;
		}
	}

	// Transaction-aware overload for the shared atomic workflow.
	public async Task<SaveResult?> SaveAsync(
		int reportResultId,
		IReadOnlyDictionary<string, string?> inputs,
		string updatedBy,
		SqlConnection connection,
		SqlTransaction transaction)
	{
		if (connection.State != System.Data.ConnectionState.Open)
			throw new InvalidOperationException("The SQL connection must be open.");

		if (!ReferenceEquals(transaction.Connection, connection))
			throw new InvalidOperationException(
				"The transaction must belong to the supplied SQL connection.");

		var header = await _results.GetEntryHeaderAsync(reportResultId);

		if (header == null)
			return null;

		var tests = await _tests.ActiveForReportAsync(header.ReportCode);
		var outcome = ResultValidator.Validate(tests, inputs);
		var form = NewForm(header, tests);

		if (!outcome.IsValid)
		{
			foreach (var field in form.Fields)
			{
				var result = outcome.For(field.TestCode);
				field.Value = result.Raw;
				field.Error = result.Error;
			}

			return new SaveResult(false, form);
		}

		await _results.SaveValuesAsync(
			reportResultId,
			header.ReportCode,
			outcome.ToSave,
			outcome.ToDelete,
			updatedBy,
			connection,
			transaction);

		return new SaveResult(true, form);
	}

	private static EntryForm NewForm(
		EntryHeader header,
		IEnumerable<Test> tests) => new()
		{
			ReportResultId = header.ReportResultId,
			MemberId = header.MemberId,
			MemberName = header.MemberName,
			PackageName = header.PackageName,
			ReportName = header.ReportName,
			UpdatedOn = header.UpdatedOn,
			Fields = tests.Select(t => new EntryField
			{
				TestCode = t.TestCode,
				TestName = t.TestName,
				DataType = t.DataType,
				Unit = t.Unit,
				MinValue = t.MinValue,
				MaxValue = t.MaxValue,
				IsRequired = t.IsRequired,
				InputStyle = t.InputStyle,
				Options = t.Options
			}).ToList()
		};

	private static string? Display(string dataType, SavedValues value) =>
		dataType switch
		{
			"NUMBER" => value.ValueNumber?.ToString(
				"0.####",
				CultureInfo.InvariantCulture),

			"DATE" => value.ValueDate?.ToString("yyyy-MM-dd"),

			_ => value.ValueText
		};
}
