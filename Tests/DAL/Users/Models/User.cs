using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Artisan.Orm;
using Newtonsoft.Json;

namespace Tests.DAL.Users.Models
{
	public class User 
	{
		public Int32 Id { get; set; }

		public String Login { get; set; }

		public String Name { get; set; }

		public String Email { get; set; }

		[JsonConverter(typeof(ByteArrayConverter))]
		public Byte[] RoleIds { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
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
				Id		=	dr.GetInt32(i++)	,
				Login	=	dr.GetString(i++)	,
				Name	=	dr.GetString(i++)	,
				Email	=	dr.GetString(i++)	,
				RoleIds	=	(i + 1 <= dr.FieldCount) ? dr.GetByteArrayFromString(i) : null
			};
		}

		public static ObjectRow CreateObjectRow(SqlDataReader dr)
		{
			var i = 0;
			
			return new ObjectRow(5)
			{
				/* 0 - Id		=	*/	dr.GetInt32(i++)	,
				/* 1 - Login	=	*/	dr.GetString(i++)	,
				/* 2 - Name		=	*/	dr.GetString(i++)	,
				/* 3 - Email	=	*/	dr.GetString(i++)	,
				/* 4 - RoleIds	=	*/	dr.GetInt16ArrayFromString(i++)
			};
		}

	
		public static DataTable CreateDataTable()
		{
			var table = new DataTable("UserTableType");
			
			table.Columns.Add(	"Id"		,	typeof( Int32	));
			table.Columns.Add(	"Login"		,	typeof( String	));
			table.Columns.Add(	"Name"		,	typeof( String	));
			table.Columns.Add(	"Email"		,	typeof( String	));
			table.Columns.Add(	"RoleIds"	,	typeof( String	));

			return table;
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
				obj.RoleIds == null ? null : String.Join(",", obj.RoleIds)
			};
		}

	}
}