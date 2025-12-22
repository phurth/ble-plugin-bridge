using System;
using System.Reflection;

namespace IDS.Portable.Common
{
	public class SingletonNotifyPropertyChanged<TSingleton> : CommonNotifyPropertyChanged where TSingleton : class
	{
		protected static readonly object SingletonLocker = new object();

		private static volatile TSingleton _instance;

		public static TSingleton Instance
		{
			get
			{
				if (_instance != null)
				{
					return _instance;
				}
				lock (SingletonLocker)
				{
					if (_instance != null)
					{
						return _instance;
					}
					try
					{
						_instance = MakeSingleton();
					}
					catch (Exception ex)
					{
						TaggedLog.Error("SingletonNotifyPropertyChanged", "Unable to create singleton instance for {0} {1}\n{2}", typeof(TSingleton).Name, ex.Message, ex.StackTrace);
						throw;
					}
				}
				return _instance;
			}
		}

		private static TSingleton MakeSingleton()
		{
			ConstructorInfo[] constructors = typeof(TSingleton).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
			if (!Array.Exists(constructors, (ConstructorInfo ci) => ci.GetParameters().Length == 0))
			{
				throw new ConstructorNotFoundException("Non-public ctor() not found.");
			}
			return Array.Find(constructors, (ConstructorInfo ci) => ci.GetParameters().Length == 0).Invoke(new object[0]) as TSingleton;
		}
	}
}
