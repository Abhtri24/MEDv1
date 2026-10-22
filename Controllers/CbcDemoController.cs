
using ChecklistApp.Services;
using ChecklistApp.ViewModels;
using MEDv1.Abstractions;
using MEDv1.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace MEDv1.Controllers;

public class CbcDemoController : Controller
{
	private readonly IEntryFormService _forms;
	private readonly IRequirementService _requirements;
	private readonly CbcDemoTransactionService _transactions;
	private readonly ChecklistWorkflowCoordinator _coordinator;

	public CbcDemoController(
		IEntryFormService forms,
		IRequirementService requirements,
		CbcDemoTransactionService transactions,
		ChecklistWorkflowCoordinator coordinator)
	{
		_forms = forms;
		_requirements = requirements;
		_transactions = transactions;
		_coordinator = coordinator;
	}

	[HttpGet]
	public async Task<IActionResult> Index(int id)
	{
		var model = await BuildPageAsync(id);

		return model == null ? NotFound() : View(model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Next(
	int id,
	[FromForm(Name = "Inputs")] Dictionary<string, string?> inputs)
	{
		var model = await BuildPageAsync(id);

		if (model == null)
			return NotFound();

		model.Inputs = inputs ?? new Dictionary<string, string?>();

		foreach (var field in model.MedicalForm.Fields)
		{
			if (model.Inputs.TryGetValue(field.TestCode, out var value))
				field.Value = value;
		}

		model.ShowChecklist = true;
		return View("Index", model);
	}


	[HttpPost]
	[ValidateAntiForgeryToken]
	[RequestSizeLimit(15 * 1024 * 1024)]
	public async Task<IActionResult> Save(
		[FromForm] CbcAtomicityDemoViewModel model)
	{
		var reportResultId = model.MedicalForm.ReportResultId;

		if (reportResultId <= 0)
			return BadRequest();

		try
		{
			var result = await _coordinator.ProcessMedicalAndChecklistSaveAsync(
				reportResultId,
				model.Inputs ?? new Dictionary<string, string?>(),
				User.Identity?.Name ?? "CBC_DEMO",
				model.Checklist);

			if (result == null)
				return NotFound();

			if (!result.Saved)
			{
				model.MedicalForm = result.Form;
				model.ErrorMessage =
					"Please correct the medical form validation errors.";
				model.ShowChecklist = false;

				return View("Index", model);
			}

			TempData["Message"] =
				"CBC values, checklist responses, and document uploads saved successfully.";

			return RedirectToAction(nameof(Index), new { id = reportResultId });
		}
		catch (InvalidOperationException ex)
		{
			var refreshed = await BuildPageAsync(reportResultId);

			if (refreshed == null)
				return NotFound();

			// Preserve the submitted values and selected file is not possible
			// after a POST; browsers do not allow file inputs to be repopulated.
			refreshed.Inputs = model.Inputs ?? new Dictionary<string, string?>();
			refreshed.MedicalForm.Fields = refreshed.MedicalForm.Fields
				.Select(field =>
				{
					if (refreshed.Inputs.TryGetValue(field.TestCode, out var value))
						field.Value = value;

					return field;
				})
				.ToList();

			refreshed.ErrorMessage = ex.Message;
			refreshed.ShowChecklist = true;

			return View("Index", refreshed);
		}
	}

	private async Task<CbcAtomicityDemoViewModel?> BuildPageAsync(int reportResultId)
	{
		var medicalForm = await _forms.BuildAsync(reportResultId);

		if (medicalForm == null)
			return null;

		var tid = await _transactions.GetOrCreateAsync(reportResultId);
		var contextId = reportResultId.ToString();

		var (items, rules) = await _requirements.GetChecklistItemsAsync(
			tid,
			"CBCReport",
			contextId,
			"CBC_ATOMICITY_DEMO");

		if (items.Count == 0)
			throw new InvalidOperationException(
				"The CBC demo checklist is empty. Verify the seed script and transaction.");

		return new CbcAtomicityDemoViewModel
		{
			MedicalForm = medicalForm,
			Inputs = medicalForm.Fields.ToDictionary(
				field => field.TestCode,
				field => field.Value),
			Checklist = new ChecklistPageViewModel
			{
				TID = tid,
				ContextType = "CBCReport",
				ContextID = contextId,
				ContextCode = "CBC_ATOMICITY_DEMO",
				EmployeeID = User.Identity?.Name ?? "CBC_DEMO",
				Items = items,
				DisplayRules = rules
			}
		};
	}
}
