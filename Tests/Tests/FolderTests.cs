using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tests.DAL.Folders;
using Tests.DAL.Folders.Models;

namespace Tests.Tests
{
	[TestClass]
	public class FolderTests
	{

		// Use dbo.GenerateFolders stored procedure to generate (and regenerate) test data into database
		// If you used publish profile (Hierarchy.DB.publish.xml) to deploy database - test data is already generated 
		// Change @PowerIndex variable in dbo.GenerateFolders stored procedure to get other quantities of records
		// And publish database again 

		[TestMethod]
		public void GetFolderById()
		{
			using (var repository = new Repository())
			{
				var folder = repository.GetFolderById(5);

				Assert.IsNotNull(folder);
				
				Console.WriteLine("GetFolderById returned");
				Console.WriteLine();

				Console.Write(ToJson(folder));
			}

			// !!!!!   SEE CONSOLE OUTPUT  !!!!!!!!
		}



		[TestMethod]
		public void GetFolderWithSubFolders()
		{
			IList<Folder> folders;

			// just to eliminate time for test preparation and make a cold run before time measure
			using (var repository = new Repository())
				folders = repository.GetFolderWithSubFolders(1);


			var sw = new Stopwatch();
			sw.Start();

			using (var repository = new Repository())
				folders = repository.GetFolderWithSubFolders(1);

			sw.Stop();

			Assert.IsTrue(folders.Count > 0);

			Console.WriteLine($"GetFolderWithSubFolders read {folders.Count} folders");
			Console.WriteLine();
			Console.WriteLine($"for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms.");
			Console.WriteLine();

			for (var i = 0; i < folders.Count; i++)
			{
				var folder = folders[i];
				var nextFolder  = (i < folders.Count - 1) ? folders[i + 1] : null;

				if (folder.Level == 0) 
					Console.WriteLine(folder.Name);
				else
				{
					var gap = Repeat("┊  ", folder.Level - 1);
					string lines;
						
					if (nextFolder == null || nextFolder.Level < folder.Level)
						lines = "└┄";
					else
						lines = "├┄";

					Console.WriteLine($"{gap}{lines} {folder.Name}" );	
				}
			}

			// !!!!!   SEE CONSOLE OUTPUT  !!!!!!!!
		}

		[TestMethod]
		public void GetFolderWithSubFoldersAsObjectRows()
		{
			ObjectRows rows;

			// just to eliminate time for test preparation and make a cold run before time measure
			using (var repository = new Repository())
				rows = repository.GetFolderWithSubFoldersAsObjectRows(1);


			var sw = new Stopwatch();
			sw.Start();

			using (var repository = new Repository())
				rows = repository.GetFolderWithSubFoldersAsObjectRows(1);

			sw.Stop();

			Assert.IsTrue(rows.Count > 0);

			Console.WriteLine($"GetFolderWithSubFoldersAsObjectRows read {rows.Count} folder ObjectRows");
			Console.WriteLine();
			Console.WriteLine($"for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms.");
			Console.WriteLine();

			Console.Write("Id, ParentId, Name, Level, HidCode, HidPath, Path");
			Console.WriteLine();

			foreach (var row in rows)
			{
				var level = Convert.ToInt32(row[3]);

				Console.WriteLine($"{Repeat("    ", level)}{JsonConvert.SerializeObject(row)}");
			}

			// !!!!!   SEE CONSOLE OUTPUT  !!!!!!!!
		}
		
		[TestMethod]
		public void GetTreeOfFolderParents()
		{
			using (var repository = new Repository())
			{
				var folders = repository.GetFolderWithSubFolders(1);

				var folder = folders.FirstOrDefault(f => f.Level == 5);

				Assert.IsNotNull(folder);

				folders = repository.GetFolderWithParents(folder.Id);

				Assert.IsTrue(folders.Count == 6);

				var folderTree = folders.ToTree();
				
				Console.WriteLine($"GetFolderWithParents read & combined {folders.Count} folders in a tree");
				Console.WriteLine();

				Console.Write(ToJson(folderTree));
			}

			// !!!!!   SEE CONSOLE OUTPUT  !!!!!!!!
		}

