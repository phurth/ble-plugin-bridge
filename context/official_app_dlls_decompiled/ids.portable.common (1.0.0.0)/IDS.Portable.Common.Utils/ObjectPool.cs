using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace IDS.Portable.Common.Utils
{
	public class ObjectPool<TObject>
	{
		private readonly ConcurrentBag<TObject> _objects;

		private readonly Func<TObject> _objectGenerator;

		private int _totalCreated;

		public static ObjectPool<TObject> MakeObjectPool<TNewObject>() where TNewObject : TObject, new()
		{
			return new ObjectPool<TObject>(() => (TObject)(object)new TNewObject());
		}

		public static ObjectPool<TObject> MakeObjectPool(Func<TObject> objectGenerator)
		{
			return new ObjectPool<TObject>(objectGenerator);
		}

		private ObjectPool(Func<TObject> objectGenerator)
		{
			_objects = new ConcurrentBag<TObject>();
			_objectGenerator = objectGenerator ?? throw new ArgumentNullException("objectGenerator");
		}

		public TObject TakeObject()
		{
			if (!_objects.TryTake(out var result))
			{
				_totalCreated++;
				return _objectGenerator();
			}
			return result;
		}

		public void PutObject(TObject item)
		{
			if (item != null)
			{
				_objects.Add(item);
			}
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(42, 3);
			defaultInterpolatedStringHandler.AppendLiteral("ObjectPool<");
			defaultInterpolatedStringHandler.AppendFormatted(typeof(TObject).Name);
			defaultInterpolatedStringHandler.AppendLiteral(">: Total Created ");
			defaultInterpolatedStringHandler.AppendFormatted(_totalCreated);
			defaultInterpolatedStringHandler.AppendLiteral(" Total Unused ");
			defaultInterpolatedStringHandler.AppendFormatted(_objects.Count);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
