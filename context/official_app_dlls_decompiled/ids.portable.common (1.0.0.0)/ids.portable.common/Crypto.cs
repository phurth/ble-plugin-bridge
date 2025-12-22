using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace IDS.Portable.Common
{
	public static class Crypto
	{
		public static string GetMd5HashString(string fromString)
		{
			using MD5CryptoServiceProvider mD5CryptoServiceProvider = new MD5CryptoServiceProvider();
			mD5CryptoServiceProvider.Initialize();
			byte[] array = mD5CryptoServiceProvider.ComputeHash(Encoding.UTF8.GetBytes(fromString));
			mD5CryptoServiceProvider.Clear();
			return Encoding.UTF8.GetString(array);
		}

		public static string Decrypt(this string encryptedString, string encryptionKey)
		{
			if (string.IsNullOrEmpty(encryptedString))
			{
				return "";
			}
			byte[] bytes = Encoding.ASCII.GetBytes(encryptionKey);
			byte[] array = Convert.FromBase64String(encryptedString);
			using DESCryptoServiceProvider dESCryptoServiceProvider = new DESCryptoServiceProvider();
			using ICryptoTransform cryptoTransform = dESCryptoServiceProvider.CreateDecryptor(bytes, bytes);
			using MemoryStream memoryStream = new MemoryStream(array);
			using StreamReader streamReader = new StreamReader(new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Read));
			return streamReader.ReadToEnd();
		}

		public static string Encrypt(this string originalString, string encryptionKey)
		{
			if (string.IsNullOrEmpty(originalString))
			{
				return "";
			}
			byte[] bytes = Encoding.ASCII.GetBytes(encryptionKey);
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
