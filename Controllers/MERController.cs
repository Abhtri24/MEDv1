
using ChecklistApp.Services;
using ChecklistApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace MEDv1.Controllers
{
	public class MERController : Controller
	{
		private readonly MERTransactionService _transactionService;
		private readonly IChecklistService _checklistService;

		public MERController(
			MERTransactionService transactionService,
			IChecklistService checklistService)
		{
			_transactionService = transactionService;
			_checklistService = checklistService;
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
			await _checklistService.SaveChecklistAsync(checklist);

			TempData["Message"] = "MER checklist saved successfully.";

			return RedirectToAction(
				nameof(Edit),
				new { id = int.Parse(checklist.ContextID) });
		}
	}
}
