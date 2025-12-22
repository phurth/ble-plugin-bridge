using System;
using System.Collections.Generic;

namespace IDS.Core
{
	public class DisposableManager : Disposable, IDisposableManager, IDisposable, System.IDisposable
	{
		private object CriticalSection = new object();

		private List<WeakReference<IDisposable>> Items = new List<WeakReference<IDisposable>>();

		private int TimeSinceLastInventory;

		private bool ShouldInventory
		{
			get
			{
				int num = Math.Max(20, Items.Count / 10);
				return ++TimeSinceLastInventory > num;
			}
		}

		public void AddDisposable(IDisposable obj)
		{
			if (base.IsDisposed || obj == null)
			{
				return;
			}
			lock (CriticalSection)
			{
				if (!ContainsDisposable(obj))
				{
					if (ShouldInventory)
					{
						RemoveDisposable(null, inventory: true);
					}
					Items.Add(new WeakReference<IDisposable>(obj));
				}
			}
		}

		private bool ContainsDisposable(IDisposable obj)
		{
			if (!base.IsDisposed)
			{
				lock (CriticalSection)
				{
					foreach (WeakReference<IDisposable> item in Items)
					{
						if (item.TryGetTarget(out var disposable) && disposable == obj)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		public void RemoveDisposable(IDisposable obj)
		{
			RemoveDisposable(obj, ShouldInventory);
		}

		private void RemoveDisposable(IDisposable obj, bool inventory)
		{
			lock (CriticalSection)
			{
				if (inventory)
				{
					TimeSinceLastInventory = 0;
				}
				for (int num = Items.Count - 1; num >= 0; num--)
				{
					if (!Items[num].TryGetTarget(out var disposable))
					{
						Items.RemoveAt(num);
					}
					else if (disposable == null)
					{
						Items.RemoveAt(num);
					}
					else if (disposable == obj)
					{
						Items.RemoveAt(num);
						if (!inventory)
						{
							break;
						}
					}
				}
			}
		}

		public override void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}
			lock (CriticalSection)
			{
				foreach (WeakReference<IDisposable> item in Items)
				{
					if (item.TryGetTarget(out var disposable))
					{
						try
						{
							disposable?.Dispose();
						}
						catch
						{
						}
					}
				}
				Items.Clear();
				Items = null;
			}
		}
	}
}
