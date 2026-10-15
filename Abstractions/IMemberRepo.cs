using MEDv1.Models;
namespace MEDv1.Abstractions;

public interface IMemberRepository
{
	Task<List<Member>> SearchByNameAsync(string name, int max = 20);
	Task<Member?> GetByIdAsync(int id);
}