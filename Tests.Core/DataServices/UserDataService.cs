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
			return  base.Get(() => _repository.GetUserById(id));
		}

		public DataReply<User> Save(User user)
		{
			return base.Get(() => _repository.SaveUser(user));
		}


		public DataReply Delete(int userId)
		{
			return base.Execute(() => _repository.DeleteUser(userId));
		}

		// async

		public async Task<DataReply<User>> GetByIdAsync(int id) 
		{
			return await base.GetAsync(() => _repository.GetUserByIdAsync(id));
		}

		public async Task<DataReply<User>> SaveAsync(User user) 
		{
			return await base.GetAsync(() => _repository.SaveUserAsync(user));
		}


		public async Task<DataReply> DeleteAsync(int userId) 
		{
			return await base.ExecuteAsync(() => _repository.DeleteUserAsync(userId));
		}

	}
}

