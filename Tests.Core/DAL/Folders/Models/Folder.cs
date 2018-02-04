using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Artisan.Orm;
using Newtonsoft.Json;

namespace Tests.DAL.Folders.Models
{
	public class Folder: INode<Folder>
	{

		public Int32 Id { get; set; }

		public Int32? ParentId { get; set; }

		[JsonIgnore]
		public Folder Parent { get; set; }

		public String Name { get; set; }
		
		public Int16 Level { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public String Path { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public String HidCode { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public String HidPath { get; set; }

		[JsonIgnore]
		public IList<Folder> Children { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public IList<Folder> SubFolders
		{
			get { return Children; }
			set { Children = value; }
		}
	}


	[MapperFor(typeof(Folder), RequiredMethod.All)] // https://github.com/lobodava/artisan-orm/wiki/Mappers
	public static class FolderMapper 
	{
		public static Folder CreateObject(SqlDataReader dr)
		{
			var i = 0;
			
			return new Folder 
			{
				Id			=	dr.GetInt32			(i)		,
				ParentId	=	dr.GetInt32Nullable	(++i)	,
				Name		=	dr.GetString		(++i)	,
				Level		=	dr.GetInt16			(++i)	,
				
				HidCode		=	++i < dr.FieldCount ? dr.GetStringNullable(i) : null,
				HidPath		=	++i < dr.FieldCount ? dr.GetStringNullable(i) : null,
				Path		=	++i < dr.FieldCount ? dr.GetStringNullable(i) : null,

			};
		}

		public static ObjectRow CreateObjectRow(SqlDataReader dr)
		{
			var i = 0;
			
			return new ObjectRow(6) // https://github.com/lobodava/artisan-orm/wiki/What-is-ObjectRow%3F
			{
				/*	Id			 0	*/	dr.GetInt32			(i)		,
				/*	ParentId	 1	*/	dr.GetInt32Nullable	(++i)	,
				/*	Name		 2	*/	dr.GetString		(++i)	,
				/*	Level		 3	*/	dr.GetInt16			(++i)	,

				/*	HidCode		 4  */	++i < dr.FieldCount ? dr.GetStringNullable(i) : null,
				/*	HidPath		 5  */	++i < dr.FieldCount ? dr.GetStringNullable(i) : null
			};
		}
	
		public static DataTable CreateDataTable()
		{
			return new DataTable("FolderTableType")
			
				.AddColumn< Int32	>(	"Id"		)
				.AddColumn< Int32	>(	"ParentId"	)
				.AddColumn< String	>(	"Name"		);
		}

		public static Object[] CreateDataRow(Folder obj)
		{
			return new object[]
			{
				obj.Id			,
				obj.ParentId	,
				obj.Name
			};
		}

	}
}