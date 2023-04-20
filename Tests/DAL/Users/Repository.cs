using System.Data;
using Artisan.Orm;
using Microsoft.Data.SqlClient;
using Tests.DAL.Users.Models;

namespace Tests.DAL.Users;


public class Repository: RepositoryBase
{
	#region [ GetUserById ]


	public User GetUserById(int id)
	{
		return GetByCommand(cmd =>
		{
			cmd.UseProcedure("dbo.GetUserById");

			cmd.AddIntParam("@Id", id);

			return cmd.ReadTo<User>();
		});
	}


	public async Task<User> GetUserByIdAsync(int id)
	{
		return await GetByCommandAsync(cmd =>
		{
			cmd.UseProcedure("dbo.GetUserById");

			cmd.AddIntParam("@Id", id);

			return cmd.ReadToAsync<User>();
		});
	}


	public User GetUserByIdWithSql(int id)
	{
		return GetByCommand(cmd =>
		{
			cmd.UseSql("select * from vwUsers where Id = @Id");
			cmd.AddIntParam("@Id", id);

			return cmd.ReadTo<User>();
		});
	}


	#endregion 


	#region [ GetUsers ]


	public IList<User> GetUsers()
	{
		return GetByCommand(cmd =>
		{
			cmd.UseProcedure("dbo.GetUsers");

			return cmd.ReadToList<User>();
		});
	}

	public async Task<IList<User>> GetUsersAsync()
	{
		return await GetByCommandAsync(cmd =>
		{
			cmd.UseProcedure("dbo.GetUsers");

			return cmd.ReadToListAsync<User>();
		});
	}


	#endregion


	#region [ GetUsers ]


	public ObjectRows GetUserRows()
	{
		return GetByCommand(cmd =>
		{
			cmd.UseProcedure("dbo.GetUsers");

			return cmd.ReadToObjectRows<User>();
		});
	}

	public async Task<ObjectRows> GetUserRowsAsync()
	{
		return await GetByCommandAsync(cmd =>
		{
			cmd.UseProcedure("dbo.GetUsers");

			return cmd.ReadToObjectRowsAsync<User>();
		});
	}


	public IList<User> GetUsersWithRoles()
	{
		return GetByCommand(cmd =>
		{
			cmd.UseSql( "select * from dbo.vwUsers; " +
						"select UserId, RoleId from dbo.UserRoles;" +
						"select Id, Code, Name from dbo.Roles");

			return cmd.GetByReader(reader =>
			{
				var users = reader.ReadToList<User>();
				var userRoles = reader.ReadToList(r => new {UserId = r.GetInt32(0), RoleId = r.GetByte(1)});
				var roles = reader.ReadAsList<Role>();
			
				reader.Close();

				foreach (var user in users)
				{
					user.Roles = new List<Role>();

					foreach (var role in roles)
						if (userRoles.Any(ur => ur.UserId == user.Id && ur.RoleId == role.Id))
							user.Roles.Add(role);
				}

				return users;
			});
		});
	}


	#endregion


	#region [ Save ONE User ]


	public User SaveUser(User user)
	{
		return GetByCommand(cmd =>
		{
			cmd.UseProcedure("dbo.SaveUser");

			cmd.AddTableRowParam("@User", user);

			cmd.AddTableParam("@RoleIds", user.RoleIds);

			return cmd.GetByReader(ReadSavedUser);
		});
	}

	public async Task<User> SaveUserAsync(User user)
	{
		return await GetByCommandAsync(cmd =>
		{
			cmd.UseProcedure("dbo.SaveUser");

			cmd.AddTableRowParam("@User", user);

			cmd.AddTableParam("@RoleIds", user.RoleIds);

			return cmd.GetByReaderAsync(ReadSavedUser);
		});
	}

	private static User ReadSavedUser(SqlDataReader reader)
	{
		CheckForDataReplyException(reader);

		return reader.ReadTo<User>();
	}
	

	#endregion


	#region [ Save MANY Users ]


	public IList<User> SaveUsers(IList<User> users)
	{
		return GetByCommand(cmd =>
		{
			cmd.UseProcedure("dbo.SaveUsers");

			cmd.AddTableParam("@Users", users);
			
			return cmd.GetByReader(ReadSavedUsers);
		});
	}

