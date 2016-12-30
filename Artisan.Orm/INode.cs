using System;
using System.Collections.Generic;

namespace Artisan.Orm
{
	// Experimental functionality for hierarchical tree reading

	public interface INode<T>
	{
		Int32 Id { get; set; }

		Int32? ParentId { get; set; }

		List<T> Nodes { get; set; }
	}

	public static class NodeExtensions
	{
		public static IList<T> ToTree<T>(this IEnumerable<T> nodes, bool areNodeListSorted = true) where T: INode<T>
		{
			if (areNodeListSorted)
				return ConvertSortedNodeListToTree(nodes);

			return ConvertUnsortedNodeListToTree(nodes);
		}

		// this method allows to build a tree for one only iteration through the sorted node list
		// sorting must be done on hierarchyid
		private static IList<T> ConvertSortedNodeListToTree<T>(IEnumerable<T> nodes) where T: INode<T>
		{
			var parentStack = new Stack<T>();
			var parent = default(T);
			var prevNode = default(T);
			var rootNodes = new List<T>();

			foreach (var node in nodes)
			{
				if (parent == null || node.ParentId == null)
				{
					rootNodes.Add(node);

					parent = node;
				}
				else if (node.ParentId == parent.Id)
				{
					if (parent.Nodes == null)
						parent.Nodes = new List<T>();

					parent.Nodes.Add(node);
				}
				else if (node.ParentId == prevNode.Id)
				{
					parentStack.Push(parent);

					parent = prevNode;

					if (parent.Nodes == null)
						parent.Nodes = new List<T>();

					parent.Nodes.Add(node);
				}
				else
				{
					var parentFound = false;

					while(parentStack.Count > 0 && parentFound == false)
					{
						parent = parentStack.Pop();

						if (node.ParentId != null && node.ParentId.Value == parent.Id)
						{
							parent.Nodes.Add(node);
							parentFound = true;
						}
					}

					if (parentFound == false)
					{
						rootNodes.Add(node);

						parent = node;
					}
				}


				prevNode = node;
			}

			return rootNodes;
		}

		private static IList<T> ConvertUnsortedNodeListToTree<T>(IEnumerable<T> nodes) where T: INode<T>
		{
			//var nodeHash = nodes.ToLookup(cat => cat.ParentId);

			//foreach (var node in nodes)
			//{
			//	node.Nodes = nodeHash[node.Id].ToList();
			//}

			throw new NotImplementedException();
		}
	}


}