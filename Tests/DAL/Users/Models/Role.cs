using Artisan.Orm;
using Microsoft.Data.SqlClient;

namespace Tests.DAL.Users.Models;

public class Role
{
	public byte Id { get; set; }

	public string Code { get; set; }

	public string Name { get; set; }
}


[MapperFor(typeof(Role), RequiredMethod.CreateObject)]
public static class RoleMapper
{
	public static Role CreateObject(SqlDataReader dr)
	{
		var i = 0;

		return new Role
		{
			Id		=	dr.GetByte(i)		,
			Code	=	dr.GetString(++i)	,
			Name	=	dr.GetString(++i)
		};
	}

}
