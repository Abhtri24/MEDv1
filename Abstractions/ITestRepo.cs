using MEDv1.Models;
namespace MEDv1.Abstractions;

public interface ITestRepository
{
	Task<List<Test>> ActiveForReportAsync(string reportCode);
}