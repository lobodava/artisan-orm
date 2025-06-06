using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json.Serialization;
using Artisan.Orm;
using Microsoft.Data.SqlClient;

namespace Tests.DAL.Users.Models
{
	public class User 
	{
		public Int32 Id { get; set; }

		public String Login { get; set; }

		public String Name { get; set; }

		public String Email { get; set; }

		public String RowVersion { get; set; }

		[JsonConverter(typeof(ByteArrayConverter))]
		public Byte[] RoleIds { get; set; }

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

			.AddColumn< Int32	>(	"Id"			)
			.AddColumn< String	>(	"Login"			)
			.AddColumn< String	>(	"Name"			)
			.AddColumn< String	>(	"Email"			)
			.AddColumn< Byte[]	>(	"RowVersion"	)
			.AddColumn< String	>(	"RoleIds"		);
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
				obj.RoleIds == null ? null : String.Join(",", obj.RoleIds)
			};
		}

	}
}