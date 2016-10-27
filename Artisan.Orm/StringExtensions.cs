namespace Artisan.Orm
{
	public static class StringExtensions
	{
		public static string TrimToNull(this string value)
		{
			if (value == null || value.Trim().Length == 0) return null;
			return value.Trim();
		}

		public static string TruncateTo(this string value, int length) {
			if (value == null) return null;
			if (value.Length <= length) return value;
			return value.Substring(0, length);
		}
	}
}