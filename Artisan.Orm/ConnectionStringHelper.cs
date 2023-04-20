using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using static System.String;

namespace Artisan.Orm;

public static class ConnectionStringHelper
{
	private static readonly ConcurrentDictionary<string, string> ConnectionStrings = new ();
	private static readonly string MachineName = Environment.MachineName.ToUpper();

	public static string GetConnectionString
	(
		string connectionStringName = "DatabaseConnection", 
		string activeSolutionConfiguration = null, 
		string connectionStringsSectionName = "ConnectionStrings",
		string jsonSettingsFileRelativePath = "appsettings.json")
	{
		string connectionStringKey = $"{jsonSettingsFileRelativePath}:{MachineName}.{activeSolutionConfiguration ?? "null"}.{connectionStringName}";

		if (ConnectionStrings.TryGetValue(connectionStringKey, out var connectionString))
		{
			return connectionString; 
		}

		connectionString = ResolveConnectionString(connectionStringName, activeSolutionConfiguration, connectionStringsSectionName, jsonSettingsFileRelativePath);

		ConnectionStrings.TryAdd(connectionStringKey, connectionString);

		return connectionString;
	}

	private static string ResolveConnectionString
	(
		string connectionStringName,
		string activeSolutionConfiguration,
		string connectionStringsSectionName,
		string jsonSettingsFileRelativePath
	)
	{
		var builder = new ConfigurationBuilder().AddJsonFile(jsonSettingsFileRelativePath);
		
		var config = builder.Build();

		string connectionString;
		string tryConnectionStringName;
		var connectionStrings = config.GetSection(connectionStringsSectionName);

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
			throw new AppSettingsPropertyNotFoundException($"ConnectionString with name '{connectionStringName}' not found");

		return connectionString;
	}

	public static string GetServerName(string connectionString)
	{
		var builder = new SqlConnectionStringBuilder {ConnectionString = connectionString};
		
		return builder.DataSource;
	}

	public static string GetDatabaseName(string connectionString)
	{
		var builder = new SqlConnectionStringBuilder {ConnectionString = connectionString};
		
		return builder.InitialCatalog;
	}
}