		[TestMethod]
		public void GetFolderTree()
		{
			IList<Folder> folders;

			using (var repository = new Repository())
				folders = repository.GetFolderWithSubFolders(1);

			Folder folderTree;

			var sw = new Stopwatch();
			sw.Start();

			using (var repository = new Repository())
			{
				folderTree = repository.GetFolderTree(1);
			}

			sw.Stop();

			Assert.IsNotNull(folderTree);
			Assert.IsNotNull(folderTree.SubFolders);
			Assert.IsTrue(folderTree.SubFolders.Count > 0);

			Console.WriteLine($"GetFolderTree read & combined {folders.Count} folders in a tree");
			Console.WriteLine();
			Console.WriteLine($"for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms.");
			Console.WriteLine();

			Console.Write(ToJson(folderTree));

			// !!!!!   SEE CONSOLE OUTPUT  !!!!!!!!
		}

		private void linkParentAndFolderAction(Folder p, Folder f)
		{
			if (p.SubFolders == null)
				p.SubFolders = new List<Folder>();

			p.SubFolders.Add(f);
			f.Parent = p;
		}


		[TestMethod]
		public void GetFolderTreeGeneric()
		{
			IList<Folder> folders;

			using (var repository = new Repository())
				folders = repository.GetFolderWithSubFolders(1);

			var sw = new Stopwatch();
			sw.Start();

			Action<Folder, Folder> linkParentAndFolderAction = (p, f) => 
			{
				if (p.SubFolders == null) 
					p.SubFolders = new List<Folder>();

				p.SubFolders.Add(f);
				f.Parent = p;
			};


			var folderTree = folders.ToTree(f => f.Id, f => f.ParentId, linkParentAndFolderAction, hierarchicallySorted: true);
	
			sw.Stop();

			Assert.IsNotNull(folderTree);
			Assert.IsNotNull(folderTree.SubFolders);
			Assert.IsTrue(folderTree.SubFolders.Count > 0);

			Console.WriteLine($"ToTree combined {folders.Count} folders in a tree");
			Console.WriteLine();
			Console.WriteLine($"for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms.");
			Console.WriteLine();

			Console.Write(ToJson(folderTree));

			// !!!!!   SEE CONSOLE OUTPUT  !!!!!!!!
		}





		[TestMethod]
		public void GetFolderTreeBranch()
		{
			IList<Folder> folders;

			using (var repository = new Repository())
				folders = repository.GetImmediateSubFolders(1);

			var firstSubFolder = folders.First();

			Folder folderTree;

			var sw = new Stopwatch();

			using (var repository = new Repository())
			{
				folders = repository.GetFolderWithSubFolders(firstSubFolder.Id);

				sw.Start();
				folderTree = repository.GetFolderTree(firstSubFolder.Id);
				sw.Stop();
			}

			Assert.IsNotNull(folderTree);
			Assert.IsNotNull(folderTree.SubFolders);
			Assert.IsTrue(folderTree.SubFolders.Count > 0);
			
			Console.WriteLine($"GetFolderTreeBranch read & combined {folders.Count-1} folders into a tree");
			Console.WriteLine();
			Console.WriteLine($"for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms.");
			Console.WriteLine();

			Console.Write(ToJson(folderTree));

			// !!!!!   SEE CONSOLE OUTPUT  !!!!!!!!
		}

