using System;
using System.Data;
using Artisan.Orm;
using Microsoft.Data.SqlClient;
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
		public static ChildRecord CreateObject(SqlDataReader dr)
		{
			var i = 0;
			
			return new ChildRecord 
			{
				Id			=	dr.GetInt32(i)		,
				RecordId	=	dr.GetInt32(++i)	,
				Name		=	dr.GetString(++i)	,
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

		public static object[] CreateDataRow(ChildRecord obj)
		{
			if (obj.Id == 0) 
				obj.Id = Int32NegativeIdentity.Next;

			if (obj.RecordId == 0 && obj.Record != null)
				obj.RecordId = obj.Record.Id;


			return new object[]
			{
				obj.Id			,
				obj.RecordId	,
				obj.Name			
			};
		}

	}
}