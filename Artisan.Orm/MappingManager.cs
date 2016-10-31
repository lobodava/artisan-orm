using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace Artisan.Orm
{
	public static class MappingManager
	{
		private static readonly Dictionary<Type, object> CreateObjectFuncDictionary = new Dictionary<Type, object>();

		private static readonly Dictionary<Type, object> CreateObjectRowFuncDictionary = new Dictionary<Type, object>();

		private static readonly Dictionary<Type, Tuple<Func<DataTable>, Delegate>> CreateDataTableFuncsDictionary = new Dictionary<Type, Tuple<Func<DataTable>, Delegate>>();

		static MappingManager()
		{
			foreach (var type in GetTypesWithMapperForAttribute())
			{
				var attributes = type.GetCustomAttributes(typeof(MapperForAttribute), true);

				if (attributes.Length == 0) continue;

				foreach (var attribute in attributes.Cast<MapperForAttribute>())
				{

					var methodInfo = type.GetMethod("CreateObject", new Type[] { typeof(SqlDataReader) });

					if (methodInfo == null)
					{
						if (attribute.RequiredMethods.Intersect(new []{RequiredMethod.All, RequiredMethod.AllMain, RequiredMethod.BothForObject, RequiredMethod.CreateObject}).Any())
							throw new NullReferenceException($"Mapper {type.Name} does not contain required method CreateObject");
					}
					else
					{
						var funcType = typeof(Func<,>).MakeGenericType(typeof(SqlDataReader), attribute.MapperForType);
						var del = Delegate.CreateDelegate(funcType, methodInfo);

						CreateObjectFuncDictionary.Add(attribute.MapperForType, del);
					}


					methodInfo = type.GetMethod("CreateObjectRow", new Type[] { typeof(SqlDataReader) });

					if (methodInfo == null)
					{
						if (attribute.RequiredMethods.Intersect(new []{RequiredMethod.All, RequiredMethod.BothForObject, RequiredMethod.CreateObjectRow}).Any())
							throw new NullReferenceException($"Mapper {type.Name} does not contain required method CreateObjectRow");
					}
					else
					{
						var funcType = typeof(Func<,>).MakeGenericType(typeof(SqlDataReader), typeof(object[]));
						var createObjectRowDelegate = Delegate.CreateDelegate(funcType, methodInfo);

						CreateObjectRowFuncDictionary.Add(attribute.MapperForType, createObjectRowDelegate);
					}



					Func<DataTable> createDataTableFunc = null;

					methodInfo = type.GetMethod("CreateDataTable");

					if (methodInfo == null)
					{
						if (attribute.RequiredMethods.Intersect(new []{RequiredMethod.All, RequiredMethod.BothForDataTable }).Any())
							throw new NullReferenceException($"Mapper {type.Name} does not contain required method CreateDataTable");
					}
					else
					{
						createDataTableFunc = (Func<DataTable>)Delegate.CreateDelegate(typeof(Func<DataTable>), methodInfo);
					}


					Delegate createDataRowDelegate = null;


					methodInfo = type.GetMethod("CreateDataRow", new Type[] { attribute.MapperForType });

					if (methodInfo == null) {
						if (attribute.RequiredMethods.Intersect(new []{RequiredMethod.All, RequiredMethod.BothForDataTable }).Any())
							throw new NullReferenceException($"Mapper {type.Name} does not contain required method CreateDataRow");
					}
					else {
						var funcType = typeof(Func<,>).MakeGenericType(attribute.MapperForType, typeof(object[]));
						createDataRowDelegate = Delegate.CreateDelegate(funcType, methodInfo);
					}

					CreateDataTableFuncsDictionary.Add(attribute.MapperForType, Tuple.Create(createDataTableFunc, createDataRowDelegate));

				}
			}
		}


		public static Func<SqlDataReader, T> GetCreateObjectFunc<T>()
		{
			object obj;

			if (CreateObjectFuncDictionary.TryGetValue(typeof(T), out obj))
				return (Func<SqlDataReader, T>)obj;

			throw new NullReferenceException($"CreateObject Func not found. Check if MapperFor {typeof(T).FullName} exists and CreateObject exist.");
		}

		public static Func<SqlDataReader, object[]> GetCreateObjectRowFunc<T>()
		{
			object obj;

			if (CreateObjectRowFuncDictionary.TryGetValue(typeof(T), out obj))
				return (Func<SqlDataReader, object[]>)obj;

			throw new NullReferenceException($"CreateRow Func not found. Check if MapperFor {typeof(T).FullName} and CreateRow exist.");
		}


		public static Func<DataTable> GetCreateDataTableFunc<T>()
		{
			Tuple<Func<DataTable>, Delegate> obj;

			return CreateDataTableFuncsDictionary.TryGetValue(typeof(T), out obj) ? obj.Item1 : null;
		}

		public static Func<T, object[]> GetCreateDataRowFunc<T>()
		{
			Tuple<Func<DataTable>, Delegate> obj;

			if (CreateDataTableFuncsDictionary.TryGetValue(typeof(T), out obj))
				return (Func<T, object[]>)obj.Item2;

			return null;
		}


		public static bool GetCreateDataFuncs<T>(out Func<DataTable> createDataTableFunc, out Func<T, object[]> createDataRowFunc)
		{
			Tuple<Func<DataTable>, Delegate> obj;

			if (CreateDataTableFuncsDictionary.TryGetValue(typeof(T), out obj))
			{
				createDataTableFunc = obj.Item1;
				createDataRowFunc = (Func<T, object[]>)obj.Item2;

				return true;
			}

			createDataTableFunc = null;
			createDataRowFunc = null;

			return false;
		}


		public static bool GetCreateDataFuncs(Type type, out Func<DataTable> createDataTableFunc, out Delegate createDataRowFunc)
		{
			Tuple<Func<DataTable>, Delegate> obj;

			if (CreateDataTableFuncsDictionary.TryGetValue(type, out obj))
			{
				createDataTableFunc = obj.Item1;
				createDataRowFunc = obj.Item2;

				return true;
			}

			createDataTableFunc = null;
			createDataRowFunc = null;

			return false;
		}




		private static IEnumerable<Type> GetTypesWithMapperForAttribute() {
			foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (Type type in assembly.GetTypes().Where(type => type.GetCustomAttributes(typeof(MapperForAttribute), true).Length > 0))
				{
					yield return type;
				}
			}
		}

	}
}
