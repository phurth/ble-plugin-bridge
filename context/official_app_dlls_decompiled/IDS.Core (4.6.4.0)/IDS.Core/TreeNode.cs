using System;
using System.Collections.Generic;

namespace IDS.Core
{
	public class TreeNode : Disposable, ITreeNode, IDisposable, System.IDisposable
	{
		public delegate ITreeNode TreeNodeFactory();

		public static TreeNodeFactory Factory = null;

		public static TreeNodeFactory GenericFactory = CreateGenericNode;

		private TreeNode mParent;

		private List<ITreeNode> mChildren = new List<ITreeNode>();

		public object Data { get; set; }

		public string Text { get; set; }

		public Enum Icon { get; set; }

		public ITreeNode Parent => mParent;

		public IEnumerable<ITreeNode> Children => mChildren;

		public bool IsSelected { get; set; }

		public bool IsExpanded { get; set; }

		public bool HasChildren => mChildren.Count > 0;

		public int NumChildren => mChildren.Count;

		private static ITreeNode CreateGenericNode()
		{
			return new TreeNode();
		}

		public static ITreeNode Create()
		{
			if (Factory == null)
			{
				throw new InvalidOperationException("Failed to create an IDS.Core.TreeNode as no IDS.Core.TreeNodeFactory has been registered with the Core.  A Factory must be registered during program initialization.  The Core provides a generic factory that can be used at IDS.Core.TreeNode.GenericFactory.");
			}
			return Factory();
		}

		public static ITreeNode Create(object data)
		{
			ITreeNode treeNode = Create();
			treeNode.Data = data;
			return treeNode;
		}

		private TreeNode()
		{
		}

		public void AddChild(ITreeNode node)
		{
			if (base.IsDisposed)
			{
				return;
			}
			if (node == this)
			{
				throw new InvalidOperationException("ITreeNode.Add() a node cannot be added to itself");
			}
			if (node.Parent != this)
			{
				node.Parent?.RemoveChild(node);
				if (this.GetRootNode() == node)
				{
					throw new InvalidOperationException("ITreeNode.Add() parent node cannot be added as a child node");
				}
				if (node is TreeNode treeNode)
				{
					treeNode.mParent = this;
				}
				mChildren?.Add(node);
			}
		}

		public void RemoveChild(ITreeNode node)
		{
			if (node.Parent == this)
			{
				if (node is TreeNode treeNode)
				{
					treeNode.mParent = null;
				}
				mChildren?.Remove(node);
			}
		}

		public override void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}
			List<ITreeNode> list = mChildren;
			mChildren = null;
			Parent?.RemoveChild(this);
			mParent = null;
			if (list == null)
			{
				return;
			}
			foreach (ITreeNode item in list)
			{
				item?.Dispose();
			}
			list.Clear();
		}
	}
}
