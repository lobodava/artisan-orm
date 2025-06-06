using System.Collections.Generic;
using System.Threading.Tasks;
using Artisan.Orm;
using Microsoft.Data.SqlClient;
using Tests.DAL.Records.Models;

namespace Tests.DAL.Records
{
	public class Repository: RepositoryBase
	{
		public Repository(string connectionString) : base(connectionString) {}

		#region [ GetRecordById ]


		public Record GetRecordById(int id)
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecordById");

				cmd.AddIntParam("Id", id);

				return cmd.ReadTo<Record>();
			});
		}
		
		public Record GetRecordByIdWithAutoMapping(int id)
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecordById");

				cmd.AddIntParam("Id", id);

				return cmd.ReadAs<Record>();
			});
		}

		public Record GetRecordByIdOnBaseLevel(int id)
		{
			//var sql = @"select
			//				Id				,
			//				GrandRecordId	,
			//				Name			,
			//				RecordTypeId	,
			//				Number			,
			//				[Date]			,
			//				Amount			,
			//				IsActive		,
			//				Comment			
			//			from
			//				dbo.Records
			//			where
			//				Id = @Id";

			var sql = "dbo.GetRecordById";

			return ReadTo<Record>(sql, new SqlParameter("Id", id));
		}
		

		public async Task<Record> GetRecordByIdAsync(int id)
		{
			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecordById");

				cmd.AddIntParam("@Id", id);

				return cmd.ReadToAsync<Record>();
			});
		}


		#endregion 


		#region [ GetRecords ]


		public IList<Record> GetRecords()
		{
			return ReadToList<Record>("dbo.GetRecords");

			//return GetByCommand(cmd =>
			//{
			//	cmd.UseProcedure("dbo.GetRecords");

			//	return cmd.ReadToList<Record>();
			//});
		}

		public IList<Record> GetRecordsWithAutoMapping()
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadAsList<Record>();
			});
		}

		public async Task<IList<Record>> GetRecordsAsync()
		{
			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadToListAsync<Record>();
			});
		}

		public async Task<IList<Record>> GetRecordsWithAutoMappingAsync()
		{
			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadAsListAsync<Record>();
			});
		}

		public IEnumerable<Record> GetRecordsToEnumerable()
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadToEnumerable<Record>();
			});
		}

		public IEnumerable<Record> GetRecordsAsEnumerable()
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadAsEnumerable<Record>();
			});
		}


		public IEnumerable<Record> GetRecordsToEnumerableOnBaseLevel()
		{
			return ReadToEnumerable<Record>("dbo.GetRecords");
		}

		public IEnumerable<Record> GetRecordsAsEnumerableOnBaseLevel()
		{
			return ReadAsEnumerable<Record>("dbo.GetRecords");
		}


		public IList<Record> GetRecordsOnBaseLevel()
		{
			return ReadToList<Record>("dbo.GetRecords");
		}


		#endregion

		#region [ GetRecordRows ]


		public ObjectRows GetRecordRows()
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadToObjectRows<Record>();
			});
		}

		public async Task<ObjectRows> GetRecordRowsAsync()
		{
			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadToObjectRowsAsync<Record>();
			});
		}

		public ObjectRows GetRecordRowsOnBaseLevel()
		{
			return ReadToObjectRows<Record>("dbo.GetRecords");
		}

		public ObjectRows GetRecordRowsWithHandMapping()
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadToObjectRows(dr => new ObjectRow(9) 
				{
					/* Id			 */	dr.GetInt32(0)				,
					/* GrandRecordId */	dr.GetInt32(1)				,
					/* Name			 */	dr.GetString(2)				,
					/* RecordTypeId	 */	dr.GetByteNullable(3)		,
					/* Number		 */	dr.GetInt16Nullable(4)		,
					/* Date			 */	dr.GetDateTimeNullable(5)	,
					/* Amount		 */	dr.GetDecimalNullable(6)	,
					/* IsActive		 */	dr.GetBooleanNullable(7)	,
					/* Comment		 */	dr.GetStringNullable(8)	
				});
			});
		}

		public async Task<ObjectRows> GetRecordRowsWithHandMappingAsync()
		{
			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadToObjectRowsAsync(dr => new ObjectRow(9) 
				{
					/* Id			 */	dr.GetInt32(0)				,
					/* GrandRecordId */	dr.GetInt32(1)				,
					/* Name			 */	dr.GetString(2)				,
					/* RecordTypeId	 */	dr.GetByteNullable(3)		,
					/* Number		 */	dr.GetInt16Nullable(4)		,
					/* Date			 */	dr.GetDateTimeNullable(5)	,
					/* Amount		 */	dr.GetDecimalNullable(6)	,
					/* IsActive		 */	dr.GetBooleanNullable(7)	,
					/* Comment		 */	dr.GetStringNullable(8)	
				});
			});
		}

		#endregion


		#region [ Save ONE Record ]

		public Record SaveRecord(Record record)
		{	
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.SaveRecords");

				cmd.AddTableRowParam("@Records", record);

				return cmd.ReadTo<Record>();
			});
		}

		public async Task<Record> SaveRecordAsync(Record record)
		{
			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.SaveRecords");

				cmd.AddTableRowParam("@Records", record);

				return cmd.ReadToAsync<Record>();
			});
		}


		#endregion

		#region [ Save MANY Records ]


		public IList<Record> SaveRecords(IList<Record> records)
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.SaveRecords");

				cmd.AddTableParam("@Records", records);

				return cmd.ReadToList<Record>();
			});
		}

		public async Task<IList<Record>> SaveRecordsAsync(IList<Record> records)
		{
			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.SaveRecords");

				cmd.AddTableParam("@Records", records.ToDataTable());

				return cmd.ReadToListAsync<Record>();
			});
		}


		#endregion

	}
}
