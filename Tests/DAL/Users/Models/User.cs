using System.Data;
using System.Text.Json.Serialization;
using Artisan.Orm;
using Microsoft.Data.SqlClient;

namespace Tests.DAL.Users.Models;

public class User 
{
	public int Id { get; set; }

	public string Login { get; set; }

	public string Name { get; set; }

	public string Email { get; set; }

	public string RowVersion { get; set; }


	[JsonConverter(typeof(ByteArrayConverter))]
	public byte[] RoleIds { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public IList<Role> Roles { get; set; }
}


[MapperFor(typeof(User), RequiredMethod.All)]
public static class UserMapper 
{
	public static User CreateObject(SqlDataReader dr)
	{
		var i = 0;
		
		return new User 
		{
			Id			=	dr.GetInt32(i)	,
			Login		=	dr.GetString(++i)	,
			Name		=	dr.GetString(++i)	,
			Email		=	dr.GetString(++i)	,
			RowVersion	=	dr.GetBase64StringFromRowVersion(++i),
			RoleIds		=	++i < dr.FieldCount ? dr.GetByteArrayFromString(i) : null
		};
	}

	public static ObjectRow CreateObjectRow(SqlDataReader dr)
	{
		var i = 0;
		
		return new ObjectRow(5)
		{
			/* 0 - Id		  =	*/	dr.GetInt32(i)	,
			/* 1 - Login	  =	*/	dr.GetString(++i)	,
			/* 2 - Name		  =	*/	dr.GetString(++i)	,
			/* 3 - Email	  =	*/	dr.GetString(++i)	,
			/* 4 - RowVersion =	*/	dr.GetBase64StringFromRowVersion(++i),
			/* 5 - RoleIds	  =	*/	dr.GetInt16ArrayFromString(++i)
		};
	}


	public static DataTable CreateDataTable()
	{
		return new DataTable("UserTableType")

		.AddColumn< int		>(	"Id"			)
		.AddColumn< string	>(	"Login"			)
		.AddColumn< string	>(	"Name"			)
		.AddColumn< string	>(	"Email"			)
		.AddColumn< byte[]	>(	"RowVersion"	)
		.AddColumn< string	>(	"RoleIds"		);
	}

	public static object[] CreateDataRow(User obj)
	{
		if (obj.Id == 0) 
			obj.Id = Int32NegativeIdentity.Next;

		return new object[]
		{
			obj.Id		,
			obj.Login	,
			obj.Name	,
			obj.Email	,
			Convert.FromBase64String(obj.RowVersion),
			obj.RoleIds == null ? null : string.Join(",", obj.RoleIds)
		};
	}

}