using System;

namespace Tests.DAL.Users.Models
{
	public class Role
	{
		public Byte Id { get; set; }

		public String Code { get; set; }

		public String Name { get; set; }
	}


//	[MapperFor(typeof(Role), RequiredMethod.CreateObject)]
//	public static class RoleMapper 
//	{
//		public static Role CreateObject(SqlDataReader dr)
//		{
//			var index = 0;

//			return new Role 
//			{
//				Id		=	dr.GetByte(index++)		,
//				Code	=	dr.GetString(index++)	,
//				Name	=	dr.GetString(index++)	
//			};
//		}

//	}
}