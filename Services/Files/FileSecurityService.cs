using System.Security.Cryptography;
using System.Text;

namespace eProtokoll.Services.Files
{
    public class FileSecurityService : IFileSecurityService
    {
        private const string KeyRaw = "12345678901234567890123456789012";
        private readonly byte[] _key;

        public FileSecurityService()
        {
            _key = Encoding.UTF8.GetBytes(KeyRaw);
        }

        public byte[] Encrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();

            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
            }

            return ms.ToArray();
        }

        public byte[] Decrypt(byte[] encryptedData)
        {
            using var aes = Aes.Create();
            aes.Key = _key;

            var iv = new byte[16];
            Array.Copy(encryptedData, 0, iv, 0, 16);
            aes.IV = iv;

            using var ms = new MemoryStream();

            using (var cs = new CryptoStream(
                new MemoryStream(encryptedData, 16, encryptedData.Length - 16),
                aes.CreateDecryptor(),
                CryptoStreamMode.Read))
            {
                cs.CopyTo(ms);
            }

            return ms.ToArray();
        }
    }
}