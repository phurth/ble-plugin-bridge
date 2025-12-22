using System;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ids.portable.ble.Platforms.Shared.ScanResults;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ids.portable.ble.BleManager
{
	[JsonObject(MemberSerialization.OptIn)]
	public readonly struct BleManagerConnectionParametersSerializable : IBleManagerConnectionParameters
	{
		private const string EncryptionKey = "5v8y/B?D";

		[JsonProperty]
		public Guid DeviceId { get; }

		[JsonProperty]
		public string DeviceName { get; }

		[JsonConverter(typeof(StringEnumConverter))]
		[DefaultValue(PairingMethod.None)]
		[JsonProperty]
		public PairingMethod PairingMethod { get; }

		[JsonIgnore]
		public uint? KeySeedCypher { get; }

		[JsonProperty]
		public string KeySeed { get; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public int? ConnectionTimeoutMs { get; }

		[JsonProperty]
		public DateTime CreateTimestamp { get; }

		public BleManagerConnectionParametersSerializable(IBleManagerConnectionParameters connectionParameters)
		{
			DeviceId = connectionParameters.DeviceId;
			DeviceName = connectionParameters.DeviceName;
			PairingMethod = connectionParameters.PairingMethod;
			KeySeedCypher = connectionParameters.KeySeedCypher;
			KeySeed = ((!KeySeedCypher.HasValue) ? string.Empty : Encrypt(KeySeedCypher.ToString()));
			ConnectionTimeoutMs = connectionParameters.ConnectionTimeoutMs;
			CreateTimestamp = DateTime.Now;
		}

		[JsonConstructor]
		public BleManagerConnectionParametersSerializable(Guid deviceId, string deviceName, PairingMethod pairingMethod, string keySeed, int? connectionTimeout)
		{
			DeviceId = deviceId;
			DeviceName = deviceName;
			PairingMethod = pairingMethod;
			KeySeed = keySeed;
			KeySeedCypher = (string.IsNullOrWhiteSpace(keySeed) ? null : new uint?(Convert.ToUInt32(Decrypt(keySeed))));
			ConnectionTimeoutMs = connectionTimeout;
			CreateTimestamp = DateTime.Now;
		}

		public static string Decrypt(string encryptedString)
		{
			if (string.IsNullOrEmpty(encryptedString))
			{
				return "";
			}
			byte[] bytes = Encoding.ASCII.GetBytes("5v8y/B?D");
			byte[] array = Convert.FromBase64String(encryptedString);
			using DESCryptoServiceProvider dESCryptoServiceProvider = new DESCryptoServiceProvider();
			using ICryptoTransform cryptoTransform = dESCryptoServiceProvider.CreateDecryptor(bytes, bytes);
			using MemoryStream memoryStream = new MemoryStream(array);
			using StreamReader streamReader = new StreamReader(new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Read));
			return streamReader.ReadToEnd();
		}

		public static string Encrypt(string originalString)
		{
			if (string.IsNullOrEmpty(originalString))
			{
				return "";
			}
			byte[] bytes = Encoding.ASCII.GetBytes("5v8y/B?D");
			byte[] bytes2 = Encoding.ASCII.GetBytes(originalString);
			using DESCryptoServiceProvider dESCryptoServiceProvider = new DESCryptoServiceProvider();
			using ICryptoTransform cryptoTransform = dESCryptoServiceProvider.CreateEncryptor(bytes, bytes);
			using MemoryStream memoryStream = new MemoryStream(bytes2);
			CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Read);
			using MemoryStream memoryStream2 = new MemoryStream();
			cryptoStream.CopyTo(memoryStream2);
			return Convert.ToBase64String(memoryStream2.ToArray());
		}
	}
}
