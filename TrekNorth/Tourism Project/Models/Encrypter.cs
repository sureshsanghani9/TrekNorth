using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Tourism_Project.Models
{
    /// <summary>
    /// Summary description for EncryptionManager
    /// </summary>
    public static class EncryptionManager
    {
        private static string encryptionKey = "tr3kn0rthen<rypt";
        public static string EncryptRijndael(string value)
        {
            try
            {
                var key = Encoding.UTF8.GetBytes(encryptionKey); //must be 16 chars
                var rijndael = new RijndaelManaged
                {
                    BlockSize = 128,
                    IV = key,
                    KeySize = 128,
                    Key = key
                };

                var transform = rijndael.CreateEncryptor();
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, transform, CryptoStreamMode.Write))
                    {
                        byte[] buffer = Encoding.UTF8.GetBytes(value);

                        cs.Write(buffer, 0, buffer.Length);
                        cs.FlushFinalBlock();
                        cs.Close();
                    }
                    ms.Close();
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string DecryptRijndael(string value)
        {
            try
            {
                var key = Encoding.UTF8.GetBytes(encryptionKey); //must be 16 chars
                var rijndael = new RijndaelManaged
                                               {
                                                   BlockSize = 128,
                                                   IV = key,
                                                   KeySize = 128,
                                                   Key = key
                                               };

                var buffer = Convert.FromBase64String(value);
                var transform = rijndael.CreateDecryptor();
                string decrypted;
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, transform, CryptoStreamMode.Write))
                    {
                        cs.Write(buffer, 0, buffer.Length);
                        cs.FlushFinalBlock();
                        decrypted = Encoding.UTF8.GetString(ms.ToArray());
                        cs.Close();
                    }
                    ms.Close();
                }

                return decrypted;
            }
            catch
            {
                return null;
            }
        }
    }
}