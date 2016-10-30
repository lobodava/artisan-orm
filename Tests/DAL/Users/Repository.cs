using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Artisan.Orm;
using Tests.DAL.Users.Models;

namespace Tests.DAL.Users
{
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


		public Rows GetUserRows()
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.GetUsers");

				return cmd.ReadToRows<User>();
			});
		}

		public async Task<Rows> GetUserRowsAsync()
		{
			return await GetByCommandAsync(cmd =>
			{
				cmd.UseProcedure("dbo.GetUsers");

				return cmd.ReadToRowsAsync<User>();
			});
		}


		#endregion



		#region [ Save ONE User ]


		public User SaveUser(User user)
		{
			return GetByCommand(cmd =>
			{
				cmd.UseProcedure("dbo.SaveUser");

				cmd.AddTableParam("@User", user);

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

				cmd.AddTableParam("@User", user);

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
