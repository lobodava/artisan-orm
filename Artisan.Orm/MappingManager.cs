using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace Artisan.Orm
{
	public static class MappingManager
	{
		private static readonly Dictionary<Type, Delegate> CreateObjectFuncDictionary = new Dictionary<Type, Delegate>();

		private static readonly Dictionary<Type, Delegate> CreateObjectRowFuncDictionary = new Dictionary<Type, Delegate>();

		private static readonly Dictionary<Type, Tuple<Func<DataTable>, Delegate>> CreateDataFuncsDictionary = new Dictionary<Type, Tuple<Func<DataTable>, Delegate>>();

		private static readonly ConcurrentDictionary<string, Delegate> AutoCreateObjectFuncDictionary  = new ConcurrentDictionary<string, Delegate>();

		private static readonly ConcurrentDictionary<string, Tuple<Func<DataTable>, Delegate>> AutoCreateDataFuncsDictionary = new ConcurrentDictionary<string, Tuple<Func<DataTable>, Delegate>>();

		private static readonly ConcurrentDictionary<string, SqlParameter[]> SqlParametersDictionary  = new ConcurrentDictionary<string, SqlParameter[]>();


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
						var funcType = typeof(Func<,>).MakeGenericType(typeof(SqlDataReader), typeof(ObjectRow));
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

					CreateDataFuncsDictionary.Add(attribute.MapperForType, Tuple.Create(createDataTableFunc, createDataRowDelegate));

				}
			}
		}


		public static Func<SqlDataReader, T> GetCreateObjectFunc<T>()
		{
			Delegate del;

			if (CreateObjectFuncDictionary.TryGetValue(typeof(T), out del))
				return (Func<SqlDataReader, T>)del;

			throw new NullReferenceException($"CreateObject Func not found. Check if MapperFor {typeof(T).FullName} exists and CreateObject exist.");
		}
		
		public static Func<SqlDataReader, ObjectRow> GetCreateObjectRowFunc<T>()
		{
			Delegate del;

			if (CreateObjectRowFuncDictionary.TryGetValue(typeof(T), out del))
				return (Func<SqlDataReader, ObjectRow>)del;

			throw new NullReferenceException($"CreateRow Func not found. Check if MapperFor {typeof(T).FullName} and CreateRow exist.");
		}


		public static Func<DataTable> GetCreateDataTableFunc<T>()
		{
			Tuple<Func<DataTable>, Delegate> tuple;

			return CreateDataFuncsDictionary.TryGetValue(typeof(T), out tuple) ? tuple.Item1 : null;
		}

		public static Func<T, object[]> GetCreateDataRowFunc<T>()
		{
			Tuple<Func<DataTable>, Delegate> tuple;

			if (CreateDataFuncsDictionary.TryGetValue(typeof(T), out tuple))
				return (Func<T, object[]>)tuple.Item2;

			return null;
		}


		public static bool GetCreateDataFuncs<T>(out Func<DataTable> createDataTableFunc, out Func<T, object[]> createDataRowFunc)
		{
			Tuple<Func<DataTable>, Delegate> tuple;

			if (CreateDataFuncsDictionary.TryGetValue(typeof(T), out tuple))
			{
				createDataTableFunc = tuple.Item1;
				createDataRowFunc = (Func<T, object[]>)tuple.Item2;

				return true;
			}

			createDataTableFunc = null;
			createDataRowFunc = null;

			return false;
		}


		public static bool GetCreateDataFuncs(Type type, out Func<DataTable> createDataTableFunc, out Delegate createDataRowFunc)
		{
			Tuple<Func<DataTable>, Delegate> funcs;

			if (CreateDataFuncsDictionary.TryGetValue(type, out funcs))
			{
				createDataTableFunc = funcs.Item1;
				createDataRowFunc = funcs.Item2;

				return true;
			}

			createDataTableFunc = null;
			createDataRowFunc = null;

			return false;
		}


		public static bool AddAutoCreateObjectFunc<T>(string key, Func<SqlDataReader, T> autoCreateObjectFunc)
		{
			return AutoCreateObjectFuncDictionary.TryAdd(key, autoCreateObjectFunc);
		}

		public static Func<SqlDataReader, T> GetAutoCreateObjectFunc<T>(string key)
		{
			Delegate del;

			if (AutoCreateObjectFuncDictionary.TryGetValue(key, out del)) {
				return (Func<SqlDataReader, T>)del;
			}
			return null;
		}

		public static bool AddAutoCreateDataFuncs<T>(string key, Func<DataTable> createDataTableFunc, Func<T, object[]> createDataRowFunc)
		{
			var funcs = new Tuple<Func<DataTable>, Delegate>(createDataTableFunc, createDataRowFunc);
			
			return AutoCreateDataFuncsDictionary.TryAdd(key, funcs);
		}

		public static bool GetAutoCreateDataFuncs<T>(string key, out Func<DataTable> createDataTableFunc, out Func<T, object[]> createDataRowFunc)
		{
			Tuple<Func<DataTable>, Delegate> funcs;

			if (AutoCreateDataFuncsDictionary.TryGetValue(key, out funcs))
			{
				createDataTableFunc = funcs.Item1;
				createDataRowFunc = (Func<T, object[]>)funcs.Item2;

				return true;
			}

			createDataTableFunc = null;
			createDataRowFunc = null;

			return false;
		}

		private static IEnumerable<Type> GetTypesWithMapperForAttribute()
		{
			foreach (Assembly assembly in GetCurrentAndDependentAssemblies())
			{
				foreach (Type type in assembly.GetTypes().Where(type => type.GetCustomAttributes(typeof(MapperForAttribute), true).Length > 0))
				{
					yield return type;
				}
			}
		}

		public static bool AddSqlParameters(string key, SqlParameter[] sqlParameters)
		{
			return SqlParametersDictionary.TryAdd(key, sqlParameters);
		}

		public static SqlParameter[] GetSqlParameters(string key)
		{
			return SqlParametersDictionary.TryGetValue(key, out var collection) ? collection : null;
		}

		#region [ Get Dependent Assemblies ]

		private static IEnumerable<Assembly> GetCurrentAndDependentAssemblies()
		{
			var currentAssembly = typeof(MappingManager).Assembly;

			return AppDomain.CurrentDomain.GetAssemblies()

				// http://stackoverflow.com/a/8850495/623190
				.Where(a => GetNamesOfAssembliesReferencedBy(a).Contains(currentAssembly.FullName))
				.Concat(new[] { currentAssembly })

				// https://www.codeproject.com/Articles/1155836/Artisan-Orm-or-How-To-Reinvent-the-Wheel?msg=5419092#xx5419092xx
				.GroupBy(a => a.FullName)
				.Select(x => x.First());
		}

		public static IEnumerable<string> GetNamesOfAssembliesReferencedBy(Assembly assembly)
		{
			return assembly.GetReferencedAssemblies()
				.Select(assemblyName => assemblyName.FullName);
		}

		#endregion

	}
}
