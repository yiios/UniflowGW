using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace UniFlowGW.Util
{
	public class EncrpyUntil
	{
		public static string CreateCryptographicallySecureGuid()
		{
			using (RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider())
			{
				byte[] array = new byte[16];
				rNGCryptoServiceProvider.GetBytes(array);
				return new Guid(array).ToString("N").Substring(0, 24);
			}
		}

		public static Tuple<string, string> ExtractDynamicSalt(string encryptedData)
		{
			string saltKey = string.Empty;
			string encryptedValue = string.Empty;

			saltKey = encryptedData.Substring(0, 24);
			encryptedValue = encryptedData.Substring(24, encryptedData.Length - 24);
			return new Tuple<string, string>(saltKey, encryptedValue);

		}

        public static string Encrypt(string value, string key)
        {
            return Encrypt(value, key, CreateCryptographicallySecureGuid());
        }

		public static string Encrypt(string value, string key, string salt)
		{
			DeriveBytes deriveBytes = new Rfc2898DeriveBytes(key, Encoding.Unicode.GetBytes(salt));
			SymmetricAlgorithm symmetricAlgorithm = new TripleDESCryptoServiceProvider();
			byte[] rgbKey = deriveBytes.GetBytes(symmetricAlgorithm.KeySize >> 3);
			byte[] rgbIv = deriveBytes.GetBytes(symmetricAlgorithm.BlockSize >> 3);
			ICryptoTransform transform = symmetricAlgorithm.CreateEncryptor(rgbKey, rgbIv);
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (CryptoStream stream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
				{
					using (StreamWriter streamWriter = new StreamWriter(stream, Encoding.Unicode))
					{
						streamWriter.Write(value);
					}
				}
				return Base64Util.Encode(memoryStream.ToArray());
			}
		}



		public static string Decrypt(string key, string sEncryptedData)
		{
			string sDecryptedData = string.Empty;
			try
			{
				string s = sEncryptedData.Substring(0, 24);
				string input = sEncryptedData.Substring(24, sEncryptedData.Length - 24);
				DeriveBytes deriveBytes = new Rfc2898DeriveBytes(key, Encoding.Unicode.GetBytes(s));
				SymmetricAlgorithm symmetricAlgorithm = new TripleDESCryptoServiceProvider();
				byte[] bytes = deriveBytes.GetBytes(symmetricAlgorithm.KeySize >> 3);
				byte[] bytes2 = deriveBytes.GetBytes(symmetricAlgorithm.BlockSize >> 3);
				ICryptoTransform transform = symmetricAlgorithm.CreateDecryptor(bytes, bytes2);
				using (MemoryStream stream = new MemoryStream(Base64Util.Decode(input)))
				{
					using (CryptoStream stream2 = new CryptoStream(stream, transform, CryptoStreamMode.Read))
					{
						using (StreamReader streamReader = new StreamReader(stream2, Encoding.Unicode))
						{
							sDecryptedData = streamReader.ReadToEnd();
						}
					}
				}
				return sDecryptedData;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}
	}

}
