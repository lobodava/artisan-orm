using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data.SqlClient;
using static System.String;

namespace Artisan.Orm
{
	public static class ConnectionStringHelper
	{
		private static readonly ConcurrentDictionary<string, string> ConnectionStrings = new ConcurrentDictionary<string, string>();
		private static readonly string  MachineName = Environment.MachineName.ToUpper();

		public static string GetConnectionString(string connectionStringName = null, string activeSolutionConfiguration = null)
		{
			if (IsNullOrWhiteSpace(connectionStringName))
			{
				connectionStringName = "DatabaseConnection";
			}
			
			string connectionStringKey = $"{MachineName}.{activeSolutionConfiguration ?? "null"}.{connectionStringName}";
			
			string connectionString;

			if (ConnectionStrings.TryGetValue(connectionStringKey, out connectionString))
			{
				return connectionString; 
			}
			
			string tryConnectionStringName;
			ConnectionStringSettings settings = null;


			if (IsNullOrWhiteSpace(activeSolutionConfiguration))
			{
				tryConnectionStringName = $"{MachineName}.{connectionStringName}";
				settings =	ConfigurationManager.ConnectionStrings[tryConnectionStringName] ??
							ConfigurationManager.ConnectionStrings[connectionStringName];
			}
			else
			{
				tryConnectionStringName = $"{MachineName}.{activeSolutionConfiguration}.{connectionStringName}";
				settings =	ConfigurationManager.ConnectionStrings[tryConnectionStringName];

				if (settings == null) 
				{
					tryConnectionStringName = $"{MachineName}.{connectionStringName}";
					settings =	ConfigurationManager.ConnectionStrings[tryConnectionStringName];

					if (settings == null) 
					{
						tryConnectionStringName = $"{activeSolutionConfiguration}.{connectionStringName}";
						settings =	ConfigurationManager.ConnectionStrings[tryConnectionStringName] ??
									ConfigurationManager.ConnectionStrings[connectionStringName];
					}
				}
			}

			if (settings == null) 
				throw new ConnectionStringNotFoundException($"ConnectionString with name '{connectionStringName}' not found");


			//https://msdn.microsoft.com/ru-ru/library/hh211418(v=vs.110).aspx
			//Starting .NET Framework 4,5, there is no need to add Asynchronous Processing=true into connection string.

			//_connectionString = new SqlConnectionStringBuilder(settings.ConnectionString) {
			//	AsynchronousProcessing = true
			//}.ToString();

			connectionString = settings.ConnectionString;

			ConnectionStrings.TryAdd(connectionStringKey, connectionString);

			return connectionString;
		}

		public static string GetServerName(string connectionString)
		{
			var builder = new SqlConnectionStringBuilder  {ConnectionString = connectionString};
			
			return builder.DataSource;
		} 

		public static string GetDatabaseName(string connectionString)
		{
			var builder = new SqlConnectionStringBuilder  {ConnectionString = connectionString};
			
			return builder.InitialCatalog;
		}
	}
}
