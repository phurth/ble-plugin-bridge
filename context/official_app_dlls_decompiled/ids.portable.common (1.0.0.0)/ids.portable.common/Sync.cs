using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace IDS.Portable.Common
{
	public class Sync
	{
		private class CheckedLockRecord
		{
			public string Tag = "CheckedLockRecord";

			public string? Description;

			public Stopwatch Stopwatch { get; private set; } = new Stopwatch();

		}

		private const string LogTag = "Sync";

		private const int CheckTimeoutMs = 2000;

		private static object _lockCheckSync = new object();

		private static Task? _lockCheckerTask;

		private static List<CheckedLockRecord> _availableLockRecordList = new List<CheckedLockRecord>();

		private static List<CheckedLockRecord> _activeLockRecordList = new List<CheckedLockRecord>();

		public static void CheckedLock(object obj, string tag, [CallerMemberName] string description = "")
		{
			CheckedLockRecord checkedLockRecord = null;
			try
			{
				lock (_lockCheckSync)
				{
					if (_lockCheckerTask == null)
					{
						_lockCheckerTask = new Task(async delegate
						{
							lock (_lockCheckSync)
							{
								foreach (CheckedLockRecord activeLockRecord in _activeLockRecordList)
								{
									_ = activeLockRecord.Stopwatch.ElapsedMilliseconds;
									_ = 2000;
								}
							}
							await Task.Delay(500);
						});
					}
					if (_availableLockRecordList.Count > 0)
					{
						checkedLockRecord = _availableLockRecordList[0];
						_availableLockRecordList.RemoveAt(0);
					}
					else
					{
						checkedLockRecord = new CheckedLockRecord();
					}
					_activeLockRecordList.Add(checkedLockRecord);
					checkedLockRecord.Tag = tag;
					checkedLockRecord.Description = description;
					checkedLockRecord.Stopwatch.Restart();
				}
				lock (obj)
				{
				}
				lock (_lockCheckSync)
				{
					checkedLockRecord.Tag = tag;
					checkedLockRecord.Description = description;
					checkedLockRecord.Stopwatch.Stop();
					_activeLockRecordList.Remove(checkedLockRecord);
					_availableLockRecordList.Add(checkedLockRecord);
				}
			}
			catch (Exception)
			{
			}
		}

		public static void AnnotatedLock(object obj, string tag, Action method, [CallerMemberName] string description = "")
		{
			TaggedLog.Debug("Sync", "{0} - {1} Start()", tag, description);
			lock (obj)
			{
				method();
			}
			TaggedLog.Debug("Sync", "{0} - {1} Stop()", tag, description);
		}

		public static void Lock(object obj, string tag, Action method, [CallerMemberName] string description = "")
		{
			lock (obj)
			{
				method();
			}
		}

		public static TResult AnnotatedLock<TResult>(object obj, string tag, Func<TResult> method, [CallerMemberName] string description = "")
		{
			TaggedLog.Debug("Sync", "{0} - {1} Start()", tag, description);
			TResult result;
			lock (obj)
			{
				result = method();
			}
			TaggedLog.Debug("Sync", "{0} - {1} Stop()", tag, description);
			return result;
		}

		public static TResult Lock<TResult>(object obj, string tag, Func<TResult> method, [CallerMemberName] string description = "")
		{
			lock (obj)
			{
				return method();
			}
		}
	}
}
