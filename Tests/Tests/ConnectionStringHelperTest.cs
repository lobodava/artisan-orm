using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Artisan.Orm;
using static System.String;

namespace Tests.Tests
{
	[TestClass]
	public class ConnectionStringHelperTest
	{
		private static readonly string MachineName = Environment.MachineName.ToUpper();
		private static readonly string CurrentConfigurationName = GetCurrentConfigurationName();
		private const string TestAppsettingJsonFileName = "test.appsettings.json";

		[TestMethod]
		public void ConnectionStringNotFoundException()
		{
			string connectionString = null;

			try
			{
				connectionString = ConnectionStringHelper.GetConnectionString("qwertyuiopasdfghjklzxcvbnm");

				Assert.Fail();
			} 
			catch(AppSettingsPropertyNotFoundException)
			{
				connectionString = Empty;
			}
			catch(Exception ex)
			{
				Assert.Fail();
			}

			Assert.IsTrue(connectionString == Empty);
		}
		
		[TestMethod]
		public void DefaultDatabaseConnection()
		{
			var connectionString = ConnectionStringHelper.GetConnectionString();

			Assert.IsNotNull(connectionString);
			Assert.AreEqual(ConnectionStringHelper.GetDatabaseName(connectionString), "Artisan");

		}

		[TestMethod]
		public void MachineConnectionString()
		{
			var testAppsettingJsonFileName = "machine.appsettings.json";

			CreateTestAppsettingJson( new
			{
				ConnectionStrings = new Dictionary<string, string>
				{	
					{ $"DatabaseConnection", "Data Source=.\\SQLEXPRESS;Initial Catalog=ArtisanDb;Integrated Security=True;" },
					{ $"{MachineName}.DatabaseConnection", $"Data Source=.\\SQLEXPRESS;Initial Catalog={MachineName}Db;Integrated Security=True;" },
					
				}
			}, testAppsettingJsonFileName);

			var connectionString = ConnectionStringHelper.GetConnectionString(jsonSettingsFileRelativePath: testAppsettingJsonFileName);

			Assert.IsNotNull(connectionString);
			Assert.AreEqual($"{MachineName}Db", ConnectionStringHelper.GetDatabaseName(connectionString));
		}

		[TestMethod]
		public void MachineDebugConnectionString()
		{
			var testAppsettingJsonFileName = "machine.configuration.appsettings.json";

			CreateTestAppsettingJson( new
			{
				ConnectionStrings = new Dictionary<string, string>
				{	
					{ $"DatabaseConnection", "Data Source=.\\SQLEXPRESS;Initial Catalog=ArtisanDb;Integrated Security=True;" },
					{ $"{MachineName}.DatabaseConnection", $"Data Source=.\\SQLEXPRESS;Initial Catalog={MachineName}Db;Integrated Security=True;" },
					{ $"{MachineName}.{CurrentConfigurationName}.DatabaseConnection", $"Data Source=.\\SQLEXPRESS;Initial Catalog={MachineName}{CurrentConfigurationName}Db;Integrated Security=True;" },
				}
			}, testAppsettingJsonFileName);

			var connectionString = ConnectionStringHelper.GetConnectionString(activeSolutionConfiguration: CurrentConfigurationName, jsonSettingsFileRelativePath: testAppsettingJsonFileName);

			Assert.IsNotNull(connectionString);
			Assert.AreEqual($"{MachineName}{CurrentConfigurationName}Db", ConnectionStringHelper.GetDatabaseName(connectionString));
		}

		[TestMethod]
		public void SolutionConfigurationConnectionString()
		{
			var testAppsettingJsonFileName = "configuration.appsettings.json";

			CreateTestAppsettingJson( new
			{
				ConnectionStrings = new Dictionary<string, string>
				{
					{ $"{CurrentConfigurationName}.DatabaseConnection", $"Data Source=.\\SQLEXPRESS;Initial Catalog={CurrentConfigurationName}Db;Integrated Security=True;" },
					{ $"DatabaseConnection", "Data Source=.\\SQLEXPRESS;Initial Catalog=ArtisanDb;Integrated Security=True;" }
				}
			}, testAppsettingJsonFileName);

			var connectionString = ConnectionStringHelper.GetConnectionString(jsonSettingsFileRelativePath: testAppsettingJsonFileName);

			Assert.IsNotNull(connectionString);
			Assert.AreEqual("ArtisanDb", ConnectionStringHelper.GetDatabaseName(connectionString));

			connectionString = ConnectionStringHelper.GetConnectionString(activeSolutionConfiguration: CurrentConfigurationName, jsonSettingsFileRelativePath: testAppsettingJsonFileName);
			
			Assert.IsNotNull(connectionString);
			Assert.AreEqual($"{CurrentConfigurationName}Db", ConnectionStringHelper.GetDatabaseName(connectionString));
		}


		[TestMethod]
		public void ConfigurationManagerStopwatch()
		{
			var sw = new Stopwatch();
			string connectionString = null;

			sw.Start();
			for (var i = 0; i < 10000; i++)
			{
				connectionString = ConnectionStringHelper.GetConnectionString("DatabaseConnection");
			}
			sw.Stop();

			Assert.IsNotNull(connectionString);
			Console.WriteLine("ConfigurationManager Stopwatch (10,000 times) = " + new TimeSpan(sw.Elapsed.Ticks).TotalMilliseconds + " ms");
		}


		[TestMethod]
		public void GetConnectionStringStopwatch()
		{
			var sw = new Stopwatch();
			string connectionString = null;

			sw.Start();
			for (var i = 0; i < 10000; i++)
			{
				connectionString = ConnectionStringHelper.GetConnectionString();
			}
			sw.Stop();

			Assert.IsNotNull(connectionString);
			Console.WriteLine("ConnectionStringHelper Stopwatch (10,000 times) = " + new TimeSpan(sw.Elapsed.Ticks).TotalMilliseconds + " ms");
	
		}

		//[TestMethod]
		//public void ConnectionStringContainsStopwatch()
		//{
		//	var connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=MachineDebugDb;Integrated Security=True";
		//	bool f = false;

		//	var sw = new Stopwatch();

		//	sw.Start();
		//	for (var i = 0; i < 1000; i++)
		//	{
		//		if (connectionString.Contains(";") && connectionString.Contains("="))
		//		{
		//			f = true;
		//		}
		//	}
		//	sw.Stop();
			
		//	Assert.IsTrue(f);
		//	Console.WriteLine("Contains (1000 times) = " + new TimeSpan(sw.Elapsed.Ticks).TotalMilliseconds + " ms");

		//}


		private static void CreateTestAppsettingJson (dynamic appsettings, string appsettingsFileName)
		{
			DeleteTestAppsettingsJson(appsettingsFileName);

			var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, appsettingsFileName);
			string json = JsonSerializer.Serialize(appsettings);

			File.WriteAllText(path, json);
		}


		private static void DeleteTestAppsettingsJson(string appsettingsFileName)
		{
			var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, appsettingsFileName);

			if (File.Exists(path))
				File.Delete(path);
		}

		private static string GetCurrentConfigurationName()
		{
			var assemblyConfigurationAttribute = typeof(ConnectionStringHelperTest).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
			return assemblyConfigurationAttribute?.Configuration;
		}
	}
}
