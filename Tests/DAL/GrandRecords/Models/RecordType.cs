using System;
using System.Data.SqlClient;
using Artisan.Orm;

namespace Tests.DAL.GrandRecords.Models
{
	public class RecordType
	{
		public Byte Id { get; set; }

		public String Code { get; set; }

		public String Name { get; set; }
	}


	[MapperFor(typeof(RecordType), RequiredMethod.CreateEntity)]
	public static class RecordTypeMapper 
	{
		public static RecordType CreateEntity(SqlDataReader dr)
		{
			var index = 0;
			return CreateEntity(dr, ref index);
		}

		public static RecordType CreateEntity(SqlDataReader dr, ref int index)
		{
			if (dr.IsDBNull(index))
			{
				index = index + 3;
				return null;
			}		
			
			return new RecordType 
			{
				Id		=	dr.GetByte(index++)		,
				Code	=	dr.GetString(index++)	,
				Name	=	dr.GetString(index++)	
			};
		}

	}
}