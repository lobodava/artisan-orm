using System.Collections.Generic;
using System.Threading.Tasks;
using Artisan.Orm;
using Tests.DAL.Records.Models;

namespace Tests.DAL.Records
{
	public class Repository: RepositoryBase
	{

		#region [ GetRecordById ]


		public Record GetRecordById(int id)
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecordById");

				cmd.AddIntParam("@Id", id);

				return cmd.ReadTo<Record>();
			});
		}


		public Record GetRecordByIdWithAutoMapping(int id)
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecordById");

				cmd.AddIntParam("@Id", id);

				return cmd.ReadAs<Record>();
			});
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
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadToList<Record>();
			});
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

		public IEnumerable<Record> GetRecordsAsEnumerable()
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadToEnumerable<Record>();
			});
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
