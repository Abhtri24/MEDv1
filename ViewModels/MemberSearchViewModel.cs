using MEDv1.Models;
namespace MEDv1.ViewModels;
// controller fills this file and view reads it 
public class MemberSearchViewModel
{
	public string? Query { get; set; }
	public List<Member> Results { get; set; } = new();
}