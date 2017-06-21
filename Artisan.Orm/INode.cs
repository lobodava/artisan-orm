using System;
using System.Collections.Generic;
using System.Linq;

namespace Artisan.Orm
{

	// https://github.com/lobodava/artisan-orm/wiki/INode-Interface-and-ToTree-Methods

	public interface INode<T> where T: class
	{
		Int32 Id { get; set; }

		Int32? ParentId { get; set; }

		IList<T> Children { get; set; }
	}
	
	public static class NodeExtensions
	{
		public static IList<T> ToTreeList<T>(this IEnumerable<T> nodes, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			if (hierarchicallySorted)
				return ConvertHierarchicallySortedNodeListToTrees(nodes);

			return ConvertHierarchicallyUnsortedNodeListToTrees(nodes);
		}

		public static T ToTree<T>(this IEnumerable<T> nodes, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			if (hierarchicallySorted)
				return ConvertHierarchicallySortedNodeListToTrees(nodes).FirstOrDefault();

			return ConvertHierarchicallyUnsortedNodeListToTrees(nodes).FirstOrDefault();
		}



		// this method allows to build a tree for one only iteration through the hierarchycally sorted node list

		private static IList<T> ConvertHierarchicallySortedNodeListToTrees<T>(IEnumerable<T> nodes) where T: class, INode<T>
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
					if (parent.Children == null)
						parent.Children = new List<T>();

					parent.Children.Add(node);
				}
				else if (node.ParentId == prevNode.Id)
				{
					parentStack.Push(parent);

					parent = prevNode;

					if (parent.Children == null)
						parent.Children = new List<T>();

					parent.Children.Add(node);
				}
				else
				{
					var parentFound = false;

					while(parentStack.Count > 0 && parentFound == false)
					{
						parent = parentStack.Pop();

						if (node.ParentId != null && node.ParentId.Value == parent.Id)
						{
							parent.Children.Add(node);
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

		private static IList<T> ConvertHierarchicallyUnsortedNodeListToTrees<T>(IEnumerable<T> nodes) where T: class, INode<T>
		{
			var dictionary = nodes.ToDictionary(n => n.Id, n => n);
			var rootNodes = new List<T>();

			foreach (var node in dictionary.Select(item => item.Value))
			{
				T parent;

				if (node.ParentId.HasValue && dictionary.TryGetValue(node.ParentId.Value, out parent))
				{
					if (parent.Children == null)
						parent.Children = new List<T>();

					parent.Children.Add(node);
				}
				else
				{
					rootNodes.Add(node);
				}
			}

			return rootNodes;
		}

	}

	//TODO INode<TObject, TId> ???

	//public interface INode<TObject, TId> where TObject: class where TId: struct
	//{
		
	//	TId Id { get; set; }

	//	TId? ParentId { get; set; }

	//	IList<TObject> Children { get; set; }

	//	TObject Parent { get; set; }
	//}

}
