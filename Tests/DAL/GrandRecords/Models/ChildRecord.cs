using System;
using System.Data;
using System.Data.SqlClient;
using Artisan.Orm;
using Newtonsoft.Json;

namespace Tests.DAL.GrandRecords.Models
{
	public class ChildRecord
	{
		public Int32 Id { get; set; }

		public Int32 RecordId { get; set; }

		[JsonIgnore]
		public Record Record { get; set; }

		public String Name { get; set; }
	}


	[MapperFor(typeof(ChildRecord), RequiredMethod.AllMain)]
	public static class ChildRecordMapper 
	{
		public static ChildRecord CreateEntity(SqlDataReader dr)
		{
			var i = 0;
			
			return new ChildRecord 
			{
				Id			=	dr.GetInt32(i++)	,
				RecordId	=	dr.GetInt32(i++)	,
				Name		=	dr.GetString(i++)	,
			};
		}
	
		public static DataTable CreateDataTable()
		{
			var table = new DataTable("ChildRecordTableType");
			
			table.Columns.Add(	"Id"		,	typeof( Int32	));
			table.Columns.Add(	"RecordId"	,	typeof( Int32	));
			table.Columns.Add(	"Name"		,	typeof( String	));

			return table;
		}

		public static Object[] CreateDataRow(ChildRecord entity)
		{
			if (entity.Id == 0) 
				entity.Id = Int32NegativeIdentity.Next;

			if (entity.RecordId == 0 && entity.Record != null)
				entity.RecordId = entity.Record.Id;


			return new object[]
			{
				entity.Id			,
				entity.RecordId		,
				entity.Name			
			};
		}

	}
}