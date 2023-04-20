using System.Data;
using Artisan.Orm;
using Microsoft.Data.SqlClient;

namespace Tests.DAL.GrandRecords.Models;

public class GrandRecord
{
	public int Id { get; set; }

	public string Name { get; set; }

	public IList<Record> Records  { get; set; }
}


[MapperFor(typeof(GrandRecord), RequiredMethod.AllMain)]
public static class GrandRecordMapper 
{

	public static GrandRecord CreateObject(SqlDataReader dr)
	{
		int i = 0;
		
		return new GrandRecord 
		{
			Id		=	dr.GetInt32(i)		,
			Name	=	dr.GetString(++i)	,

			Records =	new List<Record>()
		};
	}

	public static DataTable CreateDataTable()
	{
		var table = new DataTable("GrandRecordTableType");
		
		table.Columns.Add(	"Id"		,	typeof(int));
		table.Columns.Add(	"Name"		,	typeof(string));

		return table;
	}

	public static object[] CreateDataRow(GrandRecord obj)
	{
		if (obj.Id == 0) 
			obj.Id = Int32NegativeIdentity.Next;

		foreach (var record in obj.Records)
			record.GrandRecordId = obj.Id;


		return new object[]
		{
			obj.Id		,
			obj.Name
		};
	}

}
