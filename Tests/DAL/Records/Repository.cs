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


		public Record GetRecordByIdWithMapper(int id)
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecordById");

				cmd.AddIntParam("@Id", id);

				return cmd.ReadTo<Record>();
			});
		}

		public Record GetRecordByIdWithReflection(int id)
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

		public async Task<IList<Record>> GetRecordsAsync()
		{
			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadToListAsync<Record>();
			});
		}


		#endregion

		#region [ GetRecordDataRows ]


		public Rows GetRecordDataRows()
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadToRows<Record>();
			});
		}

		public async Task<Rows> GetRecordDataRowsAsync()
		{
			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.GetRecords");

				return cmd.ReadToRowsAsync<Record>();
			});
		}


		#endregion


		#region [ Save ONE Record ]

		public Record SaveRecord(Record record)
		{	
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.SaveRecords");

				cmd.AddTableParam("@Records", record);

				return cmd.ReadTo<Record>();
			});
		}

		public async Task<Record> SaveRecordAsync(Record record)
		{
			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.SaveRecords");

				cmd.AddTableParam("@Records", record);

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

				cmd.AddTableParam("@Records", records);

				return cmd.ReadToListAsync<Record>();
			});
		}


		#endregion


	}
}
