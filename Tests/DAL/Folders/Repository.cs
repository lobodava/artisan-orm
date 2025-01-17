using Artisan.Orm;
using Tests.DAL.Folders.Models;
// https://github.com/lobodava/artisan-orm

namespace Tests.DAL.Folders;

public class Repository: RepositoryBase
{
	public Repository(string connectionString) : base(connectionString) { }


	public Folder GetFolderById(int id)
	{
		return ReadTo<Folder>("dbo.GetFolderById", cmd => cmd.AddIntParam("@Id", id)); 
	}

	public Folder SaveFolder(Folder folder, bool withHidReorder = false)
	{
		return ReadTo<Folder>("dbo.SaveFolder", cmd => 
		{
			cmd.AddTableRowParam("@Folder", folder);
			cmd.AddBitParam("@WithHidReculc", withHidReorder);
		});
	}

	public void DeleteFolder(int folderId, bool withHidReorder = false)
	{
		Execute("dbo.DeleteFolder", cmd =>
		{
			cmd.AddIntParam("@FolderId", folderId);
			cmd.AddBitParam("@WithHidReculc", withHidReorder);
		});
	}
	
	public Folder GetUserRootFolder(int userId)
	{
		return ReadTo<Folder>("dbo.GetUserRootFolder", cmd => cmd.AddIntParam("@UserId", userId));
	}
	
	public IList<Folder> GetFolderWithSubFolders(int folderId)
	{
		return ReadToList<Folder>("dbo.GetFolderWithSubFolders", cmd => cmd.AddIntParam("@FolderId", folderId));
	}

	public ObjectRows GetFolderWithSubFoldersAsObjectRows(int folderId)
	{
		return ReadToObjectRows<Folder>("dbo.GetFolderWithSubFolders", cmd => cmd.AddIntParam("@FolderId", folderId));
	}
	
	public IList<Folder> GetImmediateSubFolders(int folderId)
	{
		return ReadToList<Folder>("dbo.GetImmediateSubFolders", cmd => cmd.AddIntParam("@FolderId", folderId));
	}

	public IList<Folder> GetFolderWithParents(int folderId)
	{
		return ReadToList<Folder>("dbo.GetFolderWithParents", cmd => cmd.AddIntParam("@FolderId", folderId));
	}
	
	public Folder GetNextSiblingFolder(int folderId)
	{
		return ReadTo<Folder>("dbo.GetNextSiblingFolder", cmd => cmd.AddIntParam("@FolderId", folderId));
	}

	public Folder GetPreviousSiblingFolder(int folderId)
	{
		return ReadTo<Folder>("dbo.GetPreviousSiblingFolder", cmd => cmd.AddIntParam("@FolderId", folderId));
	}

	public Folder GetFolderTree(int folderId)
	{
		return ReadToTree<Folder>("dbo.GetFolderWithSubFolders", cmd => cmd.AddIntParam("@FolderId", folderId), hierarchicallySorted: true);
	}

	public Folder FindFoldersWithParentTree(int userId, short level, string folderName)
	{
		return ReadToTree<Folder>("dbo.FindFoldersWithParents", cmd =>
		{
			cmd.AddIntParam("@UserId", userId);
			cmd.AddSmallIntParam("@Level", level);
			cmd.AddNVarcharParam("@FolderName", 50, folderName);
		});

		// Alternative

		//var folders = ReadToList<Folder>("dbo.FindFoldersWithParents", cmd =>
		//{
		//	cmd.AddIntParam("@UserId", userId);
		//	cmd.AddSmallIntParam("@Level", level);
		//	cmd.AddNVarcharParam("@FolderName", 50, folderName);
		//});

		//return ConvertHierarchicallySortedFolderListToTrees(folders).FirstOrDefault();
	}


	#region [ ListToTrees methods ]

	public static IList<Folder> ConvertHierarchicallySortedFolderListToTrees(IEnumerable<Folder> folders)
	{
		var parentStack = new Stack<Folder>();
		var parent = default(Folder);
		var prevNode = default(Folder);
		var rootNodes = new List<Folder>();

		foreach (var folder in folders)
		{
			if (parent == null || folder.ParentId == null)
			{
				rootNodes.Add(folder);

				parent = folder;
			}
			else if (folder.ParentId == parent.Id)
			{
				parent.SubFolders ??= new List<Folder>();

				parent.SubFolders.Add(folder);
			}
			else if (folder.ParentId == prevNode.Id)
			{
				parentStack.Push(parent);

				parent = prevNode;

				parent.SubFolders ??= new List<Folder>();

				parent.SubFolders.Add(folder);
			}
			else
			{
				var parentFound = false;

				while(parentStack.Count > 0 && parentFound == false)
				{
					parent = parentStack.Pop();

					if (folder.ParentId != null && folder.ParentId.Value == parent.Id)
					{
						parent.SubFolders.Add(folder);
						parentFound = true;
					}
				}

				if (parentFound == false)
				{
					rootNodes.Add(folder);

					parent = folder;
				}
			}
				
			prevNode = folder;
		}

		return rootNodes;
	}


	public static IList<Folder> ConvertHierarchicallyUnsortedFolderListToTrees(IEnumerable<Folder> folders)
	{
		var dictionary = folders.ToDictionary(n => n.Id, n => n);
		var rootFolders = new List<Folder>();

		foreach (var folder in dictionary.Select(item => item.Value))
		{
			if (folder.ParentId.HasValue && dictionary.TryGetValue(folder.ParentId.Value, out Folder parent))
			{
				parent.SubFolders ??= new List<Folder>();

				parent.SubFolders.Add(folder);
			}
			else
			{
				rootFolders.Add(folder);
			}
		}

		return rootFolders;
	}

	#endregion

}
