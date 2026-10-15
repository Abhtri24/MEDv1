using Dapper;
using MEDv1.Abstractions;
using MEDv1.Models;
namespace MEDv1.Data;

public class ReportResultRepository : IReportResultRepository
{
	private readonly SqlConnectionFactory _db;
	public ReportResultRepository(SqlConnectionFactory db) => _db = db;

	public async Task<List<ReportResultSummary>> ForMemberAsync(int memberId, string? search)
	{
		const string sql = @"
            SELECT rr.ReportResultId, rr.ReportCode, r.ReportName, p.PackageName,
                   av.AvailedOn, rr.Status, rr.UpdatedOn
            FROM ReportResult rr
            JOIN PackageAvailment av ON av.AvailmentId = rr.AvailmentId
            JOIN Package p ON p.PackageCode = rr.PackageCode
            JOIN Report  r ON r.ReportCode  = rr.ReportCode
            WHERE av.MemberId = @memberId
              AND (@search IS NULL
                   OR rr.ReportCode LIKE @search + '%'
                   OR r.ReportName  LIKE '%' + @search + '%')
            ORDER BY CASE WHEN rr.Status = 'PENDING' THEN 0 ELSE 1 END,
                     av.AvailedOn DESC,
                     r.ReportName";

		var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

		await using var connection = await _db.OpenAsync();
		var rows = await connection.QueryAsync<ReportResultSummary>(sql, new { memberId, search = term });
		return rows.ToList();
	}

	public async Task<EntryHeader?> GetEntryHeaderAsync(int id)
	{
		const string sql = @"
            SELECT rr.ReportResultId, av.MemberId, m.FullName AS MemberName,
                   p.PackageName, rr.ReportCode, r.ReportName, rr.UpdatedOn
            FROM ReportResult rr
            JOIN PackageAvailment av ON av.AvailmentId = rr.AvailmentId
            JOIN Member  m ON m.MemberId    = av.MemberId
            JOIN Package p ON p.PackageCode = rr.PackageCode
            JOIN Report  r ON r.ReportCode  = rr.ReportCode
            WHERE rr.ReportResultId = @id";

		await using var connection = await _db.OpenAsync();
		return await connection.QuerySingleOrDefaultAsync<EntryHeader>(sql, new { id });
	}

	public async Task<Dictionary<string, SavedValues>> GetValuesAsync(int reportResultId)
	{
		const string sql = @"
            SELECT TestCode, ValueNumber, ValueText, ValueDate
            FROM TestResult
            WHERE ReportResultId = @reportResultId";

		await using var connection = await _db.OpenAsync();
		var rows = await connection.QueryAsync<SavedValues>(sql, new { reportResultId });
		return rows.ToDictionary(v => v.TestCode);
	}

	public async Task SaveValuesAsync(int reportResultId, string reportCode,
	IEnumerable<SavedValues> toSave, IEnumerable<string> toDelete, string updatedBy)
	{
		const string upsertSql = @"
        UPDATE TestResult
        SET ValueNumber = @ValueNumber, ValueText = @ValueText, ValueDate = @ValueDate
        WHERE ReportResultId = @reportResultId AND TestCode = @TestCode;
        IF @@ROWCOUNT = 0
            INSERT INTO TestResult (ReportResultId, TestCode, ReportCode, ValueNumber, ValueText, ValueDate)
            VALUES (@reportResultId, @TestCode, @reportCode, @ValueNumber, @ValueText, @ValueDate);";

		const string deleteSql = @"
        DELETE FROM TestResult
        WHERE ReportResultId = @reportResultId AND TestCode = @TestCode;";

		const string statusSql = @"
        UPDATE ReportResult
        SET Status = 'ENTERED', UpdatedOn = SYSDATETIME(), UpdatedBy = @updatedBy
        WHERE ReportResultId = @reportResultId;";

		var upserts = toSave.Select(v => new { reportResultId, reportCode, v.TestCode, v.ValueNumber, v.ValueText, v.ValueDate });
		var deletes = toDelete.Select(code => new { reportResultId, TestCode = code });

		await using var connection = await _db.OpenAsync();
		await using var transaction = await connection.BeginTransactionAsync();

		await connection.ExecuteAsync(upsertSql, upserts, transaction);
		await connection.ExecuteAsync(deleteSql, deletes, transaction);
		await connection.ExecuteAsync(statusSql, new { reportResultId, updatedBy }, transaction);

		await transaction.CommitAsync();
	}
}