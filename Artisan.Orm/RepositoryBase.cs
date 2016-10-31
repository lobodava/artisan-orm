using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Artisan.Orm
{
	public class RepositoryBase: IDisposable
	{
		public SqlConnection Connection { get; private set; }

		public string ConnectionString { get; private set; }
		public SqlTransaction Transaction { get; set; }
		
		public RepositoryBase()
		{
			ConnectionString = ConnectionStringHelper.GetConnectionString();

			Connection = new SqlConnection(ConnectionString);

			Transaction = null;
		}
		
		public RepositoryBase(string connectionString, string activeSolutionConfiguration = null)
			: this(null, connectionString, activeSolutionConfiguration) {}

		public RepositoryBase(SqlTransaction transaction, string connectionString, string activeSolutionConfiguration = null)
		{
			if (connectionString.Contains(";") && connectionString.Contains("="))
				ConnectionString = connectionString;
			else
				ConnectionString = ConnectionStringHelper.GetConnectionString(connectionString, activeSolutionConfiguration);

			Connection = new SqlConnection(ConnectionString);

			Transaction = transaction;
		}
		

		public SqlTransaction BeginTransaction() {
			if (Connection.State == ConnectionState.Closed) 
				Connection.Open();

			Transaction = Connection.BeginTransaction();
			return Transaction;
		}
		public void CommitTransaction() {
			Transaction.Commit();
			Connection.Close();
		}

		public void RollbackTransaction() {
			Transaction.Rollback();
			Connection.Close();
		}

		public SqlCommand CreateCommand()
		{
			var command = Connection.CreateCommand();

			if (Transaction != null)
				command.Transaction = Transaction;

			return command;
		}


		/// <summary> 
		/// <para/>Prepares SqlCommand and pass it to a Func-parameter.
		/// <para/>Parameter "func" is the code where SqlCommand has to be configured with parameters, execute reader and return result. 
		/// </summary>
		public T GetByCommand<T>(Func<SqlCommand, T> func)
		{
			using (var cmd = CreateCommand())
			{
				return func(cmd);
			}
		}

		/// <summary> 
		/// <para/>Prepares SqlCommand and pass it to a Func-parameter.
		/// <para/>Parameter "func" is the code where SqlCommand has to be configured with parameters, execute reader and return result. 
		/// </summary>
		public async Task<T> GetByCommandAsync<T>(Func<SqlCommand, Task<T>> funcAsync )
		{
			using (var cmd = CreateCommand())
			{
				return await funcAsync(cmd).ConfigureAwait(false);
			}
		}
		
		/// <summary> 
		/// <para/>Executes SqlCommand which returns nothing but ReturnValue.
		/// <para/>Calls ExecuteNonQueryAsync inside.
		/// <para/>Parameter "action" is the code where SqlCommand has to be configured with parameters. 
		/// <para/>Returns ReturnValue - the value from TSQL "RETURN [Value]" statement. If there is no RETURN in TSQL then returns 0.
		/// </summary>
		public Int32 ExecuteCommand (Action<SqlCommand> action)
		{
			using (var cmd = CreateCommand())
			{
				var returnValueParam = cmd.ReturnValueParam();
				var isConnectionClosed = true;

				try
				{
					action(cmd);

					isConnectionClosed = cmd.Connection.State == ConnectionState.Closed;

					if (isConnectionClosed)
						cmd.Connection.Open();

					cmd.ExecuteNonQuery();
				}
				finally
				{
					if (isConnectionClosed)
						cmd.Connection.Close();
				}

				return (int)returnValueParam.Value;
			}
		}


		/// <summary> 
		/// <para/>Executes SqlCommand which returns nothing but ReturnValue.
		/// <para/>Calls ExecuteNonQueryAsync inside.
		/// <para/>Parameter "action" is the code where SqlCommand has to be configured with parameters. 
		/// <para/>Returns ReturnValue - the value from TSQL "RETURN [Value]" statement. If there is no RETURN in TSQL then returns 0.
		/// </summary>
		public async Task<Int32> ExecuteCommandAsync (Action<SqlCommand> action)
		{
			using (var cmd = CreateCommand())
			{
				var returnValueParam = cmd.ReturnValueParam();
				var isConnectionClosed = true;

				try
				{
					action(cmd);

					isConnectionClosed = cmd.Connection.State == ConnectionState.Closed;

					if (isConnectionClosed)
						cmd.Connection.Open();

					await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
				}
				finally
				{
					if (isConnectionClosed)
						cmd.Connection.Close();
				}

				return (int)returnValueParam.Value;
			}
		}


		public void RunCommand(Action<SqlCommand> action)
		{
			using (var cmd = CreateCommand())
			{
				action(cmd);
			}
		}
		
		public async Task RunCommandAsync(Action<SqlCommand> action)
		{
			await Task.Run(() =>
			{
				using (var cmd = CreateCommand())
				{
					action(cmd);
				}

			}).ConfigureAwait(false);
		}


		public static DataStatus? GetDataStatus (string dataStatusCode) {

			if (String.IsNullOrWhiteSpace(dataStatusCode))
				return null;
				//throw new InvalidEnumArgumentException("Cannot cast empty string to DataStatus Enum");
			
				if (!Enum.IsDefined(typeof(DataStatus), dataStatusCode))
				throw new InvalidCastException($"Cannot cast string '{dataStatusCode}' to DataStatus Enum");

			return (DataStatus)Enum.Parse(typeof(DataStatus), dataStatusCode);
		}


		
		public void Dispose()
		{
			Transaction?.Dispose();

			Connection?.Dispose();
		}
	}
}



		