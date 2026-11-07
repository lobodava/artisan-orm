using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Data.SqlClient;

namespace Artisan.Orm
{ 

	public static class MappingManager
	{
		private static readonly Dictionary<Type, Delegate> CreateObjectFuncDictionary = new Dictionary<Type, Delegate>();

		private static readonly Dictionary<Type, Delegate> CreateObjectRowFuncDictionary = new Dictionary<Type, Delegate>();

		private static readonly Dictionary<Type, Tuple<Func<DataTable>?, Delegate?>> CreateDataFuncsDictionary = new Dictionary<Type, Tuple<Func<DataTable>?, Delegate?>>();

		private static readonly ConcurrentDictionary<(Type Type, string Key), Delegate> AutoCreateObjectFuncDictionary = new ConcurrentDictionary<(Type, string), Delegate>();

		private static readonly ConcurrentDictionary<(Type Type, string Key), Tuple<Func<DataTable>?, Delegate?>> AutoCreateDataFuncsDictionary = new ConcurrentDictionary<(Type, string), Tuple<Func<DataTable>?, Delegate?>>();

		private static readonly ConcurrentDictionary<string, SqlParameter[]> SqlParametersDictionary = new ConcurrentDictionary<string, SqlParameter[]>();

		private sealed class ProcedureParameterCache : ConcurrentDictionary<(string Server, string Database, CommandType CommandType, string CommandText, string Settings, string Authentication), SqlParameter[]> { }

		private static readonly ProcedureParameterCache ProcedureParametersDictionary = new ProcedureParameterCache();
		private static readonly ConditionalWeakTable<SqlConnection, ProcedureParameterCache> ConnectionParametersDictionaries =
			new ConditionalWeakTable<SqlConnection, ProcedureParameterCache>();

		// Restrict shared metadata to explicit schema names. Other valid SQL identifier forms
		// still work, but are derived afresh instead of assuming the caller's default schema.
		private static readonly Regex SchemaQualifiedProcedure = new Regex(
			@"^\s*(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*)\s*\.\s*(?:\[[^\]]+\]|[A-Za-z_][A-Za-z0-9_]*)\s*$",
			RegexOptions.CultureInvariant);


		/// <summary>
		/// One-shot, thread-safe lazy initializer for the mapper scan. The scan is deliberately
		/// kept out of the static constructor: a failure in <see cref="Assembly.GetTypes"/>
		/// (e.g. a third-party assembly in the AppDomain with unresolvable dependencies) would
		/// otherwise propagate as <see cref="TypeInitializationException"/> and mark the type
		/// permanently unusable for the life of the process. With this pattern a broken
		/// assembly no longer disables the whole ORM — its unloadable types are simply skipped
		/// (see <see cref="SafeGetTypes"/>).
		/// </summary>
		private static readonly Lazy<bool> _initialized =
			new Lazy<bool>(ScanMappers, LazyThreadSafetyMode.ExecutionAndPublication);

		private static void EnsureInitialized() { _ = _initialized.Value; }

		private static bool ScanMappers()
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
							throw new ArtisanMappingException($"Mapper {type.Name} does not contain required method CreateObject");
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
							throw new ArtisanMappingException($"Mapper {type.Name} does not contain required method CreateObjectRow");
					}
					else
					{
						var funcType = typeof(Func<,>).MakeGenericType(typeof(SqlDataReader), typeof(ObjectRow));
						var createObjectRowDelegate = Delegate.CreateDelegate(funcType, methodInfo);

						CreateObjectRowFuncDictionary.Add(attribute.MapperForType, createObjectRowDelegate);
					}

					Func<DataTable>? createDataTableFunc = null;

					methodInfo = type.GetMethod("CreateDataTable");

					if (methodInfo == null)
					{
						if (attribute.RequiredMethods.Intersect(new []{RequiredMethod.All, RequiredMethod.AllMain, RequiredMethod.BothForDataTable }).Any())
							throw new ArtisanMappingException($"Mapper {type.Name} does not contain required method CreateDataTable");
					}
					else
					{
						createDataTableFunc = (Func<DataTable>)Delegate.CreateDelegate(typeof(Func<DataTable>), methodInfo);
					}


					Delegate? createDataRowDelegate = null;


					methodInfo = type.GetMethod("CreateDataRow", new Type[] { attribute.MapperForType });

