using Artisan.Orm;
using Tests.DAL.Users;
using Tests.DataServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests
{
	[TestClass]
	public class UserDataServiceTest
	{
		private UserDataService _service;

		[TestInitialize]
		public void TestInitialize()
		{
			_service = new UserDataService();

			var appSettings = new AppSettings();

			using (var repository = new Repository(appSettings.ConnectionStrings.DatabaseConnection))
			{
				repository.ExecuteCommand(cmd => {
					cmd.UseSql("delete from dbo.Users where Id > 14;");
				});
			};

		}
		
		[TestMethod]
		public void GetSaveDeleteUser()
		{
			var dataReplyT = _service.GetById(1);

			Assert.AreEqual(DataReplyStatus.Ok, dataReplyT.Status);

			var user = dataReplyT.Data;

			user.Id = 0;
			user.Login = $"{user.Login}a"; 
			user.Name = $"{user.Name}a"; 
			user.Email = $"a{user.Email}"; 

			dataReplyT = _service.Save(user);

			Assert.AreEqual(DataReplyStatus.Ok, dataReplyT.Status);

			user = dataReplyT.Data;

			Assert.IsNotNull(user);
			Assert.IsTrue(user.Id > 0);

			var dataReply = _service.Delete(user.Id);

			Assert.AreEqual(DataReplyStatus.Ok, dataReply.Status);
		}

		[TestMethod]
		public async Task GetSaveDeleteUserAsync()
		{
			var dataReplyT = await _service.GetByIdAsync(1);

			Assert.AreEqual(DataReplyStatus.Ok, dataReplyT.Status);

			var user = dataReplyT.Data;

			user.Id = 0;
			user.Login = $"{user.Login}a"; 
			user.Name = $"{user.Name}a"; 
			user.Email = $"a{user.Email}"; 

			dataReplyT = await _service.SaveAsync(user);

			Assert.AreEqual(DataReplyStatus.Ok, dataReplyT.Status);

			user = dataReplyT.Data;

			Assert.IsNotNull(user);
			Assert.IsTrue(user.Id > 0);

			var dataReply = await _service.DeleteAsync(user.Id);

			Assert.AreEqual(DataReplyStatus.Ok, dataReply.Status);
		}

	
		[TestMethod]
		public async Task ConcurrencyDataReplyStatusAsync()
		{
			var dataReplyT = await _service.GetByIdAsync(1);

			Assert.AreEqual(DataReplyStatus.Ok, dataReplyT.Status);

			var user = dataReplyT.Data;

			user.Id = 0;
			user.Login = $"{user.Login}b"; 
			user.Name = $"{user.Name}b"; 
			user.Email = $"b{user.Email}"; 

			dataReplyT = await _service.SaveAsync(user);

			Assert.AreEqual(DataReplyStatus.Ok, dataReplyT.Status);

			user = dataReplyT.Data;

			var dataReplyTConcurrent = await _service.GetByIdAsync(user.Id);

			var userConcurrent = dataReplyTConcurrent.Data;

			user.Name = $"{user.Name}c";

			dataReplyT = await _service.SaveAsync(user);

			Assert.AreEqual(DataReplyStatus.Ok, dataReplyT.Status);
			Assert.AreEqual(user.Name, dataReplyT.Data.Name);


			userConcurrent.Name = $"{user.Name}d";

			dataReplyTConcurrent = await _service.SaveAsync(userConcurrent);

			Assert.AreEqual(DataReplyStatus.Concurrency, dataReplyTConcurrent.Status);


			var dataReply = await _service.DeleteAsync(user.Id);

			Assert.AreEqual(DataReplyStatus.Ok, dataReply.Status);
		}


		[TestMethod]
		public void DeleteUndeletableUser()
		{
			var userId = 1;

			var dataReply = _service.Delete(userId);

			Assert.AreEqual(DataReplyStatus.Fail, dataReply.Status);

			Assert.AreEqual("UNDELETABLE", dataReply.Messages[0].Code);

			Assert.AreEqual(userId, dataReply.Messages[0].Id);
		}


		[TestMethod]
		public void DeleteMissingUser()
		{
			var userId = 10000000;

			var dataReply = _service.Delete(userId);

			Assert.AreEqual(DataReplyStatus.Missing, dataReply.Status);

			Assert.AreEqual("USER_IS_MISSING", dataReply.Messages[0].Code);

			Assert.AreEqual(userId, dataReply.Messages[0].Id);
		}

		[TestMethod]
		public void SaveUserValidation()
		{
			var dataReplyT = _service.GetById(1);

			Assert.AreEqual(DataReplyStatus.Ok, dataReplyT.Status);

			var user = dataReplyT.Data;

			user.Id = 0;

			dataReplyT = _service.Save(user);

			Assert.AreEqual(DataReplyStatus.Validation, dataReplyT.Status);

			Assert.AreEqual("NON_UNIQUE_LOGIN", dataReplyT.Messages[0].Code);

			Assert.AreEqual("NON_UNIQUE_NAME", dataReplyT.Messages[1].Code);

			Assert.AreEqual("NON_UNIQUE_EMAIL", dataReplyT.Messages[2].Code);

			Assert.IsNull(dataReplyT.Data);
		}


		
		[TestCleanup]
		public void Dispose()
		{
			_service.Dispose();
		}

	}
}
