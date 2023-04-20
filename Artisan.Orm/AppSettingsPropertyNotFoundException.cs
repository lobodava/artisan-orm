namespace Artisan.Orm;

[Serializable]
public class AppSettingsPropertyNotFoundException : Exception
{
	public AppSettingsPropertyNotFoundException()
	{
	}

	public AppSettingsPropertyNotFoundException(string message) : base(message)
	{
	}
}