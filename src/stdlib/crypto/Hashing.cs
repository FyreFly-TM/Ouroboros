using System;
using System.Security.Cryptography;
using System.Text;

namespace Ouro.StdLib.Crypto
{
    /// <summary>
    /// Cryptographic hashing functions
    /// </summary>
    public static class Hashing
    {
        /// <summary>
        /// Compute SHA256 hash of data
        /// </summary>
        public static byte[] SHA256(byte[] data)
        {
            using var sha256 = global::System.Security.Cryptography.SHA256.Create();
            return sha256.ComputeHash(data);
        }

        /// <summary>
        /// Compute SHA256 hash of string
        /// </summary>
        public static string SHA256(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var hash = SHA256(bytes);
            return BytesToHex(hash);
        }

        /// <summary>
        /// Compute SHA512 hash of data
        /// </summary>
        public static byte[] SHA512(byte[] data)
        {
            using var sha512 = global::System.Security.Cryptography.SHA512.Create();
            return sha512.ComputeHash(data);
        }

        /// <summary>
        /// Compute SHA512 hash of string
        /// </summary>
        public static string SHA512(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var hash = SHA512(bytes);
            return BytesToHex(hash);
        }

        /// <summary>
        /// Compute SHA1 hash of data (legacy, not recommended for security)
        /// </summary>
        public static byte[] SHA1(byte[] data)
        {
            using var sha1 = global::System.Security.Cryptography.SHA1.Create();
            return sha1.ComputeHash(data);
        }

        /// <summary>
        /// Compute SHA1 hash of string
        /// </summary>
        public static string SHA1(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var hash = SHA1(bytes);
            return BytesToHex(hash);
        }

        /// <summary>
        /// Compute MD5 hash of data (legacy, not recommended for security)
        /// </summary>
        public static byte[] MD5(byte[] data)
        {
            using var md5 = global::System.Security.Cryptography.MD5.Create();
            return md5.ComputeHash(data);
        }

        /// <summary>
        /// Compute MD5 hash of string
        /// </summary>
        public static string MD5(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var hash = MD5(bytes);
            return BytesToHex(hash);
        }

        /// <summary>
        /// Compute HMAC-SHA256
        /// </summary>
        public static byte[] HMACSHA256(byte[] key, byte[] data)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(data);
        }