		[TestMethod]
		public void GetTwoFolderTreeBranches()
		{
			IList<Folder> folders1, folders2;
	
			using (var repository = new Repository())
			{
				folders1 = repository.GetFolderWithSubFolders(1);
				folders2 = repository.GetFolderWithSubFolders(2);
			}

			var firstSubFolder1 = folders1.Skip(1).First();
			var firstSubFolder2 = folders2.Skip(1).First();

			var sw = new Stopwatch();
			
			using (var repository = new Repository())
			{
				folders1 = repository.GetFolderWithSubFolders(firstSubFolder1.Id);
				folders2 = repository.GetFolderWithSubFolders(firstSubFolder2.Id);
			}

			var concatFolders = folders1.Concat(folders2);

			sw.Start();

			var folderTrees = concatFolders.ToTreeList(hierarchicallySorted: true);

			sw.Stop();

			Assert.IsNotNull(folderTrees);
			Assert.IsTrue(folderTrees.Count == 2);


			Console.WriteLine($"GetFolderTreeBranch built a tree of {folders1.Count + folders2.Count} folders for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms.");
			Console.WriteLine();

			Console.Write(ToJson(folderTrees));

			// !!!!!   SEE CONSOLE OUTPUT  !!!!!!!!
		}

		[TestMethod]
		public void GetTwoFolderTreeBranchesUnsorted()
		{
			IList<Folder> folders1, folders2;
	
			using (var repository = new Repository())
			{
				folders1 = repository.GetFolderWithSubFolders(1);
				folders2 = repository.GetFolderWithSubFolders(2);
			}

			var firstSubFolder1 = folders1.Skip(1).First();
			var firstSubFolder2 = folders2.Skip(1).First();

			var sw = new Stopwatch();

			using (var repository = new Repository())
			{
				folders1 = repository.GetFolderWithSubFolders(firstSubFolder1.Id);
				folders2 = repository.GetFolderWithSubFolders(firstSubFolder2.Id);
			}

			var concatFolders = folders1.Concat(folders2).Reverse();

			sw.Start();

			var folderTrees = concatFolders.ToTreeList(hierarchicallySorted: false);

			sw.Stop();

			Assert.IsNotNull(folderTrees);
			Assert.IsTrue(folderTrees.Count == 2);

			Console.WriteLine($"GetTwoFolderTreeBranchesUnsorted built a tree of {folders1.Count + folders2.Count} folders for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms.");
			Console.WriteLine();

			Console.Write(ToJson(folderTrees));

			// !!!!!   SEE CONSOLE OUTPUT  !!!!!!!!
		}


		[TestMethod]
		public void AddAndDeleteFolder()
		{
			using (var repository = new Repository())
			{
				repository.BeginTransaction(tran =>
				{
					var folders = repository.GetFolderWithSubFolders(1);

					Console.WriteLine($"Quantity of folders found: {folders.Count}");
					Console.WriteLine();


					var root = folders.First();
				
					var firstSubFolder = folders.Skip(1).First();
					var originHidCode = firstSubFolder.HidCode;

					Console.WriteLine("First subfolder of the root:");
					Console.Write(ToJson(firstSubFolder));
					Console.WriteLine();
					Console.WriteLine();
			
					var newFolder = new Folder
					{
						ParentId = root.Id,
						Name = "000"
					};

					var sw = new Stopwatch();
					sw.Start();

					var savedFolder = repository.SaveFolder(newFolder, withHidReorder: true);
			
					sw.Stop();

					Console.WriteLine($"The new subfolder was added to the the root for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms:");
					Console.Write(ToJson(savedFolder));
					Console.WriteLine();
					Console.WriteLine();
				

					var nextSibling = repository.GetNextSiblingFolder(savedFolder.Id);

					Assert.AreEqual(nextSibling.Id, firstSubFolder.Id);
					Assert.IsTrue(String.Compare(nextSibling.HidCode, firstSubFolder.HidCode, StringComparison.InvariantCultureIgnoreCase) > 0);

					Console.WriteLine("The subfolder that was the first before the new folder was added:");
					Console.Write(ToJson(nextSibling));
					Console.WriteLine();
					Console.WriteLine();

					sw.Restart();
			
					repository.DeleteFolder(savedFolder.Id, withHidReorder: true);

					sw.Stop();
				
					var deletedFolder = repository.GetFolderById(savedFolder.Id);

					Assert.IsNull(deletedFolder);

					Console.WriteLine($"The new subfolder was deleted for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms");
					Console.WriteLine();
				

					var newFirstSubFolder = repository.GetFolderById(firstSubFolder.Id);

					Assert.AreEqual(newFirstSubFolder.Id, firstSubFolder.Id);
					Assert.AreEqual(newFirstSubFolder.HidCode, firstSubFolder.HidCode);

					Console.WriteLine("The subfolder that became first again after deleting of the new folder:");
					Console.Write(ToJson(newFirstSubFolder));

					Assert.AreEqual(originHidCode, firstSubFolder.HidCode);

					// commit transaction tran.Commit();

				});
			}

			// !!!!!   SEE CONSOLE OUTPUT  !!!!!!!!
		}

