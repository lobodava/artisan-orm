using Microsoft.Extensions.Configuration;

namespace Tests
{
	public class AppSettings
	{
		public AppSettings()
		{
			var configuration = new ConfigurationBuilder()
				.SetBasePath(AppContext.BaseDirectory)
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
				.Build();

			configuration.Bind(this);
		}

		public ConnectionStrings ConnectionStrings { get; set; }
	}

	public class ConnectionStrings
	{
		public string DatabaseConnection { get; set; }
	}
}
