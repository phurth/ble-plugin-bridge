using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace IDS.Core
{
	public class ImageCache
	{
		public interface Interface
		{
			void RegisterImageReference(Enum id);
		}

		private static readonly object CriticalSection = new object();

		private static Interface mInstance = null;

		public static Interface Instance
		{
			get
			{
				return mInstance;
			}
			set
			{
				if (mInstance != null)
				{
					throw new InvalidOperationException("Singleton failure, cannot set ImageCache.Instance more than once");
				}
				lock (CriticalSection)
				{
					if (value == null)
					{
						throw new Exception("ImageCache.Instance must be set to a valid reference");
					}
					if (mInstance == null)
					{
						mInstance = value;
					}
				}
			}
		}

		public static void RegisterEnumImageReferences(Type t)
		{
			foreach (Enum value in Enum.GetValues(t))
			{
				RegisterImageReference(value);
			}
		}

		public static void RegisterImageReference(Enum id)
		{
			if (Instance == null)
			{
				throw new Exception("Application derived ImageCache must be instatiated prior to use");
			}
			Instance.RegisterImageReference(id);
		}
	}
	public abstract class ImageCache<T> : ImageCache, ImageCache.Interface
	{
		private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Enum, T>> TopLevelDictionary = new ConcurrentDictionary<Type, ConcurrentDictionary<Enum, T>>();

		public T this[Enum id]
		{
			get
			{
				if (id == null)
				{
					return default(T);
				}
				if (TopLevelDictionary.TryGetValue(id.GetType(), out var concurrentDictionary) && concurrentDictionary.TryGetValue(id, out var result))
				{
					return result;
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Image was not registered: ");
				defaultInterpolatedStringHandler.AppendFormatted(id.GetType().FullName);
				defaultInterpolatedStringHandler.AppendLiteral(".");
				defaultInterpolatedStringHandler.AppendFormatted(id);
				throw new ArgumentOutOfRangeException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}

		protected abstract T LoadImageResource(Enum reference);

		public new void RegisterImageReference(Enum id)
		{
			Type type = id.GetType();
			if (!TopLevelDictionary.TryGetValue(type, out var concurrentDictionary))
			{
				concurrentDictionary = new ConcurrentDictionary<Enum, T>();
				TopLevelDictionary.TryAdd(type, concurrentDictionary);
				TopLevelDictionary.TryGetValue(type, out concurrentDictionary);
			}
			if (!concurrentDictionary.TryGetValue(id, out var val))
			{
				val = LoadImageResource(id);
				if (val == null)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 2);
					defaultInterpolatedStringHandler.AppendLiteral("No image exists for registered id ");
					defaultInterpolatedStringHandler.AppendFormatted(type);
					defaultInterpolatedStringHandler.AppendLiteral(".");
					defaultInterpolatedStringHandler.AppendFormatted(id);
					throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				concurrentDictionary.TryAdd(id, val);
			}
		}

		private T GetImage(Enum id)
		{
			if (TopLevelDictionary.TryGetValue(id.GetType(), out var concurrentDictionary) && concurrentDictionary.TryGetValue(id, out var result))
			{
				return result;
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Image was not registered: ");
			defaultInterpolatedStringHandler.AppendFormatted(id.GetType().FullName);
			defaultInterpolatedStringHandler.AppendLiteral(".");
			defaultInterpolatedStringHandler.AppendFormatted(id);
			throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
		}
	}
}
