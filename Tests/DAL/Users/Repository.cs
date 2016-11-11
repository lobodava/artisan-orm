using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Artisan.Orm;
using Tests.DAL.Users.Models;

namespace Tests.DAL.Users
{


	public class Repository: RepositoryBase
	{
		//#if DEBUG
		//	public Repository () : base ("AnotherConnection", "Debug") {}
		//#else
		//	public Repository () : base ("AnotherConnection", "Release") {}
		//#endif

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

				//return cmd.GetByReader(reader =>
				//{
				//	if (GetDataStatus(reader.ReadTo<string>()) == DataStatus.Warning)
				//	{
				//		throw new DataWarningException(reader.ReadToArray<DataMessage>());
				//	}

				//	return reader.ReadTo<User>();
				//});
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
			if (GetDataStatus(reader.ReadTo<string>()) == DataStatus.Warning)
			{
				throw new DataWarningException(reader.ReadToArray<DataMessage>());
			}

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

				//return cmd.GetByReader(reader =>
				//{
				//	if (GetDataStatus(reader.ReadTo<string>()) == DataStatus.Warning)
				//	{
				//		throw new DataWarningException(reader.ReadToArray<DataMessage>());
				//	}

				//	return reader.ReadToList<User>();
				//});
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
			if (GetDataStatus(reader.ReadTo<string>()) == DataStatus.Warning)
			{
				throw new DataWarningException(reader.ReadToArray<DataMessage>());
			}

			return reader.ReadToList<User>();
		}


		#endregion


		#region [ Delete User ]

		public void DeleteUser(Int32 userId)
		{
			var returnValue = ExecuteCommand(cmd =>
			{
				cmd.UseProcedure("dbo.DeleteUser");
				cmd.AddIntParam("@UserId", userId);
			});
			
			if (returnValue == 1)
				throw new DataWarningException("UNDELETABLE", "The Boss can not be deleted");

			if (returnValue == 2)
				throw new DataNotFoundException($"User with Id = {userId} does not exist and cannot be deleted");
		}


		public async Task<Boolean> DeleteUserAsync(Int32 userId)
		{
			var returnValue = await ExecuteCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.DeleteUser");
				cmd.AddIntParam("@UserId", userId);
			});
			
			if (returnValue == 1)
				throw new DataWarningException("UNDELETABLE","Heros can not be deleted", "Id", userId);

			if (returnValue == 2)
				throw new DataNotFoundException($"User with Id = {userId} does not exist and cannot be deleted");

			return true;
		}

		public void DeleteTwoUsers(Int32 userId1, Int32 userId2)
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


		public void CheckRuleForUser(Int32 userId)
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
		//			throw new DataWarningException("UNDELETABLE", "Heros can not be deleted");

		//	});
		//}


		#endregion


	}
}
