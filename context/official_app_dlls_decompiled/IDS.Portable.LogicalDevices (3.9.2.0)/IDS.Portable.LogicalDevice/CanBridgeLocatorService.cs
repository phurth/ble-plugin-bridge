using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice
{
	public class CanBridgeLocatorService : CanBridgeLocatorService<CanBridgeData>
	{
		private const string LogTag = "CanBridgeLocatorService";

		public static CanBridgeLocatorService Default = new CanBridgeLocatorService();

		private CanBridgeLocatorService()
			: base((MakeCanBridgeData)CanBridgeData.MakeCanBridgeData)
		{
		}
	}
	public class CanBridgeLocatorService<TCanBridgeData> : BackgroundOperationDisposable where TCanBridgeData : class, ICanBridgeData, IComparable<TCanBridgeData>
	{
		public delegate TCanBridgeData MakeCanBridgeData(string name, string address, int port);

		private const string LogTag = "CanBridgeLocatorService";

		private const int NetworkListenPort = 47664;

		private const string Manufacturer = "IDS";

		private const string Product = "CAN_TO_ETHERNET_GATEWAY";

		private const int CleanupGatewayDelayMs = 1000;

		private const int UdpReceiveErrorDelayMs = 1000;

		private const int UdpReceiveTimeoutMs = 1000;

		protected MakeCanBridgeData CanBridgeDataCreator;

		public ObservableCollection<TCanBridgeData> Items { get; } = new OrderedObservableCollection<TCanBridgeData>();


		public CanBridgeLocatorService(MakeCanBridgeData canBridgeDataCreator)
		{
			CanBridgeDataCreator = canBridgeDataCreator;
		}

		protected override async Task BackgroundOperationAsync(CancellationToken cancellationToken)
		{
			await base.BackgroundOperationAsync(cancellationToken);
			BackgroundOperationDisposable udpListenerBackgroundOperation = new BackgroundOperationDisposable((BackgroundOperationFunc)UdpListener);
			udpListenerBackgroundOperation.Start();
			while (!cancellationToken.IsCancellationRequested)
			{
				CleanupOldGateways();
				await TaskExtension.TryDelay(1000, cancellationToken);
			}
			udpListenerBackgroundOperation.Stop();
		}

		private async Task UdpListener(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				using UdpClient udpListener = new UdpClient(47664);
				Task<UdpReceiveResult> udpReceiveTask = null;
				while (!cancellationToken.IsCancellationRequested)
				{
					try
					{
						udpReceiveTask = ((udpReceiveTask == null || udpReceiveTask.IsCompleted) ? udpListener.ReceiveAsync() : udpReceiveTask);
						if (await Task.WhenAny(new Task[2]
						{
							udpReceiveTask,
							TaskExtension.TryDelay(1000, cancellationToken)
						}) != udpReceiveTask)
						{
							continue;
						}
						if (cancellationToken.IsCancellationRequested)
						{
							break;
						}
						if (udpReceiveTask.IsFaulted || udpReceiveTask.IsCanceled)
						{
							throw new Exception("UDP Receive Task is failed or canceled");
						}
						if (udpReceiveTask.IsCompleted)
						{
							UdpReceiveResult result = udpReceiveTask.Result;
							CanBridgeUdpData canBridgeUdpData = JsonConvert.DeserializeObject<CanBridgeUdpData>(Encoding.UTF8.GetString(result.Buffer, 0, result.Buffer.Length));
							if (string.CompareOrdinal("IDS", canBridgeUdpData.Mfg) == 0 || string.CompareOrdinal("CAN_TO_ETHERNET_GATEWAY", canBridgeUdpData.Product) == 0)
							{
								UpdateDevice(canBridgeUdpData.Name, result.RemoteEndPoint.Address.ToString(), canBridgeUdpData.Port);
							}
						}
					}
					catch (TimeoutException)
					{
					}
					catch (OperationCanceledException)
					{
					}
					catch (Exception ex3)
					{
						TaggedLog.Warning("CanBridgeLocatorService", "Error listening for CanBridge UDP packets: " + ex3.Message);
						await TaskExtension.TryDelay(1000, cancellationToken);
					}
				}
			}
		}

		private void CleanupOldGateways()
		{
			try
			{
				TCanBridgeData[] itemsToRemove = Enumerable.ToArray(Enumerable.Where(Items, (TCanBridgeData kvp) => kvp.IsExpired));
				if (itemsToRemove.Length == 0)
				{
					return;
				}
				MainThread.RequestMainThreadAction(delegate
				{
					lock (Items)
					{
						TCanBridgeData[] array = itemsToRemove;
						foreach (TCanBridgeData val in array)
						{
							Items.TryRemove(val);
							TaggedLog.Debug("CanBridgeLocatorService", $"{val.Address}:{val.Port} - {val.Name} Removed");
						}
					}
				});
			}
			catch (Exception)
			{
			}
		}

		public TCanBridgeData? Find(string address)
		{
			string address2 = address;
			lock (Items)
			{
				return Enumerable.FirstOrDefault(Items, (TCanBridgeData item) => string.CompareOrdinal(item.Address, address2) == 0);
			}
		}

		private void UpdateDevice(string name, string address, string portStr)
		{
			string portStr2 = portStr;
			string address2 = address;
			string name2 = name;
			MainThread.RequestMainThreadAction(delegate
			{
				lock (Items)
				{
					int.TryParse(portStr2, out var result);
					TCanBridgeData val = Find(address2);
					if (val == null)
					{
						val = CanBridgeDataCreator(name2, address2, result);
						Items.Add(val);
						TaggedLog.Debug("CanBridgeLocatorService", $"UpdateDevice: {val.Address}:{val.Port} - {val.Name} Added");
					}
					else
					{
						val.Update(name2, result);
					}
				}
			});
		}

		public void Clear()
		{
			if (Items.Count == 0)
			{
				return;
			}
			MainThread.RequestMainThreadAction(delegate
			{
				lock (Items)
				{
					Items.Clear();
				}
			});
		}
	}
}