					if (methodInfo == null) {
						if (attribute.RequiredMethods.Intersect(new []{RequiredMethod.All, RequiredMethod.AllMain, RequiredMethod.BothForDataTable }).Any())
							throw new ArtisanMappingException($"Mapper {type.Name} does not contain required method CreateDataRow");
					}
					else {
						var funcType = typeof(Func<,>).MakeGenericType(attribute.MapperForType, typeof(object[]));
						createDataRowDelegate = Delegate.CreateDelegate(funcType, methodInfo);
					}

					CreateDataFuncsDictionary.Add(attribute.MapperForType, Tuple.Create(createDataTableFunc, createDataRowDelegate));

				}
			}

			return true;
		}


		public static Func<SqlDataReader, T> GetCreateObjectFunc<T>()
		{
			EnsureInitialized();

			if (CreateObjectFuncDictionary.TryGetValue(typeof(T), out Delegate? del))
				return (Func<SqlDataReader, T>)del!;

			throw new ArtisanMappingException($"CreateObject Func not found. Check if MapperFor {typeof(T).FullName} exists and CreateObject exist.");
		}
	
		public static Func<SqlDataReader, ObjectRow> GetCreateObjectRowFunc<T>()
		{
			EnsureInitialized();

			if (CreateObjectRowFuncDictionary.TryGetValue(typeof(T), out Delegate? del))
				return (Func<SqlDataReader, ObjectRow>)del!;

			throw new ArtisanMappingException($"CreateRow Func not found. Check if MapperFor {typeof(T).FullName} and CreateRow exist.");
		}


		public static Func<DataTable>? GetCreateDataTableFunc<T>()
		{
			EnsureInitialized();

			return CreateDataFuncsDictionary.TryGetValue(typeof(T), out Tuple<Func<DataTable>?, Delegate?>? tuple)
				? tuple.Item1
				: null;
		}

		public static Func<T, object[]>? GetCreateDataRowFunc<T>()
		{
			EnsureInitialized();

			return CreateDataFuncsDictionary.TryGetValue(typeof(T), out Tuple<Func<DataTable>?, Delegate?>? tuple)
				? (Func<T, object[]>?)tuple.Item2
				: null;
		}


		public static bool GetCreateDataFuncs<T>(out Func<DataTable>? createDataTableFunc, out Func<T, object[]>? createDataRowFunc)
		{
			EnsureInitialized();

			if (CreateDataFuncsDictionary.TryGetValue(typeof(T), out Tuple<Func<DataTable>?, Delegate?>? tuple) &&
				tuple.Item1 != null && tuple.Item2 != null)
			{
				createDataTableFunc = tuple.Item1;
				createDataRowFunc = (Func<T, object[]>?)tuple.Item2;

				return true;
			}

			createDataTableFunc = null;
			createDataRowFunc = null;

			return false;
		}


		public static bool GetCreateDataFuncs(Type type, out Func<DataTable>? createDataTableFunc, out Delegate? createDataRowFunc)
		{
			EnsureInitialized();

			if (CreateDataFuncsDictionary.TryGetValue(type, out Tuple<Func<DataTable>?, Delegate?>? funcs) &&
				funcs.Item1 != null && funcs.Item2 != null)
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
			return AutoCreateObjectFuncDictionary.TryAdd((typeof(T), key), autoCreateObjectFunc);
		}

		public static Func<SqlDataReader, T>? GetAutoCreateObjectFunc<T>(string key)
		{
			if (AutoCreateObjectFuncDictionary.TryGetValue((typeof(T), key), out Delegate? del))
			{
				return (Func<SqlDataReader, T>?)del;
			}
			return null;
		}

		public static bool AddAutoCreateDataFuncs<T>(string key, Func<DataTable>? createDataTableFunc, Func<T, object[]>? createDataRowFunc)
		{
			var funcs = new Tuple<Func<DataTable>?, Delegate?>(createDataTableFunc, createDataRowFunc);

			return AutoCreateDataFuncsDictionary.TryAdd((typeof(T), key), funcs);
		}

		public static bool GetAutoCreateDataFuncs<T>(string key, out Func<DataTable>? createDataTableFunc, out Func<T, object[]>? createDataRowFunc)
		{
			if (AutoCreateDataFuncsDictionary.TryGetValue((typeof(T), key), out Tuple<Func<DataTable>?, Delegate?>? funcs) &&
				funcs.Item1 != null && funcs.Item2 != null)
			{
				createDataTableFunc = funcs.Item1;
				createDataRowFunc = (Func<T, object[]>?)funcs.Item2;

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
				foreach (Type type in SafeGetTypes(assembly).Where(type => type.GetCustomAttributes(typeof(MapperForAttribute), true).Length > 0))
				{
					yield return type;
				}
			}
		}

		/// <summary>
		/// Enumerates the types in <paramref name="assembly"/>, tolerant of
		/// <see cref="ReflectionTypeLoadException"/>: when some types in the assembly cannot
		/// be loaded (missing dependencies, version mismatches, etc.) the loadable ones are
		/// still returned and the rest are silently skipped. A single broken third-party
		/// assembly in the AppDomain therefore cannot disable the entire mapper registry.
		/// </summary>
		private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
		{
			if (assembly.IsDynamic) return Array.Empty<Type>();

			try
			{
				return assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				return ex.Types.Where(t => t != null)!;
			}
			catch
			{
				return Array.Empty<Type>();
			}
		}

		public static bool AddSqlParameters(string key, SqlParameter[] sqlParameters)
		{
			return SqlParametersDictionary.TryAdd(key, CloneSqlParameters(sqlParameters));
		}

		public static SqlParameter[]? GetSqlParameters(string key)
		{
			return SqlParametersDictionary.TryGetValue(key, out var collection) ? CloneSqlParameters(collection) : null;
		}

		internal static bool CacheProcedureParameters(SqlCommand command, SqlParameter[] parameters)
		{
			var key = GetProcedureParametersKey(command);
			if (key == null) return false;
			var templates = CloneSqlParameters(parameters);
			foreach (var parameter in templates) parameter.Value = null;
			return GetProcedureParametersCache(command, key.Value.Authentication).TryAdd(key.Value, templates);
		}

		internal static SqlParameter[]? GetProcedureParameters(SqlCommand command)
		{
			var key = GetProcedureParametersKey(command);
			return key != null && GetProcedureParametersCache(command, key.Value.Authentication).TryGetValue(key.Value, out var parameters)
				? CloneSqlParameters(parameters) : null;
		}

		private static SqlParameter[] CloneSqlParameters(SqlParameter[] parameters)
		{
			return parameters.Select(parameter => (SqlParameter)((ICloneable)parameter).Clone()).ToArray();
		}

		private static ProcedureParameterCache GetProcedureParametersCache(SqlCommand command, string authentication)
		{
			return authentication.Length > 0 ? ProcedureParametersDictionary :
				ConnectionParametersDictionaries.GetValue(command.Connection!, _ => new ProcedureParameterCache());
		}

		private static (string Server, string Database, CommandType CommandType, string CommandText, string Settings, string Authentication)? GetProcedureParametersKey(SqlCommand command)
		{
			var connection = command.Connection;
			if (connection == null || string.IsNullOrEmpty(connection.Database) || command.CommandType != CommandType.StoredProcedure ||
				command.CommandText.IndexOf('#') >= 0 || !SchemaQualifiedProcedure.IsMatch(command.CommandText)) return null;

			var settings = new SqlConnectionStringBuilder(connection.ConnectionString);
			settings.Remove("Password");
			var userId = connection.Credential?.UserId ?? settings.UserID;
			string authentication;
			if (!settings.IntegratedSecurity && !string.IsNullOrEmpty(userId) &&
				(settings.Authentication == SqlAuthenticationMethod.NotSpecified || settings.Authentication == SqlAuthenticationMethod.SqlPassword) &&
				string.IsNullOrEmpty(connection.AccessToken) && connection.AccessTokenCallback == null)
			{
				// A password change does not change the SQL principal. Never retain passwords/tokens.
				authentication = userId;
			}
			else
			{
				// Keep opaque identities in a weak, connection-local cache. This retains repeated
				// command reuse without keeping the connection or its credentials alive globally.
				authentication = string.Empty;
			}

			return (connection.DataSource, connection.Database, command.CommandType,
				command.CommandText, settings.ConnectionString, authentication);
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
			if (assembly.IsDynamic) return Array.Empty<string>();

			try
			{
				return assembly.GetReferencedAssemblies()
					.Select(assemblyName => assemblyName.FullName);
			}
			catch
			{
				return Array.Empty<string>();
			}
		}

		#endregion

	}

}
