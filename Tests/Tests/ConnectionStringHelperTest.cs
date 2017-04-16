using System;
using System.Configuration;
using System.Diagnostics;
using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.String;

namespace Tests.Tests
{
	[TestClass]
	public class ConnectionStringHelperTest
	{
		private static readonly string  MachineName = Environment.MachineName.ToUpper();

		[TestMethod]
		public void ConnectionStringNotFoundException()
		{
			string connectionString = null;

			try
			{
				connectionString = ConnectionStringHelper.GetConnectionString("qwertyuiopasdfghjklzxcvbnm");

				Assert.Fail();
			} 
			catch(SettingsPropertyNotFoundException)
			{
				connectionString = Empty;
			}
			catch(Exception)
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
			var connectionStringName = $"{MachineName}.MachineConnectionString";

			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

			var connectionStringSettings = new ConnectionStringSettings
			{
				Name = connectionStringName,
				ConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=MachineDb;Integrated Security=True"
			};
			
			config.ConnectionStrings.ConnectionStrings.Add(connectionStringSettings);
    
			config.Save(ConfigurationSaveMode.Modified);
			ConfigurationManager.RefreshSection("connectionStrings");
			
			var connectionString = ConnectionStringHelper.GetConnectionString("MachineConnectionString");

			Assert.IsNotNull(connectionString);
			Assert.AreEqual(ConnectionStringHelper.GetDatabaseName(connectionString), "MachineDb");

		}

		[TestMethod]
		public void MachineDebugConnectionString()
		{
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

			var connectionStringSettings = new ConnectionStringSettings
			{
				Name = $"{MachineName}.Debug.MachineConnectionString",
				ConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=MachineDebugDb;Integrated Security=True"
			};
			
			config.ConnectionStrings.ConnectionStrings.Add(connectionStringSettings);
    
			config.Save(ConfigurationSaveMode.Modified);
			ConfigurationManager.RefreshSection("connectionStrings");
			
			var connectionString = ConnectionStringHelper.GetConnectionString("MachineConnectionString", "Debug");

			Assert.IsNotNull(connectionString);
			Assert.AreEqual(ConnectionStringHelper.GetDatabaseName(connectionString), "MachineDebugDb");
		}

		[TestMethod]
		public void DebugConnectionString()
		{
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

			var connectionStringSettings = new ConnectionStringSettings
			{
				Name = "Debug.DebugConnectionString",
				ConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=DebugDb;Integrated Security=True"
			};
			
			config.ConnectionStrings.ConnectionStrings.Add(connectionStringSettings);
    
			config.Save(ConfigurationSaveMode.Modified);
			ConfigurationManager.RefreshSection("connectionStrings");

			var connectionString = ConnectionStringHelper.GetConnectionString("DebugConnectionString", "Debug");

			Assert.IsNotNull(connectionString);
			Assert.AreEqual(ConnectionStringHelper.GetDatabaseName(connectionString), "DebugDb");
		}


		[TestMethod]
		public void ConfigurationManagerStopwatch()
		{
			var sw = new Stopwatch();
			string connectionString = null;

			sw.Start();
			for (var i = 0; i < 10000; i++)
			{
				connectionString = ConfigurationManager.ConnectionStrings["DatabaseConnection"].ConnectionString;
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

		[TestMethod]
		public void ConnectionStringContainsStopwatch()
		{
			var connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=MachineDebugDb;Integrated Security=True";
			bool f = false;

			var sw = new Stopwatch();

			sw.Start();
			for (var i = 0; i < 1000; i++)
			{
				if (connectionString.Contains(";") && connectionString.Contains("="))
				{
					f = true;
				}
			}
			sw.Stop();
			
			Assert.IsTrue(f);
			Console.WriteLine("Contains (1000 times) = " + new TimeSpan(sw.Elapsed.Ticks).TotalMilliseconds + " ms");
	

		}



	}
}