		[TestMethod]
		public void EditFolder()
		{
			using (var repository = new Repository())
			{
				repository.BeginTransaction(tran =>
				{
					var folders = repository.GetFolderWithSubFolders(1);

					Console.WriteLine($"Quantity of folders found: {folders.Count}");
					Console.WriteLine();


					var firstSubFolder = folders.Skip(1).First();

					var originName = firstSubFolder.Name;
					var originHidCode = firstSubFolder.HidCode;

					Console.WriteLine("First subfolder of the root:");
					Console.Write(ToJson(firstSubFolder));
					Console.WriteLine();
					Console.WriteLine();
			

					firstSubFolder.Name = "ZZZ";

					var sw = new Stopwatch();
					sw.Start();

					var lastSubFolder = repository.SaveFolder(firstSubFolder, withHidReorder: true);

					sw.Stop();
			
					Console.WriteLine($"Change of Name to ZZZ made the firstSubFolder the last for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms:");
					Console.Write(ToJson(lastSubFolder));
					Console.WriteLine();
					Console.WriteLine();


					lastSubFolder.Name = originName;
				
					sw.Restart();

					firstSubFolder = repository.SaveFolder(lastSubFolder, withHidReorder: true);

					sw.Stop();

					Console.WriteLine($"Change of Name to {originName} made the lastSubFolder the first again for {sw.Elapsed.TotalMilliseconds.ToString("0.##")} ms:");
					Console.Write(ToJson(firstSubFolder));
					Console.WriteLine();
					Console.WriteLine();
			
					Assert.AreEqual(originHidCode, firstSubFolder.HidCode);

					// commit transaction tran.Commit();
				});
			}

			// !!!!!   SEE CONSOLE OUTPUT  !!!!!!!!
		}

		[TestMethod]
		public void ReorderSubFolders()
		{
			using (var repository = new Repository())
			{
				repository.BeginTransaction(tran =>
				{
					var rootFolder = repository.GetUserRootFolder(14);

				
					var folderA = new Folder { ParentId = rootFolder.Id, Name = "A" };
					folderA = repository.SaveFolder(folderA, withHidReorder: false);

					var folderAA = new Folder { ParentId = folderA.Id, Name = "AA" };
					folderAA = repository.SaveFolder(folderAA, withHidReorder: false);

					var folderAB = new Folder { ParentId = folderA.Id, Name = "AB" };
					folderAB = repository.SaveFolder(folderAB, withHidReorder: false);

					var folderB = new Folder { ParentId = rootFolder.Id, Name = "B" };
					folderB = repository.SaveFolder(folderB, withHidReorder: false);
					
					var folderD = new Folder { ParentId = rootFolder.Id, Name = "D" };
					folderD = repository.SaveFolder(folderD, withHidReorder: false);


					var folderTree = repository.GetFolderTree(rootFolder.Id);

					Console.WriteLine("Just created folder tree: ");
					Console.Write(ToJson(folderTree));
					Console.WriteLine();
					Console.WriteLine();
					

					folderA.Name = "C";
					folderA = repository.SaveFolder(folderA, withHidReorder: false);

					Assert.AreEqual("/2.1/", folderA.HidPath);

					folderTree = repository.GetFolderTree(rootFolder.Id);
					
					Console.WriteLine("Folder tree after rename Folder A to C without Hid reorder: ");
					Console.Write(ToJson(folderTree));
					Console.WriteLine();
					Console.WriteLine();


					folderA.Name = "Z";
					folderA = repository.SaveFolder(folderA, withHidReorder: true);

					Assert.AreEqual("/3/", folderA.HidPath);

					folderTree = repository.GetFolderTree(rootFolder.Id);

					Console.WriteLine("Folder tree after rename Folder A to Z with Hid reorder: ");
					Console.Write(ToJson(folderTree));
					Console.WriteLine();
					Console.WriteLine();
					
					// commit transaction tran.Commit();
				});
			}

			// !!!!!   SEE CONSOLE OUTPUT  !!!!!!!!
		}

		[TestMethod]
		public void ReparentSubFolders()
		{
			using (var repository = new Repository())
			{
				repository.BeginTransaction(tran =>
				{
					var rootFolder = repository.GetUserRootFolder(14);

				
					var folderA = new Folder { ParentId = rootFolder.Id, Name = "A" };
					folderA = repository.SaveFolder(folderA, withHidReorder: false);
					
						var folderAA = new Folder { ParentId = folderA.Id, Name = "AA" };
						folderAA = repository.SaveFolder(folderAA, withHidReorder: false);

							var folderAAA = new Folder { ParentId = folderAA.Id, Name = "AAA" };
							folderAAA = repository.SaveFolder(folderAAA, withHidReorder: false);

							var folderAAB = new Folder { ParentId = folderAA.Id, Name = "AAB" };
							folderAAB = repository.SaveFolder(folderAAB, withHidReorder: false);

					var folderB = new Folder { ParentId = rootFolder.Id, Name = "B" };
						folderB = repository.SaveFolder(folderB, withHidReorder: false);

						var folderBA = new Folder { ParentId = folderB.Id, Name = "BA" };
						folderBA = repository.SaveFolder(folderBA, withHidReorder: false);

			
					var folderTree = repository.GetFolderTree(rootFolder.Id);

					Console.WriteLine("Just created folder tree: ");
					Console.Write(ToJson(folderTree));
					Console.WriteLine();
					Console.WriteLine();
					

					folderAA.ParentId = folderB.Id;
					folderAA = repository.SaveFolder(folderAA, withHidReorder: false);

					Assert.AreEqual("/2/0/", folderAA.HidPath);

					folderTree = repository.GetFolderTree(rootFolder.Id);

					Console.WriteLine("Folder tree after reparent Folder AA to Folder B without Hid reorder: ");
					Console.Write(ToJson(folderTree));
					Console.WriteLine();
					Console.WriteLine();


					folderAA.ParentId = folderA.Id;
					folderAA = repository.SaveFolder(folderAA, withHidReorder: false);
					Assert.AreEqual("/1/1/", folderAA.HidPath);



					folderAA.ParentId = folderB.Id;
					folderAA = repository.SaveFolder(folderAA, withHidReorder: true);

					Assert.AreEqual("/2/1/", folderAA.HidPath);

					folderTree = repository.GetFolderTree(rootFolder.Id);

					Console.WriteLine("Folder tree after reparent Folder AA to Folder B with Hid reorder: ");
					Console.Write(ToJson(folderTree));
					Console.WriteLine();
					Console.WriteLine();

					// commit transaction tran.Commit();
				});

			}

			// !!!!!   SEE CONSOLE OUTPUT  !!!!!!!!
		}

		[TestMethod]
		public void FindFoldersWithParentTree()
		{
			using (var repository = new Repository())
			{
				var folderTree = repository.FindFoldersWithParentTree(userId: 1, level:2, folderName: "A");

				Assert.IsTrue(folderTree.SubFolders.Count > 0);

				Console.Write(ToJson(folderTree));
			}

			// !!!!!   SEE CONSOLE OUTPUT  !!!!!!!!
		}

		private static string ToJson(object obj)
		{
			return JToken.Parse(JsonConvert.SerializeObject(obj)).ToString(Formatting.Indented);
		}

		private static string Repeat(string str, int n)
		{
			return string.Concat(Enumerable.Repeat(str, n));
		}

		

	}
}
