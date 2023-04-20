using System.Data;
using System.Text.Json.Serialization;
using Artisan.Orm;
using Microsoft.Data.SqlClient;

namespace Tests.DAL.GrandRecords.Models;

public class ChildRecord
{
	public int Id { get; set; }

	public int RecordId { get; set; }

	[JsonIgnore]
	public Record Record { get; set; }

	public string Name { get; set; }
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
		
		table.Columns.Add(	"Id"		,	typeof(int));
		table.Columns.Add(	"RecordId"	,	typeof(int));
		table.Columns.Add(	"Name"		,	typeof(string));

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
