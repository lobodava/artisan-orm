using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Artisan.Orm;
using Newtonsoft.Json;

namespace Tests.DAL.GrandRecords.Models
{
	public class Record
	{
		public Int32 Id { get; set; }

		public Int32 GrandRecordId { get; set; }

		[JsonIgnore]
		public GrandRecord GrandRecord  { get; set; }
		
		public String Name { get; set; }

		public Byte? RecordTypeId { get; set; }
		public RecordType RecordType { get; set; }

		public Int16? Number { get; set; }

		public DateTime? Date { get; set; }

		public Decimal? Amount { get; set; }

		public Boolean? IsActive { get; set; }

		public String Comment { get; set; }

		public IList<ChildRecord> ChildRecords  { get; set; }
	}


	[MapperFor(typeof(Record), RequiredMethod.All)]
	public static class RecordMapper 
	{
		public static Record CreateObject(SqlDataReader dr)
		{
			var i = 0;
			
			return new Record 
			{
				Id				=	dr.GetInt32(i++)			,
				GrandRecordId	=	dr.GetInt32(i++)			,
				Name			=	dr.GetString(i++)			,
				RecordTypeId	=	dr.GetByteNullable(i++)		,
				Number			=	dr.GetInt16Nullable(i++)	,
				Date			=	dr.GetDateTimeNullable(i++)	,
				Amount			=	dr.GetDecimalNullable(i++)	,
				IsActive		=	dr.GetBooleanNullable(i++)	,
				Comment			=	dr.GetStringNullable(i++)	,

				RecordType		=	RecordTypeMapper.CreateObject(dr, ref i),

				ChildRecords	=	new List<ChildRecord>()
			};
		}

		public static Object[] CreateObjectRow(SqlDataReader dr)
		{
			var i = 0;

			return new Object[]
			{
				/*	Id				=	*/	dr.GetInt32(i++)			,
				/*	GrandRecordId	=	*/	dr.GetInt32(i++)			,
				/*	Name			=	*/	dr.GetString(i++)			,
				/*	RecordTypeId	=	*/	dr.GetByteNullable(i++)		,
				/*	Number			=	*/	dr.GetInt16Nullable(i++)	,
				/*	Date			=	*/	dr.GetDateTimeNullable(i++)	,
				/*	Amount			=	*/	dr.GetDecimalNullable(i++)	,
				/*	IsActive		=	*/	dr.GetBooleanNullable(i++)	,
				/*	Comment			=	*/	dr.GetStringNullable(i++)	,
			};
		}


		public static DataTable CreateDataTable()
		{
			var table = new DataTable("RecordTableType");
			
			table.Columns.Add(	"Id"			,	typeof( Int32		));
			table.Columns.Add(	"GrandRecordId"	,	typeof( Int32		));
			table.Columns.Add(	"Name"			,	typeof( String		));
			table.Columns.Add(	"RecordTypeId"	,	typeof( Byte		));
			table.Columns.Add(	"Number"		,	typeof( Int16		));
			table.Columns.Add(	"Date"			,	typeof( DateTime	));
			table.Columns.Add(	"Amount"		,	typeof( Decimal		));
			table.Columns.Add(	"IsActive"		,	typeof( Boolean		));
			table.Columns.Add(	"Comment"		,	typeof( String		));

			return table;
		}

		public static Object[] CreateDataRow(Record obj)
		{
			if (obj.Id == 0) 
				obj.Id = Int32NegativeIdentity.Next;

			if (obj.GrandRecordId == 0 && obj.GrandRecord != null)
				obj.GrandRecordId = obj.GrandRecord.Id;

			if (obj.RecordTypeId == null && obj.RecordType != null)
				obj.RecordTypeId = obj.RecordType.Id;

			foreach (var childRecord in obj.ChildRecords)
				childRecord.RecordId = obj.Id;


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
}