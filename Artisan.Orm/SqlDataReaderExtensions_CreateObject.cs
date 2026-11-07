using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Globalization;
using Microsoft.Data.SqlClient;

namespace Artisan.Orm
{ 

	public static partial class SqlDataReaderExtensions
	{

		#region [ CreateObject ]
	
		public static T CreateObject<T>(this SqlDataReader dr)
		{
			var key = GetAutoCreateObjectFuncKey<T>(dr);
		
			var autoMappingFunc = MappingManager.GetAutoCreateObjectFunc<T>(key); 

			return CreateObject(dr, autoMappingFunc, key);
		}

		internal static T CreateObject<T>(this SqlDataReader dr, Func<SqlDataReader, T>? autoMappingFunc, string key)
		{
			if (autoMappingFunc == null)
			{
				autoMappingFunc = CreateAutoMappingFunc<T>(dr);
				MappingManager.AddAutoCreateObjectFunc(key, autoMappingFunc);
			}

			return autoMappingFunc(dr);
		}

		public static dynamic CreateDynamic(this SqlDataReader dr)
		{
			dynamic expando = new ExpandoObject();
			var dict = (IDictionary<string, object?>)expando;  // ExpandoObject always implements this

			for (var i = 0; i < dr.FieldCount; i++)
			{
				var columnName = dr.GetName(i);
				var value = dr.GetValue(i);
				dict.Add(columnName, value == DBNull.Value ? null : value);
			}

			return expando;
		}

		#endregion

		#region [ private members ] 