	public async Task<IList<User>> SaveUsersAsync(IList<User> users)
	{
		return await GetByCommandAsync(cmd =>
		{
			cmd.UseProcedure("dbo.SaveUsers");

			cmd.AddTableParam("@Users", users);

			return cmd.GetByReaderAsync(ReadSavedUsers);
		});
	}


	private static IList<User> ReadSavedUsers(SqlDataReader reader)
	{
		CheckForDataReplyException(reader);

		return reader.ReadToList<User>();
	}


	#endregion


	#region [ Delete User ]


	public bool DeleteUser(int userId)
	{
		//var returnValue = ExecuteCommand(cmd =>
		//{
		//	cmd.UseProcedure("dbo.DeleteUser");
		//	cmd.AddIntParam("@UserId", userId);
		//});

		var returnValue = Execute("dbo.DeleteUser", cmd =>
		{
			cmd.AddIntParam("@UserId", userId);
		});

		
		if (returnValue == 1)
			throw new DataReplyException(DataReplyStatus.Fail, "UNDELETABLE", "The Heros can not be deleted", userId);

		if (returnValue == 2)
			throw new DataReplyException(DataReplyStatus.Missing, "USER_IS_MISSING", userId);

		return true;
	}


	public async Task<bool> DeleteUserAsync(int userId)
	{
		//var returnValue = await ExecuteCommandAsync(cmd =>
		//{
		//	cmd.UseProcedure("dbo.DeleteUser");
		//	cmd.AddIntParam("@UserId", userId);
		//});

		var returnValue = await ExecuteAsync("dbo.DeleteUser", cmd =>
		{
			cmd.AddIntParam("@UserId", userId);
		});
		
		if (returnValue == 1)
			throw new DataReplyException(DataReplyStatus.Fail, "UNDELETABLE", "Heros can not be deleted", userId);

		if (returnValue == 2)
			throw new DataReplyException(DataReplyStatus.Missing, "USER_NOT_FOUND", userId);

		return true;
	}

	public void DeleteTwoUsers(int userId1, int userId2)
	{
		BeginTransaction(tran =>
		{
			ExecuteCommand(cmd =>
			{
				cmd.UseProcedure("dbo.DeleteUser");
				cmd.AddIntParam("@UserId", userId1);
			});

			ExecuteCommand(cmd =>
			{
				cmd.UseProcedure("dbo.DeleteUser");
				cmd.AddIntParam("@UserId", userId2);
			});

			tran.Commit();
		});

	}


	public void CheckRuleForUser(int userId)
	{
		BeginTransaction(IsolationLevel.RepeatableRead, tran =>
		{
			var user = GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetUserById");
				cmd.AddIntParam("@Id", userId);

				return cmd.ReadTo<User>();
			});

			if (Array.IndexOf(user.RoleIds, 2) > -1 && Array.IndexOf(user.RoleIds, 3) == -1)
			{
				var newRoleIds = user.RoleIds.ToList();
				newRoleIds.Add(3);
				user.RoleIds = newRoleIds.ToArray();
			}
			
			ExecuteCommand(cmd =>
			{
				cmd.UseProcedure("dbo.SaveUser");

				cmd.AddTableRowParam("@User", user);
				cmd.AddTableParam("@RoleIds", user.RoleIds);
			});


			tran.Commit();
		});
	}



	//	if to call this method like
	//		await _repository.DeleteUserAsyncException(1);
	//	it does not throw exception 
	//
	//public async Task DeleteUserAsyncException(Int32 userId)
	//{
	//	await RunCommandAsync(async cmd =>
	//	{
	//		cmd.UseProcedure("dbo.DeleteUser");
	//		cmd.AddIntParam("@UserId", userId);

		  
	//		var returnValueParam = cmd.ReturnValueParam();

	//		try
	//		{
	//			cmd.Connection.Open();

	//			await cmd.ExecuteNonQueryAsync();
	//		}
	//		finally
	//		{
	//			cmd.Connection.Close();
	//		}

	//		int returnValue = (int)returnValueParam.Value;

	//		if (returnValue == 1)
	//			throw new DataValidationException("UNDELETABLE", "Heros can not be deleted");

	//	});
	//}


	#endregion

}
