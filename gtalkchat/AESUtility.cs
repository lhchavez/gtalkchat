using System;
using System.Text;
using System.Security.Cryptography;

namespace gtalkchat {
    class AESUtility {
        private string password;
        private Aes aes;
        private readonly static string Base64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-/";

        public AESUtility(string password) {
            this.password = password;

            this.aes = new AesManaged();
        }

        public string Decipher(string cipherText) {
            var tokens = cipherText.Split(new char[] { ':' });

            var length = int.Parse(tokens[0]);

            byte[] iv = new byte[16];
            byte[] key = new byte[256 / 8];

            EVP_BytesToKey(new byte[0], Encoding.UTF8.GetBytes(tokens[1] + "$" + password), 256, key, iv);

            var decryptor = this.aes.CreateDecryptor(key, iv);

            byte[] data = Convert.FromBase64String(tokens[2]);

            return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(data, 0, data.Length), 0, length);
        }

        public string Cipher(string plainText) {
            return Cipher(plainText, CreateSalt(8));
        }

        public string Cipher(string plainText, string salt) {
            byte[] plainData = Encoding.UTF8.GetBytes(plainText);

            byte[] iv = new byte[16];
            byte[] key = new byte[256 / 8];

            EVP_BytesToKey(new byte[0], Encoding.UTF8.GetBytes(salt + "$" + password), 256, key, iv);

            var encryptor = this.aes.CreateEncryptor(key, iv);

            return String.Format("{0}:{1}:{2}", plainData.Length, salt, Convert.ToBase64String(encryptor.TransformFinalBlock(plainData, 0, plainData.Length)));
        }

        public static string CreateSalt(int length) {
            byte[] data = new byte[length];
            char[] salt = new char[length];

            new Random().NextBytes(data);

            for (int i = 0; i < length; i++) {
                salt[i] = Base64[data[i] & 0x3F];
            }

            return new string(salt);
        }

        private static void EVP_BytesToKey(byte[] salt, byte[] pswbytes, int keySize, byte[] key, byte[] iv) {
            int hashlen = 16;
            int neededHashes = 1 + keySize / 8 / 16;

            byte[] buffer = new byte[hashlen + pswbytes.Length + salt.Length];

            System.Array.Copy(pswbytes, 0, buffer, hashlen, pswbytes.Length);
            System.Array.Copy(salt, 0, buffer, hashlen + pswbytes.Length, salt.Length);

            byte[] MD = MD5Core.GetHash(buffer, hashlen, buffer.Length - hashlen);

            System.Array.Copy(MD, 0, key, 0, hashlen);

            for (int round = 1; round < neededHashes; round++) {
                System.Array.Copy(MD, 0, buffer, 0, hashlen);

                MD = MD5Core.GetHash(buffer, 0, buffer.Length);

                if (round < neededHashes - 1) {
                    System.Array.Copy(MD, 0, key, hashlen * round, hashlen);
                } else {
                    System.Array.Copy(MD, 0, iv, 0, hashlen);
                }
            }
        }
    }
}
