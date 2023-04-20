using System.Threading.Tasks;
using Artisan.Orm;
using Tests.DAL.Users;
using Tests.DAL.Users.Models;

namespace Tests.DataServices
{
	public class UserDataService: DataServiceBase
	{
		private readonly Repository _repository;

		public UserDataService()
		{
			base.Repository = _repository = new Repository();
		}


		// sync

		public DataReply<User> GetById(int id)
		{
			return Get(() => _repository.GetUserById(id));
		}

		public DataReply<User> Save(User user)
		{
			return Get(() => _repository.SaveUser(user));
		}


		public DataReply Delete(int userId)
		{
			return Execute(() => _repository.DeleteUser(userId));
		}

		// async

		public async Task<DataReply<User>> GetByIdAsync(int id) 
		{
			return await GetAsync(() => _repository.GetUserByIdAsync(id));
		}

		public async Task<DataReply<User>> SaveAsync(User user) 
		{
			return await GetAsync(() => _repository.SaveUserAsync(user));
		}


		public async Task<DataReply> DeleteAsync(int userId) 
		{
			return await ExecuteAsync(() => _repository.DeleteUserAsync(userId));
		}

	}
}

