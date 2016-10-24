using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Artisan.Orm;
using Newtonsoft.Json;

namespace Tests.DAL.GrandRecords.Models
{
	public class Record : IEntity
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
		public static Record CreateEntity(SqlDataReader dr)
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

				RecordType		=	RecordTypeMapper.CreateEntity(dr, ref i),

				ChildRecords	=	new List<ChildRecord>()
			};
		}

		public static Object[] CreateEntityRow(SqlDataReader dr)
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

		public static Object[] CreateDataRow(Record entity)
		{
			if (entity.Id == 0) 
				entity.Id = Int32NegativeIdentity.Next;

			if (entity.GrandRecordId == 0 && entity.GrandRecord != null)
				entity.GrandRecordId = entity.GrandRecord.Id;

			if (entity.RecordTypeId == null && entity.RecordType != null)
				entity.RecordTypeId = entity.RecordType.Id;

			foreach (var childRecord in entity.ChildRecords)
				childRecord.RecordId = entity.Id;


			return new object[]
			{
				entity.Id				,
				entity.GrandRecordId	,
				entity.Name				,
				entity.RecordTypeId		,
				entity.Number			,
				entity.Date				,
				entity.Amount			,
				entity.IsActive			,
				entity.Comment			
			};
		}

	}
}