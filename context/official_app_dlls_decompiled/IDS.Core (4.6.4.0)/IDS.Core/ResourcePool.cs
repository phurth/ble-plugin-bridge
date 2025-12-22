using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using IDS.Core.Collections;

namespace IDS.Core
{
	public class ResourcePool : Disposable
	{
		public interface IObject
		{
			bool IsMemberOfPool { get; }

			bool IsRetained { get; }

			void Retain();

			void ReturnToPool();
		}

		public abstract class Object : IObject
		{
			internal ResourcePool Pool;

			private int RetainCount;

			public bool IsMemberOfPool => Pool != null;

			public bool IsRetained
			{
				get
				{
					if (IsMemberOfPool)
					{
						return RetainCount > 0;
					}
					return false;
				}
			}

			public void Retain()
			{
				if (IsMemberOfPool && Interlocked.Increment(ref RetainCount) == 1)
				{
					Pool.InUseObjects.Add(this);
				}
			}

			public void ReturnToPool()
			{
				if (IsRetained && Interlocked.Decrement(ref RetainCount) == 0)
				{
					Pool.InUseObjects.Remove(this);
					ResetPoolObjectState();
					Pool.FreeObjects?.Enqueue(this);
				}
			}

			protected abstract void ResetPoolObjectState();
		}

		public readonly string Name;

		protected ConcurrentQueue<Object> FreeObjects = new ConcurrentQueue<Object>();

		protected ConcurrentHashSet<Object> InUseObjects = new ConcurrentHashSet<Object>();

		public int Capacity { get; internal set; }

		public bool Verbose { get; set; }

		public int NumObjectsCreated => NumObjectsAvailable + NumObjectsInUse;

		public int NumObjectsAvailable
		{
			get
			{
				ConcurrentQueue<Object> freeObjects = FreeObjects;
				if (freeObjects == null)
				{
					return 0;
				}
				return Enumerable.Count(freeObjects);
			}
		}

		public int NumObjectsInUse => InUseObjects.Count;

		protected ResourcePool(string debug_name)
			: this(debug_name, 0)
		{
		}

		protected ResourcePool(string debug_name, int max_capacity)
		{
			Name = debug_name;
			Capacity = max_capacity;
		}

		public override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (NumObjectsInUse != 0)
				{
					throw new InvalidOperationException("Cannot dispose the resource pool while one or more pooled items are in use");
				}
				FreeObjects = null;
			}
		}
	}
	public class ResourcePool<T> : ResourcePool where T : ResourcePool.Object, new()
	{
		private static readonly ResourcePool<T> Instance = new ResourcePool<T>();

		public new static int Capacity
		{
			get
			{
				return ((ResourcePool)Instance).Capacity;
			}
			set
			{
				if (value < ((ResourcePool)Instance).Capacity)
				{
					throw new ArgumentException("ResourcePool.Capacity cannot be decreased");
				}
				((ResourcePool)Instance).Capacity = value;
			}
		}

		public new static bool Verbose
		{
			get
			{
				return ((ResourcePool)Instance).Verbose;
			}
			set
			{
				((ResourcePool)Instance).Verbose = value;
			}
		}

		public static T GetObject()
		{
			return Instance.Get();
		}

		public ResourcePool()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(14, 1);
			defaultInterpolatedStringHandler.AppendLiteral("ResourcePool<");
			defaultInterpolatedStringHandler.AppendFormatted(typeof(T));
			defaultInterpolatedStringHandler.AppendLiteral(">");
			base._002Ector(defaultInterpolatedStringHandler.ToStringAndClear());
		}

		public ResourcePool(int max_capacity)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(14, 1);
			defaultInterpolatedStringHandler.AppendLiteral("ResourcePool<");
			defaultInterpolatedStringHandler.AppendFormatted(typeof(T));
			defaultInterpolatedStringHandler.AppendLiteral(">");
			base._002Ector(defaultInterpolatedStringHandler.ToStringAndClear(), max_capacity);
		}

		public T Get()
		{
			T val = null;
			Object @object = null;
			ConcurrentQueue<Object> freeObjects = FreeObjects;
			if (freeObjects != null && freeObjects.TryDequeue(out @object))
			{
				val = @object as T;
			}
			if (val == null)
			{
				if (base.IsDisposed)
				{
					throw new InvalidOperationException("<" + Name + "> cannot Get() an object from a disposed ResourcePool");
				}
				if (base.Capacity > 0 && base.NumObjectsCreated >= base.Capacity)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(67, 2);
					defaultInterpolatedStringHandler.AppendLiteral("<");
					defaultInterpolatedStringHandler.AppendFormatted(Name);
					defaultInterpolatedStringHandler.AppendLiteral("> maximum limit of ");
					defaultInterpolatedStringHandler.AppendFormatted(base.Capacity);
					defaultInterpolatedStringHandler.AppendLiteral(" objects reached, cannot create any new objects");
					throw new InvalidOperationException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				val = new T
				{
					Pool = this
				};
			}
			val.Retain();
			return val;
		}
	}
}
