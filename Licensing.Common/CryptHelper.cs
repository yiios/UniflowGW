using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Licensing
{
    public static class CryptHelper
    {
        static Random r = new Random();
        public static byte[] MakeRandomKey()
        {
            var buffer = new byte[16];
            r.NextBytes(buffer);
            return buffer;
        }
        public static byte[] ToHashedKey(this string password)
        {
            return MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(password))
                .Take(16).ToArray();
        }
        public static byte[] AesEncrypt<T>(this T data, byte[] key, byte[] iv)
        {
            using (var stream = new MemoryStream())
            {
                var ser = new XmlSerializer(typeof(T));
                ser.Serialize(stream, data);
                stream.Position = 0;
                using (var r = new StreamReader(stream))
                {
                    string xml = r.ReadToEnd();
                    var encrypted = AesEncrypt(Encoding.UTF8.GetBytes(xml), key, iv);
                    return encrypted;
                }
            }
        }
        public static byte[] AesEncrypt(this byte[] data, byte[] key, byte[] iv)
        {
            using (var aes = Rijndael.Create())
            {
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(key, iv), CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();
                        return ms.ToArray();
                    }
                }
            }
        }

        public static T AesDecrypt<T>(this byte[] data, byte[] key, byte[] iv)
        {
            var decrypted = AesDecrypt(data, key, iv);
            var xml = Encoding.UTF8.GetString(decrypted);
            using (var stream = new StringReader(xml))
            {
                var ser = new XmlSerializer(typeof(T));
                return (T)ser.Deserialize(stream);
            }
        }
        public static byte[] AesDecrypt(this byte[] data, byte[] key, byte[] iv)
        {
            using (var aes = Rijndael.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                using (var ms = new MemoryStream(data))
                {
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(key, iv), CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();
                        return ms.ToArray().Take((int)ms.Position).ToArray();
                    }
                }
            }
        }

        public static byte[] RsaEncrypt<T>(this T data, string pubKey)
        {
            using (var stream = new MemoryStream())
            {
                var ser = new XmlSerializer(typeof(T));
                ser.Serialize(stream, data);
                stream.Position = 0;
                using (var r = new StreamReader(stream))
                {
                    string xml = r.ReadToEnd();
                    var encrypted = RsaEncrypt(Encoding.UTF8.GetBytes(xml), pubKey);
                    return encrypted;
                }
            }
        }
        public static byte[] RsaEncrypt(this byte[] data, string pubKey)
        {
            using (var rsa = RSA.Create())
            {
                LoadRSA(pubKey, rsa);

                int BlockSize = rsa.KeySize / 8 - 11;

                List<byte> result = new List<byte>(2048);
                for (int start = 0; start < data.Length; start += BlockSize)
                {
                    byte[] block = data.Skip(start).Take(BlockSize).ToArray();
                    result.AddRange(rsa.Encrypt(block, RSAEncryptionPadding.Pkcs1));
                }
                return result.ToArray();
            }
        }

        public static T RsaDecrypt<T>(this byte[] data, string privKey)
        {
            var decrypted = RsaDecrypt(data, privKey);
            var xml = Encoding.UTF8.GetString(decrypted);
            using (var stream = new StringReader(xml))
            {
                var ser = new XmlSerializer(typeof(T));
                return (T)ser.Deserialize(stream);
            }
        }
        public static byte[] RsaDecrypt(this byte[] data, string privKey)
        {
            using (var rsa = RSA.Create())
            {
                LoadRSA(privKey, rsa);

                int BlockSize = rsa.KeySize / 8;

                List<byte> result = new List<byte>(2048);
                for (int start = 0; start < data.Length; start += BlockSize)
                {
                    byte[] block = data.Skip(start).Take(BlockSize).ToArray();
                    result.AddRange(rsa.Decrypt(block, RSAEncryptionPadding.Pkcs1));
                }
                return result.ToArray();
            }
        }

        public static byte[] GetRSASignature(this byte[] data, string privKey)
        {
            using (var rsa = RSA.Create())
            {
                LoadRSA(privKey, rsa);

                return rsa.SignData(data,
                    HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
            }
        }

        public static bool VerifyRSASignature(this byte[] data, byte[] signature, string pubKey)
        {
            try
            {
                using (var rsa = RSA.Create())
                {
                    LoadRSA(pubKey, rsa);

                    return rsa.VerifyData(data, signature,
                        HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                }
            }
            catch
            {
                return false;
            }
        }

        private static void LoadRSA(string key, RSA rsa)
        {
            var xml = XElement.Parse(key);
            var p = new RSAParameters();

            var e1 = xml.Element("Modulus");
            var e2 = xml.Element("Exponent");
            if (e1 != null && e2 != null)
            {
                p.Modulus = Convert.FromBase64String(e1.Value);
                p.Exponent = Convert.FromBase64String(e2.Value);
            }

            var e3 = xml.Element("P");
            var e4 = xml.Element("Q");
            var e5 = xml.Element("DP");
            var e6 = xml.Element("DQ");
            var e7 = xml.Element("InverseQ");
            var e8 = xml.Element("D");
            if (!new[] { e3, e4, e5, e6, e7, e8 }.Contains(null))
            {
                p.P = Convert.FromBase64String(e3.Value);
                p.Q = Convert.FromBase64String(e4.Value);
                p.DP = Convert.FromBase64String(e5.Value);
                p.DQ = Convert.FromBase64String(e6.Value);
                p.InverseQ = Convert.FromBase64String(e7.Value);
                p.D = Convert.FromBase64String(e8.Value);
            }

            rsa.ImportParameters(p);
        }

    }
}
