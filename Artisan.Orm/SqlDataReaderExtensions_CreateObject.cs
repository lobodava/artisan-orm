using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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

		internal static T CreateObject<T>(this SqlDataReader dr, Func<SqlDataReader, T> autoMappingFunc, string key)
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
			var dict = expando as IDictionary<string, object>;
		
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

		private static readonly PropertyInfo CommandProperty = typeof(SqlDataReader).GetProperties(BindingFlags.NonPublic | BindingFlags.Instance).First(p => p.Name == "Command");
	
		internal static string GetCommandText(this SqlDataReader dr)
		{
			var command = (SqlCommand)CommandProperty.GetValue(dr);
			return command.CommandText;
		}
	
		internal static string GetAutoCreateObjectFuncKey<T>(SqlDataReader dr)
		{
			return GetAutoCreateObjectFuncKey<T>(dr.GetCommandText());
		}

		internal static string GetAutoCreateObjectFuncKey<T>(string commandText)
		{
			return $"{commandText}+{typeof(T).FullName}";
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
				if (ReaderGetMethodNames.TryGetValue(underlyingType, out string methodName))
					return Expression.Call(
						sqlDataReaderParam,
						typeof(SqlDataReader).GetMethod(methodName, new[] { typeof(int) }),
						indexConst
					);

				if (underlyingType == typeof(Char))
					return Expression.Call(
						null, 
						typeof(SqlDataReaderExtensions).GetMethod("GetCharacter", new[] { typeof(SqlDataReader), typeof(int) }), 
						sqlDataReaderParam,
						indexConst
					);
			}

			isDefaultGetValueMethod = true;

			return  Expression.Call(
				null,
				typeof(Convert).GetMethod("ChangeType", new[] { typeof(object), typeof(Type) }),
				Expression.Call(
					sqlDataReaderParam,
					typeof(SqlDataReader).GetMethod("GetValue", new[] { typeof(int) }),
					indexConst
				),
				Expression.Constant(underlyingType, typeof(Type))
			);
		}
	
		internal static Func<SqlDataReader, T> CreateAutoMappingFunc<T>(SqlDataReader dr)
		{ 
			var properties = typeof(T)
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(p => p.CanWrite && p.PropertyType.IsSimpleType()).ToList();

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

					Expression getValueExp = null;

					if (prop.PropertyType.IsNullableValueType())
					{
						getValueExp = Expression.Condition (
							Expression.Call(
								readerParam,
								typeof(SqlDataReader).GetMethod("IsDBNull", new[] { typeof(int) }),
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
		
					var binding = Expression.Bind(prop, getValueExp);
					memberBindings.Add(binding);
				}
			}

			if (memberBindings.Count == 0)
				throw new ApplicationException($"Creation of AutoMapping Func failed. No property-field name matching found for class = '{typeof(T).FullName}' and CommandText = '{dr.GetCommandText()}'");
		

			var ctor = Expression.New(typeof(T));
			var memberInit = Expression.MemberInit(ctor, memberBindings);

			var func = Expression.Lambda<Func<SqlDataReader, T>>(memberInit, readerParam).Compile();


			return func;
		}
	
		#endregion
	}

}
