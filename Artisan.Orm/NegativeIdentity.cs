using System;
using System.Threading;

namespace Artisan.Orm
{

	/// <summary>
	/// Thread-safe generator of unique negative <see cref="long"/> identity values,
	/// used to mark new entities in object graphs before the real identity is assigned by the database.
	/// Values are produced monotonically starting from <see cref="long.MinValue"/>.
	/// </summary>
	public static class Int64NegativeIdentity
	{
		private static long _identity = long.MinValue;

		/// <summary>Returns the next negative identity without reusing any value.</summary>
		/// <exception cref="InvalidOperationException">The negative identity range has been exhausted.</exception>
		public static long Next
		{
			get
			{
				while (true)
				{
					var identity = Volatile.Read(ref _identity);
					if (identity >= 0)
						throw new InvalidOperationException("The Int64 negative identity range has been exhausted.");
					if (Interlocked.CompareExchange(ref _identity, identity + 1, identity) == identity)
						return identity;
				}
			}
		}
	}

	/// <summary>
	/// Thread-safe generator of unique negative <see cref="int"/> identity values,
	/// used to mark new entities in object graphs before the real identity is assigned by the database.
	/// Values are produced monotonically starting from <see cref="int.MinValue"/>.
	/// </summary>
	public static class Int32NegativeIdentity
	{
		private static int _identity = int.MinValue;

		/// <summary>Returns the next negative identity without reusing any value.</summary>
		/// <exception cref="InvalidOperationException">The negative identity range has been exhausted.</exception>
		public static int Next
		{
			get
			{
				while (true)
				{
					var identity = Volatile.Read(ref _identity);
					if (identity >= 0)
						throw new InvalidOperationException("The Int32 negative identity range has been exhausted.");
					if (Interlocked.CompareExchange(ref _identity, identity + 1, identity) == identity)
						return identity;
				}
			}
		}
	}

	/// <summary>
	/// Thread-safe generator of unique negative <see cref="short"/> identity values,
	/// used to mark new entities in object graphs before the real identity is assigned by the database.
	/// Values are produced monotonically starting from <see cref="short.MinValue"/>.
	/// Note: <see cref="Interlocked"/> has no 16-bit overload, so the counter is kept in an <see cref="int"/>
	/// and the result is narrowed back to <see cref="short"/>. The generator throws after all
	/// 32,768 negative values have been allocated; it never wraps or resets.
	/// </summary>
	public static class Int16NegativeIdentity
	{
		private static int _identity = short.MinValue;

		/// <summary>Returns the next negative identity without reusing any value.</summary>
		/// <exception cref="InvalidOperationException">The negative identity range has been exhausted.</exception>
		public static short Next
		{
			get
			{
				while (true)
				{
					var identity = Volatile.Read(ref _identity);
					if (identity >= 0)
						throw new InvalidOperationException("The Int16 negative identity range has been exhausted.");
					if (Interlocked.CompareExchange(ref _identity, identity + 1, identity) == identity)
						return (short)identity;
				}
			}
		}
	}

}
