using Dapper;
using MEDv1.Models;
namespace MEDv1.Data;

public class TestRepository
{
	private readonly SqlConnectionFactory _db;
	public TestRepository(SqlConnectionFactory db) => _db = db;

	public async Task<List<Test>> ActiveForReportAsync(string reportCode)
	{
		const string sql = @"
            SELECT TestCode, ReportCode, TestName, DataType, Unit,
                   MinValue, MaxValue, IsRequired, DisplayOrder
            FROM Test
            WHERE ReportCode = @reportCode AND IsActive = 1
            ORDER BY DisplayOrder, TestName";

		await using var connection = await _db.OpenAsync();
		var rows = await connection.QueryAsync<Test>(sql, new { reportCode });
		return rows.ToList();
	}
}