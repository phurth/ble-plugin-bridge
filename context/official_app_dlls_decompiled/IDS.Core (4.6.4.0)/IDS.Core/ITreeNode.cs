using System;
using System.Collections.Generic;

namespace IDS.Core
{
	public interface ITreeNode : IDisposable, System.IDisposable
	{
		string Text { get; set; }

		Enum Icon { get; set; }

		object Data { get; set; }

		ITreeNode Parent { get; }

		IEnumerable<ITreeNode> Children { get; }

		bool HasChildren { get; }

		int NumChildren { get; }

		bool IsSelected { get; set; }

		bool IsExpanded { get; set; }

		void AddChild(ITreeNode node);

		void RemoveChild(ITreeNode node);
	}
}
