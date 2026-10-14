using Microsoft.Data.SqlClient;
namespace MEDv1.Data;

public class SqlConnectionFactory
{
	private readonly string _connectionString;

	public SqlConnectionFactory(IConfiguration config) =>
		_connectionString = config.GetConnectionString("Default")
			?? throw new InvalidOperationException("Connection string 'Default' is missing.");

	public async Task<SqlConnection> OpenAsync()
	{
		var connection = new SqlConnection(_connectionString);
		try
		{
			await connection.OpenAsync();
			return connection;
		}
		catch
		{
			await connection.DisposeAsync();
			throw;
		}
	}
}