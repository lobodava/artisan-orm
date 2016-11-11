using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Artisan.Orm
{
	public static class DataTableExtensions
	{

		public static DataTable ToDataTable<T>(this IEnumerable<T> list) 
		{
			Func<DataTable> createDataTableFunc;
			Func<T, object[]> createDataRowFunc;

			if (!MappingManager.GetCreateDataFuncs(out createDataTableFunc, out createDataRowFunc))
				throw new KeyNotFoundException($"No mapping function found to create DataTable and DataRow for type {typeof(T).FullName}");
			
			var table = createDataTableFunc();

			foreach (var entity in list.Where(entity => entity != null))
			{
				table.Rows.Add(createDataRowFunc(entity));
			}

			return table;
		}

		public static DataTable ToDataTable<T>(this IEnumerable<T> list, Func<DataTable> getDataTableFunc, Func<T, object[]> convertToDataRowFunc) 
		{
			var table = getDataTableFunc();

			foreach (var entity in list.Where(entity => entity != null))
			{
				table.Rows.Add(convertToDataRowFunc(entity));
			}

			return table;
		}

		public static DataTable AsDataTable<T>(this IEnumerable<T> list, string tableName = null, string columnNames = null) 
		{
			var baseType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
			var isSimpleType = typeof(T).IsSimpleType();

			if (tableName == null)
				 throw new NullReferenceException("Parameter 'tableName' is required for AsDataTable<T> method.");

			var table = new DataTable(tableName); 

			var columnNameArray = columnNames?.Split(',', ';').Select(s => s.Trim()).ToArray();
			
			if (isSimpleType)
			{
				var columnName = columnNameArray?.Length > 0 ? columnNameArray[0] : null;

				if (String.IsNullOrWhiteSpace(columnName))
					columnName = baseType.Name;

				table.Columns.Add(columnName, baseType);

				if (list != null)
				foreach (var value in list)
					table.Rows.Add(value);
			}
			else
			{

				var binding = BindingFlags.Public | BindingFlags.Instance ; 
				var properties = typeof(T).GetProperties(binding).Where(p => p.PropertyType.IsSimpleType() && p.CanRead).ToList();
				

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


				foreach (var property in properties) 
				{ 
					table.Columns.Add(property.Name, property.PropertyType); 
				} 

				object[] values = new object[properties.Count]; 
 
				foreach (T item in list) 
				{ 
					for (int i = 0; i < properties.Count; i++) 
					{ 
						values[i] = properties[i].GetValue(item, null); 
					} 
 
					table.Rows.Add(values); 
				}
			}


			return table;
		}

		public static DataTable ToIntIdDataTable(this IEnumerable<int> ids, string dataTableName = "IntIdTableType", string columnName = "Id") 
		{
			var table = new DataTable(dataTableName);
			table.Columns.Add( columnName, typeof( int ));

			if (ids != null)
				foreach (var id in ids)
					table.Rows.Add(id);

			return table;
		}

		public static DataTable ToSmallIntIdDataTable(this IEnumerable<short> ids, string dataTableName = "SmallIntIdTableType", string columnName = "Id" )
		{
			var table = new DataTable(dataTableName);
			table.Columns.Add( columnName, typeof( short ));

			if (ids != null)
				foreach (var id in ids)
					table.Rows.Add(id);

			return table;
		}

		public static DataTable ToTinyIntIdDataTable(this IEnumerable<byte> ids, string dataTableName = "TinyIntIdTableType", string columnName = "Id" )
		{
			var table = new DataTable(dataTableName);
			table.Columns.Add( columnName, typeof( byte ));

			if (ids != null)
				foreach (var id in ids)
					table.Rows.Add(id);

			return table;
		}
		
	}
}
