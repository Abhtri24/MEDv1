
using Microsoft.AspNetCore.Mvc;
using MEDv1.Abstractions;

namespace MEDv1.Controllers;

public class EntryController : Controller
{
	private readonly IEntryFormService _forms;

	public EntryController(IEntryFormService forms) => _forms = forms;

	[HttpGet]
	public IActionResult Lookup()
	{
		return View();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Lookup(string labReportId)
	{
		if (string.IsNullOrWhiteSpace(labReportId))
		{
			ViewData["Error"] = "Please enter a lab report ID.";
			return View();
		}

		var form = await _forms.BuildByLabReportIdAsync(labReportId.Trim());

		if (form == null)
		{
			ViewData["Error"] =
				"No report was found for this lab report ID. Please check the ID and try again.";

			return View();
		}

		return View("Form", form);
	}

	[HttpGet]
	public async Task<IActionResult> Form(int id)
	{
		var form = await _forms.BuildAsync(id);

		if (form == null)
			return NotFound();

		return View(form);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Form(
		int id,
		[FromForm(Name = "Inputs")] Dictionary<string, string?> inputs)
	{
		var result = await _forms.SaveAsync(
			id,
			inputs ?? new(),
			User.Identity?.Name ?? "staff");

		if (result == null)
			return NotFound();

		if (!result.Saved)
			return View(result.Form);

		TempData["Message"] =
			$"{result.Form.ReportName} saved for {result.Form.MemberName}.";

		return RedirectToAction(
			"Details",
			"Members",
			new { id = result.Form.MemberId });
	}
}
