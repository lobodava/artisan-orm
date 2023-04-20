namespace Artisan.Orm;

public static class Int64NegativeIdentity
{
	private static long _identity = long.MinValue;

	public static long Next => _identity++;
}

public static class Int32NegativeIdentity
{
	private static int _identity = int.MinValue;

	public static int Next => _identity++;
}

public static class Int16NegativeIdentity
{
	private static short _identity = short.MinValue;

	public static short Next => _identity++;
}
