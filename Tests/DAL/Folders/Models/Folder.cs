using System.Data;
using System.Text.Json.Serialization;
using Artisan.Orm;
using Microsoft.Data.SqlClient;

namespace Tests.DAL.Folders.Models;

public class Folder: INode<Folder>
{

	public int Id { get; set; }

	public int? ParentId { get; set; }

	[JsonIgnore]
	public Folder Parent { get; set; }

	public string Name { get; set; }
	
	public short Level { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string Path { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string HidCode { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string HidPath { get; set; }

	[JsonIgnore]
	public IList<Folder> Children { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
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
		
			.AddColumn<int>(	"Id"		)
			.AddColumn<int>(	"ParentId"	)
			.AddColumn<string>(	"Name"		);
	}

	public static object[] CreateDataRow(Folder obj)
	{
		return new object[]
		{
			obj.Id			,
			obj.ParentId	,
			obj.Name
		};
	}

}
