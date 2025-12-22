using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Leveler;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerType4Sim : LogicalDeviceLevelerType4, ILogicalDeviceSimulated, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		private CancellationTokenSource _levelerSimTaskCancelSource = new CancellationTokenSource();

		public const int TickIntervalMs = 250;

		private bool _autoLevelComplete;

		private const int UpdateAutoStepsDelay = 1000;

		private readonly SimBubbleLevel _bubbleLevel;

		private const int BubbleLevelTimeToReachZeroInSeconds = 60;

		private readonly BaseObservableCollection<LogicalDeviceLevelerAutoStepType4> _stepCollection = new BaseObservableCollection<LogicalDeviceLevelerAutoStepType4>
		{
			LogicalDeviceLevelerAutoStepType4.ClearTongueJack,
			LogicalDeviceLevelerAutoStepType4.EmptyAirBags,
			LogicalDeviceLevelerAutoStepType4.ExtendFront,
			LogicalDeviceLevelerAutoStepType4.ExtendJacks,
			LogicalDeviceLevelerAutoStepType4.ExtendRear,
			LogicalDeviceLevelerAutoStepType4.ExtendTongue,
			LogicalDeviceLevelerAutoStepType4.FillAirBags,
			LogicalDeviceLevelerAutoStepType4.FindHitch,
			LogicalDeviceLevelerAutoStepType4.GroundFront,
			LogicalDeviceLevelerAutoStepType4.GroundJacks,
			LogicalDeviceLevelerAutoStepType4.GroundRear,
			LogicalDeviceLevelerAutoStepType4.GroundTongue,
			LogicalDeviceLevelerAutoStepType4.LeftFront,
			LogicalDeviceLevelerAutoStepType4.Level,
			LogicalDeviceLevelerAutoStepType4.LevelFront,
			LogicalDeviceLevelerAutoStepType4.LevelRear,
			LogicalDeviceLevelerAutoStepType4.LiftRear,
			LogicalDeviceLevelerAutoStepType4.LowerAxle,
			LogicalDeviceLevelerAutoStepType4.LowerFront,
			LogicalDeviceLevelerAutoStepType4.LowerRear,
			LogicalDeviceLevelerAutoStepType4.RaiseAxle,
			LogicalDeviceLevelerAutoStepType4.RetractFront,
			LogicalDeviceLevelerAutoStepType4.RetractJacks,
			LogicalDeviceLevelerAutoStepType4.RetractMiddle,
			LogicalDeviceLevelerAutoStepType4.RetractRear,
			LogicalDeviceLevelerAutoStepType4.RetractTongue,
			LogicalDeviceLevelerAutoStepType4.StowAxle,
			LogicalDeviceLevelerAutoStepType4.Stabilize,
			LogicalDeviceLevelerAutoStepType4.VerifyGround,
			LogicalDeviceLevelerAutoStepType4.Level
		};

		private int _stepsCompleted;

		public override LogicalDeviceActiveConnection ActiveConnection => LogicalDeviceActiveConnection.Direct;

		public override bool ActiveSession => IdsCanCommandRunner.CommandSessionActivated;

		public override ILogicalDevicePidFixedPoint VoltageMeasurementBatteryPid { get; } = new LogicalDevicePidSimFixedPoint(FixedPointType.UnsignedBigEndian16x16, PID.BATTERY_VOLTAGE, 10.5f);


		public LogicalDeviceLevelerType4Sim(ILogicalDeviceId logicalDeviceId, LogicalDeviceLevelerCapabilityType4 levelerCapability, ILogicalDeviceService deviceService = null, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, levelerCapability, deviceService, isFunctionClassChangeable)
		{
			LogicalDeviceLevelerType4Sim logicalDeviceLevelerType4Sim = this;
			Leveler4RunnerSim runnerSim = new Leveler4RunnerSim();
			IdsCanCommandRunner = runnerSim;
			_bubbleLevel = new SimBubbleLevel(250, 60);
			Task.Run(async delegate
			{
				while (!logicalDeviceLevelerType4Sim._levelerSimTaskCancelSource.IsCancellationRequested)
				{
					(runnerSim.LevelerStatus.XAngle, runnerSim.LevelerStatus.YAngle) = logicalDeviceLevelerType4Sim._bubbleLevel.UpdateAngle();
					if (logicalDeviceLevelerType4Sim._autoLevelComplete)
					{
						runnerSim.GotoHome();
						logicalDeviceLevelerType4Sim._autoLevelComplete = false;
					}
					logicalDeviceLevelerType4Sim.UpdateDeviceStatus(runnerSim.LevelerStatus.Data, 8u);
					logicalDeviceLevelerType4Sim.UpdateTextConsole(runnerSim);
					await TaskExtension.TryDelay(250, logicalDeviceLevelerType4Sim._levelerSimTaskCancelSource.Token);
				}
			}, _levelerSimTaskCancelSource.Token);
		}

		protected override ILogicalDevicePidULong MakeUiSupportedFeaturesPid()
		{
			FeatureSupport featureSupport = FeatureSupport.All;
			switch (base.DeviceCapability.Chassis)
			{
			case LevelerConfigurationChassis.ClassA:
			case LevelerConfigurationChassis.ClassC:
			case LevelerConfigurationChassis.FifthWheel:
				featureSupport &= ~FeatureSupport.AutoHitch;
				break;
			}
			return new LogicalDevicePidSimULong(PID.LEVELER_UI_SUPPORTED_FEATURES, (ulong)featureSupport);
		}

		public override Task<(LogicalDeviceLevelerScreenType4 stepsScreen, int stepsCount, int stepsCompleted)> GetAutoStepsProgressAsync(CancellationToken cancelToken)
		{
			if (_autoLevelComplete)
			{
				return Task.FromResult((LogicalDeviceLevelerScreenType4.Unknown, 0, 0));
			}
			LogicalDeviceLevelerScreenType4 logicalDeviceLevelerScreenType = DeviceStatus?.ScreenSelected ?? LogicalDeviceLevelerScreenType4.Unknown;
			if (!logicalDeviceLevelerScreenType.IsOperationAuto())
			{
				return Task.FromResult((LogicalDeviceLevelerScreenType4.Unknown, 0, 0));
			}
			if (_stepsCompleted > _stepCollection.Count)
			{
				_stepsCompleted = 0;
				_autoLevelComplete = true;
				return Task.FromResult((LogicalDeviceLevelerScreenType4.Unknown, 0, 0));
			}
			_stepsCompleted++;
			return Task.FromResult((logicalDeviceLevelerScreenType, _stepCollection.Count, _stepsCompleted - 1));
		}

		public override async Task UpdateAutoStepsCollectionWithLatestDetails(int expectedStepsCount, BaseObservableCollection<(LogicalDeviceLevelerAutoStepType4 autoStep, int index)> collection, CancellationToken cancelToken)
		{
			await MainThread.RequestMainThreadActionAsync(delegate
			{
				using (collection.SuppressEvents())
				{
					collection.Clear();
					int num = 0;
					foreach (LogicalDeviceLevelerAutoStepType4 item in _stepCollection)
					{
						collection.Add((item, num++));
					}
				}
			});
			await TaskExtension.TryDelay(1000, cancelToken);
		}

		public override Task<List<LogicalDeviceLevelerAutoStepType4>> GetAutoStepListDetailsAsync(int expectedStepsCount, CancellationToken cancelToken)
		{
			return Task.FromResult(Enumerable.ToList(_stepCollection));
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			_levelerSimTaskCancelSource?.TryCancelAndDispose();
			_levelerSimTaskCancelSource = null;
		}
	}
}
