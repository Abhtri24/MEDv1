
using Dapper;
using MEDv1.Data;
using System.Data;

namespace ChecklistApp.Services
{
	public class MERTransactionService
	{
		private readonly SqlConnectionFactory _connectionFactory;

		public MERTransactionService(SqlConnectionFactory connectionFactory)
		{
			_connectionFactory = connectionFactory;
		}

		public async Task<int> GetOrCreateMERTransactionAsync(int memberId)
		{
			await using var connection = await _connectionFactory.OpenAsync();
			using var transaction =
				connection.BeginTransaction(IsolationLevel.Serializable);

			try
			{
				const string contextType = "Member";
				string contextId = memberId.ToString();
				const string contextCode = "MER";
				const string definitionCode = "MEDV1_MER";

				var existingTid = await connection.QuerySingleOrDefaultAsync<int?>(
					"""
                    SELECT TID
                    FROM dbo.ChecklistTransactions WITH (UPDLOCK, HOLDLOCK)
                    WHERE ContextType = @ContextType
                      AND ContextID = @ContextID
                      AND ContextCode = @ContextCode;
                    """,
					new
					{
						ContextType = contextType,
						ContextID = contextId,
						ContextCode = contextCode
					},
					transaction);

				if (existingTid.HasValue)
				{
					await transaction.CommitAsync();
					return existingTid.Value;
				}

				var definitionExists = await connection.ExecuteScalarAsync<int>(
					"""
                    SELECT COUNT(1)
                    FROM dbo.ChecklistDefinitions
                    WHERE DefinitionCode = @DefinitionCode
                      AND IsActive = 1;
                    """,
					new { DefinitionCode = definitionCode },
					transaction);

				if (definitionExists == 0)
					throw new InvalidOperationException(
						"The MER checklist definition is missing or inactive.");

				var qids = (await connection.QueryAsync<int>(
					"""
                    SELECT QID
                    FROM dbo.ChecklistDefinitionRequirements
                    WHERE DefinitionCode = @DefinitionCode
                    ORDER BY QID;
                    """,
					new { DefinitionCode = definitionCode },
					transaction)).ToList();

				if (qids.Count == 0)
					throw new InvalidOperationException(
						"No requirements are configured for the MER checklist.");

				long nextTid = await connection.ExecuteScalarAsync<long>(
					"SELECT NEXT VALUE FOR dbo.ChecklistTransactionIdSequence;",
					transaction: transaction);

				int tid = checked((int)nextTid);

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
						ContextType = contextType,
						ContextID = contextId,
						ContextCode = contextCode,
						DefinitionCode = definitionCode,
						CreatedBy = "EMP001"
					},
					transaction);

				await connection.ExecuteAsync(
					"""
                    INSERT INTO dbo.ChecklistTransactionRequirements (TID, QID)
                    SELECT @TID, QID
                    FROM dbo.ChecklistDefinitionRequirements
                    WHERE DefinitionCode = @DefinitionCode;
                    """,
					new
					{
						TID = tid,
						DefinitionCode = definitionCode
					},
					transaction);

				await transaction.CommitAsync();
				return tid;
			}
			catch
			{
				await transaction.RollbackAsync();
				throw;
			}
		}
	}
}
