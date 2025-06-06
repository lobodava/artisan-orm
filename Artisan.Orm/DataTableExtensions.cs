using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static System.String;

namespace Artisan.Orm
{
	public static class DataTableExtensions
	{
		public static DataTable AddColumn<T>(this DataTable dataTable, string columnName, bool isNullable = true) 
		{
			var underlyingType = typeof(T).GetUnderlyingType();
			var column = new DataColumn(columnName, underlyingType) {AllowDBNull = isNullable};
			dataTable.Columns.Add(column);
			return dataTable;
		}

		public static DataTable ToDataTable<T>(this IEnumerable<T> list) 
		{
			Func<DataTable> createDataTableFunc;
			Func<T, object[]> createDataRowFunc;

			if (!MappingManager.GetCreateDataFuncs(out createDataTableFunc, out createDataRowFunc))
				throw new KeyNotFoundException($"No mapping function found to create DataTable and DataRow for type {typeof(T).FullName}");

			var dataTable = createDataTableFunc();

			foreach (var entity in list.Where(entity => entity != null))
			{
				dataTable.Rows.Add(createDataRowFunc(entity));
			}

			return dataTable;
		}

		public static DataTable ToDataTable<T>(this IEnumerable<T> list, Func<DataTable> getDataTableFunc, Func<T, object[]> convertToDataRowFunc) 
		{
			var dataTable = getDataTableFunc();

			foreach (var entity in list.Where(entity => entity != null))
			{
				dataTable.Rows.Add(convertToDataRowFunc(entity));
			}

			return dataTable;
		}

		public static DataTable ToTinyIntIdDataTable(this IEnumerable<byte> ids, string dataTableName = "TinyIntIdTableType", string columnName = "Id" )
		{
			return ids.ToIdDataTable(dataTableName, columnName);
		}

		public static DataTable ToSmallIntIdDataTable(this IEnumerable<short> ids, string dataTableName = "SmallIntIdTableType", string columnName = "Id" )
		{
			return ids.ToIdDataTable(dataTableName, columnName);
		}

		public static DataTable ToIntIdDataTable(this IEnumerable<int> ids, string dataTableName = "IntIdTableType", string columnName = "Id") 
		{
			return ids.ToIdDataTable(dataTableName, columnName);
		}

		public static DataTable ToBigIntIdDataTable(this IEnumerable<long> ids, string dataTableName = "BigIntIdTableType", string columnName = "Id") 
		{
			return ids.ToIdDataTable(dataTableName, columnName);
		}
	
		private static DataTable ToIdDataTable<T>(this IEnumerable<T> ids, string dataTableName, string columnName) where T: struct 
		{
			var dataTable = new DataTable(dataTableName);
			dataTable.Columns.Add(columnName, typeof(T));

			if (ids != null)
				foreach (var id in ids)
					dataTable.Rows.Add(id);

			return dataTable;
		}
	

		public static DataTable AsDataTable<T>(this IEnumerable<T> list, string tableName = null, string columnNames = null) 
		{
			var columnNameArray = columnNames?.Split(',', ';').Select(s => s.Trim()).ToArray();
			return list.AsDataTable(tableName, columnNameArray);
		}
	
		public static DataTable AsDataTable<T>(this IEnumerable<T> list, string tableName, string[] columnNameArray) 
		{
			if (typeof(T).IsSimpleType())
				return GetSimpleTypeDataTable(list, tableName, columnNameArray);
	
			return GetObjectDataTable(list, tableName, columnNameArray);
		}
	
		private static DataTable GetSimpleTypeDataTable<T>(IEnumerable<T> list, string tableName, string[] columnNameArray)
		{
			var underlyingType = typeof(T).GetUnderlyingType();

			var dataTable = new DataTable(tableName);

			var columnName = columnNameArray?.Length > 0 ? columnNameArray[0] : null;

			if (IsNullOrWhiteSpace(columnName))
				columnName = underlyingType.Name;

			dataTable.Columns.Add(columnName, underlyingType);

			if (list != null)
				foreach (var value in list)
					dataTable.Rows.Add(value);

			return dataTable;
		}

		private static DataTable GetObjectDataTable<T>(IEnumerable<T> list, string tableName, string[] columnNameArray)
		{
			string key = GetAutoCreateDataFuncsKey<T>(tableName, columnNameArray);

			Func<DataTable> createDataTableFunc;
			Func<T, object[]> createDataRowFunc;

			if (!MappingManager.GetAutoCreateDataFuncs(key, out createDataTableFunc, out createDataRowFunc))
			{
				CreateAutoMappingFunc<T>(tableName, columnNameArray, out createDataTableFunc, out createDataRowFunc);

				MappingManager.AddAutoCreateDataFuncs(key, createDataTableFunc, createDataRowFunc);
			}

			return list.ToDataTable(createDataTableFunc, createDataRowFunc);
		}

