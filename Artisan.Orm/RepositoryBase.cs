using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
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

		public void BeginTransaction(IsolationLevel isolationLevel, Action<SqlTransaction> action)
		{
			var isConnectionClosed = Connection.State == ConnectionState.Closed;

			if (isConnectionClosed) 
				Connection.Open();

			Transaction = Connection.BeginTransaction(isolationLevel);


			try
			{
				action(Transaction);
			}
			catch 
			{
				Transaction.Rollback();
				throw;
			}
			finally
			{
				Transaction?.Dispose();
				Transaction = null;

				if(isConnectionClosed)
					Connection.Close();
			}

		}

		public void BeginTransaction(Action<SqlTransaction> action)
		{
			BeginTransaction(IsolationLevel.Unspecified, action);
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

		/// <summary>
		/// <para>Creates SqlCommand, passes it to Action argument as SqlCommand parameter, returns nothing.</para>
		/// <para>See GitHub Wiki about this method: <a href="https://github.com/lobodava/artisan-orm/wiki/RepositoryBase-methods-for-SqlCommand-initialization#runcommand">https://github.com/lobodava/artisan-orm/wiki/RepositoryBase-methods-for-SqlCommand-initialization#runcommand</a></para>
		/// </summary>
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


		#region [ ReadTo, ReadAs ]
		
		public T ReadTo<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using (var cmd = CreateCommand())
			{
				cmd.ConfigureCommand(sql, sqlParameters);
				
				return cmd.ReadTo<T>();
			}
		}
		
		public async Task<T> ReadToAsync<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using (var cmd = CreateCommand())
			{
				cmd.ConfigureCommand(sql, sqlParameters);
				
				return await cmd.ReadToAsync<T>();
			}
		}


		public T ReadAs<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using (var cmd = CreateCommand())
			{
				cmd.ConfigureCommand(sql, sqlParameters);
				
				return cmd.ReadAs<T>();
			}
		}
		
		public async Task<T> ReadAsAsync<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using (var cmd = CreateCommand())
			{
				cmd.ConfigureCommand(sql, sqlParameters);
				
				return await cmd.ReadAsAsync<T>();
			}
		}

		#endregion

		#region [ ReadToList, ReadAsList ]

		public IList<T> ReadToList<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using (var cmd = CreateCommand())
			{
				cmd.ConfigureCommand(sql, sqlParameters);
				
				return cmd.ReadToList<T>();
			}
		}
		
		public async Task<IList<T>> ReadToListAsync<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using (var cmd = CreateCommand())
			{
				cmd.ConfigureCommand(sql, sqlParameters);
				
				return await cmd.ReadToListAsync<T>();
			}
		}

		public IList<T> ReadAsList<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using (var cmd = CreateCommand())
			{
				cmd.ConfigureCommand(sql, sqlParameters);
				
				return cmd.ReadAsList<T>();
			}
		}
		
		public async Task<IList<T>> ReadAsListAsync<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using (var cmd = CreateCommand())
			{
				cmd.ConfigureCommand(sql, sqlParameters);
				
				return await cmd.ReadAsListAsync<T>();
			}
		}
		
		#endregion

		#region [ ReadToObjectRow, ReadAsObjectRow ]

		public ObjectRow ReadToObjectRow<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using (var cmd = CreateCommand())
			{
				cmd.ConfigureCommand(sql, sqlParameters);
				
				return cmd.ReadToObjectRow<T>();
			}
		}
		
		public async Task<ObjectRow> ReadToObjectRowAsync<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using (var cmd = CreateCommand())
			{
				cmd.ConfigureCommand(sql, sqlParameters);
				
				return await cmd.ReadToObjectRowAsync<T>();
			}
		}
		
		public ObjectRow ReadAsObjectRow(string sql, params SqlParameter[] sqlParameters)
		{
			using (var cmd = CreateCommand())
			{
				cmd.ConfigureCommand(sql, sqlParameters);
				
				return cmd.ReadAsObjectRow();
			}
		}
		
		public async Task<ObjectRow> ReadAsObjectRowAsync(string sql, params SqlParameter[] sqlParameters)
		{
			using (var cmd = CreateCommand())
			{
				cmd.ConfigureCommand(sql, sqlParameters);
				
				return await cmd.ReadAsObjectRowAsync();
			}
		}
		
		#endregion

		#region [ ReadToObjectRows, ReadAsObjectRows ]

		public ObjectRows ReadToObjectRows<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using (var cmd = CreateCommand())
			{
				cmd.ConfigureCommand(sql, sqlParameters);
				
				return cmd.ReadToObjectRows<T>();
			}
		}
		
		public async Task<ObjectRows> ReadToObjectRowsAsync<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using (var cmd = CreateCommand())
			{
				cmd.ConfigureCommand(sql, sqlParameters);
				
				return await cmd.ReadToObjectRowsAsync<T>();
			}
		}
		
		public ObjectRows ReadAsObjectRows(string sql, params SqlParameter[] sqlParameters)
		{
			using (var cmd = CreateCommand())
			{
				cmd.ConfigureCommand(sql, sqlParameters);
				
				return cmd.ReadAsObjectRows();
			}
		}
		
		public async Task<ObjectRows> ReadAsObjectRowsAsync(string sql, params SqlParameter[] sqlParameters)
		{
			using (var cmd = CreateCommand())
			{
				cmd.ConfigureCommand(sql, sqlParameters);
				
				return await cmd.ReadAsObjectRowsAsync();
			}
		}


		#endregion
		
		#region [ ReadToDictionary ]

		public IDictionary<TKey, TValue> ReadToDictionary<TKey, TValue>(string sql, params SqlParameter[] sqlParameters) 
		{
			using (var cmd = CreateCommand())
			{
				cmd.ConfigureCommand(sql, sqlParameters);
				
				return cmd.ReadToDictionary<TKey, TValue>();
			}
		}

		public async Task<IDictionary<TKey, TValue>> ReadToDictionaryAsync<TKey, TValue>(string sql, params SqlParameter[] sqlParameters) 
		{
			using (var cmd = CreateCommand())
			{
				cmd.ConfigureCommand(sql, sqlParameters);
				
				return await cmd.ReadToDictionaryAsync<TKey, TValue>();
			}
		}

		#endregion 

		#region [ ReadToEnumerable ]

		public IEnumerable<T> ReadToEnumerable<T>(string sql, params SqlParameter[] sqlParameters)
		{
			using (var cmd = CreateCommand())
			{
				cmd.ConfigureCommand(sql, sqlParameters);
				
				return cmd.ReadToEnumerable<T>();
			}
		}

		#endregion




		[Obsolete("Replaced with ParseDataStatus")]
		public static DataStatus? GetDataStatus (string dataStatusCode) {

			if (String.IsNullOrWhiteSpace(dataStatusCode))
				return null;
		
				if (!Enum.IsDefined(typeof(DataStatus), dataStatusCode))
				throw new InvalidCastException($"Cannot cast string '{dataStatusCode}' to DataStatus Enum");

			return (DataStatus)Enum.Parse(typeof(DataStatus), dataStatusCode);
		}

		public static DataStatus? ParseDataStatus(string dataStatusCode)
		{
			return DataReply.ParseDataStatus(dataStatusCode);
		}
		
		public void Dispose()
		{
			Transaction?.Dispose();

			Connection?.Dispose();
		}
	}
}



		