using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using MEDv1.Data;
using MEDv1.Models;
using MEDv1.ViewModels;
namespace MEDv1.Controllers;

public class EntryController : Controller
{
	private readonly ReportResultRepository _reportResults;
	private readonly TestRepository _tests;

	public EntryController(ReportResultRepository reportResults, TestRepository tests)
	{
		_reportResults = reportResults;
		_tests = tests;
	}

	[HttpGet]
	public async Task<IActionResult> Form(int id)
	{
		var vm = await BuildFormAsync(id);
		if (vm == null) return NotFound();
		return View(vm);
	}

	private async Task<EntryFormViewModel?> BuildFormAsync(int id)
	{
		var header = await _reportResults.GetEntryHeaderAsync(id);
		if (header == null) return null;

		var tests = await _tests.ActiveForReportAsync(header.ReportCode);
		var saved = await _reportResults.GetValuesAsync(id);

		return new EntryFormViewModel
		{
			ReportResultId = header.ReportResultId,
			MemberId = header.MemberId,
			MemberName = header.MemberName,
			PackageName = header.PackageName,
			ReportName = header.ReportName,
			UpdatedOn = header.UpdatedOn,
			Fields = tests.Select(t => ToField(t, saved.GetValueOrDefault(t.TestCode))).ToList()
		};
	}

	private static FieldViewModel ToField(Test test, SavedValues? saved) => new()
	{
		TestCode = test.TestCode,
		TestName = test.TestName,
		DataType = test.DataType,
		Unit = test.Unit,
		MinValue = test.MinValue,
		MaxValue = test.MaxValue,
		IsRequired = test.IsRequired,
		Value = saved == null ? null : test.DataType switch
		{
			"NUMBER" => saved.ValueNumber?.ToString("0.####", CultureInfo.InvariantCulture),
			"DATE" => saved.ValueDate?.ToString("yyyy-MM-dd"),
			_ => saved.ValueText
		}
	};
}