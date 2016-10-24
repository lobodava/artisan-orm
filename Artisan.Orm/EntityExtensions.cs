using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Artisan.Orm
{
	public static class EntityExtensions
	{
		public static DataTable ToDataTable<T>(this T entity) where T: IEntity
		{
			if (entity == null) return null;

			Func<DataTable> createDataTableFunc;
			Func<T, object[]> createDataRowFunc;

			if (!MappingManager.GetCreateDataFuncs(out createDataTableFunc, out createDataRowFunc))
				throw new KeyNotFoundException($"No mapping function found to create DataTable and DataRow for type {typeof(T).FullName}");

			var table = createDataTableFunc();

			table.Rows.Add(createDataRowFunc(entity));

			return table;
		}

		public static DataTable ToDataTable<T>(this IEnumerable<T> list) where T: IEntity
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
		
		public static DataTable ToDataTable<T>(this T entity, Func<DataTable> getDataTableFunc, Func<T, object[]> convertToRowFunc) where T: IEntity
		{
			var table = getDataTableFunc();

			table.Rows.Add(convertToRowFunc(entity));

			return table;
		}

		public static DataTable ToDataTable<T>(this IEnumerable<T> list, Func<DataTable> getDataTableFunc, Func<T, object[]> convertToRowFunc) where T: IEntity
		{
			var table = getDataTableFunc();

			foreach (var entity in list.Where(entity => entity != null))
			{
				table.Rows.Add(convertToRowFunc(entity));
			}

			return table;
		}

		public static DataTable ToIntIdDataTable(this IEnumerable<int> ids )
		{
			var table = new DataTable("IntIdTableType");
			table.Columns.Add( "Id", typeof( int ));

			if (ids != null)
				foreach (var id in ids)
					table.Rows.Add(id);

			return table;
		}

		public static DataTable ToSmallIntIdDataTable(this IEnumerable<short> ids )
		{
			var table = new DataTable("SmallIntIdTableType");
			table.Columns.Add( "Id", typeof( short ));

			if (ids != null)
				foreach (var id in ids)
					table.Rows.Add(id);

			return table;
		}

		public static DataTable ToTinyIntIdDataTable(this IEnumerable<byte> ids )
		{
			var table = new DataTable("TinyIntIdTableType");
			table.Columns.Add( "Id", typeof( byte ));

			if (ids != null)
				foreach (var id in ids)
					table.Rows.Add(id);

			return table;
		}

	}
}