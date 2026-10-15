using Dapper;
using MEDv1.Models;
namespace MEDv1.Data;

using MEDv1.Abstractions;

public class MemberRepository : IMemberRepository
{
	private readonly SqlConnectionFactory _db;
	public MemberRepository(SqlConnectionFactory db) => _db = db;

	public async Task<List<Member>> SearchByNameAsync(string name, int max = 20)
	{
		const string sql = @"
            SELECT TOP (@max) MemberId, FullName
            FROM Member
            WHERE FullName LIKE '%' + @name + '%'
            ORDER BY FullName";

		await using var connection = await _db.OpenAsync();
		var rows = await connection.QueryAsync<Member>(sql, new { name, max });
		return rows.ToList();
	}

	public async Task<Member?> GetByIdAsync(int id)
	{
		const string sql = "SELECT MemberId, FullName FROM Member WHERE MemberId = @id";

		await using var connection = await _db.OpenAsync();
		return await connection.QuerySingleOrDefaultAsync<Member>(sql, new { id });
	}
}	