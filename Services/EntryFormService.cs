using System.Globalization;
using MEDv1.Abstractions;
using MEDv1.Models;
namespace MEDv1.Services;

public class EntryFormService : IEntryFormService
{
	private readonly IReportResultRepository _results;
	private readonly ITestRepository _tests;

	public EntryFormService(IReportResultRepository results, ITestRepository tests)
	{
		_results = results;
		_tests = tests;
	}

	public async Task<EntryForm?> BuildAsync(int reportResultId)
	{
		var header = await _results.GetEntryHeaderAsync(reportResultId);
		if (header == null) return null;

		var tests = await _tests.ActiveForReportAsync(header.ReportCode);
		var saved = await _results.GetValuesAsync(reportResultId);

		var form = NewForm(header, tests);
		foreach (var field in form.Fields)
			if (saved.TryGetValue(field.TestCode, out var value))
				field.Value = Display(field.DataType, value);
		return form;
	}

	public async Task<SaveResult?> SaveAsync(int reportResultId, IReadOnlyDictionary<string, string?> inputs, string updatedBy)
	{
		var header = await _results.GetEntryHeaderAsync(reportResultId);
		if (header == null) return null;

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

		await _results.SaveValuesAsync(reportResultId, header.ReportCode, outcome.ToSave, outcome.ToDelete, updatedBy);
		return new SaveResult(true, form);
	}

	private static EntryForm NewForm(EntryHeader header, IEnumerable<Test> tests) => new()
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

	private static string? Display(string dataType, SavedValues value) => dataType switch
	{
		"NUMBER" => value.ValueNumber?.ToString("0.####", CultureInfo.InvariantCulture),
		"DATE" => value.ValueDate?.ToString("yyyy-MM-dd"),
		_ => value.ValueText
	};
}