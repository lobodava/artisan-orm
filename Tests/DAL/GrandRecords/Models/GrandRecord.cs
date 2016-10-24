using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Artisan.Orm;

namespace Tests.DAL.GrandRecords.Models
{
	public class GrandRecord : IEntity
	{
		public Int32 Id { get; set; }

		public String Name { get; set; }


		public IList<Record> Records  { get; set; }
	}


	[MapperFor(typeof(GrandRecord), RequiredMethod.AllMain)]
	public static class GrandRecordMapper 
	{

		public static GrandRecord CreateEntity(SqlDataReader dr)
		{
			int i = 0;
			
			return new GrandRecord 
			{
				Id		=	dr.GetInt32(i++)	,
				Name	=	dr.GetString(i++)	,

				Records =	new List<Record>()
			};
		}
	
		public static DataTable CreateDataTable()
		{
			var table = new DataTable("GrandRecordTableType");
			
			table.Columns.Add(	"Id"		,	typeof( Int32	));
			table.Columns.Add(	"Name"		,	typeof( String	));

			return table;
		}

		public static Object[] CreateDataRow(GrandRecord entity)
		{
			if (entity.Id == 0) 
				entity.Id = Int32NegativeIdentity.Next;

			foreach (var record in entity.Records)
				record.GrandRecordId = entity.Id;


			return new object[]
			{
				entity.Id		,
				entity.Name		
			};
		}

	}
}