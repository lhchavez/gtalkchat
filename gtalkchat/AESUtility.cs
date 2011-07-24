using System;
using System.Text;
using System.Security.Cryptography;

namespace gtalkchat {
    internal class AesUtility {
        private readonly string password;
        private readonly Aes aes;
        private const string Base64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-/";

        public AesUtility(string password) {
            this.password = password;

            aes = new AesManaged();
        }

        public string Decipher(string cipherText) {
            var tokens = cipherText.Split(new[] {':'});

            var length = int.Parse(tokens[0]);

            var iv = new byte[16];
            var key = new byte[256 / 8];

            EVP_BytesToKey(new byte[0], Encoding.UTF8.GetBytes(tokens[1] + "$" + password), 256, key, iv);

            var decryptor = aes.CreateDecryptor(key, iv);

            var data = Convert.FromBase64String(tokens[2]);

            return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(data, 0, data.Length), 0, length);
        }

        public string Cipher(string plainText) {
            return Cipher(plainText, CreateSalt(8));
        }

        public string Cipher(string plainText, string salt) {
            var plainData = Encoding.UTF8.GetBytes(plainText);

            var iv = new byte[16];
            var key = new byte[256 / 8];

            EVP_BytesToKey(new byte[0], Encoding.UTF8.GetBytes(salt + "$" + password), 256, key, iv);

            var encryptor = aes.CreateEncryptor(key, iv);

            return String.Format(
                "{0}:{1}:{2}", plainData.Length, salt,
                Convert.ToBase64String(encryptor.TransformFinalBlock(plainData, 0, plainData.Length)));
        }

        public static string CreateSalt(int length) {
            var data = new byte[length];
            var salt = new char[length];

            new Random().NextBytes(data);

            for (int i = 0; i < length; i++) {
                salt[i] = Base64[data[i] & 0x3F];
            }

            return new string(salt);
        }

        private static void EVP_BytesToKey(byte[] salt, byte[] pswbytes, int keySize, byte[] key, byte[] iv) {
            const int hashlen = 16;
            var neededHashes = 1 + keySize / 8 / 16;

            var buffer = new byte[hashlen + pswbytes.Length + salt.Length];

            Array.Copy(pswbytes, 0, buffer, hashlen, pswbytes.Length);
            Array.Copy(salt, 0, buffer, hashlen + pswbytes.Length, salt.Length);

            byte[] md = Md5Core.GetHash(buffer, hashlen, buffer.Length - hashlen);

            Array.Copy(md, 0, key, 0, hashlen);

            for (int round = 1; round < neededHashes; round++) {
                Array.Copy(md, 0, buffer, 0, hashlen);

                md = Md5Core.GetHash(buffer, 0, buffer.Length);

                if (round < neededHashes - 1) {
                    Array.Copy(md, 0, key, hashlen * round, hashlen);
                } else {
                    Array.Copy(md, 0, iv, 0, hashlen);
                }
            }
        }
    }
}