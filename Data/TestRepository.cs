using Dapper;
using MEDv1.Abstractions;
using MEDv1.Models;
namespace MEDv1.Data;

public class TestRepository : ITestRepository
{
	private readonly SqlConnectionFactory _db;
	public TestRepository(SqlConnectionFactory db) => _db = db;

	public async Task<List<Test>> ActiveForReportAsync(string reportCode)
	{
		const string testSql = @"
            SELECT TestCode, ReportCode, TestName, DataType, InputStyle, Unit,
                   MinValue, MaxValue, IsRequired, DisplayOrder
            FROM Test
            WHERE ReportCode = @reportCode AND IsActive = 1
            ORDER BY DisplayOrder, TestName";

		const string optionSql = @"
            SELECT o.TestCode, o.OptionCode, o.OptionLabel
            FROM TestOption o
            JOIN Test t ON t.TestCode = o.TestCode
            WHERE t.ReportCode = @reportCode AND t.IsActive = 1 AND o.IsActive = 1
            ORDER BY o.DisplayOrder, o.OptionLabel";

		await using var connection = await _db.OpenAsync();
		var tests = (await connection.QueryAsync<Test>(testSql, new { reportCode })).ToList();
		var options = (await connection.QueryAsync<TestOption>(optionSql, new { reportCode }))
					  .ToLookup(o => o.TestCode);

		foreach (var test in tests)
			test.Options = options[test.TestCode].ToList();

		return tests;
	}
}