		private static string GetAutoCreateDataFuncsKey<T>(string tableName, string[] columnNameArray)
		{
			var typeFullName = typeof(T).FullName;

			var tableNamePart = !IsNullOrWhiteSpace(tableName) ? $"+{tableName}" : "";  

			var columnNames = columnNameArray?.Length > 0 ? $"+{Join("+", columnNameArray)}" : "";

			return $"{typeFullName}{tableNamePart}{columnNames}";
		}

		private static void CreateAutoMappingFunc<T>(string tableName, string[] columnNameArray, out Func<DataTable> createDataTableFunc, out Func<T, object[]> createDataRowFunc)
		{ 
			var bindingFlags = BindingFlags.Public | BindingFlags.Instance ; 
			var properties = typeof(T).GetProperties(bindingFlags).Where(p => p.PropertyType.IsSimpleType() && p.CanRead).ToList();

			if (columnNameArray?.Length > 0)
			{
				var sortedProperties = new List<PropertyInfo>();

				foreach (var columnName in columnNameArray)
				{
					var property = properties.FirstOrDefault(p => p.Name == columnName);

					if (property != null)
						sortedProperties.Add(property);
				}

				properties = sortedProperties;
			}

			createDataTableFunc = GetCreateDataTableFunc<T>(tableName, properties);

			createDataRowFunc = GetCreateDataRowFunc<T>(properties);
		}

		private static Func<DataTable> GetCreateDataTableFunc<T>(string tableName, List<PropertyInfo> properties)
		{
			var dataTableCtor = Expression.New(typeof (DataTable));
			var tableNameProp = typeof (DataTable).GetProperty("TableName");
			var tableNameConst = Expression.Constant(tableName, typeof (string));
			var tableNameBinding = Expression.Bind(tableNameProp, tableNameConst);
			var dataTableInit = Expression.MemberInit(dataTableCtor, tableNameBinding);

			var dataTable = Expression.Variable(typeof (DataTable), "dataTable");
			var columns = Expression.PropertyOrField(dataTable, "Columns");

			List<Expression> addColumnCalls = new List<Expression>();

			foreach (var property in properties)
			{
				var columnCtor = Expression.New(typeof (DataColumn));

				var columnNameProp = typeof (DataColumn).GetProperty("ColumnName");
				var columnNameConst = Expression.Constant(property.Name, typeof (string));
				var columnNameBinding = Expression.Bind(columnNameProp, columnNameConst);

				var columnTypeProp = typeof (DataColumn).GetProperty("DataType");
				var columnTypeConst = Expression.Constant(property.PropertyType.GetUnderlyingType(), typeof (Type));
				var columnTypeBinding = Expression.Bind(columnTypeProp, columnTypeConst);

				var columnBindings = new List<MemberBinding> {columnNameBinding, columnTypeBinding};

				var columnInit = Expression.MemberInit(columnCtor, columnBindings);

				addColumnCalls.Add(
					Expression.Call(
						columns,
						typeof (DataColumnCollection).GetMethod("Add", new[] {typeof (DataColumn)}),
						columnInit
						)
					);
			}

			BlockExpression addColumnsBlock = Expression.Block(addColumnCalls);

			var body = Expression.Block(

				// DataTable dataTable;
				new[] {dataTable},

				// dataTable = new DataTable("TableName");
				Expression.Assign(dataTable, dataTableInit),

				// foreach - dataTable.Columns.Add("ColumnName", ColumnType)
				addColumnsBlock,

				// return dataTable
				dataTable
			);

			var createDataTableFunc = Expression.Lambda<Func<DataTable>>(body).Compile();

			return createDataTableFunc;
		}

		private static Func<T, object[]> GetCreateDataRowFunc<T>(List<PropertyInfo> properties)
		{
			var objParam = Expression.Parameter( typeof (T), "obj");

			var items = new List<Expression>();

			foreach (var property in properties)
			{
				var getterMethodInfo = property.GetGetMethod();
				var getterCall = Expression.Call(objParam, getterMethodInfo);
				var castToObject = Expression.Convert(getterCall, typeof(object));

				items.Add(castToObject);
			}

			var objectArrayExpression = Expression.NewArrayInit(typeof(object), items);

			var createDataRowFunc = Expression.Lambda<Func<T, object[]>>(objectArrayExpression, objParam).Compile();

			return createDataRowFunc;
		}
	}
}
