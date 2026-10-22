
using System.Data;
using Dapper;
using MEDv1.Data;

namespace ChecklistApp.Services;

public class CbcDemoTransactionService
{
	private const string DefinitionCode = "MEDV1_CBC_ATOMICITY_DEMO";
	private const string ContextType = "CBCReport";
	private const string ContextCode = "CBC_ATOMICITY_DEMO";

	private readonly SqlConnectionFactory _connectionFactory;

	public CbcDemoTransactionService(SqlConnectionFactory connectionFactory)
	{
		_connectionFactory = connectionFactory;
	}

	public async Task<int> GetOrCreateAsync(int reportResultId)
	{
		if (reportResultId <= 0)
			throw new ArgumentOutOfRangeException(nameof(reportResultId));

		await using var connection = await _connectionFactory.OpenAsync();
		using var transaction =
			connection.BeginTransaction(IsolationLevel.Serializable);

		try
		{
			var contextId = reportResultId.ToString();

			var existingTid = await connection.QuerySingleOrDefaultAsync<int?>(
				"""
                SELECT TID
                FROM dbo.ChecklistTransactions WITH (UPDLOCK, HOLDLOCK)
                WHERE ContextType = @ContextType
                  AND ContextID = @ContextID
                  AND ContextCode = @ContextCode;
                """,
				new { ContextType, ContextID = contextId, ContextCode },
				transaction);

			if (existingTid.HasValue)
			{
				transaction.Commit();
				return existingTid.Value;
			}

			var definitionExists = await connection.ExecuteScalarAsync<int>(
				"""
                SELECT COUNT(1)
                FROM dbo.ChecklistDefinitions
                WHERE DefinitionCode = @DefinitionCode
                  AND IsActive = 1;
                """,
				new { DefinitionCode },
				transaction);

			if (definitionExists == 0)
				throw new InvalidOperationException(
					"The CBC demo checklist definition is missing or inactive. Run the seed script first.");

			var qids = (await connection.QueryAsync<int>(
				"""
                SELECT QID
                FROM dbo.ChecklistDefinitionRequirements
                WHERE DefinitionCode = @DefinitionCode;
                """,
				new { DefinitionCode },
				transaction)).ToList();

			if (qids.Count == 0)
				throw new InvalidOperationException(
					"The CBC demo checklist has no configured questions.");

			var nextTid = await connection.ExecuteScalarAsync<long>(
				"SELECT NEXT VALUE FOR dbo.ChecklistTransactionIdSequence;",
				transaction: transaction);

			var tid = checked((int)nextTid);

			await connection.ExecuteAsync(
				"""
                INSERT INTO dbo.ChecklistTransactions
                    (TID, ContextType, ContextID, ContextCode,
                     DefinitionCode, Status, CreatedBy)
                VALUES
                    (@TID, @ContextType, @ContextID, @ContextCode,
                     @DefinitionCode, 'InProgress', @CreatedBy);
                """,
				new
				{
					TID = tid,
					ContextType,
					ContextID = contextId,
					ContextCode,
					DefinitionCode,
					CreatedBy = "CBC_DEMO"
				},
				transaction);

			await connection.ExecuteAsync(
				"""
                INSERT INTO dbo.ChecklistTransactionRequirements (TID, QID)
                SELECT @TID, QID
                FROM dbo.ChecklistDefinitionRequirements
                WHERE DefinitionCode = @DefinitionCode;
                """,
				new { TID = tid, DefinitionCode },
				transaction);

			transaction.Commit();
			return tid;
		}
		catch
		{
			try { transaction.Rollback(); } catch { }
			throw;
		}
	}
}
