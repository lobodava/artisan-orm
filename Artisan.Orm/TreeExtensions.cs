using System;
using System.Collections.Generic;
using System.Linq;

namespace Artisan.Orm
{ 

	// https://github.com/lobodava/artisan-orm/wiki/INode-Interface-and-ToTree-Methods

	public static class TreeExtensions
	{
		public static IList<T> ToTreeList<T>(this IEnumerable<T> nodes, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			if (hierarchicallySorted)
				return ConvertHierarchicallySortedNodeListToTrees(nodes);

			return ConvertHierarchicallyUnsortedNodeListToTrees(nodes);
		}

		public static IList<TNode> ToTreeList<TNode, TId>(
			this IEnumerable<TNode> nodes, 
			Func<TNode, TId> idSelector,
			Func<TNode, TId?> parentIdSelector,
			Action<TNode, TNode> linkParentAndNodeAction,
			bool hierarchicallySorted = false
		)	
			where TNode: class 
			where TId: struct 
		{
			if (hierarchicallySorted)
				return ConvertHierarchicallySortedNodeListToTrees(nodes, idSelector, parentIdSelector, linkParentAndNodeAction);

			return ConvertHierarchicallyUnsortedNodeListToTrees(nodes, idSelector, parentIdSelector, linkParentAndNodeAction);
		}




		public static T ToTree<T>(this IEnumerable<T> nodes, bool hierarchicallySorted = false) where T: class, INode<T>
		{
			if (hierarchicallySorted)
				return ConvertHierarchicallySortedNodeListToTrees(nodes).FirstOrDefault();

			return ConvertHierarchicallyUnsortedNodeListToTrees(nodes).FirstOrDefault();
		}

		public static TNode ToTree<TNode, TId>(
			this IEnumerable<TNode> nodes, 
			Func<TNode, TId> idSelector,
			Func<TNode, TId?> parentIdSelector,
			Action<TNode, TNode> linkParentAndNodeAction,
			bool hierarchicallySorted = false
		)	
			where TNode: class 
			where TId: struct 
		{
			if (hierarchicallySorted)
				return ConvertHierarchicallySortedNodeListToTrees(nodes, idSelector, parentIdSelector, linkParentAndNodeAction).FirstOrDefault();

			return ConvertHierarchicallyUnsortedNodeListToTrees(nodes, idSelector, parentIdSelector, linkParentAndNodeAction).FirstOrDefault();
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
					parent.Children ??= new List<T>();

					parent.Children.Add(node);
				}
				else if (node.ParentId == prevNode.Id)
				{
					parentStack.Push(parent);

					parent = prevNode;

					parent.Children ??= new List<T>();

					parent.Children.Add(node);
				}
				else
				{
					var parentFound = false;

					while(parentStack.Count > 0 && parentFound == false)
					{
						parent = parentStack.Pop();

						if (node.ParentId.Value == parent.Id)
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


		private static IList<TNode> ConvertHierarchicallySortedNodeListToTrees<TNode, TId>
		(
			IEnumerable<TNode> nodes, 
			Func<TNode, TId> idSelector,
			Func<TNode, TId?> parentIdSelector,
			Action<TNode, TNode> linkParentAndNodeAction
		)	
			where TNode: class 
			where TId: struct 
		{
			var parentStack = new Stack<TNode>();
			var parent = default(TNode);
			var prevNode = default(TNode);
			var rootNodes = new List<TNode>();

			foreach (var node in nodes)
			{
				var parentId = parentIdSelector(node);


				if (parent == null || parentId == null)
				{
					rootNodes.Add(node);

					parent = node;
				}
				else if (parentId.Equals(idSelector(parent)))
				{
					linkParentAndNodeAction(parent, node);
				}
				else if (parentId.Equals(idSelector(prevNode)))
				{
					parentStack.Push(parent);

					parent = prevNode;

					linkParentAndNodeAction(parent, node);
				}
				else
				{
					var parentFound = false;

					while(parentStack.Count > 0 && parentFound == false)
					{
						parent = parentStack.Pop();

						if (parentId.Equals(idSelector(parent)))
						{
							linkParentAndNodeAction(parent, node);

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
				if (node.ParentId.HasValue && dictionary.TryGetValue(node.ParentId.Value, out T parent))
				{
					parent.Children ??= new List<T>();

					parent.Children.Add(node);
				}
				else
				{
					rootNodes.Add(node);
				}
			}

			return rootNodes;
		}


		private static IList<TNode> ConvertHierarchicallyUnsortedNodeListToTrees<TNode, TId>
		(
			IEnumerable<TNode> nodes, 
			Func<TNode, TId> idSelector,
			Func<TNode, TId?> parentIdSelector,
			Action<TNode, TNode> linkParentAndNodeAction
		)
			where TNode: class
			where TId: struct
		{
			var dictionary = nodes.ToDictionary(idSelector, n => n);
			var rootNodes = new List<TNode>();

			foreach (var node in dictionary.Select(item => item.Value))
			{
				var paretnId = parentIdSelector(node);

				if (paretnId.HasValue && dictionary.TryGetValue(paretnId.Value, out TNode parent))
				{
					linkParentAndNodeAction(parent, node);
				}
				else
				{
					rootNodes.Add(node);
				}
			}

			return rootNodes;
		}

	}

}