		/// <summary>
		/// Builds a cache key for an auto-generated mapping function from the
		/// <i>shape</i> of the reader (field count, field names, field types) and the
		/// target type <typeparamref name="T"/>. Two queries that yield the same result
		/// shape share the same compiled mapping function.
		///
		/// <para>The key is intentionally not derived from the command text: it used to be,
		/// which required reflection into the internal <c>SqlDataReader.Command</c>
		/// property (fragile across Microsoft.Data.SqlClient upgrades and AOT-hostile)
		/// and caused the cache to grow without bound for ad-hoc SQL.</para>
		/// </summary>
		internal static string GetAutoCreateObjectFuncKey<T>(SqlDataReader dr)
		{
			// MappingManager keys the target by its actual Type identity. Encode every shape
			// component separately so aliases containing ':' or '|' cannot merge fields.
			var sb = new StringBuilder();
			var count = dr.FieldCount;
			sb.Append(count.ToString(CultureInfo.InvariantCulture)).Append(':');
			for (var i = 0; i < count; i++)
			{
				var name = dr.GetName(i);
				var typeName = dr.GetFieldType(i).AssemblyQualifiedName!;
				sb.Append(name.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(name);
				sb.Append(typeName.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(typeName);
			}
			return sb.ToString();
		}
	
		private static readonly Dictionary<Type, string> ReaderGetMethodNames = new Dictionary<Type, string>()
		{
			{ typeof(Boolean)		 , "GetBoolean"			},
			{ typeof(Byte)			 , "GetByte"			},
			{ typeof(Int16)			 , "GetInt16"			},
			{ typeof(Int32)			 , "GetInt32"			},
			{ typeof(Int64)			 , "GetInt64"			},
			{ typeof(Single)		 , "GetFloat"			},
			{ typeof(Double)		 , "GetDouble"			},
			{ typeof(Decimal)		 , "GetDecimal"			},
			{ typeof(String)		 , "GetString"			},
			{ typeof(DateTime)		 , "GetDateTime"		},
			{ typeof(DateTimeOffset) , "GetDateTimeOffset"	},
			{ typeof(TimeSpan)		 , "GetTimeSpan"		},
			{ typeof(Guid)			 , "GetGuid"			}
		};

		private static MethodCallExpression GetTypedValueMethodCallExpression(Type propertyType, Type fieldType, ParameterExpression sqlDataReaderParam, ConstantExpression indexConst, out bool isDefaultGetValueMethod)
		{
			var underlyingType = propertyType.GetUnderlyingType();

			isDefaultGetValueMethod = false;

			if (propertyType == fieldType)
			{
				if (ReaderGetMethodNames.TryGetValue(underlyingType, out string? methodName))
					return Expression.Call(
						sqlDataReaderParam,
						typeof(SqlDataReader).GetMethod(methodName, new[] { typeof(int) })!,  // method name from ReaderGetMethodNames, always valid on SqlDataReader
						indexConst
					);

				if (underlyingType == typeof(Char))
					return Expression.Call(
						null,
						typeof(SqlDataReaderExtensions).GetMethod("GetCharacter", new[] { typeof(SqlDataReader), typeof(int) })!,  // static method defined in this class
						sqlDataReaderParam,
						indexConst
					);
			}

			isDefaultGetValueMethod = true;

			return Expression.Call(
				null,
				typeof(SqlDataReaderExtensions).GetMethod(nameof(ConvertValue), BindingFlags.Static | BindingFlags.NonPublic)!,
				Expression.Call(
					sqlDataReaderParam,
					typeof(SqlDataReader).GetMethod("GetValue", new[] { typeof(int) })!,  // SqlDataReader.GetValue(int) always exists
					indexConst
				),
				Expression.Constant(underlyingType, typeof(Type))
			);
		}
	
		internal static Func<SqlDataReader, T> CreateAutoMappingFunc<T>(SqlDataReader dr)
		{ 
			var properties = typeof(T)
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(p => p.GetSetMethod() != null && p.GetIndexParameters().Length == 0 && p.PropertyType.IsSimpleType()).ToList();

			var memberBindings = new List<MemberBinding>();
			var readerParam = Expression.Parameter(typeof(SqlDataReader), "reader");

			for (var i = 0; i < dr.FieldCount; i++)
			{
				var columnName = dr.GetName(i);
				var prop = properties.FirstOrDefault(p => p.Name == columnName);
			
				if (prop != null)
				{
					var indexConst = Expression.Constant(i, typeof(int));
					var fieldType = dr.GetFieldType(i);

					MethodCallExpression getTypedValueExp = GetTypedValueMethodCallExpression(prop.PropertyType, fieldType, readerParam, indexConst, out bool isDefaultGetValueMethod);

					Expression? getValueExp = null;

					if (prop.PropertyType.IsNullableValueType())
					{
						getValueExp = Expression.Condition(
							Expression.Call(
								readerParam,
								typeof(SqlDataReader).GetMethod("IsDBNull", new[] { typeof(int) })!,  // SqlDataReader.IsDBNull(int) always exists
								indexConst
							),
		
							Expression.Default(prop.PropertyType),

							Expression.Convert(getTypedValueExp, prop.PropertyType)
						);
					}
					else if (isDefaultGetValueMethod)
					{
						getValueExp  = Expression.Convert(getTypedValueExp, prop.PropertyType);
					}
					else
					{
						getValueExp = getTypedValueExp;
					}
		
					var binding = Expression.Bind(prop, getValueExp!);  // set in every branch of the if/else if/else above
					memberBindings.Add(binding);
				}
			}

			if (memberBindings.Count == 0)
			{
				var columnList = new StringBuilder();
				for (var i = 0; i < dr.FieldCount; i++)
				{
					if (i > 0) columnList.Append(", ");
					columnList.Append(dr.GetName(i)).Append(':').Append(dr.GetFieldType(i).Name);
				}
				throw new ArtisanMappingException(
					$"Creation of AutoMapping Func failed. None of the reader columns [{columnList}] " +
					$"matches a public writable property of '{typeof(T).FullName}'.");
			}
		

			var ctor = Expression.New(typeof(T));
			var memberInit = Expression.MemberInit(ctor, memberBindings);

			var func = Expression.Lambda<Func<SqlDataReader, T>>(memberInit, readerParam).Compile();


			return func;
		}
	
		#endregion
	}

}
