using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Artisan.Orm;
using Tests.DAL.GrandRecords.Models;

namespace Tests.DAL.GrandRecords
{
	public class Repository: RepositoryBase
	{

		#region [ Get ONE Grand Record (with its descendants) ]


		public GrandRecord GetGrandRecordById(int id)
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetGrandRecordById");

				cmd.AddIntParam("@Id", id);

				return cmd.GetByReader(ReadGrandRecord);
			});
		}
		
		public async Task<GrandRecord> GetGrandRecordByIdAsync(int id)
		{
			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.GetGrandRecordById");

				cmd.AddIntParam("@Id", id);

				return cmd.GetByReaderAsync(ReadGrandRecord);
			});
		}

		private static GrandRecord ReadGrandRecord(SqlDataReader reader)
		{
			var grandRecord = reader.ReadTo<GrandRecord>();

			grandRecord.Records = reader.ReadToList<Record>();
			// or reader.ReadToList(grandRecord.Records);

			var childRecords = reader.ReadToList<ChildRecord>();

			reader.Close();

			// the following code allows joining two collections for a single pass
			// it works only if these collections are sorted by RecordId  (!)

			grandRecord.Records.MergeJoin(
			    r => { r.GrandRecord = grandRecord; },
			    childRecords, 
			    (r, cr) => r.Id == cr.RecordId,
			    (r, cr) => {cr.Record = r; r.ChildRecords.Add(cr);}
			);
			
			// THE MergeJoin CODE ABOVE IS THE REPLACEMENT FOR THE COMMENTED CODE BELOW :)

			//var childRecordEnumerator = childRecords.GetEnumerator();
			//var childRecord = childRecordEnumerator.MoveNext() ? childRecordEnumerator.Current : null;
			
			//foreach (var record in grandRecord.Records)
			//{
			//	record.GrandRecord = grandRecord;

			//	while (childRecord != null && childRecord.RecordId == record.Id)
			//	{
			//		childRecord.Record = record;
			//		record.ChildRecords.Add(childRecord);

			//		childRecord = childRecordEnumerator.MoveNext() ? childRecordEnumerator.Current : null;
			//	}

			//	record.GrandRecord = grandRecord;
			//}

			return grandRecord;
		}


		#endregion


		#region [ Get MANY Grand Records (with their descendants) ]


		public IList<GrandRecord> GetGrandRecords()
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetGrandRecords");

				return cmd.GetByReader(ReadGrandRecords);
			});
		}
		
		public async Task<IList<GrandRecord>> GetGrandRecordsAsync()
		{
			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.GetGrandRecords");

				return cmd.GetByReaderAsync(ReadGrandRecords);
			});
		}

		#endregion


		#region [ Save MANY Grand Records (with their descendants) ]


		public IList<GrandRecord> SaveGrandRecords(IList<GrandRecord> grandRecords)
		{
			var records = grandRecords.SelectMany(gr => gr.Records);
			var childRecords = records.SelectMany(r => r.ChildRecords);

			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.SaveGrandRecords");

				cmd.AddTableParam("@GrandRecords", grandRecords);

				cmd.AddTableParam("@Records", records);

				cmd.AddTableParam("@ChildRecords", childRecords);

				return cmd.GetByReader(ReadGrandRecords);
			});
		}

		public async Task<IList<GrandRecord>> SaveGrandRecordsAsync(IList<GrandRecord> grandRecords)
		{
			var records = grandRecords.SelectMany(gr => gr.Records);
			var childRecords = records.SelectMany(r => r.ChildRecords);

			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.SaveGrandRecords");

				cmd.AddTableParam("@GrandRecords", grandRecords);

				cmd.AddTableParam("@Records", records);

				cmd.AddTableParam("@ChildRecords", childRecords);

				return cmd.GetByReaderAsync(ReadGrandRecords);
			});
		}

		#endregion


		private static IList<GrandRecord> ReadGrandRecords(SqlDataReader reader)
		{
			var grandRecords = reader.ReadToList<GrandRecord>();

			var records = reader.ReadToList<Record>();

			var childRecords = reader.ReadToList<ChildRecord>();

			reader.Close();

			// the following code allows joining two collections for a single pass
			// it works only if these collections are sorted by RecordId  (!)

			grandRecords.MergeJoin(
				records, 
				(gr, r) => gr.Id == r.GrandRecordId,
				(gr, r) => { r.GrandRecord = gr; gr.Records.Add(r); },

				childRecords,
				(r, cr) => r.Id == cr.RecordId,
				(r, cr) => { cr.Record = r; r.ChildRecords.Add(cr); }
			);

			// THE MergeJoin CODE ABOVE IS THE REPLACEMENT FOR THE COMMENTED CODE BELOW :)

			//var recordEnumerator = records.GetEnumerator();
			//var record = recordEnumerator.MoveNext() ? recordEnumerator.Current : null;

			//var childRecordEnumerator = childRecords.GetEnumerator();
			//var childRecord = childRecordEnumerator.MoveNext() ? childRecordEnumerator.Current : null;


			//foreach (var grandRecord in grandRecords)
			//{
			//	while (record != null && record.GrandRecordId == grandRecord.Id)
			//	{
			//		while (childRecord != null && childRecord.RecordId == record.Id)
			//		{
			//			childRecord.Record = record;
			//			record.ChildRecords.Add(childRecord);

			//			childRecord = childRecordEnumerator.MoveNext() ? childRecordEnumerator.Current : null;
			//		}

			//		record.GrandRecord = grandRecord;
			//		grandRecord.Records.Add(record);

			//		record = recordEnumerator.MoveNext() ? recordEnumerator.Current : null;
			//	}
			//}

			return grandRecords;
		}

	}
}
