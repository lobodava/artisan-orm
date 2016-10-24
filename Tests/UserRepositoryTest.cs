using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Tests.DAL.Users;
using Tests.DAL.Users.Models;

namespace Tests
{
	[TestClass]
	public class UserRepositoryTest
	{
		private Repository _repository;

		[TestInitialize]
		public void TestInitialize()
		{
			_repository = new Repository();

			_repository.ExecuteCommand(cmd => {
				cmd.UseSql("delete from dbo.Users where Id > 14;");	
			});

		}

		[TestMethod]
		public void GetUserById()
		{
			User user = null;

			var sw = new Stopwatch();
			sw.Start();
			
			for (var i = 1; i <= 14; i++)
			{
				user = _repository.GetUserById(i);

				Assert.IsTrue(user.Id == i || user == null);
			}

			sw.Stop();

			Console.WriteLine($"GetUserById reads 14 times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / 14).ToString("0.##")} ms for one read" );
			Console.Write(JsonConvert.SerializeObject(user));
		}


		[TestMethod]
		public async Task GetUserByIdAsync()
		{
			User user = null;

			var sw = new Stopwatch();
			sw.Start();
			
			for (var i = 1; i <= 14; i++)
			{
				user = await _repository.GetUserByIdAsync(i);

				Assert.IsTrue(user.Id == i || user == null);
			}

			sw.Stop();

			Console.WriteLine($"GetUserByIdAsync reads 14 times for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms, or {(sw.Elapsed.TotalMilliseconds / 14).ToString("0.##")} ms for one read" );
			Console.Write(JsonConvert.SerializeObject(user));
		}
		


		[TestMethod]
		public void GetUsers()
		{
			var sw = new Stopwatch();
			sw.Start();

			var users  = _repository.GetUsers();
	
			sw.Stop();

			Assert.IsNotNull(users);
			Assert.IsTrue(users.Count > 0);

			Console.WriteLine($"GetUsers reads {users.Count} users for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.Write(JsonConvert.SerializeObject(users));
		}


		[TestMethod]
		public async Task GetUsersAsync()
		{
			var sw = new Stopwatch();
			sw.Start();

			var users  = await _repository.GetUsersAsync();
	
			sw.Stop();

			Assert.IsNotNull(users);
			Assert.IsTrue(users.Count > 0);

			Console.WriteLine($"GetUsersAsync reads {users.Count} users for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.Write(JsonConvert.SerializeObject(users));
		}


		[TestMethod]
		public void GetUserRows()
		{
			var sw = new Stopwatch();
			sw.Start();

			var users  = _repository.GetUserRows();
	
			sw.Stop();

			Assert.IsNotNull(users);
			Assert.IsTrue(users.Count > 0);

			Console.WriteLine($"GetUsers reads {users.Count} users for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.Write(JsonConvert.SerializeObject(users));

		}


		[TestMethod]
		public async Task  GetUserRowsAsync()
		{
			var sw = new Stopwatch();
			sw.Start();

			var users  = await _repository.GetUserRowsAsync();
	
			sw.Stop();

			Assert.IsNotNull(users);
			Assert.IsTrue(users.Count > 0);

			Console.WriteLine($"GetUsers reads {users.Count} users for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.Write(JsonConvert.SerializeObject(users));

		}




		[TestMethod]
		public void SaveUser()
		{
			var user = CreateNewUser();

			var sw = new Stopwatch();
			sw.Start();

			var savedUser = _repository.SaveUser(user);

			sw.Stop();

			Assert.IsNotNull(savedUser);
			Assert.IsTrue(savedUser.Id > 14);
			Assert.AreEqual(savedUser.Login, user.Login);

			Console.WriteLine($"Method SaveUser saved a User and read it back for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.WriteLine();
			Console.Write(JsonConvert.SerializeObject(savedUser));
		}




		[TestMethod]
		public async Task SaveUserAsync()
		{
			var user = CreateNewUser();

			var sw = new Stopwatch();
			sw.Start();

			var savedUser = await _repository.SaveUserAsync(user);

			sw.Stop();

			Assert.IsNotNull(savedUser);
			Assert.IsTrue(savedUser.Id > 14);
			Assert.AreEqual(savedUser.Login, user.Login);

			Console.WriteLine($"Method SaveUserAsync saved a User and read it back for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.WriteLine();
			Console.Write(JsonConvert.SerializeObject(savedUser));
		}




		[TestMethod]
		public void SaveUsers()
		{
			var users = new List<User>() {CreateNewUser(), CreateNewUser(), CreateNewUser()};

			var sw = new Stopwatch();
			sw.Start();

			var savedUsers = _repository.SaveUsers(users);

			sw.Stop();

			Assert.IsNotNull(savedUsers);
			Assert.IsTrue(savedUsers.Count == 3);
			Assert.IsTrue(savedUsers.First().Id > 14);

			Console.WriteLine($"Method SaveUsers saved and read back 3 Users for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.WriteLine();
			Console.WriteLine("The saved Users look like: ");
			Console.WriteLine();
			Console.Write(JsonConvert.SerializeObject(savedUsers.Take(10)));
		}

		[TestMethod]
		public async Task SaveUsersAsync()
		{
			var users = new List<User>() {CreateNewUser(), CreateNewUser(), CreateNewUser()};

			var sw = new Stopwatch();
			sw.Start();

			var savedUsers = await _repository.SaveUsersAsync(users);

			sw.Stop();

			Assert.IsNotNull(savedUsers);
			Assert.IsTrue(savedUsers.Count == 3);
			Assert.IsTrue(savedUsers.First().Id > 14);

			Console.WriteLine($"Method SaveUsers saved and read back 3 Users for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
			Console.WriteLine();
			Console.WriteLine("The saved Users look like: ");
			Console.WriteLine();
			Console.Write(JsonConvert.SerializeObject(savedUsers.Take(10)));
		}



		
		[TestMethod]
		public void SaveUserDataWarningException()
		{
			var user = _repository.GetUserById(1);
			user.Id = 0;

			try
			{
				var savedUser = _repository.SaveUser(user);
				Assert.Fail();
			}
			catch (DataWarningException ex)
			{
				Assert.IsNotNull(ex.DataMessages.FirstOrDefault(dm => dm.Code == "NON_UNIQUE_LOGIN"));
				Assert.IsNotNull(ex.DataMessages.FirstOrDefault(dm => dm.Code == "NON_UNIQUE_NAME"));
				Assert.IsNotNull(ex.DataMessages.FirstOrDefault(dm => dm.Code == "NON_UNIQUE_EMAIL"));

				Console.WriteLine("DataWarningException.DataMessages: ");
				Console.WriteLine();
				Console.Write(JsonConvert.SerializeObject(ex.DataMessages));
			}
			catch (Exception )
			{
				Assert.Fail();
			}
		}
		
		[TestMethod]
		public async Task SaveUserDataWarningExceptionAsync()
		{
			var user = _repository.GetUserById(1);
			user.Id = 0;

			try
			{
				var savedUser = await _repository.SaveUserAsync(user);
				Assert.Fail();
			}
			catch (DataWarningException ex)
			{
				Assert.IsNotNull(ex.DataMessages.FirstOrDefault(dm => dm.Code == "NON_UNIQUE_LOGIN"));
				Assert.IsNotNull(ex.DataMessages.FirstOrDefault(dm => dm.Code == "NON_UNIQUE_NAME"));
				Assert.IsNotNull(ex.DataMessages.FirstOrDefault(dm => dm.Code == "NON_UNIQUE_EMAIL"));

				Console.WriteLine("DataWarningException.DataMessages: ");
				Console.WriteLine();
				Console.Write(JsonConvert.SerializeObject(ex.DataMessages));
			}
			catch (Exception )
			{
				Assert.Fail();
			}
		}
		
		[TestMethod]
		public void SaveUsersDataWarningException()
		{
			var user1 = _repository.GetUserById(1);
			var user2 = _repository.GetUserById(2);
			user1.Id = 0;
			user2.Id = 0;

			var users = new List<User>() {user1, user2};
			
			try
			{
				var savedUser = _repository.SaveUsers(users);
				Assert.Fail();
			}
			catch (DataWarningException ex)
			{
				Assert.IsNotNull(ex.DataMessages.FirstOrDefault(dm => dm.Code == "NON_UNIQUE_LOGIN"));
				Assert.IsNotNull(ex.DataMessages.FirstOrDefault(dm => dm.Code == "NON_UNIQUE_NAME"));
				Assert.IsNotNull(ex.DataMessages.FirstOrDefault(dm => dm.Code == "NON_UNIQUE_EMAIL"));

				Console.WriteLine("DataWarningException.DataMessages: ");
				Console.WriteLine();
				Console.Write(JsonConvert.SerializeObject(ex.DataMessages));
			}
			catch (Exception )
			{
				Assert.Fail();
			}
		}
		
		[TestMethod]
		public async Task SaveUsersDataWarningExceptionAsync()
		{
			var user1 = _repository.GetUserById(1);
			var user2 = _repository.GetUserById(2);
			user1.Id = 0;
			user2.Id = 0;

			var users = new List<User>() {user1, user2};

			try
			{
				var savedUser = await _repository.SaveUsersAsync(users);
				Assert.Fail();
			}
			catch (DataWarningException ex)
			{
				Assert.IsNotNull(ex.DataMessages.FirstOrDefault(dm => dm.Code == "NON_UNIQUE_LOGIN"));
				Assert.IsNotNull(ex.DataMessages.FirstOrDefault(dm => dm.Code == "NON_UNIQUE_NAME"));
				Assert.IsNotNull(ex.DataMessages.FirstOrDefault(dm => dm.Code == "NON_UNIQUE_EMAIL"));

				Console.WriteLine("DataWarningException.DataMessages: ");
				Console.WriteLine();
				Console.Write(JsonConvert.SerializeObject(ex.DataMessages));
			}
			catch (Exception )
			{
				Assert.Fail();
			}
		}


		[TestMethod]
		public void DeleteUser()
		{
			var user = CreateNewUser();
			var savedUser = _repository.SaveUser(user);

			Assert.IsNotNull(savedUser);
			Assert.IsTrue(savedUser.Id > 14);
			Assert.AreEqual(savedUser.Login, user.Login);
	
			var userId = savedUser.Id;

			_repository.DeleteUser(userId);

			user = _repository.GetUserById(userId);

			Assert.IsNull(user);
		}


		[TestMethod]
		public async Task DeleteUserAsync()
		{
			var user = CreateNewUser();
			var savedUser = await _repository.SaveUserAsync(user);

			Assert.IsNotNull(savedUser);
			Assert.IsTrue(savedUser.Id > 14);
			Assert.AreEqual(savedUser.Login, user.Login);
	
			var userId = savedUser.Id;

			var isSuccess = await _repository.DeleteUserAsync(userId);

			user = await _repository.GetUserByIdAsync(userId);

			Assert.IsNull(user);
		}

		[TestMethod]
		public async Task DeleteUserAsyncException()
		{
			try
			{
				var isSuccess = await _repository.DeleteUserAsync(1);
				Assert.Fail();
			}
			catch (DataWarningException ex)
			{
				Assert.IsNotNull(ex.DataMessages.FirstOrDefault(dm => dm.Code == "UNDELETABLE"));

				Console.WriteLine("DataWarningException.DataMessages: ");
				Console.WriteLine();
				Console.Write(JsonConvert.SerializeObject(ex.DataMessages));
			}
			catch (Exception )
			{
				Assert.Fail();
			}
		}


		//[TestMethod]
		//public async Task DeleteUserAsyncException()
		//{
		//	await _repository.DeleteUserAsyncException(1);
		//}




		private User CreateNewUser()
		{
			var user = _repository.GetUserById(1);
			
			var roleIds = new byte[]
			{
				(byte)random.Next(1, 22),
				(byte)random.Next(1, 22),
				(byte)random.Next(1, 22)
			}
			.Distinct().ToArray();


			user.Id = 0;
			user.Login = RandomString(10);
			user.Name = user.Login;
			user.Email = $"{user.Login}@mail.com";
			user.RoleIds = roleIds;

			return user;
		}

		// http://stackoverflow.com/a/1344242/623190
		private static Random random = new Random();
		public static string RandomString(int length)
		{
			const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
			return new string(Enumerable.Repeat(chars, length)
			  .Select(s => s[random.Next(s.Length)]).ToArray());
		}


		[TestCleanup]
		public void Dispose()
		{
			_repository.Dispose();
		}

	}
}
