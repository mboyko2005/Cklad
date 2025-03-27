using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace УправлениеСкладом
{
	public static class EncryptionHelper
	{
		// Ключ длиной 16 символов для AES-128
		private static readonly string key = "Your16CharKey123";

		// Шифрование строки – возвращает зашифрованный текст в формате Base64
		public static string EncryptString(string plainText)
		{
			byte[] iv = new byte[16];
			byte[] encrypted;
			using (Aes aes = Aes.Create())
			{
				aes.Key = Encoding.UTF8.GetBytes(key);
				aes.IV = iv;
				ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
				using (MemoryStream ms = new MemoryStream())
				{
					using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
					using (StreamWriter sw = new StreamWriter(cs))
					{
						sw.Write(plainText);
					}
					encrypted = ms.ToArray();
				}
			}
			return Convert.ToBase64String(encrypted);
		}

		// Расшифровка строки (Base64 -> исходный текст)
		public static string DecryptString(string cipherText)
		{
			byte[] iv = new byte[16];
			byte[] buffer = Convert.FromBase64String(cipherText);
			using (Aes aes = Aes.Create())
			{
				aes.Key = Encoding.UTF8.GetBytes(key);
				aes.IV = iv;
				ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
				using (MemoryStream ms = new MemoryStream(buffer))
				using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
				using (StreamReader sr = new StreamReader(cs))
				{
					return sr.ReadToEnd();
				}
			}
		}

		// Шифрование массива байт (например, файла)
		public static byte[] EncryptBytes(byte[] plainBytes)
		{
			byte[] iv = new byte[16];
			using (Aes aes = Aes.Create())
			{
				aes.Key = Encoding.UTF8.GetBytes(key);
				aes.IV = iv;
				ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
				using (MemoryStream ms = new MemoryStream())
				using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
				{
					cs.Write(plainBytes, 0, plainBytes.Length);
					cs.FlushFinalBlock();
					return ms.ToArray();
				}
			}
		}

		// Расшифровка массива байт
		public static byte[] DecryptBytes(byte[] cipherBytes)
		{
			byte[] iv = new byte[16];
			using (Aes aes = Aes.Create())
			{
				aes.Key = Encoding.UTF8.GetBytes(key);
				aes.IV = iv;
				ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
				using (MemoryStream ms = new MemoryStream(cipherBytes))
				using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
				using (MemoryStream result = new MemoryStream())
				{
					cs.CopyTo(result);
					return result.ToArray();
				}
			}
		}
	}
}
