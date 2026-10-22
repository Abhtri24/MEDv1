
using ChecklistApp.Services;
using ChecklistApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace MEDv1.Controllers
{
	public class MERController : Controller
	{
		private readonly MERTransactionService _transactionService;
		private readonly IChecklistService _checklistService;

		private readonly ChecklistWorkflowCoordinator _workflowCoordinator;

		public MERController(
			MERTransactionService transactionService,
			IChecklistService checklistService,
			ChecklistWorkflowCoordinator workflowCoordinator)
		{
			_transactionService = transactionService;
			_checklistService = checklistService;
			_workflowCoordinator = workflowCoordinator;
		}

		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			int tid = await _transactionService
				.GetOrCreateMERTransactionAsync(id);

			ViewBag.MemberId = id;
			ViewBag.TID = tid;

			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SaveChecklist(
			[FromForm] ChecklistPageViewModel checklist)
		{
			await _workflowCoordinator.ProcessAtomicSaveAsync(checklist);

			TempData["Message"] = "MER checklist saved successfully.";

			return RedirectToAction(
				nameof(Edit),
				new { id = int.Parse(checklist.ContextID) });
		}
	}
}
