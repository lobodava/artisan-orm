using System;
using System.Linq;

namespace Artisan.Orm
{
	// taken from http://stackoverflow.com/a/15578098/623190

	internal static class TypeExtensions
	{
		private static readonly Type[] SimpleTypes;
	
		static TypeExtensions()
		{
			var types = new[]
			{
				//typeof (Enum),
				typeof (String),
				typeof (Char),
				typeof (Guid),

				typeof (Boolean),
				typeof (Byte),
				typeof (Int16),
				typeof (Int32),
				typeof (Int64),
				typeof (Single),
				typeof (Double),
				typeof (Decimal),

				typeof (SByte),
				typeof (UInt16),
				typeof (UInt32),
				typeof (UInt64),

				typeof (DateTime),
				typeof (DateTimeOffset),
				typeof (TimeSpan),
			};
		
			var nullTypes =	types
							.Where(t => t.IsValueType)
							.Select(t => typeof (Nullable<>)
							.MakeGenericType(t));

			SimpleTypes = types.Concat(nullTypes).ToArray();
		}

		internal static bool IsSimpleType(this Type type)
		{
			if (SimpleTypes.Any(x => x.IsAssignableFrom(type)))
				return true;

			var nut = Nullable.GetUnderlyingType(type);
			return nut != null && nut.IsEnum;
		}

		internal static bool IsNullableValueType(this Type type)
		{
			//type.IsValueType && Nullable.GetUnderlyingType(type) != null || type == typeof(String)

			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
				|| type == typeof(String);
		}


		internal static Type GetUnderlyingType(this Type type)
		{
			return Nullable.GetUnderlyingType(type) ?? type;
		}

	}

}
