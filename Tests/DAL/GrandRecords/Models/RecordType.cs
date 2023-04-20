using Artisan.Orm;
using Microsoft.Data.SqlClient;

namespace Tests.DAL.GrandRecords.Models;

public class RecordType
{
	public byte Id { get; set; }

	public string Code { get; set; }

	public string Name { get; set; }
}


[MapperFor(typeof(RecordType), RequiredMethod.CreateObject)]
public static class RecordTypeMapper 
{
	public static RecordType CreateObject(SqlDataReader dr)
	{
		var index = 0;
		return CreateObject(dr, ref index);
	}

	public static RecordType CreateObject(SqlDataReader dr, ref int index)
	{
		if (dr.IsDBNull(++index))
		{
			index += 2;
			return null;
		}		
		
		return new RecordType 
		{
			Id		=	dr.GetByte(index)		,
			Code	=	dr.GetString(++index)	,
			Name	=	dr.GetString(++index)	
		};
	}

}