        /// <summary>
        /// Compute HMAC-SHA256 for strings
        /// </summary>
        public static string HMACSHA256(string key, string data)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var hash = HMACSHA256(keyBytes, dataBytes);
            return BytesToHex(hash);
        }

        /// <summary>
        /// Compute HMAC-SHA512
        /// </summary>
        public static byte[] HMACSHA512(byte[] key, byte[] data)
        {
            using var hmac = new HMACSHA512(key);
            return hmac.ComputeHash(data);
        }

        /// <summary>
        /// Convert bytes to hexadecimal string
        /// </summary>
        public static string BytesToHex(byte[] bytes)
        {
            var hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }

        /// <summary>
        /// Convert hexadecimal string to bytes
        /// </summary>
        public static byte[] HexToBytes(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have even length");

            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        /// <summary>
        /// Compute file hash
        /// </summary>
        public static string HashFile(string filePath, HashAlgorithm algorithm = HashAlgorithm.SHA256)
        {
            using var stream = global::System.IO.File.OpenRead(filePath);
            byte[] hash;

            switch (algorithm)
            {
                case HashAlgorithm.SHA256:
                    using (var sha256 = global::System.Security.Cryptography.SHA256.Create())
                        hash = sha256.ComputeHash(stream);
                    break;
                case HashAlgorithm.SHA512:
                    using (var sha512 = global::System.Security.Cryptography.SHA512.Create())
                        hash = sha512.ComputeHash(stream);
                    break;
                case HashAlgorithm.SHA1:
                    using (var sha1 = global::System.Security.Cryptography.SHA1.Create())
                        hash = sha1.ComputeHash(stream);
                    break;
                case HashAlgorithm.MD5:
                    using (var md5 = global::System.Security.Cryptography.MD5.Create())
                        hash = md5.ComputeHash(stream);
                    break;
                default:
                    throw new ArgumentException($"Unsupported algorithm: {algorithm}");
            }

            return BytesToHex(hash);
        }

        /// <summary>
        /// Verify hash
        /// </summary>
        public static bool VerifyHash(string data, string hash, HashAlgorithm algorithm = HashAlgorithm.SHA256)
        {
            string computed = algorithm switch
            {
                HashAlgorithm.SHA256 => SHA256(data),
                HashAlgorithm.SHA512 => SHA512(data),
                HashAlgorithm.SHA1 => SHA1(data),
                HashAlgorithm.MD5 => MD5(data),
                _ => throw new ArgumentException($"Unsupported algorithm: {algorithm}")
            };

            return string.Equals(computed, hash, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Hash algorithm enumeration
    /// </summary>
    public enum HashAlgorithm
    {
        MD5,
        SHA1,
        SHA256,
        SHA512
    }

    /// <summary>
    /// Random number generation
    /// </summary>
    public static class RandomGenerator
    {
        private static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();

        /// <summary>
        /// Generate cryptographically secure random bytes
        /// </summary>
        public static byte[] GenerateBytes(int length)
        {
            var bytes = new byte[length];
            rng.GetBytes(bytes);
            return bytes;
        }

        /// <summary>
        /// Generate random integer
        /// </summary>
        public static int GenerateInt32(int min = 0, int max = int.MaxValue)
        {
            if (min >= max)
                throw new ArgumentException("Min must be less than max");

            uint range = (uint)(max - min);
            uint randomValue;

            do
            {
                var bytes = GenerateBytes(4);
                randomValue = BitConverter.ToUInt32(bytes, 0);
            } while (randomValue >= uint.MaxValue - (uint.MaxValue % range));

            return (int)(randomValue % range) + min;
        }

        /// <summary>
        /// Generate random string
        /// </summary>
        public static string GenerateString(int length, string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789")
        {
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[GenerateInt32(0, chars.Length)];
            }
            return new string(result);
        }

        /// <summary>
        /// Generate secure token
        /// </summary>
        public static string GenerateToken(int byteLength = 32)
        {
            var bytes = GenerateBytes(byteLength);
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        /// <summary>
        /// Generate UUID v4
        /// </summary>
        public static Guid GenerateUuid()
        {
            var bytes = GenerateBytes(16);
            
            // Set version (4) and variant bits
            bytes[6] = (byte)((bytes[6] & 0x0F) | 0x40);
            bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
            
            return new Guid(bytes);
        }
    }

    /// <summary>
    /// Basic encryption/decryption (using AES)
    /// </summary>
    public static class Encryption
    {
        /// <summary>
        /// Encrypt data using AES
        /// </summary>
        public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }

        /// <summary>
        /// Decrypt data using AES
        /// </summary>
        public static byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(data, 0, data.Length);
        }

        /// <summary>
        /// Encrypt string
        /// </summary>
        public static string EncryptString(string plainText, string password)
        {
            var salt = RandomGenerator.GenerateBytes(16);
            var (key, iv) = DeriveKeyAndIV(password, salt);
            
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encrypted = Encrypt(plainBytes, key, iv);
            
            // Prepend salt to encrypted data
            var result = new byte[salt.Length + encrypted.Length];
            Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
            Buffer.BlockCopy(encrypted, 0, result, salt.Length, encrypted.Length);
            
            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Decrypt string
        /// </summary>
        public static string DecryptString(string cipherText, string password)
        {
            var cipherBytes = Convert.FromBase64String(cipherText);
            
            // Extract salt
            var salt = new byte[16];
            Buffer.BlockCopy(cipherBytes, 0, salt, 0, salt.Length);
            
            // Extract encrypted data
            var encrypted = new byte[cipherBytes.Length - salt.Length];
            Buffer.BlockCopy(cipherBytes, salt.Length, encrypted, 0, encrypted.Length);
            
            var (key, iv) = DeriveKeyAndIV(password, salt);
            var decrypted = Decrypt(encrypted, key, iv);
            
            return Encoding.UTF8.GetString(decrypted);
        }

        /// <summary>
        /// Derive key and IV from password
        /// </summary>
        private static (byte[] key, byte[] iv) DeriveKeyAndIV(string password, byte[] salt)
        {
            using var derive = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            return (derive.GetBytes(32), derive.GetBytes(16)); // 256-bit key, 128-bit IV
        }

        /// <summary>
        /// Generate AES key
        /// </summary>
        public static byte[] GenerateKey(int bits = 256)
        {
            if (bits != 128 && bits != 192 && bits != 256)
                throw new ArgumentException("Key size must be 128, 192, or 256 bits");
            
            return RandomGenerator.GenerateBytes(bits / 8);
        }

        /// <summary>
        /// Generate AES IV
        /// </summary>
        public static byte[] GenerateIV()
        {
            return RandomGenerator.GenerateBytes(16); // 128 bits
        }
    }
} 