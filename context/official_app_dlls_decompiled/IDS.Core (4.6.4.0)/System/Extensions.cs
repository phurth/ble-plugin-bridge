using System.Collections.Generic;
using IDS.Core;

namespace System
{
	public static class Extensions
	{
		public static ITreeNode GetRootNode(this ITreeNode node)
		{
			while (node.Parent != null)
			{
				node = node.Parent;
			}
			return node;
		}

		public static bool IsRootNode(this ITreeNode node)
		{
			return node.GetRootNode() == null;
		}

		public static bool IsLeaf(this ITreeNode node)
		{
			return !node.HasChildren();
		}

		public static bool HasChildren(this ITreeNode node)
		{
			using (IEnumerator<ITreeNode> enumerator = node.Children.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					_ = enumerator.Current;
					return true;
				}
			}
			return false;
		}

		public static int Level(this ITreeNode node)
		{
			int num = 0;
			while (node.Parent != null)
			{
				num++;
				node = node.Parent;
			}
			return num;
		}

		public static int GetNodeCount(this ITreeNode node, bool includeSubTrees)
		{
			int num = 0;
			foreach (ITreeNode child in node.Children)
			{
				num++;
				if (includeSubTrees)
				{
					num += child.GetNodeCount(includeSubTrees: true);
				}
			}
			return num;
		}

		public static ITreeNode FirstNode(this ITreeNode node)
		{
			using (IEnumerator<ITreeNode> enumerator = node.Children.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					return enumerator.Current;
				}
			}
			return null;
		}

		public static ITreeNode NextNode(this ITreeNode node)
		{
			if (node.Parent != null)
			{
				bool flag = false;
				foreach (ITreeNode child in node.Parent.Children)
				{
					if (flag)
					{
						return child;
					}
					if (child == node)
					{
						flag = true;
					}
				}
			}
			return null;
		}

		public static bool IsRelatedNode(this ITreeNode n, ITreeNode node)
		{
			return n.GetRootNode() == node.GetRootNode();
		}

		public static void RemoveFromParent(this ITreeNode node)
		{
			node.Parent?.RemoveChild(node);
		}
	}
}
