using System.Data;
using Artisan.Orm;
using Microsoft.Data.SqlClient;

namespace Tests.DAL.Records.Models;

public class Record 
{
	public int Id { get; set; }

	public int GrandRecordId { get; set; }

	public string Name { get; set; }

	public byte? RecordTypeId { get; set; }

	public short? Number { get; set; }

	public DateTime? Date { get; set; }

	public decimal? Amount { get; set; }

	public bool? IsActive { get; set; }

	public string Comment { get; set; }
}


[MapperFor(typeof(Record), RequiredMethod.All)]
public static class RecordMapper 
{
	public static Record CreateObject(SqlDataReader dr)
	{
		var i = 0;

		return new Record 
		{
			Id				=	dr.GetInt32(i)				,
			GrandRecordId	=	dr.GetInt32(++i)			,
			Name			=	dr.GetString(++i)			,
			RecordTypeId	=	dr.GetByteNullable(++i)		,
			Number			=	dr.GetInt16Nullable(++i)	,
			Date			=	dr.GetDateTimeNullable(++i)	,
			Amount			=	dr.GetDecimalNullable(++i)	,
			IsActive		=	dr.GetBooleanNullable(++i)	,
			Comment			=	dr.GetStringNullable(++i)	,
		};

		//return new Record
		//{
		//	Id				= (Int32)dr.GetValue(++i)	,
		//	GrandRecordId	= (Int32)dr.GetValue(++i)	,
		//	Name			= (String)dr.GetValue(++i)	,
		//	RecordTypeId	= dr.IsDBNull(i) ? default(Byte?)		: (Byte?)dr.GetValue(++i)	,
		//	Number			= dr.IsDBNull(i) ? default(Int16?)		: (Int16?)dr.GetValue(++i)	,
		//	Date			= dr.IsDBNull(i) ? default(DateTime?)	: (DateTime?)dr.GetValue(++i)	,
		//	Amount			= dr.IsDBNull(i) ? default(Decimal?)	: (Decimal?)dr.GetValue(++i)	,
		//	IsActive		= dr.IsDBNull(i) ? default(Boolean?)	: (Boolean?)dr.GetValue(++i)	,
		//	Comment			= dr.IsDBNull(i) ? null					: (String)dr.GetValue(++i)	
		//};


		// cast with "as" is a fast way and a dangerous one
		// if cast fails "as" returns <null> and not throw an exception

		//return new Record 
		//{
		//	Id				=	(Int32)dr.GetValue(++i)			,
		//	GrandRecordId	=	(Int32)dr.GetValue(++i) 		, 
		//	Name			=	(String)dr.GetValue(++i) 		, 
		//	RecordTypeId	=	dr.GetValue(++i) as Byte?		,
		//	Number			=	dr.GetValue(++i) as Int16?		, 
		//	Date			=	dr.GetValue(++i) as DateTime?	, 
		//	Amount			=	dr.GetValue(++i) as Decimal?	, 
		//	IsActive		=	dr.GetValue(++i) as Boolean?	, 
		//	Comment			=	dr.GetValue(i) as String 
		//};

	}

	public static ObjectRow CreateObjectRow(SqlDataReader dr)
	{
		var i = 0;

		return new ObjectRow(9)
		{
			/*	0 - Id				=	*/	dr.GetInt32(i)				,
			/*	1 - GrandRecordId	=	*/	dr.GetInt32(++i)			,
			/*	2 - Name			=	*/	dr.GetString(++i)			,
			/*	3 - RecordTypeId	=	*/	dr.GetByteNullable(++i)		,
			/*	4 - Number			=	*/	dr.GetInt16Nullable(++i)	,
			/*	5 - Date			=	*/	dr.GetDateTimeNullable(++i)	,
			/*	6 - Amount			=	*/	dr.GetDecimalNullable(++i)	,
			/*	7 - IsActive		=	*/	dr.GetBooleanNullable(++i)	,
			/*	8 - Comment			=	*/	dr.GetStringNullable(++i)	,
		};
	}


	public static DataTable CreateDataTable()
	{
		//var table = new DataTable("RecordTableType");
		
		//table.Columns.Add(	"Id"			,	typeof( Int32		));
		//table.Columns.Add(	"GrandRecordId"	,	typeof( Int32		));
		//table.Columns.Add(	"Name"			,	typeof( String		));
		//table.Columns.Add(	"RecordTypeId"	,	typeof( Byte		));
		//table.Columns.Add(	"Number"		,	typeof( Int16		));
		//table.Columns.Add(	"Date"			,	typeof( DateTime	));
		//table.Columns.Add(	"Amount"		,	typeof( Decimal		));
		//table.Columns.Add(	"IsActive"		,	typeof( Boolean		));
		//table.Columns.Add(	"Comment"		,	typeof( String		));

		//return table;

		return new DataTable( "RecordTableType"	)
			.AddColumn<int>	( "Id"				)
			.AddColumn<int>	( "GrandRecordId"	)
			.AddColumn<string>	( "Name"			)
			.AddColumn<byte>	( "RecordTypeId"	)
			.AddColumn<short>	( "Number"			)
			.AddColumn<DateTime>( "Date"			)
			.AddColumn<decimal>	( "Amount"			)
			.AddColumn<bool>	( "IsActive"		)
			.AddColumn<string>	( "Comment"			);

	}

	public static object[] CreateDataRow(Record obj)
	{
		if (obj.Id == 0) 
			obj.Id = Int32NegativeIdentity.Next;


		return new object[]
		{
			obj.Id				,
			obj.GrandRecordId	,
			obj.Name			,
			obj.RecordTypeId	,
			obj.Number			,
			obj.Date			,
			obj.Amount			,
			obj.IsActive		,
			obj.Comment			
		};
	}

}