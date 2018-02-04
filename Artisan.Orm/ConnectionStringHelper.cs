using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using Microsoft.Extensions.Configuration;
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

			if (ConnectionStrings.TryGetValue(connectionStringKey, out var connectionString))
			{
				return connectionString; 
			}
			
			if (IsNullOrWhiteSpace(AppContext.TargetFrameworkName))
				connectionString = GetConnectionStringForNetCore(connectionStringName, activeSolutionConfiguration);
			else
				connectionString = GetConnectionStringForNetFramework(connectionStringName, activeSolutionConfiguration);

			ConnectionStrings.TryAdd(connectionStringKey, connectionString);

			return connectionString;
		}

		private static string GetConnectionStringForNetCore(string connectionStringName, string activeSolutionConfiguration)
		{
			var builder = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");
			//.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
			
			var config = builder.Build();

			string connectionString;
			string tryConnectionStringName;
			var connectionStrings = config.GetSection("ConnectionStrings");

			if (IsNullOrWhiteSpace(activeSolutionConfiguration))
			{
				tryConnectionStringName = $"{MachineName}.{connectionStringName}";
				connectionString = connectionStrings[tryConnectionStringName] ?? connectionStrings[connectionStringName];
			}
			else
			{
				tryConnectionStringName = $"{MachineName}.{activeSolutionConfiguration}.{connectionStringName}";
				connectionString = connectionStrings[tryConnectionStringName];

				if (connectionString == null)
				{
					tryConnectionStringName = $"{MachineName}.{connectionStringName}";
					connectionString = connectionStrings[tryConnectionStringName];

					if (connectionString == null)
					{
						tryConnectionStringName = $"{activeSolutionConfiguration}.{connectionStringName}";
						connectionString = connectionStrings[tryConnectionStringName] ?? connectionStrings[connectionStringName];
					}
				}
			}

			if (connectionString == null)
				throw new SettingsPropertyNotFoundException($"ConnectionString with name '{connectionStringName}' not found");

			return connectionString;
		}


		private static string GetConnectionStringForNetFramework(string connectionStringName, string activeSolutionConfiguration)
		{
			string tryConnectionStringName;
			ConnectionStringSettings settings;

			if (IsNullOrWhiteSpace(activeSolutionConfiguration))
			{
				tryConnectionStringName = $"{MachineName}.{connectionStringName}";
				settings = ConfigurationManager.ConnectionStrings[tryConnectionStringName] ??
							ConfigurationManager.ConnectionStrings[connectionStringName];
			}
			else
			{
				tryConnectionStringName = $"{MachineName}.{activeSolutionConfiguration}.{connectionStringName}";
				settings = ConfigurationManager.ConnectionStrings[tryConnectionStringName];

				if (settings == null)
				{
					tryConnectionStringName = $"{MachineName}.{connectionStringName}";
					settings = ConfigurationManager.ConnectionStrings[tryConnectionStringName];

					if (settings == null)
					{
						tryConnectionStringName = $"{activeSolutionConfiguration}.{connectionStringName}";
						settings = ConfigurationManager.ConnectionStrings[tryConnectionStringName] ??
									ConfigurationManager.ConnectionStrings[connectionStringName];
					}
				}
			}

			if (settings == null)
				throw new SettingsPropertyNotFoundException($"ConnectionString with name '{connectionStringName}' not found");

			return settings.ConnectionString;
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
