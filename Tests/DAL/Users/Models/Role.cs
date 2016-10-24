//using System;
//using System.Data.SqlClient;
//using Artisan.Orm;

//namespace Tests.DAL.Users.Models
//{
//	public class Role : IEntity
//	{
//		public Byte Id { get; set; }

//		public String Code { get; set; }

//		public String Name { get; set; }
//	}


//	[MapperFor(typeof(Role), RequiredMethod.CreateEntity)]
//	public static class RoleMapper 
//	{
//		public static Role CreateEntity(SqlDataReader dr)
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
//}