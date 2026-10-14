using Microsoft.AspNetCore.Mvc;
using MEDv1.Data;
using MEDv1.ViewModels;
namespace MEDv1.Controllers;

public class MembersController : Controller
{
	private readonly MemberRepository _members;
	private readonly ReportResultRepository _reportResults;

	public MembersController(MemberRepository members, ReportResultRepository reportResults)
	{
		_members = members;
		_reportResults = reportResults;
	}

	public async Task<IActionResult> Index(string? q)
	{
		var vm = new MemberSearchViewModel { Query = q };
		if (!string.IsNullOrWhiteSpace(q))
			vm.Results = await _members.SearchByNameAsync(q.Trim());
		return View(vm);
	}

	public async Task<IActionResult> Details(int id, string? q)
	{
		var member = await _members.GetByIdAsync(id);
		if (member == null) return NotFound();

		var vm = new MemberReportsViewModel
		{
			MemberId = member.MemberId,
			MemberName = member.FullName,
			Query = q,
			Reports = await _reportResults.ForMemberAsync(id, q)
		};
		return View(vm);
	}
}