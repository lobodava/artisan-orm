using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;


namespace Artisan.Orm
{
	public static class DataTableHelpers
	{

		public static DataTable GetDataTableFor<T>(T obj)
		{
			if (obj == null) return null;

			Func<DataTable> createDataTableFunc;
			Func<T, object[]> createDataRowFunc;

			if (!MappingManager.GetCreateDataFuncs(out createDataTableFunc, out createDataRowFunc))
				throw new KeyNotFoundException($"No mapping function found to create DataTable and DataRow for type {typeof(T).FullName}");

			var table = createDataTableFunc();

			table.Rows.Add(createDataRowFunc(obj));

			return table;
		}

		public static DataTable GetDataTableFor(object obj, Type objType)
		{
			var method = typeof (DataTableHelpers).GetMethod("GetDataTableForObject", BindingFlags.Static | BindingFlags.NonPublic);
			var generic = method.MakeGenericMethod(objType);
			return (DataTable) generic.Invoke(null, new[] {obj});
		}
		private static DataTable GetDataTableForObject<T>(T obj)
		{
			return GetDataTableFor<T>(obj);
		}

		public static DataTable GetDataTableFor<T>(T obj, Func<DataTable> getDataTableFunc, Func<T, object[]> convertToRowFunc)
		{
			var table = getDataTableFunc();

			table.Rows.Add(convertToRowFunc(obj));

			return table;
		}
		
		
		public static DataTable GetDataTableForList<T>(IEnumerable<T> list)
		{
			if (list == null) return null;

			Func<DataTable> createDataTableFunc;
			Func<T, object[]> createDataRowFunc;

			if (!MappingManager.GetCreateDataFuncs(out createDataTableFunc, out createDataRowFunc))
				throw new KeyNotFoundException($"No mapping function found to create DataTable and DataRow for type {typeof(T).FullName}");
			
			var table = createDataTableFunc();

			foreach (var item in list)
			{
				if (item != null)
					table.Rows.Add(createDataRowFunc(item));
			}

			return table;
		}

		public static DataTable GetDataTableForList(object list, Type itemType)
		{
			var method = typeof (DataTableHelpers).GetMethod("GetDataTableForListOfObjects", BindingFlags.Static | BindingFlags.NonPublic);
			var generic = method.MakeGenericMethod(itemType);
			return  (DataTable) generic.Invoke(null, new[] {list});
		}
		private static DataTable GetDataTableForListOfObjects<T>(IEnumerable<T> list)
		{
			return GetDataTableForList<T>(list);
		}

		
		public static DataTable GetDataTableForList<T>(IEnumerable<T> list, Func<DataTable> getDataTableFunc, Func<T, object[]> convertToRowFunc)
		{
			var table = getDataTableFunc();

			foreach (var item in list)
			{
				if (item != null)
					table.Rows.Add(convertToRowFunc(item));
			}

			return table;
		}

		public static DataTable GetIntIdDataTable(IEnumerable<int> ids, string dataTableName = "IntIdTableType", string columnName = "Id")
		{
			var table = new DataTable(dataTableName);
			table.Columns.Add( columnName, typeof( int ));

			if (ids != null)
				foreach (var id in ids)
					table.Rows.Add(id);

			return table;
		}

		public static DataTable GetSmallIntIdDataTable(IEnumerable<short> ids, string dataTableName = "SmallIntIdTableType", string columnName = "Id")
		{
			var table = new DataTable(dataTableName);
			table.Columns.Add( columnName, typeof( short ));

			if (ids != null)
				foreach (var id in ids)
					table.Rows.Add(id);

			return table;
		}

		public static DataTable GetTinyIntIdDataTable(IEnumerable<byte> ids, string dataTableName = "TinyIntIdTableType", string columnName = "Id" )
		{
			var table = new DataTable(dataTableName);
			table.Columns.Add( columnName, typeof( byte ));

			if (ids != null)
				foreach (var id in ids)
					table.Rows.Add(id);

			return table;
		}

		internal static DataTable GetValueTypeDataTable(object list)
		{
			if (list is IEnumerable<int>)
				return DataTableHelpers.GetIntIdDataTable((IEnumerable<int>)list);

			if (list is IEnumerable<short>)
				return DataTableHelpers.GetSmallIntIdDataTable((IEnumerable<short>)list);

			if (list is IEnumerable<byte>)
				return DataTableHelpers.GetTinyIntIdDataTable((IEnumerable<byte>)list);
			
			throw new NullReferenceException($"No mapping function found to create DataTable and DataRow for type {list.GetType().FullName}");
		}
		
		

		


	}
}